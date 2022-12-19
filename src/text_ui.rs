use crate::controller::{ChannelPair, ControlMessage, UIControlMessage};

// A simple, text-based UI for controlling the printing operation.
//
// This UI is meant to be simple, reliable, and quick to construct.
pub struct TextUI {
    // The lines of communication to and from the controller.
    controller: ChannelPair<UIControlMessage, ControlMessage>,
}

impl TextUI {
    pub fn new(controller: ChannelPair<UIControlMessage, ControlMessage>) -> Self {
        TextUI {
            controller,
        }
    }
}
