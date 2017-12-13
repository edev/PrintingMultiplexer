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

        private PrintQueue queue;
        public PrintQueue Queue
        {
            get { return queue; }
            set
            {
                // Update the queue, then reset the ticket to the default for the printer.
                queue = value;
                Ticket = queue?.UserPrintTicket;
            }
        }

        public PrintTicket Ticket { get; set; }

        private PrintDialog dialog;
        public PrintDialog Dialog
        {
            get
            {
                if (dialog != null
                    && dialog.PrintQueue == queue
                    && dialog.PrintTicket == Ticket)
                {
                    return dialog;
                }

                // Otherwise, make a new one.
                dialog = new PrintDialog();
                dialog.PrintQueue = queue;
                dialog.PrintTicket = Ticket;
                return dialog;
            }
        }

        public ListBoxPrinter(string name, PrintQueue queue) : base()
        {
            Content = name;
            Queue = queue;
            // Ticket will be auto-set.
        }

        public ListBoxPrinter(String name, PrintQueue queue, PrintTicket ticket) : base()
        {
            Content = name;
            Queue = queue;
            Ticket = ticket;
        }
    }
}
