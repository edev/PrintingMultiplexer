﻿using Printing_Multiplexer_Modules;
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
        FolderWatcher folderWatcher = new FolderWatcher();
        ImageReviewer imageReviewer = new ImageReviewer();

        public MainWindow()
        {
            InitializeComponent();

            // The folderWatcher connects to the imageReviewer...
            folderWatcher.Outputs.SetOutput(FolderWatcher.NextModule, imageReviewer);
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
            Task.Run(() =>
           {
               ImagePreview.Source = imageReviewer.NextImage();
           });
        }
    }
}