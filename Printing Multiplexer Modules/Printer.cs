using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Printing;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Printing_Multiplexer_Modules
{
    public class Printer
    {
        const int sleepMilliseconds = 1000;
        // TODO Find an elegant way to introduce concurrency control around Refresh calls without requiring the user to manage a lock or cloning the queue (which would be very bad).
        //cprivate SpinLock qLock;
        private PrintQueue queue;
        public PrintQueue Queue
        {
            get { return queue; }
            set
            {
                // qLockEnter();
                // Update the queue, then reset the ticket to the default for the printer.
                queue = value;
                Ticket = queue?.UserPrintTicket;
                // qLock.Exit();
            }
        }
        public Dispatcher Dispatcher { get; private set; }

        // Events that can be raised periodically, following Queue.Refresh() calls.
        // None yet.

        public PrintTicket Ticket { get; set; }

        public Printer(Dispatcher dispatcher, PrintQueue queue, PrintTicket ticket=null)
        {
            this.Dispatcher = dispatcher;
            Queue = queue;
            Ticket = ticket;
            // TODO Start background loop
        }

        // We may not actually need this! We only need to refresh when we actually check printers, after all, and PrinterMultiplexer does this in a background task, so delays (e.g. blocking on Refresh()) are not a problem.
        /*
        private void backgroundRefreshLoop()
        {
            while(Queue != null)
            {
                qLockEnter();
                Queue?.Refresh();
                qLock.Exit();

                // Now, check for event conditions and fire events.


                Thread.Sleep(sleepMilliseconds);
            }
        }

        private void qLockEnter()
        {
            bool gotLock = false;
            while (!gotLock) qLock.Enter(ref gotLock);
            return;
        }
        */

        public Visual DrawVisual(FileInfo file)
        {
            if (file == null) return null;

            // Borrowed from https://stackoverflow.com/questions/265062/load-image-from-file-and-print-it-using-wpf-how
            // Heavily adapted to crop appropriately.

            var bi = new BitmapImage();
            bi.BeginInit();
            bi.CacheOption = BitmapCacheOption.OnLoad;
            bi.UriSource = new Uri(file.FullName);
            bi.EndInit();

            var vis = new DrawingVisual();
            var dc = vis.RenderOpen();
            dc.DrawImage(bi, new Rect { Width = (double) Ticket.PageMediaSize.Width, Height = (double)Ticket.PageMediaSize.Height });
            dc.Close();

            return vis;
        }

    }
}
