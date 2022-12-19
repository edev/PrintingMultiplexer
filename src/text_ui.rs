use crate::controller::{ChannelPair, ControlMessage, StatusMessage, UIControlMessage};

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
        self.controller
            .sender
            .send(UIControlMessage::Status(StatusMessage::Notice(
                "TextUI just popping by.".to_string(),
            )))
            .unwrap();
        self.controller.sender.send(UIControlMessage::Exit).unwrap();

        match self.controller.receiver.recv().unwrap() {
            ControlMessage::Close => {
                self.controller
                    .sender
                    .send(UIControlMessage::Status(StatusMessage::Notice(
                        "TextUI gracefully closing".to_string(),
                    )))
                    .unwrap();
            }
        }
    }
}
