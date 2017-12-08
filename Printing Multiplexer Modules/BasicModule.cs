using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Printing_Multiplexer
{
    abstract class BasicModule
    {
        // TODO Add naming or other chaining information as part of the module connection subsystem.
        public OutputCollection Outputs;

        // Give an item to this module. When you Give() an item, you MUST relinquish all knowledge of it, since the module may modify, move, or delete it (and may simply need a file lock).
        // Note: in future, the object being given may become an Interface of some kind that allows for non-filesystem objects such as in-memory objects read from the network to be passed.
        abstract public void Give(FileInfo file);
    }
}
