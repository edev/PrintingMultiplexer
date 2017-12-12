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
            int selectedIndex = PrinterList.SelectedIndex;
            if (selectedIndex < 0) return;
            modifyPrinter((ListBoxPrinter)PrinterList.Items[selectedIndex]);
        }

        private bool modifyPrinter(ListBoxPrinter printer)
        {
            if (printer == null) return false;

            // Initialize PrintDialog to use printer's Queue and Ticket
            PrintDialog pd = new PrintDialog();
            pd.PrintQueue = printer.Queue;
            pd.PrintTicket = printer.Ticket;
            pd.UserPageRangeEnabled = false;

            // Show the dialog; if the user clicks Cancel, return.
            if (pd.ShowDialog() == false) return false;

            // Save the ticket only if the user hits Print
            printer.Ticket = pd.PrintTicket;
            return true;
        }
    }
}
