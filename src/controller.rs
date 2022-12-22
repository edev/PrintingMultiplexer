use crate::auto_printer::AutoPrinter;
use crate::logger::{Log, LogMessage};
use crossbeam::channel;
use std::thread::{self, JoinHandle};

// Messages that `Controller` might send out.
pub enum ControlMessage {
    Close,
}

// Messages that an AutoPrinter can send (to a Controller).
pub enum PrinterMessage {
    // Nothing here yet.
}

// Messages that a FolderWatcher can send (to a Controller).
pub enum FolderWatcherMessage {
    // Nothing here yet.
}

// Control messages that the UI can send to the controller, e.g. to request service.
pub enum UIControlMessage {
    AddPrinter(String),
    ListPrinters,
    RemovePrinter(u8),
    Exit,
}

// A pair of channels for talking to another party.
pub struct ChannelPair<SenderType, ReceiverType> {
    pub sender: channel::Sender<SenderType>,
    pub receiver: channel::Receiver<ReceiverType>,
}

impl<SenderType, ReceiverType> ChannelPair<SenderType, ReceiverType> {
    // A convenience method for easily creating a ChannelPair.
    pub fn new(
        sender: channel::Sender<SenderType>,
        receiver: channel::Receiver<ReceiverType>,
    ) -> Self {
        ChannelPair { sender, receiver }
    }
}

// An internal representation of everything the controller knows about a printer.
struct Printer {
    channels: ChannelPair<ControlMessage, PrinterMessage>,
    name: String,
}

// Helper to generate origin fields for various LogMessage values. Normally, this would be an
// associated method, but with Controller, this causes problems due to partial moves as we
// shut down the program.
//
// TODO Revisit making origin() an associated function after reviewing the shutdown process.
fn origin() -> String {
    "Controller".to_string()
}

// The central hub for all coordination and control between other parts of the program.
//
// All control and log messages pass through this struct. Other communication, e.g. the MPMC
// channel for communication between watched folder(s) and printer(s), can and probably should
// happen directly rather than through this struct. The job of this struct is to respond to, route,
// and dispatch control messages throughout the program's lifecycle.
pub struct Controller<JoinHandleType> {
    // Channels for talking to the UI.
    ui: ChannelPair<ControlMessage, UIControlMessage>,

    // The handle for the UI thread.
    ui_handle: JoinHandle<JoinHandleType>,

    // Channels for talking to the FolderWatcher.
    folder_watcher: ChannelPair<ControlMessage, FolderWatcherMessage>,

    // The handle for the FolderWatcher thread.
    folder_watcher_handle: JoinHandle<JoinHandleType>,

    // The controller's copy of the receiver that AutoPrinters use to pull jobs from the print
    // queue. The controller should never try to receive from this, but it clones this receiver and
    // passes the copies to newly created AutPrinters.
    printer_receiver: channel::Receiver<String>,

    // All currently-in-use printers.
    printers: Vec<Printer>,

    // The control channel for talking to the program's logger. Unlike other modules, this is
    // one-way communication: the logger never communicates back to the rest of the program.
    //
    // Note that this is NOT the logging channel.
    logger: channel::Sender<ControlMessage>,

    // The program's log. Send log entries here.
    log: channel::Sender<LogMessage>,

    // The handle for the Logger thread.
    logger_handle: JoinHandle<JoinHandleType>,
}

impl<JoinHandleType> Controller<JoinHandleType> {
    // I don't see any better alternative to using all of these arguments. Therefore, I've silenced
    // Clippy on the matter. Structs for ui, folder_watcher, and logger arguments, for instance,
    // would complicate the calling process without much gain: aside from the handles, every other
    // argument (at time of writing) has unique types, so it's impossible to mix up most arguments.
    // Furthermore, arguments with the same types aren't next to one another.
    #[allow(clippy::too_many_arguments)]
    pub fn new(
        ui: ChannelPair<ControlMessage, UIControlMessage>,
        ui_handle: JoinHandle<JoinHandleType>,
        folder_watcher: ChannelPair<ControlMessage, FolderWatcherMessage>,
        folder_watcher_handle: JoinHandle<JoinHandleType>,
        printer_receiver: channel::Receiver<String>,
        logger: channel::Sender<ControlMessage>,
        log: channel::Sender<LogMessage>,
        logger_handle: JoinHandle<JoinHandleType>,
    ) -> Self {
        Controller {
            ui,
            ui_handle,
            folder_watcher,
            folder_watcher_handle,
            printer_receiver,
            printers: vec![],
            logger,
            log,
            logger_handle,
        }
    }

    pub fn run(mut self) {
        // The main controller loop: listen for messages and respond to them.
        loop {
            // Set up a crossbeam select operation to randomly choose from the available messages
            // among all receivers or block if there are no pending messages.
            let mut select = channel::Select::new();

            // Put the printers first so that they're indexed from 0.
            for printer in &self.printers {
                select.recv(&printer.channels.receiver);
            }
            let ui_index = select.recv(&self.ui.receiver);
            let fw_index = select.recv(&self.folder_watcher.receiver);
            let operation = select.select();
            match operation.index() {
                // Receive a message from a printer.
                i if i < self.printers.len() => {
                    match operation.recv(&self.printers[i].channels.receiver) {
                        Ok(_) => {
                            // Do something once we have printer status messages.
                        }
                        Err(_) => {
                            // The printer's disconnected, so we'll remove it. And just in case the
                            // printer's still running, we'll ask it to please close.
                            self.log.log(LogMessage::Disconnected {
                                origin: origin(),
                                channel: self.printers[i].name.clone(),
                            });
                            match self.printers[i]
                                .channels
                                .sender
                                .try_send(ControlMessage::Close)
                            {
                                Ok(()) => self.log.log(LogMessage::Notice {
                                    origin: origin(),
                                    message: format!(
                                        "Sent Close message to disconnected printer \"{}\".",
                                        self.printers[i].name,
                                    ),
                                }),
                                Err(e) => self.log.log(LogMessage::Error {
                                    origin: origin(),
                                    message: format!(
                                        "Failed to send Close message to disconnected printer \
                                        \"{}\": {}",
                                        self.printers[i].name, e,
                                    ),
                                }),
                            }
                            self.printers.remove(i);
                        }
                    }
                }

                // Receive a message from the UI.
                i if i == ui_index => {
                    match operation.recv(&self.ui.receiver) {
                        // TODO Reconsider all of the logic in this match statement once the system
                        // is more fully fleshed out. Consider all that's here to be a placeholder,
                        // including the handler functions.
                        Ok(message) => {
                            if let UIControlMessage::Exit = message {
                                // Close the program gracefully and exit.
                                self.folder_watcher.sender.send(ControlMessage::Close).ok();
                                self.ui.sender.send(ControlMessage::Close).ok();
                                // TODO Determine whether we should clear out any remaining
                                // messages rather than breaking immediately. (Almost certainly
                                // yes.)
                                break;
                            }
                            self.handle_ui_control_message(message);
                        }
                        Err(_) => {
                            self.log.log(LogMessage::Disconnected {
                                origin: origin(),
                                channel: "ui".to_string(),
                            });
                            break;
                        }
                    }
                }

                // Receive a message from the folder watcher.
                i if i == fw_index => match operation.recv(&self.folder_watcher.receiver) {
                    Ok(_) => {
                        // Do something once we have folder watcher messages.
                    }
                    Err(_) => {
                        self.log.log(LogMessage::Disconnected {
                            origin: origin(),
                            channel: "folder_watcher".to_string(),
                        });
                        break;
                    }
                },

                // Uh oh. We somehow received an index that shouldn't have been possible. Bug!
                i => panic!(
                    "Controller::run() received an invalid operation index from select: {}",
                    i
                ),
            }
        }

        // TODO Review, improve, and refactor the shutdown sequence.
        drop(self.ui.sender);
        drop(self.ui.receiver);
        drop(self.folder_watcher.sender);
        drop(self.folder_watcher.receiver);

        // Wait for all threads to complete before exiting.
        if let Err(code) = self.ui_handle.join() {
            self.log.log(LogMessage::Error {
                origin: origin(),
                message: format!("UI thread panicked with code {:?}", code),
            });
        }
        self.log.log(LogMessage::Notice {
            origin: origin(),
            message: "UI thread closed without errors".to_string(),
        });
        if let Err(code) = self.folder_watcher_handle.join() {
            self.log.log(LogMessage::Error {
                origin: origin(),
                message: format!("FolderWatcher thread panicked with code {:?}", code),
            });
        }
        self.log.log(LogMessage::Notice {
            origin: origin(),
            message: "FolderWatcher thread closed without errors".to_string(),
        });

        // TODO Add a goodbye/end log message. Be careful to block but not to panic in case the
        // logger is disconnected.

        // Now we can close the logger.
        match self.logger.try_send(ControlMessage::Close) {
            Ok(()) => {
                if let Err(code) = self.logger_handle.join() {
                    eprintln!("Logger thread panicked with code: {:?}", code);
                }
            }
            Err(_) => eprintln!("Tried to close logger gracefully, but it was disconnected."),
        }
    }

    fn handle_ui_control_message(&mut self, message: UIControlMessage) {
        match message {
            UIControlMessage::AddPrinter(name) => {
                let (to_controller, from_printer) = channel::unbounded();
                let (to_printer, from_controller) = channel::unbounded();
                let printer_channels = ChannelPair::new(to_controller, from_controller);
                let controller_channels = ChannelPair::new(to_printer, from_printer);

                self.log.log(LogMessage::Notice {
                    origin: origin(),
                    message: format!("Adding printer: \"{}\"", name),
                });
                let printer = Printer {
                    channels: controller_channels,
                    name: name.clone(),
                };
                self.printers.push(printer);

                let printer = AutoPrinter::new(
                    printer_channels,
                    self.printer_receiver.clone(),
                    name,
                    self.log.clone(),
                );
                thread::spawn(move || printer.run());
            }
            UIControlMessage::ListPrinters => {
                for (u, printer) in self.printers.iter().enumerate() {
                    // TODO Return a list of printer names via a message rather than printing. This
                    // makes it UI-agnostic.
                    println!("{}. {}", u, printer.name);
                }
            }
            UIControlMessage::RemovePrinter(u) => {
                let printer = self.printers.remove(u.into());
                self.log.log(LogMessage::Notice {
                    origin: origin(),
                    message: format!("Removing printer \"{}\"", printer.name),
                });
                printer.channels.sender.send(ControlMessage::Close).ok();
            }
            UIControlMessage::Exit => unreachable!(),
        }
    }
}
