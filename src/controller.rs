use crate::auto_printer::AutoPrinter;
use crossbeam::channel;
use std::thread::JoinHandle;

// Messages that `Controller` might send out.
pub enum ControlMessage {
    Close,
}

// Generic status messages anyone might send to the controller.
pub enum StatusMessage {
    Notice(String),
    Error(String),
}

// Control messages that the UI can send to the controller, e.g. to request service.
pub enum UIControlMessage {
    Status(StatusMessage),
    AddPrinter,
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
    folder_watcher: ChannelPair<ControlMessage, StatusMessage>,

    // The handle for the FolderWatcher thread.
    folder_watcher_handle: JoinHandle<JoinHandleType>,

    // The controller's copy of the receiver that AutoPrinters use to pull jobs from the print
    // queue. The controller should never try to receive from this, but it clones this receiver and
    // passes the copies to newly created AutPrinters.
    printer_receiver: channel::Receiver<String>,

    // All currently-in-use printers.
    printers: Vec<AutoPrinter>,
}

impl<JoinHandleType> Controller<JoinHandleType> {
    pub fn new(
        ui: ChannelPair<ControlMessage, UIControlMessage>,
        ui_handle: JoinHandle<JoinHandleType>,
        folder_watcher: ChannelPair<ControlMessage, StatusMessage>,
        folder_watcher_handle: JoinHandle<JoinHandleType>,
        printer_receiver: channel::Receiver<String>,
    ) -> Self {
        Controller {
            ui,
            ui_handle,
            folder_watcher,
            folder_watcher_handle,
            printer_receiver,
            printers: vec![],
        }
    }

    pub fn run(self) {
        // The main controller loop: listen for messages and respond to them.
        loop {
            // Set up a crossbeam select operation to randomly choose from the available messages
            // among all receivers or block if there are no pending messages.
            let mut select = channel::Select::new();
            let ui_index = select.recv(&self.ui.receiver);
            let fw_index = select.recv(&self.folder_watcher.receiver);
            let operation = select.select();
            match operation.index() {
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
                            eprintln!("UI channel disconnected unexpectedly");
                            break;
                        }
                    }
                }

                // Receive a message from the folder watcher.
                i if i == fw_index => match operation.recv(&self.folder_watcher.receiver) {
                    Ok(message) => self.handle_status_message(message),
                    Err(_) => {
                        eprintln!("Folder watcher channel disconnected unexpectedly");
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

        drop(self.ui.sender);
        drop(self.ui.receiver);
        drop(self.folder_watcher.sender);
        drop(self.folder_watcher.receiver);

        // Wait for all threads to complete before exiting.
        if let Err(message) = self.ui_handle.join() {
            eprintln!("{:?}", message);
        }
        println!("UI thread is closed.");
        if let Err(message) = self.folder_watcher_handle.join() {
            eprintln!("{:?}", message);
        }
        println!("Folder watcher thread is closed.");
    }

    fn handle_ui_control_message(&self, message: UIControlMessage) {
        match message {
            UIControlMessage::Status(message) => self.handle_status_message(message),
            UIControlMessage::AddPrinter => {
                println!("Adding printer (NYI)");
            }
            UIControlMessage::Exit => unreachable!(),
        }
    }

    fn handle_status_message(&self, message: StatusMessage) {
        match message {
            StatusMessage::Notice(message) => println!("{}", message),
            StatusMessage::Error(message) => eprintln!("{}", message),
        }
    }
}
