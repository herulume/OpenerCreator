using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using OpenerCreator.Actions;
using OpenerCreator.Helpers;

namespace OpenerCreator.Managers;

public class OpenerManager(IActionManager actions)
{
    private static OpenerManager? SingletonInstance;
    private static readonly object LockObject = new();
    private readonly Dictionary<Jobs, Dictionary<string, List<int>>> defaultOpeners = new();
    private readonly Dictionary<Jobs, Dictionary<string, List<int>>> openers = new();

    private OpenerManager(IActionManager actions, ValueTuple _) : this(actions)
    {
        OpenersFile = Path.Combine(OpenerCreator.PluginInterface.ConfigDirectory.FullName, "openers.json");
        openers = LoadOpeners(OpenersFile);
        defaultOpeners = LoadOpeners(Path.Combine(OpenerCreator.PluginInterface.AssemblyLocation.Directory!.FullName,
                                                  "openers.json"));
    }

    public List<int> Loaded { get; set; } = [];
    private string OpenersFile { get; init; } = "empty";

    public static OpenerManager Instance
    {
        get
        {
            if (SingletonInstance == null)
            {
                lock (LockObject)
                {
                    SingletonInstance ??= new OpenerManager(PvEActions.Instance, new ValueTuple());
                }
            }

            return SingletonInstance;
        }
    }

    public void AddOpener(string name, Jobs job, IEnumerable<int> opener)
    {
        if (!openers.TryGetValue(job, out var value))
        {
            value = new Dictionary<string, List<int>>();
            openers[job] = value;
        }

        value[name] = [..opener];
    }

    public List<Tuple<Jobs, List<string>>> GetDefaultNames()
    {
        return defaultOpeners.Select(x => Tuple.Create(x.Key, x.Value.Keys.ToList())).ToList();
    }

    public List<int> GetDefaultOpener(string name, Jobs job)
    {
        return [..defaultOpeners[job][name]];
    }

    public List<int> GetOpener(string name, Jobs job)
    {
        return [..openers[job][name]];
    }

    public List<Tuple<Jobs, List<string>>> GetNames()
    {
        return openers.Select(x => Tuple.Create(x.Key, x.Value.Keys.ToList())).ToList();
    }

    public void DeleteOpener(string name, Jobs job)
    {
        if (openers.TryGetValue(job, out var value))
        {
            value.Remove(name);
            if (value.Count == 0) openers.Remove(job);
        }
    }

    public void Compare(List<int> used, Action<Feedback> provideFeedback, Action<int> wrongAction)
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

            if (!AreActionsEqual(used, openerIndex, i, out var intended, out var actual))
            {
                error = true;
                feedback.AddMessage(Feedback.MessageType.Error,
                                    $"Difference in action {i + 1}: Substituted {intended} for {actions.GetActionName((uint)actual)}");
                wrongAction(openerIndex);

                if (ShouldShift(openerIndex, size, used[i])) shift++;
            }
        }

        if (!error && shift == 0)
            feedback.AddMessage(Feedback.MessageType.Success, "Great job! Opener executed perfectly.");

        if (shift != 0)
        {
            feedback.AddMessage(Feedback.MessageType.Info,
                                $"You shifted your opener by {shift} {(shift == 1 ? "action" : "actions")}.");
        }

        provideFeedback(feedback);
    }

    private bool AreActionsEqual(
        IReadOnlyList<int> used, int openerIndex, int usedIndex, out string intended, out int actualId)
    {
        var intendedId = Loaded[openerIndex];
        intended = actions.GetActionName((uint)intendedId);
        actualId = used[usedIndex];

        return AreActionsEqual(intendedId, intended, actualId);
    }

    public bool AreActionsEqual(int intendedId, string intendedName, int actualId)
    {
        if (intendedId < 0)
            return GroupOfActions.DefaultGroups().First(g => g.HasId(intendedId)).IsMember((uint)actualId);
        return intendedId == actualId ||
               intendedId == IActionManager.CatchAllActionId ||
               actions.SameActionsByName(intendedName, (uint)actualId);
    }

    private bool ShouldShift(int openerIndex, int size, int usedValue)
    {
        var nextIntended = actions.GetActionName((uint)Loaded[openerIndex]);
        return openerIndex + 1 < size &&
               (Loaded[openerIndex + 1] == usedValue || actions.SameActionsByName(nextIntended, (uint)usedValue));
    }

    private static Dictionary<Jobs, Dictionary<string, List<int>>> LoadOpeners(string path)
    {
        try
        {
            var jsonData = File.ReadAllText(path);
            return JsonSerializer.Deserialize<Dictionary<Jobs, Dictionary<string, List<int>>>>(jsonData)!;
        }
        catch (Exception e)
        {
            OpenerCreator.PluginLog.Error("Failed to load Openers", e);
            return new Dictionary<Jobs, Dictionary<string, List<int>>>();
        }
    }

    public void SaveOpeners()
    {
        try
        {
            var jsonData = JsonSerializer.Serialize(openers);
            File.WriteAllText(OpenersFile, jsonData);
        }
        catch (Exception e)
        {
            OpenerCreator.PluginLog.Error("Failed to save Openers", e);
        }
    }
}
