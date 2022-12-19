use crate::controller::{ChannelPair, ControlMessage, StatusMessage};

pub struct FolderWatcher {
    // The lines of communication to and from the controller.
    controller: ChannelPair<StatusMessage, ControlMessage>,
}

impl FolderWatcher {
    pub fn new(controller: ChannelPair<StatusMessage, ControlMessage>) -> Self {
        FolderWatcher {
            controller,
        }
    }
}
