use crate::controller::{ChannelPair, ControlMessage, StatusMessage};
use crossbeam::channel;
use notify::event::{Event, EventKind};
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
    pub fn new(
        controller: ChannelPair<StatusMessage, ControlMessage>,
        folder: String,
        print_queue: channel::Sender<String>,
    ) -> Self {
        // Configure an event handler for a notify::Watcher. We'll keep it and use it elsewhere.
        let to_controller = controller.sender.clone();
        let watcher = notify::recommended_watcher(move |result: Result<Event, notify::Error>| {
            match result {
                Ok(event) => {
                    // TODO Send the file to the print queue the requested number of times.
                    // TODO Filter by correct event types, and determine whether we need to wait
                    // for files to be fully copied before we touch them.

                    // Log the event.
                    to_controller
                        .send(StatusMessage::Notice(format!(
                            "FolderWatcher event: {:?}",
                            event
                        )))
                        .unwrap();

                    // Add newly created images to print queue.
                    // We use _ instead of CreateKind because the value isn't fully consistent in
                    // our initial testing. Instead, we'll check the file ourselves.
                    if let EventKind::Create(_) = event.kind {
                        match event.paths.first() {
                            Some(path) => {
                                // Convert the path, which is a &PathBuf, to a &str.
                                let path = match path.to_str() {
                                    Some(path) => path,
                                    None => {
                                        to_controller
                                            .send(StatusMessage::Notice(
                                                "FolderWatcher: converting path to a Unicode string failed. Skipping."
                                                .to_string()
                                            ))
                                            .unwrap();
                                        return
                                    }
                                };

                                let lowercase_path = path.to_lowercase();
                                if lowercase_path.ends_with(".jpg")
                                    || lowercase_path.ends_with(".jpeg")
                                {
                                    print_queue.send(path.to_string()).unwrap();
                                    to_controller
                                        .send(StatusMessage::Notice(format!(
                                            "FolderWatcher: added image to print queue: {}",
                                            path
                                        )))
                                        .unwrap();
                                } else {
                                    to_controller
                                        .send(StatusMessage::Notice(format!(
                                            "FolderWatcher: ignoring non-JPEG file: {}",
                                            path
                                        )))
                                        .unwrap();
                                }
                            }
                            None => {
                                to_controller
                                    .send(StatusMessage::Notice(
                                        "FolderWatcher: Create event had no paths! Skipping."
                                            .to_string(),
                                    ))
                                    .unwrap();
                            }
                        }
                    }
                }
                Err(e) => {
                    to_controller
                        .send(StatusMessage::Error(format!(
                            "FolderWatcher error: {:?}",
                            e
                        )))
                        .unwrap();
                }
            }
        })
        .unwrap();
        FolderWatcher {
            controller,
            folder,
            watcher,
        }
    }

    pub fn run(&mut self) {
        self.watcher
            .watch(Path::new(&self.folder), notify::RecursiveMode::Recursive);

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
