// Note: this project uses a homegrown logging solution, because it's simple, it minimizes external
// dependencies, and I found writing the logger to be a satisfying experience. However, we might
// replace the logger with a standard solution in the future.
use crate::controller::ControlMessage;
use chrono::prelude::*;
use crossbeam::channel;
use std::fs::File;
use std::io::Write;
use std::path::PathBuf;

// The types of log messages the logger can receive.
//
// These messages are tranlated into text by the logger itself for consistency, ease of
// maintenance, DRYness, ease of parsing, and so on.
#[derive(Clone, Debug)]
pub enum LogMessage {
    // Indicates that we are falling back to on-screen logging for some reason.
    FallBackToScreenLogging { reason: String },

    // An error message. The logger simply logs it; whether the program continues is decided
    // elsewhere (e.g. the originating module or the controller).
    Error { origin: String, message: String },

    // A notice message, i.e. a typical log message that does not indicate an error.
    Notice { origin: String, message: String },

    // Indicates that the originating module is shutting down. This will be logged.
    Closing { origin: String },

    // Indicates that the specified channel was disconnected.
    Disconnected { origin: String, channel: String },
    // TODO Add a Hello/Welcome/Starting message and have each thread send it as it starts.
}

pub trait Log {
    fn log(&self, message: LogMessage);
}

impl Log for channel::Sender<LogMessage> {
    fn log(&self, message: LogMessage) {
        let result = self.send(message.clone());
        if result.is_err() {
            // The logger's disconnected, so we have no choice but to revert to error output.
            println!("(LOGGER DISCONNECTED) {:?}", message);
        }
    }
}

// A logger that writes to a log file and, if necessary, falls back to writing to stdout/stderr.
//
// This logger is designed to keep logging however it can and for as long as it can.
pub struct Logger {
    // The file to which we write our logs, if any. The logger expects to write to a file, but it
    // falls back to writing to the screen via stdout and stderr if it encounters any error writing
    // to the file. If log is Some(..), then it's logging to the file. If it's None, then it's in
    // fallback mode.
    log: Option<File>,

    // Just for good measure, we store the path to the log file.
    #[allow(dead_code)]
    path: PathBuf,

    // The channel receiver to listen to the controller. This is a Some value so we can simply stop
    // listening to the controller if the controller disconnects. That should never happen, but if
    // it does, logging is even more important than ever, because there's probably a bug!
    // Therefore, the logger keeps on logging however it can for as long as it's running.
    controller: Option<channel::Receiver<ControlMessage>>,

    // The channel receiver that any part of the program can use to send log entries. If this
    // disconnects, the logger has no more reason to run and will log an error and exit.
    log_receiver: channel::Receiver<LogMessage>,
}

impl Logger {
    // Try to open the log file. If that fails, fall back to on-screen logging.
    //
    // In order to actually process logs, you then need to call run() in a new thread.
    pub fn new(
        controller: channel::Receiver<ControlMessage>,
        log_receiver: channel::Receiver<LogMessage>,
        path: PathBuf,
    ) -> Self {
        // If opening the file fails, we really need to tell the user why. Doing so using the
        // logging system, though, is slightly tricky, because the Logger doesn't exist when that
        // error occurs. Therefore, we save it for later.
        let mut error: Option<LogMessage> = None;

        // Either open the file or fall back if an error occurs.
        let log = match File::options().create(true).append(true).open(&path) {
            Ok(file) => Some(file),
            Err(e) => {
                error = Some(LogMessage::FallBackToScreenLogging {
                    reason: format!("{e} ({})", path.display()),
                });
                None
            }
        };

        let mut logger = Logger {
            controller: Some(controller),
            log_receiver,
            log,
            path,
        };

        // Now, we can report the error (if any) with the Logger.
        if let Some(e) = error {
            logger.log(e);
        }

        logger
    }

    // Start logging in the current thread.
    //
    // You probably want to start this in a new thread dedicated to logging.
    pub fn run(&mut self) {
        loop {
            // Set up a crossbeam select operation among the logger's open channels.
            //
            // This select is different from other in the project in that it will simply ignore any
            // non-essential channels that disconnect. As long as the log receiver is connected,
            // the logger will keep trying to emit log messages as they come in.
            let mut select = channel::Select::new();

            // If the controller is still connected, add its channel to the select. Otherwise, skip
            // it so we can keep logging without it.
            let controller_index = self.controller.as_ref().map(|c| select.recv(c));

            let receiver_index = select.recv(&self.log_receiver);
            let operation = select.select();
            match operation.index() {
                // Receive a controller message.
                i if controller_index.is_some() && i == *controller_index.as_ref().unwrap() => {
                    match operation.recv(self.controller.as_ref().unwrap()) {
                        // The controller has instructions for us.
                        Ok(message) => match message {
                            ControlMessage::Close => {
                                self.log(LogMessage::Closing {
                                    origin: self.origin(),
                                });
                                break;
                            }
                        },

                        // self.controller is presumably disconnected. Set it to None so we don't
                        // listen to it. Log the error. Keep logging for as long as possible.
                        Err(_) => {
                            self.log(LogMessage::Disconnected {
                                origin: self.origin(),
                                channel: "controller".to_string(),
                            });
                            self.controller = None;
                        }
                    }
                }

                // Receive a log message.
                i if i == receiver_index => {
                    match operation.recv(&self.log_receiver) {
                        // Got the log message! Write it.
                        Ok(message) => self.log(message),

                        // self.log_receiver is presumably disconnected. We can't continue.
                        Err(_) => {
                            self.log(LogMessage::Disconnected {
                                origin: self.origin(),
                                channel: "log_receiver".to_string(),
                            });
                            break;
                        }
                    }
                }

                // The operation index was invalid, or we're missing one or more cases here. Either
                // way, reaching this arm is probably a bug.
                i => {
                    // We need to drop immutable borrows of self so we can borrow mutably to log
                    // the error. Fortunately, we're done with those references; the compiler just
                    // doesn't know that.
                    drop(operation);

                    // Log the probable bug.
                    self.log(LogMessage::Error {
                        origin: self.origin(),
                        message: format!(
                            "Received invalid operation index {i} in Logger::run(). Probably a bug!"
                        ),
                    });
                }
            }
        }
    }

    // Takes a LogMessage value and emits a textual log message to the log file or stdout/stderr.
    fn log(&mut self, message: LogMessage) {
        // Pull a timestamp for the log.
        let timestamp = Local::now();

        // Translate the LogMessage value into text.
        //
        // Note: We have to be careful not to consume message, as we're not done with it yet.
        let mut message_text: String = match &message {
            LogMessage::FallBackToScreenLogging { reason } => {
                format!("[{timestamp}] ERROR Falling back to on-screen logging: {reason}")
            }
            LogMessage::Error { origin, message } => {
                format!("[{timestamp}] ERROR ({origin}) {message}")
            }
            LogMessage::Notice { origin, message } => {
                format!("[{timestamp}] NOTICE ({origin}) {message}")
            }
            LogMessage::Closing { origin } => {
                // Convert to Notice; recurse and return.
                self.log(LogMessage::Notice {
                    origin: origin.to_string(),
                    message: "Closing gracefully".to_string(),
                });
                return;
            }
            LogMessage::Disconnected { origin, channel } => {
                // Convert to Error; recurse and return.
                self.log(LogMessage::Error {
                    origin: origin.clone(),
                    message: format!("Channel disconnected unexpectedly: {channel}"),
                });
                return;
            }
        };
        message_text.push('\n');
        let message_text = message_text;

        // Emit the log message.
        match self.log {
            // We have an active log file, so write to it.
            Some(ref mut file) => {
                let result = file.write_all(message_text.as_bytes());
                if let Err(e) = result {
                    // Fall back to on-screen logging, and then log the message there. Don't try to
                    // flush the file.
                    self.log(LogMessage::FallBackToScreenLogging {
                        reason: e.to_string(),
                    });
                    self.log(message);
                    return;
                }

                let result = file.flush();
                if let Err(e) = result {
                    // Fall back to on-screen logging, and then log the message there. It might have
                    // been written to the log file, but we can't count on that, since we weren't
                    // able to flush. Therefore, we want to make sure we log the message on screen.
                    self.log(LogMessage::FallBackToScreenLogging {
                        reason: e.to_string(),
                    });
                    self.log(message);
                }
            }

            // We're in fallback mode.
            None => {
                // Determine whether to use stdout or stderr.
                let is_error = match &message {
                    LogMessage::FallBackToScreenLogging { .. } => true,
                    LogMessage::Error { .. } => true,
                    LogMessage::Notice { .. } => false,
                    LogMessage::Closing { .. } => false,
                    LogMessage::Disconnected { .. } => true,
                };

                if is_error {
                    eprintln!("{}", message_text);
                } else {
                    println!("{}", message_text);
                }
            }
        }
    }

    fn origin(&self) -> String {
        "Logger".to_string()
    }
}
