using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;

namespace Printing_Multiplexer
{
    public class FolderWatcher : BasicModule
    {
        const string NextModule = "NextModule";

        FileSystemWatcher fsw = new FileSystemWatcher();

        public FolderWatcher()
        {
            Outputs = new OutputCollection(NextModule);

            // Now, Set up the FileSystemWatcher, aside from the Path.

            fsw.IncludeSubdirectories = false;
            // Use the LastWrite filter so that we can (in theory) check once per file write and not have to loop to wait for the file to become available. That SHOULD mean that, in OnChanged, we simply check whether the file is available to open, yet, and if not, we wait for another event.
            fsw.NotifyFilter = NotifyFilters.LastWrite;
            fsw.Changed += new FileSystemEventHandler(OnChanged);
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
                return false;
            }

            try
            {
                fsw.Path = folder;
                fsw.EnableRaisingEvents = true;
            }
            catch (ArgumentException e)
            {
                return false;
            }

            return true;
        }

        // When a file is created, wait for it to be ready, then give it to NextModule.
        private void OnChanged(Object source, FileSystemEventArgs e)
        {
            // We use Changed instead of Created because Changed events will fire as the file is written; Created is only fired once, at the start, and we need to detect the end of the file copy.
            if (e.ChangeType != WatcherChangeTypes.Changed)
            {
                // Wrong event type. Ignore.
                return;
            }
            FileInfo f = null;

            try
            {
                f = new FileInfo(e.FullPath);
            }
            catch(Exception exception)
            {
                // Well, guess we won't be handling this file.... Ignore.
                return;
            }

            if(!isFileUnlocked(f))
            {
                // File's not ready yet. Ignore.
                return;
            }

            // File is READY!
            Outputs.GetOutput(NextModule)?.Give(f);
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
            catch (IOException)
            {
                // The file is unavailable because it is:
                // still being written to
                // or being processed by another thread
                // or does not exist (has already been processed).
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
