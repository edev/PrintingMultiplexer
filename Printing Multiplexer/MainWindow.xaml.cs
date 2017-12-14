using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Printing;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Printing_Multiplexer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static readonly string fileToPrint = "2017-09-09 11.13.08.jpg";

        private PrintTicket ticket;

        public MainWindow()
        {
            InitializeComponent();

            // Populate initial list of printers.
            LocalPrintServer pServer = new LocalPrintServer();
            PrintQueueCollection printers = pServer.GetPrintQueues();
            // IEnumerator printerEnumerator = localPrinterCollection.GetEnumerator();

            // Lists printers (just their names, no backing data structure)
            // foreach (PrintQueue q in printers)
            // {
            //     ListBoxItem lbi = new ListBoxItem();
            //     lbi.Content = q.Name;
            //     PrinterList.Items.Add(lbi);
            // }

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
        }

        private void RemovePrinterButton_Click(object sender, RoutedEventArgs e)
        {
            int index = PrinterList.SelectedIndex;
            // Only remove if we have a selected index. Don't pass garbage on.
            if (index < 0) return;
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
            PrintDialog pd = new PrintDialog();
            pd.PrintQueue = printer.Queue;
            // pd.PrintTicket = printer.Ticket;
            pd.UserPageRangeEnabled = false;

            if (pd.ShowDialog() == false) return false;

            // TODO FIXME This doesn't check that the printer is actually the same printer! It could be an invalid ticket!
            printer.Ticket = pd.PrintTicket;

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
            dc.DrawImage(bi, new Rect { Width = (double)((ListBoxPrinter)PrinterList.SelectedItem).Ticket.PageMediaSize.Width, Height = (double)((ListBoxPrinter)PrinterList.SelectedItem).Ticket.PageMediaSize.Height });
            dc.Close();

            // Print the file to the selected printer.
            PrintDialog dialog = new PrintDialog();
            dialog.PrintQueue = ((ListBoxPrinter)PrinterList.SelectedItem).Queue;
            dialog.PrintTicket = ((ListBoxPrinter)PrinterList.SelectedItem).Ticket;
            dialog.PrintVisual(vis, fileToPrint);
            // ((ListBoxPrinter)PrinterList.SelectedItem).Dialog.PrintVisual(vis, fileToPrint);
            // */
        }

        private void printImage(FileInfo source, PrintQueue queue, PrintTicket ticket, PrintDialog dialog)
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
    }
}
