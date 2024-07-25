using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Interface.Textures;
using OpenerCreator.Helpers;
using LuminaAction = Lumina.Excel.GeneratedSheets.Action;

namespace OpenerCreator.Actions;

public class PvEActions : IActionManager
{
    private static PvEActions? SingletonInstance;
    private static readonly object LockObject = new();
    private readonly IEnumerable<LuminaAction> actionsSheet;
    private readonly Dictionary<uint, LuminaAction> actionsSheetDictionary;

    private PvEActions()
    {
        var pve = OpenerCreator.DataManager.GetExcelSheet<LuminaAction>()!
                               .Where(IsPvEAction)
                               .ToList();
        actionsSheetDictionary = pve.ToDictionary(a => a.RowId);
        actionsSheet = pve;
    }

    public static uint TrueNorthId => 7546;

    public static PvEActions Instance
    {
        get
        {
            if (SingletonInstance == null)
            {
                lock (LockObject)
                {
                    SingletonInstance ??= new PvEActions();
                }
            }

            return SingletonInstance;
        }
    }

    public bool IsActionOGCD(int id)
    {
        return id >= 0
            ? (id == IActionManager.CatchAllActionId
                ? false
                : actionsSheetDictionary.TryGetValue((uint)id, out var action)
                    && ActionTypesExtension.GetType(action) == ActionTypes.OGCD)
            : GroupOfActions.TryGetDefault(id, out var group)
                && !group.IsGCD;
    }

    public string GetActionName(int id)
    {
        return id >= 0
            ? (id == IActionManager.CatchAllActionId
                ? IActionManager.CatchAllActionName
                : actionsSheetDictionary.GetValueOrDefault((uint)id)?.Name.ToString() ?? IActionManager.OldActionName)
            : (GroupOfActions.TryGetDefault(id, out var group)
                ? group.Name
                : IActionManager.OldActionName);
    }

    public bool SameActionsByName(string name, int aId)
    {
        return GetActionName(aId).Contains(name, StringComparison.CurrentCultureIgnoreCase);
    }

    public List<int> ActionsIdList(ActionTypes actionType)
    {
        return actionsSheet
               .Where(a => ActionTypesExtension.GetType(a) == actionType || actionType == ActionTypes.ANY)
               .Select(a => (int)a.RowId)
               .ToList();
    }

    public LuminaAction GetAction(uint id)
    {
        return actionsSheetDictionary[id];
    }

    public ushort? GetActionIcon(uint id)
    {
        return id == IActionManager.CatchAllActionId
                   ? IActionManager.GetCatchAllIcon
                   : actionsSheetDictionary.GetValueOrDefault(id)?.Icon;
    }


    public List<int> GetNonRepeatedActionsByName(string name, Jobs job, ActionTypes actionType)
    {
        return actionsSheet
               .AsParallel()
               .Where(a =>
                          a.Name.ToString().Contains(name, StringComparison.CurrentCultureIgnoreCase)
                          && (ActionTypesExtension.GetType(a) == actionType || actionType == ActionTypes.ANY)
                          && ((a.ClassJobCategory?.Value?.Name != null
                               && a.ClassJobCategory.Value.Name.ToString().Contains(job.ToString()))
                              || job == Jobs.ANY)
               )
               .Select(a => (int)a.RowId)
               .OrderBy(id => id)
               .ToList();
    }

    public static bool IsPvEAction(LuminaAction a)
    {
        return a.ActionCategory.Row is 2 or 3 or 4          // GCD or Weaponskill or oGCD
               && a is { IsPvP: false, ClassJobLevel: > 0 } // not an old action
               && a.ClassJobCategory.Row != 0;              // not an old action
    }

    public static ISharedImmediateTexture GetIconTexture(uint id)
    {
        var icon = Instance.GetActionIcon(id)?.ToString("D6");
        if (icon != null)
        {
            var path = $"ui/icon/{icon[0]}{icon[1]}{icon[2]}000/{icon}_hr1.tex";
            return OpenerCreator.TextureProvider.GetFromGame(path);
        }

        return IActionManager.GetUnknownActionTexture;
    }
}
