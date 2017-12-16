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
            DrawingContext dc = vis.RenderOpen();

            // Basic command to simply stretch and fill the whole page exactly (except maybe losing some pixels if the printer adds borders). This is the basis for our correct version.
            // dc.DrawImage(bi, new Rect { Width = (double) Ticket.PageMediaSize.Width, Height = (double)Ticket.PageMediaSize.Height });

            drawBitmap(dc, bi);

            dc.Close();

            return vis;
        }

        private void drawBitmap(DrawingContext context, BitmapSource bitmap)
        {
            // Before we begin, some convenience variables.
            double pageWidth = (double)Ticket.PageMediaSize.Width;
            double pageHeight = (double)Ticket.PageMediaSize.Height;

            // First step: check rotation and rotate bitmap to match the orientation of the canvas (if needed).
            BitmapImage image;
            if(rotate90(Ticket.PageMediaSize, bitmap))
            {
                // We have a mismatch between the orientations of the paper and the bitmap, so we need to rotate the bitmap 90 degrees to match.
                TransformedBitmap tb = new TransformedBitmap();
                tb.BeginInit();
                tb.Source = bitmap;
                tb.Transform = new RotateTransform(90D);
                tb.EndInit();
                image = new BitmapImage();
                bitmap = tb; // Orientation of bitmap did NOT swap. (???)
            }

            // Now, calculate the scale factor when scaling to the width of the page and to the height of the page.
            double scaleWidth = pageWidth / bitmap.Width;
            double scaleHeight = pageHeight / bitmap.Height;

            // Then, choose the max, which will be the correct scale factor to completely and minimally cover the printed page.
            double scaleFactor = Math.Max(scaleWidth, scaleHeight);

            // Next, compute the X, Y, Width, and Height of the rectangle to be drawn on the context.
            double x, y, width, height;
            width = bitmap.Width * scaleFactor;
            height = bitmap.Height * scaleFactor;
            x = (pageWidth - width) / 2;
            y = (pageHeight - height) / 2;

            // And produce the rectangle in which to draw the image.
            Rect pageSpaceImageBounds = new Rect(x, y, width, height);

            // Finally, draw the image!
            context.DrawImage(bitmap, pageSpaceImageBounds);
        }

        private void drawBitmapFirstDraft(DrawingContext context, BitmapImage bitmap)
        {
            // Fit the image to the long dimension exactly.
            bool isPageLandscape = (Ticket.PageMediaSize.Width / Ticket.PageMediaSize.Height > 1 ? true : false);
            double scaleFactor;
            if (isPageLandscape)
            {
                scaleFactor = scaleWidth(Ticket.PageMediaSize, bitmap);
            }
            else
            {
                scaleFactor = scaleHeight(Ticket.PageMediaSize, bitmap);
            }

            // Now scale the short dimension up (never down) iff it's shorter than the page.
            if(isPageLandscape)
            {
                // We're examining height.
                if (bitmap.Height * scaleFactor < Ticket.PageMediaSize.Height)
                {
                    scaleFactor = scaleHeight(Ticket.PageMediaSize, bitmap);
                }
            }
            else
            {
                // We're examining width.
                if (bitmap.Width * scaleFactor < Ticket.PageMediaSize.Width)
                {
                    scaleFactor = scaleWidth(Ticket.PageMediaSize, bitmap);
                }
            }

            // Finally, determine the values used to draw the image.

            double topLeftX, topLeftY, scaledWidth, scaledHeight;

            // Width and height are at least as large as page size but might be larger (implying that cropping will occur).
            scaledWidth = bitmap.Width * scaleFactor;
            scaledHeight = bitmap.Height * scaleFactor;

            // Top left corner (which defaults to (0, 0)) needs to be adjusted to accept half of the cropped pixels in order to center the image in both dimensions.
            topLeftX = ((double)Ticket.PageMediaSize.Width - scaledWidth) / 2;
            topLeftY = ((double)Ticket.PageMediaSize.Height - scaledHeight) / 2;

            // TODO Rotate if necessary, possibly before this whole scaling business.

            context.DrawImage(bitmap, new Rect(topLeftX, topLeftY, scaledWidth, scaledHeight));
        }

        private bool rotate90(PageMediaSize pageMediaSize, BitmapSource bitmap)
        {
            bool isPageLandscape = (pageMediaSize.Width / pageMediaSize.Height > 1 ? true : false);
            bool isBitmapLandscape = (bitmap.Width / bitmap.Height > 1 ? true : false);

            // We need to rotate if these don't match.
            return (isPageLandscape ^ isBitmapLandscape);
        }

        private double scaleWidth(PageMediaSize pageMediaSize, BitmapImage bitmap)
        {
            return (double) pageMediaSize.Width / bitmap.Width;
        }

        private double scaleHeight(PageMediaSize pageMediaSize, BitmapImage bitmap)
        {
            return (double) pageMediaSize.Height/ bitmap.Height;
        }
    }
}
