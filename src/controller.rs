use crossbeam::channel;

// Messages that `Controller` might send out.
pub enum ControlMessage {
    Close,
}

// Generic status messages anyone might send to the controller.
pub enum StatusMessage {
    Notice(String),
    Error(String),
}

// Control messages that the UI can send to the controller, e.g. to request service.
pub enum UIControlMessage {
    Status(StatusMessage),
    WatchFolder(String),
}

// A pair of channels for talking to another party.
pub struct ChannelPair<SenderType, ReceiverType> {
    pub sender: channel::Sender<SenderType>,
    pub receiver: channel::Receiver<ReceiverType>,
}

impl<SenderType, ReceiverType> ChannelPair<SenderType, ReceiverType> {
    // A convenience method for easily creating a ChannelPair.
    pub fn new(sender: channel::Sender<SenderType>, receiver: channel::Receiver<ReceiverType>) -> Self {
        ChannelPair {
            sender,
            receiver,
        }
    }
}

// The central hub for all coordination and control between other parts of the program.
//
// All control and log messages pass through this struct. Other communication, e.g. the MPMC
// channel for communication between watched folder(s) and printer(s), can and probably should
// happen directly rather than through this struct. The job of this struct is to respond to, route,
// and dispatch control messages throughout the program's lifecycle.
pub struct Controller {
    // Channels for talking to the UI.
    ui: ChannelPair<ControlMessage, UIControlMessage>,
}

impl Controller {
    pub fn new(ui: ChannelPair<ControlMessage, UIControlMessage>) -> Self {
        Controller {
            ui,
        }
    }
}
