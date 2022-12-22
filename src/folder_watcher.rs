use crate::controller::{ChannelPair, ControlMessage, FolderWatcherMessage};
use crate::logger::{Log, LogMessage};
use crossbeam::channel;
use notify::event::{Event, EventKind};
use notify::Watcher;
use std::path::Path;

// Helper to generate origin fields for various LogMessage values. Normally, this would be an
// associated method, but with the move closure that actually watches folders, there is no self.
fn origin() -> String {
    "FolderWatcher".to_string()
}

pub struct FolderWatcher {
    // The lines of communication to and from the controller.
    controller: ChannelPair<FolderWatcherMessage, ControlMessage>,

    // The path to the folder to watch, as a string.
    folder: String,

    watcher: notify::RecommendedWatcher,

    // The program's log. Send log entries here.
    log: channel::Sender<LogMessage>,
}

impl FolderWatcher {
    pub fn new(
        controller: ChannelPair<FolderWatcherMessage, ControlMessage>,
        folder: String,
        print_queue: channel::Sender<String>,
        log: channel::Sender<LogMessage>,
    ) -> Self {
        // Configure an event handler for a notify::Watcher. We'll keep it and use it elsewhere.
        let to_logger = log.clone();
        let watcher = notify::recommended_watcher(move |result: Result<Event, notify::Error>| {
            match result {
                Ok(event) => {
                    // TODO Send the file to the print queue the requested number of times.
                    // TODO Filter by correct event types, and determine whether we need to wait
                    // for files to be fully copied before we touch them.

                    // Log the event.
                    to_logger.log(LogMessage::Notice {
                        origin: origin(),
                        message: format!("{:?}", event),
                    });

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
                                        to_logger
                                            .send(LogMessage::Error {
                                                origin: origin(),
                                                message: "Converting path to a Unicode string \
                                                    failed. Skipping."
                                                    .to_string(),
                                            })
                                            .unwrap();
                                        return;
                                    }
                                };

                                let lowercase_path = path.to_lowercase();
                                if lowercase_path.ends_with(".jpg")
                                    || lowercase_path.ends_with(".jpeg")
                                {
                                    print_queue.send(path.to_string()).unwrap();
                                    to_logger.log(LogMessage::Notice {
                                        origin: origin(),
                                        message: format!("Added image to print queue: {}", path),
                                    });
                                } else {
                                    to_logger.log(LogMessage::Notice {
                                        origin: origin(),
                                        message: format!("Ignoring non-JPEG file: {}", path),
                                    });
                                }
                            }
                            None => {
                                to_logger.log(LogMessage::Notice {
                                    origin: origin(),
                                    message: "Create event had no paths! Skipping.".to_string(),
                                });
                            }
                        }
                    }
                }
                Err(e) => {
                    to_logger.log(LogMessage::Error {
                        origin: origin(),
                        message: format!("handle_event returned an error: {e}"),
                    });
                }
            }
        })
        .unwrap();
        FolderWatcher {
            controller,
            folder,
            watcher,
            log,
        }
    }

    pub fn run(&mut self) {
        // Start the folder watcher.
        let result = self
            .watcher
            .watch(Path::new(&self.folder), notify::RecursiveMode::Recursive);
        if let Err(e) = result {
            // Couldn't start watching. Log the error and exit.
            self.log.log(LogMessage::Error {
                origin: origin(),
                message: format!("Exiting due to error: {e}"),
            });
            return;
        }

        // Start listening for Controller messages.
        // TODO Remove this clippy annocation once there's at least one non-exiting match arm.
        #[allow(clippy::never_loop)]
        loop {
            match self.controller.receiver.recv().unwrap() {
                ControlMessage::Close => {
                    self.log.log(LogMessage::Closing { origin: origin() });
                    break;
                }
            }
        }

        // Once it's time to stop, end the watch.
        let result = self.watcher.unwatch(Path::new(&self.folder));
        if let Err(e) = result {
            self.log.log(LogMessage::Error {
                origin: origin(),
                message: format!("Could not unwatch {}: {}", self.folder, e),
            });
        }
    }
}
