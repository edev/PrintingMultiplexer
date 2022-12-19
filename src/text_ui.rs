use crate::controller::{ChannelPair, ControlMessage, StatusMessage, UIControlMessage};
use crossbeam::channel;
use std::io;
use std::thread;

// A simple, text-based UI for controlling the printing operation.
//
// This UI is meant to be simple, reliable, and quick to construct.
pub struct TextUI {
    // The lines of communication to and from the controller.
    controller: ChannelPair<UIControlMessage, ControlMessage>,
}

impl TextUI {
    pub fn new(controller: ChannelPair<UIControlMessage, ControlMessage>) -> Self {
        TextUI { controller }
    }

    pub fn run(&self) {
        self.print_main_menu();

        // Start reading stdin in a new thread.
        let stdin = self.read_stdin();

        loop {
            // Set up a crossbeam select operation among our various receivers.
            let mut select = channel::Select::new();
            let stdin_index = select.recv(&stdin);
            let controller_index = select.recv(&self.controller.receiver);
            let operation = select.select();
            match operation.index() {
                // Receive a line from standard input.
                i if i == stdin_index => {
                    match operation.recv(&stdin) {
                        Ok(line) => {
                            // Process the line that the user entered.
                            match line.trim().parse() {
                                Ok(1) => {
                                    println!("Adding printers is not yet supported, sorry!\n\n");
                                }
                                Ok(2) => {
                                    println!("Removing printers is not yet supported, sorry!\n\n");
                                }
                                Ok(0) => {
                                    self.controller.sender.send(UIControlMessage::Exit).unwrap();
                                    break;
                                }
                                _ => println!("Invalid input. Please try again.\n\n"),
                            }
                            self.print_main_menu();
                        }
                        Err(_) => {
                            eprintln!("TextUI's stdin channel disconnected unexpectedly");
                            break;
                        }
                    }
                }

                // Receive a message from the controller.
                i if i == controller_index => match operation.recv(&self.controller.receiver) {
                    Ok(message) => match message {
                        ControlMessage::Close => {
                            self.controller
                                .sender
                                .send(UIControlMessage::Status(StatusMessage::Notice(
                                    "TextUI gracefully closing".to_string(),
                                )))
                                .unwrap();
                            break;
                        }
                    },

                    Err(_) => {
                        eprintln!("Controller's TextUI channel disconnected unexpectedly");
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
    }

    // Prints the main menu to stdout.
    fn print_main_menu(&self) {
        println!(
            "Main menu:\n\
            1. Add printer\n\
            2. Remove printer\n\
            0. Exit\n\
            \n\
            Your choice: "
        );
    }

    // Spawns a new thread that reads from stdin and sends lines a crossbeam::channel::Receiver.
    // This prevents blocking in the main TextUI thread and allows the main TextUI thread to select
    // from this or any other ready Receiver.
    fn read_stdin(&self) -> channel::Receiver<String> {
        let (sender, receiver) = channel::unbounded();
        thread::spawn(move || {
            let stdin = io::stdin();
            loop {
                let mut buffer = String::new();
                match stdin.read_line(&mut buffer) {
                    Ok(_) => sender.send(buffer).expect(
                        "Stdin reader tried to send text, but the channel was disconnected!",
                    ),
                    Err(e) => panic!("{:?}", e),
                }
            }
        });
        receiver
    }
}
