use crate::controller::{ChannelPair, ControlMessage, StatusMessage};

pub struct FolderWatcher {
    // The lines of communication to and from the controller.
    controller: ChannelPair<StatusMessage, ControlMessage>,
}

impl FolderWatcher {
    pub fn new(controller: ChannelPair<StatusMessage, ControlMessage>) -> Self {
        FolderWatcher { controller }
    }

    pub fn run(&self) {
        self.controller
            .sender
            .send(StatusMessage::Notice(
                "FolderWatcher just saying hi".to_string(),
            ))
            .unwrap();

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
