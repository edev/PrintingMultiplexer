using Printing_Multiplexer_Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Image_Reviewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        FolderWatcher folderWatcher;
        ImageReviewer imageReviewer;

        public MainWindow()
        {
            InitializeComponent();

            // Initialize folderWatcher and ImageReviewer with our logging method.
            folderWatcher = new FolderWatcher(Log, Dispatcher);
            imageReviewer = new ImageReviewer(Log, Dispatcher);

            // The folderWatcher connects to the imageReviewer...
            folderWatcher.Outputs.SetOutput(FolderWatcher.NextModule, imageReviewer);

            // Load the first image whenever it's ready.
            nextImage();
        }

        private void InputFolderButton_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog folderBrowser = new FolderBrowserDialog();
            if(folderBrowser.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                InputFolderTextBox.Text = folderBrowser.SelectedPath;
                folderWatcher.SetFolder(folderBrowser.SelectedPath);
            }
        }

        private void AcceptButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO Do something here
            nextImage();
        }

        private void RejectButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO Do something here
            nextImage();
        }

        private void nextImage()
        {
            // Get the next image in a different thread, in case the queue is empty.
            /*
            Task.Run(() =>
           {
               ImagePreview.Source = imageReviewer.NextImage();
           });
           */

            // For testing, let's just do it synchronously. Let's suppose we only press the button once there's an image ready.
            // ImagePreview.Source = imageReviewer.NextImage();
            ImagePreview.Source = imageReviewer.NextImage(nextImage, Dispatcher);
        }

        // Callback function 
        private void nextImageCallback(ImageSource source)
        {
            Console.WriteLine(source.ToString());
            ImagePreview.Source = source;
        }

        public void Log(string text)
        {
            TextLog.AppendText(text);
            TextLog.AppendText("\n");
        }
    }
}
