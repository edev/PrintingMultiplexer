using Printing_Multiplexer_Modules;
using System;
using System.Collections.Generic;
using System.IO;
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

namespace Image_Reviewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        FolderWatcher folderWatcher;
        ImageReviewer imageReviewer;
        FileMover acceptMover;
        FileMover rejectMover;

        public MainWindow()
        {
            InitializeComponent();

            // Initialize modules, with our logging method.
            folderWatcher = new FolderWatcher(Log, Dispatcher);
            imageReviewer = new ImageReviewer(Log, Dispatcher);
            acceptMover = new FileMover(Log, Dispatcher);
            rejectMover = new FileMover(Log, Dispatcher);

            // Connect the modules in their intended orders.
            folderWatcher.Outputs.SetOutput(FolderWatcher.NextModule, imageReviewer);
            imageReviewer.Outputs.SetOutput(ImageReviewer.AcceptOutput, acceptMover);
            imageReviewer.Outputs.SetOutput(ImageReviewer.RejectOutput, rejectMover);

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

            // Now, as a courtesy, let's see if there are existing folders we can pre-set for accept and reject.
            string acceptPath = Path.Combine(folderBrowser.SelectedPath, ImageReviewer.AcceptOutput);
            string rejectPath = Path.Combine(folderBrowser.SelectedPath, ImageReviewer.RejectOutput);
            // These properties error-check their inputs, so we'll use that to our advantage here.
            acceptMover.DestinationFolder = acceptPath;
            rejectMover.DestinationFolder = rejectPath;
            if (acceptMover.DestinationFolder != null) AcceptFolderTextBox.Text = acceptPath;
            if (rejectMover.DestinationFolder != null) RejectFolderTextBox.Text = rejectPath;
        }

        private void AcceptFolderButton_Click(object Sender, RoutedEventArgs e)
        {
            FolderBrowserDialog folderBrowser = new FolderBrowserDialog();
            if (folderBrowser.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                AcceptFolderTextBox.Text = folderBrowser.SelectedPath;
                acceptMover.DestinationFolder = folderBrowser.SelectedPath;
            }
        }

        private void RejectFolderButton_Click(object Sender, RoutedEventArgs e)
        {
            FolderBrowserDialog folderBrowser = new FolderBrowserDialog();
            if (folderBrowser.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                RejectFolderTextBox.Text = folderBrowser.SelectedPath;
                rejectMover.DestinationFolder = folderBrowser.SelectedPath;
            }
        }

        private void AcceptButton_Click(object sender, RoutedEventArgs e)
        {
            imageReviewer.Accept();
            nextImage();
        }

        private void RejectButton_Click(object sender, RoutedEventArgs e)
        {
            imageReviewer.Reject();
            nextImage();
        }

        private void nextImage()
        {
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
