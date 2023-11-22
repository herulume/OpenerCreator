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
        private static OpenerManager? SingletonInstance;
        private static readonly object LockObject = new();
        public List<uint> Loaded { get; set; } = new List<uint>();
        private readonly Dictionary<Jobs, Dictionary<string, List<uint>>> openers;
        private readonly Dictionary<Jobs, Dictionary<string, List<uint>>> defaultOpeners;
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
                if (SingletonInstance == null)
                {
                    lock (LockObject)
                    {
                        SingletonInstance ??= new OpenerManager();
                    }
                }
                return SingletonInstance;
            }
        }

        public void AddOpener(string name, Jobs job, List<uint> actions)
        {
            if (!openers.ContainsKey(job))
            {
                openers[job] = new Dictionary<string, List<uint>>();
            }
            openers[job][name] = new List<uint>(actions);
        }

        public List<Tuple<Jobs, List<string>>> GetDefaultNames() => defaultOpeners.Select(x => Tuple.Create(x.Key, x.Value.Keys.ToList())).ToList();

        public List<uint> GetDefaultOpener(string name, Jobs job) => new(defaultOpeners[job][name]);

        public List<uint> GetOpener(string name, Jobs job) => new(openers[job][name]);

        public List<Tuple<Jobs, List<string>>> GetNames() => openers.Select(x => Tuple.Create(x.Key, x.Value.Keys.ToList())).ToList();

        public void DeleteOpener(string name, Jobs job)
        {
            if (openers.ContainsKey(job))
            {
                openers[job].Remove(name);
                if (openers[job].Count == 0)
                {
                    openers.Remove(job);
                }
            }
        }

        public void Compare(List<uint> used, Action<List<string>> provideFeedback, Action<int> wrongAction)
        {
            var feedback = new List<string>();
            used = used.Take(Loaded.Count).ToList();

            if (Loaded.SequenceEqual(used))
            {
                feedback.Add(Messages.SuccessExec());
                provideFeedback(feedback);
                return;
            }

            var error = false;
            var size = Math.Min(Loaded.Count, used.Count);
            var shift = 0;

            for (var i = 0; i + shift < size; i++)
            {
                var openerIndex = i + shift;

                if (HasActionDifference(used, openerIndex, i, out var intended, out var actual))
                {
                    error = true;
                    feedback.Add(Messages.ActionDiff(i, intended, Actions.Instance.GetActionName(actual)));
                    wrongAction(openerIndex);

                    if (ShouldShift(openerIndex, size, used[i]))
                    {
                        shift++;
                    }
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

            provideFeedback(feedback);
        }
        private bool HasActionDifference(List<uint> used, int openerIndex, int usedIndex, out string intended, out uint actual)
        {
            intended = Actions.Instance.GetActionName(Loaded[openerIndex]);
            actual = used[usedIndex];

            return Loaded[openerIndex] != actual &&
                   !Actions.Instance.SameActionsByName(intended, actual) &&
                   actual != 0;
        }

        private bool ShouldShift(int openerIndex, int size, uint usedValue)
        {
            var nextIntended = Actions.Instance.GetActionName(Loaded[openerIndex]);
            return openerIndex + 1 < size && (Loaded[openerIndex + 1] == usedValue || Actions.Instance.SameActionsByName(nextIntended, usedValue));
        }

        private static Dictionary<Jobs, Dictionary<string, List<uint>>> LoadOpeners(string path)
        {
            try
            {
                var jsonData = File.ReadAllText(path);
                return JsonSerializer.Deserialize<Dictionary<Jobs, Dictionary<string, List<uint>>>>(jsonData)!;
            }
            catch (Exception e)
            {
                OpenerCreator.PluginLog.Error("Failed to load Openers", e);
                return new Dictionary<Jobs, Dictionary<string, List<uint>>>();
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
