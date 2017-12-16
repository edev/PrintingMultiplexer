using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Printing_Multiplexer_Modules
{
    class PushedPrintQueue
    {
        // Some kind of printer data structure

        // Some kind of constructor

        // Add and remove printer methods, plus maybe set priority, etc.

        public bool TryPrint(FileInfo file)
        {
            // if (data structure has no printer available) return false;

            // print image, maybe asynchronously

            return true;
        }

        // Background updater
    }
}
