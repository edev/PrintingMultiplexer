using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;

namespace Printing_Multiplexer
{
    class FolderWatcher : BasicModule
    {
        FileSystemWatcher fsw = new FileSystemWatcher();

        public FolderWatcher()
        {
            Outputs = new OutputCollection("NextModule");

            // Setup the FileSystemWatcher, aside from the Path.
            fsw.IncludeSubdirectories = false;
            fsw.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName;
            fsw.Changed += new FileSystemEventHandler(OnChanged);
            fsw.Renamed += new RenamedEventHandler(OnRenamed);
        }

        public override void Give(string path)
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

        // TODO Do something when files are changed, etc. and verify that all desired changes actually notify.
        // TODO Make the notifications customizable?
        private static void OnChanged(Object source, FileSystemEventArgs e)
        {
            Console.WriteLine("File: " + e.FullPath + " " + e.ChangeType);
        }

        private static void OnRenamed(object source, RenamedEventArgs e)
        {
            Console.WriteLine("File: {0} renamed to {1}", e.OldFullPath, e.FullPath);
        }
    }
}
