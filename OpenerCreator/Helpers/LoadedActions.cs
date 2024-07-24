using System.Collections.Generic;
using Dalamud.Utility;
using OpenerCreator.Actions;

namespace OpenerCreator.Helpers;

internal class LoadedActions
{
    private readonly HashSet<int> wrongActionsIndex = [];
    private List<int> actions = []; // int instead of uint until c# has tagged unions
    public string Name = "";        // needs to be public for ImGui refs


    internal bool IsWrongActionAt(int i)
    {
        return wrongActionsIndex.Contains(i);
    }

    internal void AddWrongActionAt(int i)
    {
        wrongActionsIndex.Add(i);
    }

    internal void ClearWrongActions()
    {
        wrongActionsIndex.Clear();
    }

    internal int ActionsCount()
    {
        return actions.Count;
    }

    internal int GetActionAt(int i)
    {
        return actions[i];
    }

    internal void AddAction(int action)
    {
        actions.Add(action);
    }

    internal void RemoveActionAt(int i)
    {
        actions.RemoveAt(i);
    }

    internal void InsertActionAt(int i, int action)
    {
        actions.Insert(i, action);
    }

    internal List<int> GetActionsByRef()
    {
        return actions;
    }

    internal void ClearActions()
    {
        actions.Clear();
    }

    internal void AddActionsByRef(List<int> l)
    {
        actions = l;
    }

    internal bool HasName()
    {
        return !Name.IsNullOrEmpty();
    }

    public bool HasTrueNorth()
    {
        return actions.Contains((int)PvEActions.TrueNorthId);
    }
}
