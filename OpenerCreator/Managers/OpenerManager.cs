using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using OpenerCreator.Helpers;

namespace OpenerCreator.Managers
{
    public class OpenerManager
    {
        private static OpenerManager? instance;
        private static readonly object LockObject = new();
        public List<uint> Loaded { get; set; } = new List<uint>();
        private readonly Dictionary<string, Dictionary<string, List<uint>>> openers;
        private readonly Dictionary<string, Dictionary<string, List<uint>>> defaultOpeners;
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

        public void AddOpener(string name, List<uint> actions) => openers["Any"][name] = new List<uint>(actions);

        public List<string> GetDefaultNames() => defaultOpeners["Any"].Keys.ToList();

        public List<uint> GetDefaultOpener(string name) => new(defaultOpeners["Any"][name]);

        public List<uint> GetOpener(string name) => new(openers["Any"][name]);

        public List<string> GetNames() => openers.Keys.ToList(); // TODO: check if exists, for all tbh lmao

        public void DeleteOpener(string name) => openers["Any"].Remove(name);

        // TODO: Clean
        public void Compare(List<uint> used, Action<List<string>> provideFeedback, Action<int> wrongAction)
        {
            var feedback = new List<string>();
            used = used.Take(Loaded.Count).ToList();
            var error = false;

            if (Loaded.SequenceEqual(used))
            {
                feedback.Add(Messages.SuccessExec());
                provideFeedback(feedback);
                return;
            }
            else
            {
                var size = Math.Min(Loaded.Count, used.Count);
                var shift = 0;
                // Identify differences
                for (var i = 0; i + shift < size; i++)
                {
                    var openerIndex = i + shift;
                    var intended = Actions.Instance.GetActionName(Loaded[openerIndex]);
                    if (Loaded[openerIndex] != used[i] && !Actions.Instance.SameActions(intended, used[i]))
                    {
                        error = true;
                        var actual = Actions.Instance.GetActionName(used[i]);
                        feedback.Add(Messages.ActionDiff(i, intended, actual));
                        wrongAction(openerIndex);
                        var nextIntended = Actions.Instance.GetActionName(Loaded[openerIndex]);
                        if (openerIndex + 1 < size && (Loaded[openerIndex + 1] == used[i] || Actions.Instance.SameActions(nextIntended, used[i])))
                            shift++;
                    }
                }

                if (!error)
                {
                    feedback.Add(Messages.SuccessExec());
                }

                if (shift != 0)
                {
                    feedback.Add(Messages.OpenerShift(shift));
                }
            }
            provideFeedback(feedback);
        }

        private Dictionary<string, Dictionary<string, List<uint>>> LoadOpeners(string path)
        {
            try
            {
                var jsonData = File.ReadAllText(path);
                return JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, List<uint>>>>(jsonData)!;
            }
            catch (Exception e)
            {
                OpenerCreator.PluginLog.Error("Failed to load Openers", e);
                return new Dictionary<string, Dictionary<string, List<uint>>>();
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
