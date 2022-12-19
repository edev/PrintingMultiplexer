use crate::controller::{ChannelPair, ControlMessage, StatusMessage};
use notify::Watcher;
use std::path::Path;

pub struct FolderWatcher {
    // The lines of communication to and from the controller.
    controller: ChannelPair<StatusMessage, ControlMessage>,

    // The path to the folder to watch, as a string.
    folder: String,

    watcher: notify::RecommendedWatcher,
}

impl FolderWatcher {
    pub fn new(controller: ChannelPair<StatusMessage, ControlMessage>, folder: String) -> Self {
        // Configure an event handler for a notify::Watcher. We'll keep it and use it elsewhere.
        let sender = controller.sender.clone();
        let watcher = notify::recommended_watcher(move |result| {
            match result {
                Ok(event) => {
                    // TODO Send the file to the print queue the requested number of times.
                    // TODO Filter by correct event types, and determine whether we need to wait
                    // for files to be fully copied before we touch them.

                    // Log the event.
                    sender.send(StatusMessage::Notice(format!("FolderWatcher event: {:?}", event))).unwrap();
                },
                Err(e) => {
                    sender.send(StatusMessage::Error(format!("FolderWatcher error: {:?}", e))).unwrap();
                }
            }
        }).unwrap();
        FolderWatcher { controller, folder, watcher }
    }

    pub fn run(&mut self) {
        self.watcher.watch(Path::new(&self.folder), notify::RecursiveMode::Recursive);

        match self.controller.receiver.recv().unwrap() {
            ControlMessage::Close => {
                self.controller
                    .sender
                    .send(StatusMessage::Notice(
                        "FolderWatcher gracefully closing".to_string(),
                    ))
                    .unwrap();
            }
        }
    }
}
