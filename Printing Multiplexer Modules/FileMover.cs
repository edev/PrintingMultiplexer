using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace Printing_Multiplexer_Modules
{
    public class FileMover : BasicModule
    {
        public const string NextModule = "NextModule";

        private string destinationFolder;
        public string DestinationFolder
        {
            get { return destinationFolder; }
            set
            {
                if (Directory.Exists(value))
                {
                    try
                    {
                        // Will throw an exception if the user can't write to the directory.0
                        Directory.GetAccessControl(value);

                        // Success!
                        destinationFolder = value;
                        return;
                    }
                    catch (Exception e)
                    {
                        log($"FileMover.Set_DestinationFolder: Directory access exception: {e.Message}");
                    }
                }

                // If we're still here, there's been a failure.
                destinationFolder = null;
            }
        }

        public FileMover() { initialize(); }
        public FileMover(Logger logger, Dispatcher dispatcher) : base(logger, dispatcher) { initialize(); }

        private void initialize()
        {
            Outputs = new OutputCollection(NextModule);
        }

        public override void Give(FileInfo file)
        {
            // Run the method to move the file and give it to the next module as a task, because the method itself is synchronous, but we don't want to block the I/O thread.
            Task.Run(() => moveAndGiveFile(file));
        }

        private void moveAndGiveFile(FileInfo file)
        {
            if (file == null) return;

            // If we have nowhere to put the file, pass it along (or let it disappear from the pipeline, if there's nowhere to go).
            if (DestinationFolder == null)
            {
                log($"FileMover.Give: Nowhere to move file and nowhere to Give file: {file.FullName}");
                Outputs.GetOutput(NextModule)?.Give(file);
                return;
            }

            try
            {
                string destinationPath = DestinationFolder + "/" + file.Name;

                try
                {
                    // Remove the file if it exists.
                    if (File.Exists(destinationPath)) File.Delete(destinationPath);
                    log($"FileMover.moveAndGiveFile: Deleted existing file: {destinationPath}");
                }
                catch(Exception e)
                {
                    log($"FileMover.moveAndGiveFile: Could not delete existing file: {destinationPath}");
                }

                // Attempt the move.
                file.MoveTo(destinationPath);
                log($"FileMover.moveAndGiveFile: Moved file: {file.FullName}");
            }
            catch (Exception e)
            {
                log($"FileMover.Give: file.MoveTo threw exception: {e.Message}");

                // And we DON'T return here, because we don't want to break the pipeline if there's a chance it could still work.
            }

            // Now we pass it on.
            Outputs.GetOutput(NextModule)?.Give(file);
        }
    }
}
