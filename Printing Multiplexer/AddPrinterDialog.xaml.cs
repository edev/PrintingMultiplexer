using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Printing;

namespace Printing_Multiplexer
{
    /// <summary>
    /// Interaction logic for AddPrinterDialog.xaml
    /// </summary>
    internal partial class AddPrinterDialog : Window
    {
        internal ListBoxPrinter SelectedPrinter { get; private set; }

        public AddPrinterDialog(ItemCollection printersToIgnore)
        {
            InitializeComponent();
            LocalPrintServer pServer = new LocalPrintServer();
            PrintQueueCollection printerQueueCollection = pServer.GetPrintQueues();

            // First, remove ignore printers from the printer list.
            SortedDictionary<string, PrintQueue> printers = new SortedDictionary<string, PrintQueue>();
            foreach (PrintQueue q in printerQueueCollection) printers[q.Name] = q;
            foreach (ListBoxPrinter p in printersToIgnore) printers.Remove(p.Content as string);
            
            // Now printers contains only printers that aren't on the ignore list. So let's add them to the UI.
            foreach(PrintQueue q in printers.Values)
            {
                PrinterListBox.Items.Add(new ListBoxPrinter(q.Name, q));
            }

            // Default to the first printer.
            PrinterListBox.SelectedIndex = 0;
        }

        private void okButton_Click(object sender, RoutedEventArgs e)
        {
            SelectedPrinter = (ListBoxPrinter) PrinterListBox.SelectedItem;
            // If nothing is selected, don't close the box.
            if (SelectedPrinter == null) return;
            PrinterListBox.Items.Remove(SelectedPrinter);
            DialogResult = true;
        }

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            SelectedPrinter = null;
            DialogResult = false;
        }
    }
}
