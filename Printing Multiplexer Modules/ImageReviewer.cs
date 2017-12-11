using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Printing_Multiplexer_Modules
{
    public delegate void NextImageCallback ();

    public class ImageReviewer : BasicModule
    {
        public static readonly string AcceptOutput = "Accept";
        public static readonly string RejectOutput = "Reject";
        public static readonly string[] OutputNames = { AcceptOutput, RejectOutput };

        // This lock protects ALL private member fields.
        SpinLock fileLock = new SpinLock();
        Queue<FileInfo> files = new Queue<FileInfo>();
        NextImageCallback nextImageCallback = null;
        Dispatcher nextImageCallbackDispatcher = null;
        
        FileInfo fileUnderReview = null;

        public ImageReviewer() { initialize(); }
        public ImageReviewer(Logger logger, Dispatcher dispatcher) : base(logger, dispatcher) { initialize(); }

        private void initialize()
        {
            Outputs = new OutputCollection(OutputNames);
        }

        public override void Give(FileInfo file)
        {
            enqueue(file);
        }

        // Dequeue the next file, load it as a BitmapImage, and return it. If there is no file, store the callback and dispatcher, to be called when the next file is enqueued.
        // If a file is under review, it will NOT be passed to the next module; it will silently exit the workflow.
        public ImageSource NextImage(NextImageCallback callback, System.Windows.Threading.Dispatcher dispatcher)
        {
            // TODO Consider reusing the dispatcher from BasicModule, which was added as part of logging.
            ImageSource returnValue = null;
            bool gotLock = false;
            while (!gotLock) fileLock.Enter(ref gotLock);

            // We have the lock.

            // Clear any saved callbacks; this request overrides them.
            nextImageCallback = null;
            nextImageCallbackDispatcher = null;

            if (files.Count > 0)
            {
                // Great! We can dequeue.

                FileInfo file;

                // Get the next file to process.
                file = files.Dequeue();

                log($"ImageReviewer.NextImage: Dequeued file: {file.FullName}");

                // Save the file so we have it when the decision to accept/reject comes back.
                fileUnderReview = file;
                fileLock.Exit();

                // Load the image, AFTER exiting.
                try
                {
                    returnValue = makeImage(file);
                }
                catch (Exception e)
                {
                    log($"ImageReviewer.NextImage: makeImage threw exception: {e.Message}");
                    log("ImageReviewer.NextImage: Ignoring this image and recursing.");
                    returnValue = NextImage(callback, dispatcher);
                }
            }
            else
            {
                // Nothing to dequeue. Save a callback.

                nextImageCallback = callback;
                nextImageCallbackDispatcher = dispatcher;

                fileLock.Exit();

                // And clear the current saved file.
                fileUnderReview = null;

                log($"ImageReviewer.NextImage: Nothing to dequeue. Saved callback.");
            }
            return returnValue;
        }

        private void enqueue(FileInfo file)
        {
            bool gotLock = false;
            while (!gotLock) fileLock.Enter(ref gotLock);

            // We have the lock.

            files.Enqueue(file);

            if (nextImageCallback != null)
            {
                // Someone's waiting. Hand the image straight to them.

                // First, let's take care of the work that must be locked.
                // We'll save a copy of the callback and clear the shared memory.
                NextImageCallback callback = nextImageCallback;
                Dispatcher dispatcher = nextImageCallbackDispatcher;
                nextImageCallback = null;
                nextImageCallbackDispatcher = null;

                // Save the file since it's being reviewed.
                fileUnderReview = file;

                fileLock.Exit();

                log($"ImageReviewer.enqueue: Enqueued and invoking callback for: {file.FullName}");

                // Now we do the real work: make a new BitmapImage and pass it on.
                dispatcher.Invoke(() => callback());
            }
            else
            {
                // We're just enqueueing in the background. Nothing more to do.
                fileLock.Exit();

                log($"ImageReviewer.enqueue: Enqueued: {file.FullName}");
            }
        }

        private BitmapImage makeImage(FileInfo file)
        {
            if (file == null) return null;

            BitmapImage bitmap = new BitmapImage();
            bitmap.BeginInit();
            // Cache it on load so we can safely move, delete, and so on.
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.UriSource = new Uri(file.FullName);
            bitmap.EndInit();
            log($"ImageReviewer.makeImage: Loaded image: {file.FullName}");
            return bitmap;
        }

        // Sends the file under review to the Accept module, if any, then moves on to the next image and returns a bitmap of it.
        public void Accept()
        {
            if (fileUnderReview == null) return;
            Outputs.GetOutput(AcceptOutput)?.Give(fileUnderReview);
            fileUnderReview = null;
        }

        // Sends the file under review to the Reject module, if any, then moves on to the next image and returns a bitmap of it.
        public void Reject()
        {
            if (fileUnderReview == null) return;
            Outputs.GetOutput(RejectOutput)?.Give(fileUnderReview);
            fileUnderReview = null;
        }
    }
}
