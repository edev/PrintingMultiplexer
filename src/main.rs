mod auto_printer;
mod controller;
mod folder_watcher;
mod logger;
mod text_ui;

use controller::*;
use folder_watcher::*;
use logger::Logger;
use std::env;
use std::path::PathBuf;
use std::str::FromStr;
use std::thread;
use text_ui::*;

const LOGFILE: &str = "printer_multiplexer.log";

// Holds a sender and receiver pair for a channel, allowing easier organization within main().
// Other methods and structs will not need both a sender and a receiver for the same channel.
struct Channel<T> {
    sender: crossbeam::channel::Sender<T>,
    receiver: crossbeam::channel::Receiver<T>,
}

impl<T> Channel<T> {
    // A convenience method that creates a channel and stores its components in a struct.
    fn new() -> Self {
        let (sender, receiver) = crossbeam::channel::unbounded();
        Channel { sender, receiver }
    }
}

fn main() {
    // For the text UI only, we expect the user to pass in the path to the inbox folder as the
    // first and only argument. We will probably remove this once we have a GUI.
    let mut args = env::args();
    if args.len() != 2 {
        eprintln!(
            "Error: incorrect arguments.\n\
            \n\
            Usage:\n\
            PrintingMultiplexer.exe <path-to-inbox>\n"
        );
        return;
    }
    let watch_folder = args.nth(1).unwrap();
    println!("Watch folder: {}", watch_folder);

    // Construct the logger, before we do anything else.
    let to_logger = Channel::new();
    let controller_to_logger = Channel::new();
    let mut logger = Logger::new(
        controller_to_logger.receiver,
        to_logger.receiver,
        PathBuf::from_str(LOGFILE).unwrap(),
    );
    let logger_handle = thread::spawn(move || {
        logger.run();
    });
    // TODO Add a welcome/start log message.

    // Construct the UI.
    let from_controller = Channel::new();
    let to_controller = Channel::new();
    let ui_channels = ChannelPair::new(to_controller.sender, from_controller.receiver);
    let controller_ui_channels = ChannelPair::new(from_controller.sender, to_controller.receiver);
    let ui = TextUI::new(ui_channels, to_logger.sender.clone());
    let ui_handle = thread::spawn(move || {
        ui.run();
    });

    // Construct a folder watcher.
    let print_queue = Channel::new();
    let from_controller = Channel::new();
    let to_controller = Channel::new();
    let fw_channels = ChannelPair::new(to_controller.sender, from_controller.receiver);
    let controller_fw_channels = ChannelPair::new(from_controller.sender, to_controller.receiver);
    let mut folder_watcher = FolderWatcher::new(
        fw_channels,
        watch_folder,
        print_queue.sender,
        to_logger.sender.clone(),
    );
    let fw_handle = thread::spawn(move || {
        folder_watcher.run();
    });

    // Construct the controller, passing along the collected channels from above.
    let controller = Controller::new(
        controller_ui_channels,
        ui_handle,
        controller_fw_channels,
        fw_handle,
        print_queue.receiver,
        controller_to_logger.sender,
        to_logger.sender,
        logger_handle,
    );
    let controller_handle = thread::spawn(move || {
        controller.run();
    });

    // Wait for the controller to finish before closing the program. The controller will wait for
    // all other threads.
    controller_handle.join().unwrap();
}
