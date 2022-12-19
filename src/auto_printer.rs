use crate::controller::{ChannelPair, ControlMessage, StatusMessage};
use crossbeam::channel;

pub struct AutoPrinter {
    // The lines of communication to and from the controller.
    controller: ChannelPair<StatusMessage, ControlMessage>,

    // A reciver for the MPMC print queue: this is where we pull print jobs.
    // Note that we pass the path to the file rather than, say, a parsed JPEG. This is to free up
    // the folder watcher's thread for speedier operation overall.
    print_queue: channel::Receiver<String>,
}

impl AutoPrinter {
    pub fn new(
        controller: ChannelPair<StatusMessage, ControlMessage>,
        print_queue: channel::Receiver<String>,
    ) -> Self {
        AutoPrinter {
            controller,
            print_queue,
        }
    }

    pub fn run(&self) {
    }
}
