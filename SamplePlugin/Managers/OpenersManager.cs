using System.Collections.Generic;

namespace SamplePlugin.Managers
{
    public class OpenerManager
    {
        private static OpenerManager? instance;
        private static readonly object LockObject = new();
        private readonly Dictionary<string, List<uint>> openerDictionary;

        private OpenerManager()
        {
            // TODO: load commmon openers from a file
            openerDictionary = new Dictionary<string, List<uint>>();
        }

        public static OpenerManager Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (LockObject)
                    {
                        instance ??= new OpenerManager();
                    }
                }
                return instance;
            }
        }

        public List<uint> GetOpener(string name)
        {
            if (openerDictionary.TryGetValue(name, out var opener))
            {
                return opener;
            }
            return new List<uint>();
        }

        public void AddOrUpdate(string name, List<uint> opener)
        {
            openerDictionary[name] = opener;
        }
    }
}
