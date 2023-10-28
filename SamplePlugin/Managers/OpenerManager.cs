using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.Text;

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

        public static void Compare(List<uint> opener, List<uint> used)
        {
            bool areEqual = opener.SequenceEqual(used);
            if (areEqual)
            {
                Plugin.ChatGui.Print(new XivChatEntry
                {
                    Message = "Great job! Opener executed perfectly.",
                    Type = XivChatType.Echo
                });
            }
            else
            {
                Plugin.ChatGui.Print(new XivChatEntry
                {
                    Message = "There were some mistakes: ",
                    Type = XivChatType.Echo
                });

                // Identify differences
                for (var i = 0; i < Math.Min(opener.Count, used.Count); i++)
                {
                    if (opener[i] != used[i])
                    {
                        Plugin.ChatGui.Print(new XivChatEntry
                        {
                            Message = $"Difference found at action number {i + 1}: Should use {opener[i]}, used {used[i]}",
                            Type = XivChatType.Echo
                        });
                    }
                }
            }
        }
    }
}
