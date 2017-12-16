using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Printing_Multiplexer_Modules
{
    class PushyPrintQueue : BasicModule
    {
        SpinLock qLock;
        Queue<FileInfo> files;
        bool taskRunning = false;


        // TODO Constructor

        public override void Give(FileInfo file)
        {
            if (file == null) return;

            // Acquire qLock
            files.Enqueue(file);
            // Release qLock

            // Start trying to print if we're not already doing so.
            pushyPrintAsync();
        }

        private async void pushyPrintAsync()
        {
            // If we're already running a task chain, then don't start a new one. Singleton pattern.
            // Acquire qLock
            if (taskRunning == true)
            {
                // Release qLock
                return;
            }

            // Else start a new chain.
            FileInfo file;
            while (files.Count > 0)
            {
                file = files.Dequeue();
                taskRunning = true;
                // Release qLock
                await Task.Run(() => backgroundPrint(file));
                // Acquire qLock
            }

            taskRunning = false;
            // Release qLock
        }

        private void backgroundPrint(FileInfo file)
        {
            // Keep trying to print every second until it goes through.
            // while (!tryPrint(file)) sleep(1000);
        }
    }
}
