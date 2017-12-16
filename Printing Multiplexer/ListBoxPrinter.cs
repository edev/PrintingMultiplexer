using Printing_Multiplexer_Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Printing;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Printing_Multiplexer
{
    internal class ListBoxPrinter : ListBoxItem
    {
        // Extends ListBoxItem to add associated information.
        public Printer Printer;

        private PrintDialog dialog;
        public PrintDialog Dialog
        {
            get
            {
                // If nothing has changed, reuse the old dialog.
                if (dialog != null
                    && dialog.PrintQueue == Printer.Queue
                    && dialog.PrintTicket == Printer.Ticket)
                {
                    return dialog;
                }

                // Otherwise, make a new one.
                dialog = new PrintDialog();
                dialog.PrintQueue = Printer.Queue;
                dialog.PrintTicket = Printer.Ticket;
                return dialog;
            }
        }

        public ListBoxPrinter(String name, PrintQueue queue, PrintTicket ticket=null) : base()
        {
            Content = name;
            Printer = new Printer(Dispatcher, queue, ticket);
        }
    }
}
