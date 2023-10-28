using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Dalamud.Game.Text;
using OpenerCreator.Helpers;

namespace OpenerCreator.Managers
{
    public class OpenerManager
    {
        private static OpenerManager? instance;
        private static readonly object LockObject = new();
        public List<uint> Loaded { get; set; } = new List<uint>();
        private readonly Dictionary<string, List<uint>> openers;
        private readonly Dictionary<string, List<uint>> defaultOpeners;
        private readonly string openersFile = Path.Combine(OpenerCreator.PluginInterface.ConfigDirectory.FullName, "openers.json");

        private OpenerManager()
        {
            this.defaultOpeners = LoadOpeners(Path.Combine(OpenerCreator.PluginInterface.AssemblyLocation.Directory!.FullName, "openers.json"));
            this.openers = LoadOpeners(openersFile);
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

        public void AddOpener(string name, List<uint> actions) => openers[name] = new List<uint>(actions);

        public static void Compare(List<uint> opener, List<uint> used)
        {
            used = used.Take(opener.Count).ToList();
            var error = false;

            if (opener.SequenceEqual(used))
            {
                SuccessMessage();
                return;
            }
            else
            {
                // Identify differences
                for (var i = 0; i < Math.Min(opener.Count, used.Count); i++)
                {
                    var intended = ActionDictionary.Instance.GetActionName(opener[i]);
                    if (opener[i] != used[i] && !ActionDictionary.Instance.SameActions(intended, used[i]))
                    {
                        error = true;
                        var actual = ActionDictionary.Instance.GetActionName(used[i]);
                        OpenerCreator.ChatGui.Print(new XivChatEntry
                        {
                            Message = $"Difference in action {i + 1}: Substituted {intended} for {actual}",
                            Type = XivChatType.Echo
                        });
                    }
                }
                if (!error)
                {
                    SuccessMessage();
                }
            }
        }
        private static void SuccessMessage() => OpenerCreator.ChatGui.Print(new XivChatEntry
        {
            Message = "Great job! Opener executed perfectly.",
            Type = XivChatType.Echo
        });

        private Dictionary<string, List<uint>> LoadOpeners(string path)
        {
            try
            {
                var jsonData = File.ReadAllText(path);
                return JsonSerializer.Deserialize<Dictionary<string, List<uint>>>(jsonData)!;
            }
            catch (Exception e)
            {
                OpenerCreator.PluginLog.Error("Failed to load Openers", e);
                return new Dictionary<string, List<uint>>();
            }
        }

        public void SaveOpeners()
        {
            try
            {
                var jsonData = JsonSerializer.Serialize(openers);
                File.WriteAllText(openersFile, jsonData);
            }
            catch (Exception e)
            {
                OpenerCreator.PluginLog.Error("Failed to save Openers", e);

            }
        }
    }
}
