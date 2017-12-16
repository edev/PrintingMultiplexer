using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Printing_Multiplexer_Modules
{
    // TODO Consider whether this should simply be a QueueManager, as its own module. Interesting possibilities. (Maybe for later.) The potential difficulty of this is that it uses custom verbs.

    public class ModelPrintQueueManager : BasicModule
    {
        public ModelPrintQueueManager()
        {
            // Initialize Outputs.
        }

        public override void Give(FileInfo file)
        {
            // Lock
            // Enqueue file
            // Unlock
            // Fire FileReady Event
        }

        // Event: FileReady. Fired when a file is added to the queue.

        public FileInfo Peek()
        {
            // Standard queue peek.
            return null;
        }

        public FileInfo Dequeue()
        {
            // Standard dequeue.
            return null;
        }
    }

    // Currently coupled with ModelPrintQueueManager in that it knows queue verbs and depends on them. Not dependent on actual data structure used by ModelPrintQueueManager - it could easily be any data structure as long as the data structure used provides a single "next" item to be peeked at or retrieved.
    // TODO Consider making this a private nested class.
    public class ModelPrinterManager
    {
        public ModelPrinterManager(ModelPrintQueueManager pqm)
        {
            // Store reference to pqm.
            // Subscribe to pqm.FileReady event.
        }

        public void AddPrinter(Printer p)
        {
            // Add p to some printer 
        }

        public void RemovePrinter(Printer p)
        {

        }

        // Possibly a data model for printers, e.g. public List<Printer> ListPrinters()

        public async Task PrintAsync(FileInfo file)
        {
            /*
            // When GetPrinter returns, the Printer is ours and so is the Lock so no one else can modify printer state.
            await GetPrinter(out Printer, out Lock)
            Print(Printer, file, lock)
            Lock.Release()
             */
        }

        
        private bool print(Printer printer, FileInfo file, SpinLock pLock)
        {
            // if (!lock.haveLock) return false;
            if (!pLock.IsHeldByCurrentThread) return false;

            // print file to printer.Queue using printer.Ticket (and presumably printer.Dialog)
            // In other words, THIS is where the actual printing code probably goes

            // Print was sent to the printer. It's out of our hands (for now).
            return true;
        }

        /*
        private async Task<ExclusivePrinter> GetPrinter()
        {

        }
        */

        private class ExclusivePrinter
        {
            public Printer Printer { get; set; }
            public SpinLock Lock { get; set; }

            public ExclusivePrinter(Printer p, SpinLock l)
            {
                Printer = p;
                Lock = l;
            }
        }
    }
}
