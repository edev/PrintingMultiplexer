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
        internal MainWindow.Printer SelectedPrinter { get; private set; }

        public AddPrinterDialog()
        {
            InitializeComponent();
            LocalPrintServer pServer = new LocalPrintServer();
            PrintQueueCollection printers = pServer.GetPrintQueues();
            foreach(PrintQueue p in printers)
            {
                PrinterListBox.Items.Add(new MainWindow.Printer(p.Name, p));
            }
        }

        private void okButton_Click(object sender, RoutedEventArgs e)
        {
            SelectedPrinter = (MainWindow.Printer) PrinterListBox.SelectedItem;
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
