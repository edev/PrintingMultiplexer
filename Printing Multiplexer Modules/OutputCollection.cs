using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Printing_Multiplexer
{
    class OutputCollection
    {
        string[] keys;
        Dictionary<string, BasicModule> outputs = new Dictionary<string, BasicModule>();

        // Creates the names of ouputs that then become immutable.
        // Note that names are stored and subsequently retrieved in the order in which they appear in the names array.
        public OutputCollection(params string[] names)
        {
            List<string> keyList = new List<string>();

            foreach (string n in names)
            {
                if (!outputs.ContainsKey(n))
                {
                    outputs[n] = null;
                    keyList.Add(n);
                }
            }
            // TODO Make this actually immutable, if possible?
            keys = keyList.ToArray();
        }

        public string[] Names
        {
            // Retrieves an array of output names, in the order in which they were created.
            get
            {
                // Copy the data for safety, please!
                var names = new string[keys.Length];
                Array.Copy(keys, names, keys.Length);

                // Have fun with your copy.
                return names;
            }
        }

        // Associates the given BasicModule with the given name, if the name is a valid output name. To clear an output, pass null as the output.
        // Returns true if the association was made and false if it could not be made (i.e. the name was invalid).
        public bool SetOutput(string name, BasicModule output)
        {
            if (name == null || !outputs.ContainsKey(name))
            {
                return false;
            }

            outputs[name] = output;
            return true;
        }

        // Returns the BasicModule associated with the given output name, if one exists.
        // Returns null if the output doesn't exist or no name is given.
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
