# Printing Multiplexer

This project aims to solve the problem space of reviewing documents and using multiple printers to print them. 

The motivation behind this project is the annual Christmas dinner for the [Sharing God's Bounty](http://sharinggodsbounty.com) soup kitchen in Sacramento, California. After eating dinner, families line up to take photos with Santa. A team of dedicated volunteers photographs, proofs, prints, and frames photos. The volunteers use multiple printers to keep the line moving as fast as possible, but without using specialized software, it is difficult to manage more than two printers efficiently. Since the workflow is highly repetitive, this project aims to automate it.

## Modular Architecture

Workflows are broken into simple (perhaps atomic) tasks, represented as modules, that accept input in a standard format and define named output hooks (e.g. Accept and Reject, for a review module). When a module receives an object (e.g. a photo), the module sending the object passes responsibility for that object to the receiver.

The system is architected to allow maximum flexibility, including at runtime, to configure any desired workflow.

## Initial Deliverables

Given that the author is simultaneously learning C#, WPF, and Visual Studio 2017 for this project, and that the initial version of the project must be functional within 10 days, the goal for December 16, 2017 is to build a DLL assembly containing the core modules, either with or without integrating WPF controls into the modules themselves, and to build two initial, hard-coded workflows as separate assemblies.

The first workflow will monitor a folder for changes and open for review any images added to the folder. When an image is accepted or rejected, it will be moved to an appropriate folder.

The second workflow will monitor a folder for changes, print any images added to the folder (distributing the workload across available printers), and move printed images to a folder for printed images. It will support printing multiple copies of images and will retain a log of photos printed.

## Future Goals

Given the modular architecture, the nearest-term goal after completing the initial deliverables is to use the lessons learned from the initial deliverables to flesh out the modular architecture and create a graphical, runtime framework for configuring workflows, so that the end user can easily configure any sane combination of connected modules.

The next goal is to refine the basic modules, with a paritcular focus on expanding the customization options that the GUI for each module exposes. Some elements may be hard-coded as part of the workflows for the initial deliverables, even though the modular architecture supports much greater flexibility; exposing this flexibility through WPF controls is a primary goal at this stage.

The tertiary future goal is to expand the selection of modules available, such as adding modules for reviewing alternate file types ranging from other image types (e.g. Canon, Nikon, and Sony RAW files, if possible) to non-image file types (with appropriate embedded previews).

Finally, an implicit and ongoing goal is to make the project more idiomatic as the author becomes more familiar with C#, WPF, VS2017, and so on - including a comprehensive battery of tests.

## Limitations

Current limitations include (but are not limited to):

- Renaming files from outside the application, while they're being processed through a workflow, will probably break things or cause an application crash.
- No customization of workflows, yet.

## Available Modules

_Note: all modules inherit from the BasicModule abstract class._

- FolderWatcher: uses System.IO.FileSystemWatcher to monitor a folder for new files.
- ImageReviewer: maintains a queue of files it's given, feeds them as ImageSource objects via ImageReviewer.NextImage, and dispatches them to an Accept or Reject module.
- FileMover: moves files it's given to a new folder and gives them to the next module.
- PrinterMultiplexer: manages printers and prints any images it receives using the next available printer, scaling and cropping the images to cover the full page. (Proprietary printer port types like HP auto-configured ports are not currently supported.)

## Module Wish-List

- An option in FolderWatcher to add all of the files already present into the folder.
- DriveWatcher module: watches for a drive to be inserted, scours the drive (or a subfolder) for files of a given type, and Gives them. This would be useful for inserting SD/CF cards: use a DriveWatcher connected to a FileMover that will effectively clear out the flash drive and move files into a review folder.
- A Windows Portable Devices module with similar capabilities to FolderWatcher.
- A branch module (1 input, 0+ outputs)
- A drive eject module: combine a DriveWatcher, a FileMover, and a Branch to both an Eject and an ImageReview module to create a seamless removable storage workflow, i.e. running SD cards.

## Supporting Classes

- BasicModule: the abstract ancestor of all modules, which provides the framework for module interconnection.
- OutputCollection: allows a BasicModule to create pairs of associations between outputs and receivers. On initialization, receives an array of output labels (e.g. Accept, Reject), each of which can be assigned a BasicModule. For instance, FolderWatcher defines one output called "NextModule"; it invokes the _Give_ method on this object any time a file is received.