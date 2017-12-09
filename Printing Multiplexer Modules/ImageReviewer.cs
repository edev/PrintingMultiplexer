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
        // This lock protects ALL private member fields.
        SpinLock fileLock = new SpinLock();
        Queue<FileInfo> files = new Queue<FileInfo>();
        NextImageCallback nextImageCallback = null;
        Dispatcher nextImageCallbackDispatcher = null;
        
        FileInfo fileUnderReview = null;

        // ManualResetEvent signal = new ManualResetEvent(false);
        // Thread queueProcessor;

        public override void Give(FileInfo file)
        {
            enqueue(file);
        }

        // Dequeue the next file, if one exists, and load it as the Source of destination. If the queue is empty, wait for the signal.
        /*
        public BitmapImage NextImage()
        {
            FileInfo file = null;
            BitmapImage bitmap = new BitmapImage();
            bool retrievedImage = false;
            bool gotLock;

            while (!retrievedImage)
            {
                // Acquire the lock.
                gotLock = false;
                while (!gotLock) fileLock.Enter(ref gotLock);

                if (files.Count > 0)
                {
                    // Great! We can dequeue.
                    file = files.Dequeue();
                    fileLock.Exit();

                    // Load the image.
                    bitmap.BeginInit();
                    // Cache it on load so we can safely move, delete, and so on.
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.UriSource = new Uri(file.FullName);
                    bitmap.EndInit();

                    // Don't loop again.
                    retrievedImage = true;
                }
                else
                {
                    // Wait for signal and try again.
                    signal.Reset();
                    fileLock.Exit();
                    signal.WaitOne();
                }
            }

            return bitmap;
        }

        // Either add the file to the wait queue or, if someone is already waiting on a response, simply send the file directly along.
        void enqueue(FileInfo file)
        {
            if (file == null) return;

            bool gotLock = false;
            while (!gotLock) fileLock.Enter(ref gotLock);

            // Now that we're locked, enqueue, then set the signal.
            files.Enqueue(file);
            signal.Set();

            fileLock.Exit();
        }
        */

        public ImageSource NextImage(NextImageCallback callback, System.Windows.Threading.Dispatcher dispatcher)
        {
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

                // Save the file so we have it when the decision to accept/reject comes back.
                fileUnderReview = file;
                fileLock.Exit();

                // Load the image, AFTER exiting.
                returnValue = makeImage(file);

            }
            else
            {
                // Nothing to dequeue. Save a callback.

                nextImageCallback = callback;
                nextImageCallbackDispatcher = dispatcher;

                fileLock.Exit();

                // And clear the current saved file.
                fileUnderReview = null;
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

                // Now we do the real work: make a new BitmapImage and pass it on.
                dispatcher.Invoke(() => callback());
            }
            else
            {
                fileLock.Exit();
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
            return bitmap;
        }
    }
}
