mod controller;
mod folder_watcher;
mod text_ui;

use controller::*;
use folder_watcher::*;
use std::thread;
use text_ui::*;

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
        Channel {
            sender,
            receiver
        }
    }
}

fn main() {
    // Construct the UI.
    let from_controller = Channel::new();
    let to_controller = Channel::new();
    let ui_channels = ChannelPair::new(to_controller.sender, from_controller.receiver);
    let controller_ui_channels = ChannelPair::new(from_controller.sender, to_controller.receiver);
    let ui = TextUI::new(ui_channels);
    let ui_handle = thread::spawn(move || {
        ui.run();
    });

    // Construct a folder watcher.
    let from_controller = Channel::new();
    let to_controller = Channel::new();
    let fw_channels = ChannelPair::new(to_controller.sender, from_controller.receiver);
    let controller_fw_channels = ChannelPair::new(from_controller.sender, to_controller.receiver);
    let folder_watcher = FolderWatcher::new(fw_channels);
    let fw_handle = thread::spawn(move || {
        folder_watcher.run();
    });

    // Construct the controller, passing along the collected channels from above.
    let controller = Controller::new(
        controller_ui_channels,
        ui_handle,
        controller_fw_channels,
        fw_handle,
    );
    let controller_handle = thread::spawn(move || {
        controller.run();
    });

    // Wait for the controller to finish before closing the program. The controller will wait for
    // all other threads.
    controller_handle.join().unwrap();
}