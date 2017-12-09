using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using System.Threading;

namespace Printing_Multiplexer_Modules
{
    public class FolderWatcher : BasicModule
    {
        public const string NextModule = "NextModule";

        FileSystemWatcher fsw = new FileSystemWatcher();

        // A small data structure to cache recent entries and try to make sure we don't send multiple notifications.
        // Note that the technique suggested on StackOverflow doesn't seem to be working as expected, and timers are the alternate method.
        SpinLock fileLock = new SpinLock();
        const int listSize = 16;
        List<string> fileList = new List<string>(listSize);
        public FolderWatcher()
        {
            Outputs = new OutputCollection(NextModule);

            // Now, Set up the FileSystemWatcher, aside from the Path.

            fsw.IncludeSubdirectories = false;
            // Use the LastWrite filter so that we can (in theory) check once per file write and not have to loop to wait for the file to become available. That SHOULD mean that, in OnChanged, we simply check whether the file is available to open, yet, and if not, we wait for another event.
            fsw.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName;
            fsw.Filter = "*.jpg";
            fsw.Changed += new FileSystemEventHandler(OnChanged);
            fsw.Created += new FileSystemEventHandler(OnCreated);
        }

        private void OnCreated(object sender, FileSystemEventArgs e)
        {
            // Remove the item if it was there, because clearly the user wants to add it again, so our caching needs to be sure not to block it!
            bool gotLock = false;
            while (!gotLock) fileLock.Enter(ref gotLock);
            fileList.Remove(e.FullPath);
            fileLock.Exit();
            System.Diagnostics.Debug.WriteLine($"Clearing {e.FullPath} from the cache.");
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

            // Wrong event type. Ignore.
            if (e.ChangeType != WatcherChangeTypes.Changed) return;

            // We've recently processed this same file.
            bool gotLock = false;
            while (!gotLock) fileLock.Enter(ref gotLock);
            if (fileList.Contains(e.FullPath))
            {
                fileLock.Exit();
                System.Diagnostics.Debug.WriteLine("File Already Processed. Ignoring.");
                return;
            }
            fileLock.Exit();

            // Make a FileInfo object out of it.
            FileInfo f = null;
            try
            {
                f = new FileInfo(e.FullPath);
            }
            catch (Exception exception)
            {
                // Well, guess we won't be handling this file.... Ignore.
                return;
            }

            // File's not ready yet. Ignore.
            if (!isFileUnlocked(f)) return;

            // Add the file to our recent cache in case it comes up again....
            gotLock = false;
            while (!gotLock) fileLock.Enter(ref gotLock);
            if (fileList.Count == listSize)
            {
                // But first, trim the oldest element to keep it down to listSize elements.
                fileList.RemoveAt(0);
            }
            fileList.Add(e.FullPath);
            fileLock.Exit();

            // File is READY!
            Outputs.GetOutput(NextModule)?.Give(f);
        }

        // Copied and lightly modified from:
        // https://stackoverflow.com/questions/876473/is-there-a-way-to-check-if-a-file-is-in-use/937558#937558
        private bool isFileUnlocked(FileInfo file)
        {
            FileStream stream = null;
            System.Diagnostics.Debug.WriteLine("Is File Unlocked?...");
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

            System.Diagnostics.Debug.WriteLine("Yes.");
            // File is not locked.
            return true;
        }
    }
}
