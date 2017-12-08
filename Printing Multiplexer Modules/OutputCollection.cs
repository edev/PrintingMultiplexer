using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Printing_Multiplexer
{
    class OutputCollection
    {
        Dictionary<string, BasicModule> outputs = new Dictionary<string, BasicModule>();

        // Creates the names of ouputs that then become immutable.
        public OutputCollection(params string[] args)
        {
            foreach (string s in args)
            {
                if (!outputs.ContainsKey(s))
                {
                    outputs[s] = null;
                }
            }
        }

        public string[] Names
        {
            get
            {
                return outputs.Keys.ToArray();
            }
        }

        public bool SetOutput(string name, BasicModule output)
        {
            if (name == null || !outputs.ContainsKey(name))
            {
                return false;
            }

            outputs[name] = output;
            return true;
        }

        public BasicModule GetOutput(string name)
        {
            if (name == null || !outputs.ContainsKey(name))
            {
                return null;
            }

            return outputs[name];
        }
    }
}
