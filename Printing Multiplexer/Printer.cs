using System;
using System.Collections.Generic;
using System.Linq;
using System.Printing;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Printing_Multiplexer
{
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
