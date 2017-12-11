using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using System.Threading;
using System.Windows.Threading;

namespace Printing_Multiplexer_Modules
{
    public class FolderWatcher : BasicModule
    {
        public const string NextModule = "NextModule";

        FileSystemWatcher fsw = new FileSystemWatcher();

        // Milliseconds to sleep before trying to open a file again.
        const int threadSleepTime = 10;

        // Creates a new FolderWatcher that doesn't log anywhere.
        public FolderWatcher() { initialize(); }

        // Creates a new FolderWatcher that logs to the provided logger.
        public FolderWatcher(Logger logger, Dispatcher dispatcher) : base(logger, dispatcher) { initialize(); }

        // Initialization function, for use only in constructors. Used to eliminate duplication in the constructors, since they can't call one another while also invoking different base class constructors.
        private void initialize()
        {
            Outputs = new OutputCollection(NextModule);

            // Now, Set up the FileSystemWatcher, aside from Path and EnableRaisingEvents.

            fsw.IncludeSubdirectories = false;

            // Use the LastWrite filter so that we can (in theory) check once per file write and not have to loop to wait for the file to become available. That SHOULD mean that, in OnChanged, we simply check whether the file is available to open, yet, and if not, we wait for another event.
            fsw.NotifyFilter = NotifyFilters.FileName;

            // TODO Expose this option to the user.
            fsw.Filter = "*.jpg";

//             fsw.Changed += new FileSystemEventHandler(OnChanged);
            fsw.Created += new FileSystemEventHandler(OnCreated);
        }

        private void OnCreated(object sender, FileSystemEventArgs e)
        {
            log($"FileSystemWatcher: File creation detected: {e.FullPath}");
            
            // Make a FileInfo object out of it.
            FileInfo f = null;
            try
            {
                f = new FileInfo(e.FullPath);
            }
            catch (Exception exception)
            {
                // Well, guess we won't be handling this file.... Ignore.
                log($"FileSystemWatcher: OnChanged: Exception creating FileInfo object: {exception.Message}");
                return;
            }

            // File's not ready yet. Ignore.
            while (!isFileUnlocked(f))
            {
                log($"FileSystemWatcher: OnChanged: Ignoring locked file: {e.FullPath}");
                Thread.Sleep(threadSleepTime);

                // TODO Detect issues that aren't going to resolve themselves and cancel. Maybe have isFileUnlocked rethrow if the exception is not an in-use file?
            }

            // File is READY!
            log($"FileSystemWatcher: Giving file to ImageReviewer module: {e.FullPath}");
            Outputs.GetOutput(NextModule)?.Give(f);
        }

        public override void Give(FileInfo file)
        {
            // TODO Figure out what, if anything, to do with items given to FolderWatcher.
        }

        // Sets the folder to watch. Returns true if successfully set to watch the folder, or false if an error occurs.
        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        public bool SetFolder(string folder)
        {
            if(folder == null)
            {
                log($"FileSystemWatcher: SetFolder(null) called. Ignoring.");
                return false;
            }

            try
            {
                fsw.Path = folder;
                fsw.EnableRaisingEvents = true;
                log($"FileSystemWatcher: Now watching folder {fsw.Path}");
            }
            catch (ArgumentException e)
            {
                log($"FileSystemWatcher: SetFolder({folder}) raised exception: {e.Message}");
                return false;
            }

            return true;
        }

        // Copied and lightly modified from:
        // https://stackoverflow.com/questions/876473/is-there-a-way-to-check-if-a-file-is-in-use/937558#937558
        private bool isFileUnlocked(FileInfo file)
        {
            FileStream stream = null;
            try
            {
                stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.None);
            }
            catch (IOException e)
            {
                // The file is unavailable because it is:
                // still being written to
                // or being processed by another thread
                // or does not exist (has already been processed).
                log($"FileSystemWatcher: isFileUnlocked: IOException: {e.Message}");
                return false;
            }
            finally
            {
                if (stream != null)
                    stream.Close();
            }

            // File is not locked.
            return true;
        }
    }
}
