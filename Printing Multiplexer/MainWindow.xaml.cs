using Printing_Multiplexer_Modules;
using System;
using System.IO;
using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Printing_Multiplexer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        FolderWatcher folderWatcher;
        PrinterMultiplexer printerMultiplexer;
        FileMover fileMover;

        private static readonly string fileToPrint = "2017-09-09 11.13.08.jpg";

        private PrintTicket ticket;

        public MainWindow()
        {
            InitializeComponent();


            folderWatcher = new FolderWatcher(Log, Dispatcher);
            printerMultiplexer = new PrinterMultiplexer(Log, Dispatcher);
            fileMover = new FileMover(Log, Dispatcher);
            // TODO Add completion detection (in the multiplexer module)

            folderWatcher.Outputs.SetOutput(FolderWatcher.NextModule, printerMultiplexer);
            printerMultiplexer.Outputs.SetOutput(PrinterMultiplexer.NextModule, fileMover);

            // Initializes some things to use in PrintButton testing.
            ticket = new PrintTicket();
        }


        private void AddPrinterButton_Click(object sender, RoutedEventArgs e)
        {
            // Create and show the Add Printer dialog.
            AddPrinterDialog addPrinterDialog = new AddPrinterDialog(PrinterList.Items);
            addPrinterDialog.ShowDialog();

            // If the user clicks "Cancel" on the Add Printer dialog, go no further.
            if (addPrinterDialog.DialogResult == false) return;

            // If the user clicks "Cancel" on the Print dialog, go no further.
            if (!modifyPrinter(addPrinterDialog.SelectedPrinter)) return;

            // User clicked OK PrintQueue and PrintTicket are fully set. Add the printer.
            PrinterList.Items.Add(addPrinterDialog.SelectedPrinter);
            printerMultiplexer.AddPrinter(addPrinterDialog.SelectedPrinter.Printer);
        }

        private void RemovePrinterButton_Click(object sender, RoutedEventArgs e)
        {
            int index = PrinterList.SelectedIndex;

            // Only remove if we have a selected index. Don't pass garbage on.
            if (index < 0) return;

            // Now actually remove the printer, first from the multiplexer, then from our list.
            printerMultiplexer.RemovePrinter(((ListBoxPrinter)PrinterList.SelectedItem).Printer);
            PrinterList.Items.RemoveAt(index);
        }

        private void ModifyPrinterButon_Click(object sender, RoutedEventArgs e)
        {
            if (PrinterList.SelectedItem == null) return;
            modifyPrinter((ListBoxPrinter)PrinterList.SelectedItem);
        }

        private bool modifyPrinter(ListBoxPrinter printer)
        {
            if (printer == null) return false;

            // Initialize PrintDialog to use printer's Queue and Ticket
            System.Windows.Controls.PrintDialog pd = new System.Windows.Controls.PrintDialog();
            pd.PrintQueue = printer.Printer.Queue;
            // pd.PrintTicket = printer.Ticket;
            pd.UserPageRangeEnabled = false;

            if (pd.ShowDialog() == false) return false;

            // TODO FIXME This doesn't check that the printer is actually the same printer! It could be an invalid ticket!
            printer.Printer.Ticket = pd.PrintTicket;

            /*
            printer.Dialog.UserPageRangeEnabled = false;

            // Show the dialog; if the user clicks Cancel, return.
            if (printer.Dialog.ShowDialog() == false) return false;

            var pc = printer.Dialog.PrintQueue.GetPrintCapabilities();
            // Save the ticket only if the user hits Print
            printer.Ticket = printer.Dialog.PrintTicket;
            */
            return true;
        }

        private void PrintButton_Click(object sender, RoutedEventArgs e)
        {

            if (PrinterList.SelectedItem == null) return;
            if (!File.Exists(fileToPrint)) return;

            /*
            printImage(new FileInfo(fileToPrint), 
                ((ListBoxPrinter)PrinterList.SelectedItem).Queue, 
                ((ListBoxPrinter)PrinterList.SelectedItem).Ticket,
                ((ListBoxPrinter)PrinterList.SelectedItem).Dialog);
            */

            /* Problem with this method: it prints a small selection of pixels, and it has borders on 2 sides.... */
            // Prepare the file to print.
            // Borrowed from https://stackoverflow.com/questions/265062/load-image-from-file-and-print-it-using-wpf-how
            var bi = new BitmapImage();
            bi.BeginInit();
            bi.CacheOption = BitmapCacheOption.OnLoad;
            bi.UriSource = new Uri((new FileInfo(fileToPrint)).FullName);
            bi.EndInit();

            var vis = new DrawingVisual();
            var dc = vis.RenderOpen();
            dc.DrawImage(bi, new Rect { Width = (double)((ListBoxPrinter)PrinterList.SelectedItem).Printer.Ticket.PageMediaSize.Width, Height = (double)((ListBoxPrinter)PrinterList.SelectedItem).Printer.Ticket.PageMediaSize.Height });
            dc.Close();

            // Print the file to the selected printer.
            System.Windows.Controls.PrintDialog dialog = new System.Windows.Controls.PrintDialog();
            dialog.PrintQueue = ((ListBoxPrinter)PrinterList.SelectedItem).Printer.Queue;
            dialog.PrintTicket = ((ListBoxPrinter)PrinterList.SelectedItem).Printer.Ticket;
            dialog.PrintVisual(vis, fileToPrint);
            // ((ListBoxPrinter)PrinterList.SelectedItem).Dialog.PrintVisual(vis, fileToPrint);
            // */
        }

        private void printImage(FileInfo source, PrintQueue queue, PrintTicket ticket, System.Windows.Controls.PrintDialog dialog)
        {
            if (source == null || queue == null || ticket == null || dialog == null) return;

            // Code below is adapted from https://stackoverflow.com/questions/5314369/photo-printing-in-c-sharp

            // Pasted code below.
            PrintCapabilities capabilities = queue.GetPrintCapabilities();
            Size pageSize = new Size(capabilities.PageImageableArea.ExtentWidth, capabilities.PageImageableArea.ExtentHeight);
            Size printableSize = new Size();
            var imageableArea = ticket.PageMediaSize;
            var borderlessSupported = ticket.PageBorderless;
            var dpi = ticket.PageResolution;
            // For iX6820 set to borderless 4x6 photo preset,
            // imageableArea = ~8x10.5 @ 96 DPI
            // borderlessSupported = None
            // dpi = 600x600
            // Huh???

            // Question: do I simply need to draw a 4x6 at proper resolution and it'll print it correctly? I think that's possible.


            // Console.WriteLine();
            /*
            e.Graphics.DrawImage(nextImage, e.PageSettings.PrintableArea.X - e.PageSettings.HardMarginX, e.PageSettings.PrintableArea.Y - e.PageSettings.HardMarginY, e.PageSettings.Landscape ? e.PageSettings.PrintableArea.Height : e.PageSettings.PrintableArea.Width, e.PageSettings.Landscape ? e.PageSettings.PrintableArea.Width : e.PageSettings.PrintableArea.Height);

            DrawingVisual drawVisual = new DrawingVisual();
            ImageBrush imageBrush = new ImageBrush();
            imageBrush.ImageSource = new BitmapImage(aUri);
            imageBrush.Stretch = Stretch.Fill;
            imageBrush.TileMode = TileMode.None;
            imageBrush.AlignmentX = AlignmentX.Center;
            imageBrush.AlignmentY = AlignmentY.Center;
            if (imageBrush.ImageSource.Width > imageBrush.ImageSource.Height)
                printableSize = new Size(768, 576); //8x6
            else printableSize = new Size(576, 768); //6x8 
            double xcor = 0; double ycor = 0;
            if (imageBrush.ImageSource.Width > imageBrush.ImageSource.Height)
            {
                if ((pageSize.Width - printableSize.Height) > 0)
                    xcor = (pageSize.Width - printableSize.Height) / 2;
                if ((pageSize.Height - printableSize.Width) > 0)
                    ycor = (pageSize.Height - printableSize.Width) / 2;
            }
            else
            {
                if ((pageSize.Width - printableSize.Width) > 0)
                    xcor = (pageSize.Width - printableSize.Width) / 2;
                if ((pageSize.Height - printableSize.Height) > 0)
                    ycor = (pageSize.Height - printableSize.Height) / 2;
            }
            using (DrawingContext drawingContext = drawVisual.RenderOpen())
            {
                if (imageBrush.ImageSource.Width > imageBrush.ImageSource.Height)
                {
                    drawingContext.PushTransform(new RotateTransform(90, printableSize.Width / 2, printableSize.Height / 2));
                }
                drawingContext.DrawRectangle(imageBrush, null, new Rect(xcor, ycor, printableSize.Width, printableSize.Height));
            }
            SelectedPrinter.PrintVisual(drawVisual, "Print");
            */
        }

        private void InputFolderButton_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog folderBrowser = new FolderBrowserDialog();
            if (folderBrowser.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                InputFolderTextBox.Text = folderBrowser.SelectedPath;
                folderWatcher.SetFolder(folderBrowser.SelectedPath);
            }
        }

        public void Log(string text)
        {
            TextLog.AppendText(text);
            TextLog.AppendText("\n");
        }

        private void PrintedFolderButton_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog folderBrowser = new FolderBrowserDialog();
            if (folderBrowser.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                PrintedFolderTextBox.Text = folderBrowser.SelectedPath;
                fileMover.DestinationFolder = folderBrowser.SelectedPath;
            }
        }

        private void Copies_TextChanged(object sender, TextChangedEventArgs e)
        {
            int number;
            if (int.TryParse(Copies.Text, out number) == false
                || number < 0)
            {
                number = 1;
                Copies.Text = number.ToString();
            }
            if (printerMultiplexer != null)
            {
                try
                {
                    printerMultiplexer.Times = number;
                }
                catch (ArgumentOutOfRangeException)
                {
                    Log($"MainWindow.Copies_TextChanged: printerMultiplexer refused to change Times to {number}");
                }
            }
        }
    }
}
