using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Printing_Multiplexer_Modules
{
    public class ImageReviewer : BasicModule
    {
        Queue<FileInfo> files = new Queue<FileInfo>();
        SpinLock fileLock = new SpinLock();
        ManualResetEvent signal = new ManualResetEvent(false);
        Thread queueProcessor;

        public override void Give(FileInfo file)
        {
            enqueue(file);
        }

        // Dequeue the next file, if one exists, and load it as the Source of destination. If the queue is empty, wait for the signal.
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
    }
}
