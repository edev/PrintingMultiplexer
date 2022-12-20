use crate::controller::{ChannelPair, ControlMessage, StatusMessage};
use crossbeam::channel;
use std::process::Command;

pub struct AutoPrinter {
    // TODO Add a printer name, once we have one. Use it in messages.

    // The lines of communication to and from the controller.
    controller: ChannelPair<StatusMessage, ControlMessage>,

    // A reciver for the MPMC print queue: this is where we pull print jobs.
    // Note that we pass the path to the file rather than, say, a parsed JPEG. This is to free up
    // the folder watcher's thread for speedier operation overall.
    print_queue: channel::Receiver<String>,

    printer_name: String,
}

impl AutoPrinter {
    pub fn new(
        controller: ChannelPair<StatusMessage, ControlMessage>,
        print_queue: channel::Receiver<String>,
        printer_name: String,
    ) -> Self {
        AutoPrinter {
            controller,
            print_queue,
            printer_name,
        }
    }

    pub fn run(&self) {
        loop {
            // Set up a crossbeam select operation among our various receivers.
            let mut select = channel::Select::new();
            let print_queue_index = select.recv(&self.print_queue);
            let controller_index = select.recv(&self.controller.receiver);
            let operation = select.select();
            match operation.index() {
                // Receive a line from standard input.
                i if i == print_queue_index => match operation.recv(&self.print_queue) {
                    Ok(path) => {
                        self.controller
                            .sender
                            .send(StatusMessage::Notice(format!(
                                "AutoPrinter \"{}\" picked up file to print: {}",
                                self.printer_name, path
                            )))
                            .unwrap();
                        self.print_to_mspaint(path);
                    }
                    Err(_) => {
                        eprintln!("Print queue senders disconnected unexpectedly");
                        break;
                    }
                },
                // Receive a message from the controller.
                i if i == controller_index => match operation.recv(&self.controller.receiver) {
                    Ok(message) => match message {
                        ControlMessage::Close => {
                            self.controller
                                .sender
                                .send(StatusMessage::Notice(
                                    "AutoPrinter gracefully closing".to_string(),
                                ))
                                .ok();
                            break;
                        }
                    },

                    Err(_) => {
                        eprintln!("Controller's AutoPrinter channel disconnected unexpectedly");
                        break;
                    }
                },

                // Uh oh. We somehow received an index that shouldn't have been possible. Bug!
                i => panic!(
                    "AutoPrinter::run() received an invalid operation index from select: {}",
                    i
                ),
            }
        }
    }

    // This is a temporary, hackish workaround. We will send the image's path to mspaint.exe with
    // appropriate options, and it will use the printer's default settings (hopefully). After that,
    // we will wait 35 seconds.
    fn print_to_mspaint(&self, image_path: String) {
        println!("Printing via mspaint: {}", &image_path);
        Command::new("mspaint")
            .args(["/p", &image_path, "/pt", &self.printer_name])
            .status()
            .ok();
    }
}
