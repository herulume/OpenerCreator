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
        private const uint CatchAllAction = 0;
        private static OpenerManager? SingletonInstance;
        private static readonly object LockObject = new();
        private readonly IActionManager actions;
        public List<uint> Loaded { get; set; } = new List<uint>();
        private readonly Dictionary<Jobs, Dictionary<string, List<uint>>> openers;
        private readonly Dictionary<Jobs, Dictionary<string, List<uint>>> defaultOpeners;
        private string openersFile { get; init; }

        // Just for testing
        // need a better approach
        public OpenerManager(IActionManager actions)
        {
            this.actions = actions;
            this.openersFile = "empty";
            this.openers = new Dictionary<Jobs, Dictionary<string, List<uint>>>();
            this.defaultOpeners = new Dictionary<Jobs, Dictionary<string, List<uint>>>();
        }

        private OpenerManager(IActionManager actions, ValueTuple _) : this(actions)
        {
            this.openersFile = Path.Combine(OpenerCreator.PluginInterface.ConfigDirectory.FullName, "openers.json");
            this.openers = LoadOpeners(openersFile);
            this.defaultOpeners = LoadOpeners(Path.Combine(OpenerCreator.PluginInterface.AssemblyLocation.Directory!.FullName, "openers.json"));
        }

        public static OpenerManager Instance
        {
            get
            {
                if (SingletonInstance == null)
                {
                    lock (LockObject)
                    {
                        SingletonInstance ??= new OpenerManager(Actions.Instance, new ValueTuple());
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

        public void Compare(List<uint> used, Action<Feedback> provideFeedback, Action<int> wrongAction)
        {
            var feedback = new Feedback();
            used = used.Take(Loaded.Count).ToList();

            if (Loaded.SequenceEqual(used))
            {
                feedback.AddMessage(Feedback.MessageType.Success, "Great job! Opener executed perfectly.");
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
                    feedback.AddMessage(Feedback.MessageType.Error, $"Difference in action {i + 1}: Substituted {intended} for {actions.GetActionName(actual)}");
                    wrongAction(openerIndex);

                    if (ShouldShift(openerIndex, size, used[i]))
                    {
                        shift++;
                    }
                }
            }

            if (!error && shift == 0)
            {
                feedback.AddMessage(Feedback.MessageType.Success, "Great job! Opener executed perfectly.");
            }

            if (shift != 0)
            {
                feedback.AddMessage(Feedback.MessageType.Info, $"You shifted your opener by {shift} {(shift == 1 ? "action" : "actions")}.");
            }

            provideFeedback(feedback);
        }
        private bool HasActionDifference(List<uint> used, int openerIndex, int usedIndex, out string intended, out uint actual)
        {
            var intendedId = Loaded[openerIndex];
            intended = actions.GetActionName(intendedId);
            actual = used[usedIndex];

            return Loaded[openerIndex] != actual &&
                   !actions.SameActionsByName(intended, actual) &&
                   intendedId != CatchAllAction;
        }

        private bool ShouldShift(int openerIndex, int size, uint usedValue)
        {
            var nextIntended = actions.GetActionName(Loaded[openerIndex]);
            return openerIndex + 1 < size && (Loaded[openerIndex + 1] == usedValue || actions.SameActionsByName(nextIntended, usedValue));
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
