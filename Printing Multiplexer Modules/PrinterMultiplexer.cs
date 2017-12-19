using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Printing;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Printing_Multiplexer_Modules
{
    public class PrinterMultiplexer : BasicModule
    {
        public const string NextModule = "NextModule";
        const int retryDelayMS = 1000;

        // List of printers
        PrinterManager printers;

        // Files and background task monitor - updated atomically using qLock for atomicity and concurrency control.
        SpinLock qLock;
        Queue<FileInfo> files;
        bool taskRunning = false;

        public PrinterMultiplexer() { initialize(); }
        public PrinterMultiplexer(Logger logger, Dispatcher dispatcher) : base(logger, dispatcher) { initialize(); }

        private void initialize()
        {
            Outputs = new OutputCollection(NextModule);
            files = new Queue<FileInfo>();
            printers = new PrinterManager(log);
            qLock = new SpinLock();
        }

        // Quickly add file to queue and return. From there on, it's the Multiplexer's responsibility.
        public override void Give(FileInfo file)
        {
            if (file == null) return;

            bool gotLock = false;
            while (!gotLock) qLock.Enter(ref gotLock);
            // Note that we can't enqueue a Visual, because we don't know the printer yet, so we can't render it for the right page size!
            files.Enqueue(file);
            qLock.Exit();

            log($"PrinterMultiplexer.Give: Enqueued file {file.FullName}");
            // Start trying to print if we're not already doing so.
            printAsync();
        }

        private async void printAsync()
        {
            // If we're already running a task chain, then don't start a new one. Singleton pattern.
            bool gotLock = false;
            while (!gotLock) qLock.Enter(ref gotLock);
            if (taskRunning == true)
            {
                qLock.Exit();
                log($"PrinterMultiplexer.printAsync: Print task chain already running. Ignoring.");
                return;
            }

            // Else start a new chain.
            FileInfo file;
            while (files.Count > 0)
            {
                file = files.Dequeue();
                taskRunning = true;
                qLock.Exit();
                log($"PrinterMultiplexer.printAsync: Starting background print: {file.FullName}");
                await Task.Run(() => backgroundPrint(file));
                gotLock = false;
                while (!gotLock) qLock.Enter(ref gotLock);
            }

            taskRunning = false;
            qLock.Exit();
        }

        private void backgroundPrint(FileInfo file)
        {
            // Keep trying to print every second until it goes through.
            while (!printers.TryPrint(file)) Thread.Sleep(retryDelayMS);

            // Then give the file to the next module.
            Outputs.GetOutput(NextModule).Give(file);
        }

        /*
        // Simulates event handling for PrintQueue job completion.
        private void printLoop()
        {
            foreach(Printer printer in printers)
            {
                // Check printer for status and, if available, try to print.
                Task.Run(() => tryPrinter(printer));
            }
        }

        private void tryPrinter(Printer printer)
        {
            printer.Queue.Refresh();
            if (printer.Queue.NumberOfJobs == 0 && printer.Queue.IsNotAvailable == false)
            {
                // TODO Figure out how to detect failure and try a different printer (as well as take this printer offline).
                FileInfo file = null;
                
                // Attempt to dequeue the next file, if one is available. Proceed only if a file is actually retrieved.
                while (files.Count > 0 && files.TryDequeue(out file) == false) ;
                if (file == null) return;

                printFile(file);
            }
        }

        private void printFile(FileInfo file)
        {
            // TODO Get printing code right and place it here.
        }

        public void AddPrinter(Printer printer)
        {
            if (printer == null) return;
            printers.Add(printer);
        }

        public void RemovePrinter(PrintQueue printer)
        {
            // TODO Change to a different method so we can actually remove printers....
        }

        class ConcurrentPriorityQueue
        {
            // TODO Write this class and use it instead of a Bag.
        }
        */

        public void AddPrinter(Printer p)
        {
            if (p == null) return;

            printers.AddPrinter(p);
            log($"PrinterMultiplexer.AddPrinter: Added {p.Queue.Name}. Queue size: {printers.Count}");
        }

        public void RemovePrinter(Printer p)
        {
            if (p == null) return;

            string logContains;
            if (printers.Contains(p))
            {
                logContains = "found";
            }
            else
            {
                logContains = "couldn't find";
            }

            int beforeCount = printers.Count;
            printers.RemovePrinter(p);
            int afterCount = printers.Count;
            log($"PrinterMultiplexer.RemovePrinter: {logContains} {p.Queue.Name}. Printer count changed from {beforeCount} to {afterCount}.");
        }

        private class PrinterManager
        {
            // Ideally a reasonable upper bound, so we don't have to deal with copies. It won't be a very large structure, regardless.
            const int initialListSize = 10;

            // Concurrency-controlled List that's used essentially like a queue (but requires full list behavior).
            SpinLock qLock;
            List<Printer> printers;

            // Local logger, i.e. the one containing to the parent object.
            Logger logger;

            public PrinterManager(Logger localLogger)
            {
                qLock = new SpinLock();
                printers = new List<Printer>(initialListSize);
                logger = localLogger;
            }
            
            public void AddPrinter(Printer printer)
            {
                if (printer == null) return;

                // Tail add / Enqueue
                lockEnter();
                printers.Add(printer);
                lockExit();
            }

            public void RemovePrinter(Printer printer)
            {
                if (printer == null) return;

                // TODO Make sure this actually works! Add logging. And, frankly, learn how to correctly override Equals or whatever on custom classes.
                lockEnter();
                printers.Remove(printer);
                lockExit();
            }

            public bool TryPrint(FileInfo file)
            {
                // Get the next available printer. If none, try again later!
                Printer printer = dequeue();
                if (printer == null) return false;

                // Actually print the image
                printer.Dispatcher.Invoke(() => doPrint(printer, file));
                log($"PrinterMultiplexer.PrinterManager.TryPrint: Printed {file.FullName} on {printer.Queue.Name}");

                // Add to the back of the queue.
                AddPrinter(printer);

                return true;
            }

            private void doPrint(Printer printer, FileInfo file)
            {
                if (printer == null || file == null) return;

                Visual visual = printer.DrawVisual(file);
                if (visual == null) return;

                PrintDialog dialog = new PrintDialog();
                dialog.PrintQueue = printer.Queue;
                dialog.PrintTicket = printer.Ticket;
                try
                {
                    dialog.PrintVisual(visual, file.Name);
                }
                catch (ArgumentNullException)
                { }
            }

            // Removes from the list the first printer with Count == 0.
            private Printer dequeue()
            {
                lockEnter();
                Printer returnValue = null;

                foreach(Printer p in printers)
                {
                    // TODO If necessary, optimize this by doing background refreshes simultaneously using a task group, instead of synchronously and serially, here.
                    if (p.Dispatcher.Invoke(() => checkIfPrinterIsAvailable(p)))
                    {
                        printers.Remove(p);
                        returnValue = p;
                        break;
                    }
                }
                lockExit();

                log($"PrinterMultiplexer.PrinterManager.dequeue: returning {returnValue?.Queue?.Name}");
                return returnValue;
            }

            private bool checkIfPrinterIsAvailable(Printer p)
            {
                if (p == null) return false;

                try
                {
                    p.Queue.Refresh();
                    return (p.Queue.NumberOfJobs == 0);
                }
                catch (PrintSystemException e)
                {
                    log($"PrintMultiplexer.checkIfPrinterIsAvailable: Refreshing {p.Queue.Name} raised exception: {e.Message}");
                    return false;
                }
            }

            private void lockEnter()
            {
                bool gotLock = false;
                while (!gotLock) qLock.Enter(ref gotLock);
            }

            private void lockExit()
            {
                qLock.Exit();
            }

            public int Count
            {
                get
                {
                    lockEnter();
                    int count = printers.Count;
                    lockExit();
                    return count;
                }
            }

            public bool Contains(Printer p)
            {
                return printers.Contains(p);
            }

            private void log(string text)
            {
                logger(text);
            }
        }
    }
}
