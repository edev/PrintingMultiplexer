using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Threading;

namespace Printing_Multiplexer_Modules
{
    public delegate void Logger(string text);

    public abstract class BasicModule
    {
        // TODO Add naming or other chaining information as part of the module connection subsystem.
        public OutputCollection Outputs;
        private Logger logger;
        private Dispatcher dispatcher;

        public BasicModule() { }
        public BasicModule(Logger logMethod, Dispatcher wpfDispatcher)
        {
            logger = logMethod;
            dispatcher = wpfDispatcher;
        }

        // Give an item to this module. When you Give() an item, you MUST relinquish all knowledge of it, since the module may modify, move, or delete it (and may simply need a file lock).
        // Note: in future, the object being given may become an Interface of some kind that allows for non-filesystem objects such as in-memory objects read from the network to be passed.
        abstract public void Give(FileInfo file);

        protected void log(string text)
        {
            dispatcher?.BeginInvoke(logger, text);
        }
    }
}
