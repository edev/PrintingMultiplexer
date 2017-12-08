# Printing Multiplexer

This project aims to solve the problem space of reviewing documents and using multiple printers to print them. 

The motivation behind this project is the annual Christmas dinner for the [Sharing God's Bounty](sharinggodsbounty.com) soup kitchen in Sacramento, California. After eating dinner, families line up to take photos with Santa. A team of dedicated volunteers photographs, proofs, prints, and frames photos. The volunteers use multiple printers to keep the line moving as fast as possible, but without using specialized software, it is difficult to manage more than two printers efficiently. Since the workflow is highly repetitive, this project aims to automate it.

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