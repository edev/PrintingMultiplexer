using System;
using System.Collections;
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

            // If we're still here, the user clicked OK.
            
            // User clicked OK. Add the printer.
            PrinterList.Items.Add(addPrinterDialog.SelectedPrinter);
        }

        private void RemovePrinterButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ModifyPrinterButon_Click(object sender, RoutedEventArgs e)
        {

        }

        private void PrintButton_Click(object sender, RoutedEventArgs e)
        {
            // Let's get the basic print method down, and then we'll figure out the architectural concerns.

            // A print queue is essentially our handle to a single printer.
            // PrintQueue printQueue;

            // Our window into the printing system on the local machine. Allows us to see the printers on the system, etc.
            // LocalPrintServer localPrintServer = new LocalPrintServer();

            // A collection of print queues - we may use this as our data structure to manage printing operations.
            // Question: we can iterate over available printers using an enumerator, but what is an "available" printer in this context?
            // If it performs simple round robin, we don't want to do that. That's the wrong algorithm.
            // I'm imagining that we'll use event handlers to add printers to the queue when they're done printing, so we might add and remove from this collection, but of course, that raises the question of whether they add at front, back, or anywhere they please. It's possible we may have to use a ConcurrentQueue<PrintQueue> instead.
            // PrintQueueCollection localPrinterCollection = localPrintServer.GetPrintQueues();

            // IEnumerator printerEnumerator = localPrinterCollection.GetEnumerator();

            // if (printerEnumerator.MoveNext())
            // {
            //     // Here's our printer.
            //     printQueue = (PrintQueue) printerEnumerator.Current;
            // }
            // else
            // {
            //     // No printer available.
            //     // NOTE: This is NOT the final algorithm! I'm just following the PrintTicket example from the Microsoft API reference!
            //     return;
            // }

            // I believe the print ticket is what holds our settings for a given printer, unless we use some other print schema object that holds some proprietary settings.
            // I think we're going to want to store this, probably in a small class that also holds the associated print queue and deals with keeping them consistent with one another.
            // Question: how do we customize the print ticket?
            // PrintTicket printTicket = printQueue.DefaultPrintTicket;

            // A print capabilities object represents what the printer can do; we can use this to customize a printer's settings.
            // We won't use PrintCapabilities, most likely, since we want the user to customize the settings and give us a PrintTicket to hold on to.

            PrintDialog pd = new PrintDialog();
            pd.PrintTicket = ticket;
            pd.ShowDialog();
            ticket = pd.PrintTicket;
        }

        internal class Printer : ListBoxItem
        {
            // Extends ListBoxItem to add associated information.

            private PrintQueue queue;
            public PrintQueue Queue
            {
                get { return queue; }
                set
                {
                    // Update the queue, then reset the ticket to the default for the printer.
                    queue = value;
                    Ticket = queue?.DefaultPrintTicket;
                }
            }

            public PrintTicket Ticket { get; set; }

            public Printer(string name, PrintQueue queue) : base()
            {
                Content = name;
                Queue = queue;
                // Ticket will be auto-set.
            }

            public Printer(String name, PrintQueue queue, PrintTicket ticket) : base()
            {
                Content = name;
                Queue = queue;
                Ticket = ticket;
            }
        }
    }
}
