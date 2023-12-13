using System.Collections.Generic;
using System.Linq;
using Dalamud.Interface.Internal;
using Lumina.Data.Files;
using LuminaAction = Lumina.Excel.GeneratedSheets.Action;

namespace OpenerCreator.Helpers;

public interface IActionManager
{
    string GetActionName(uint action);
    bool SameActionsByName(string action1, uint action2);
}

public class Actions : IActionManager
{
    private static Actions? SingletonInstance;
    private static readonly object LockObject = new();
    private readonly IEnumerable<LuminaAction> actionsSheet;
    private readonly Dictionary<uint, LuminaAction> actionsSheetDictionary;

    private Actions()
    {
        var pve = OpenerCreator.DataManager.GetExcelSheet<LuminaAction>()!
                               .Where(IsPvEAction).ToList();
        actionsSheetDictionary = pve.ToDictionary(a => a.RowId);
        actionsSheet = pve;
    }

    public static Actions Instance
    {
        get
        {
            if (SingletonInstance == null)
            {
                lock (LockObject)
                {
                    SingletonInstance ??= new Actions();
                }
            }

            return SingletonInstance;
        }
    }

    public string GetActionName(uint id)
    {
        return actionsSheetDictionary[id].Name.ToString();
    }

    public bool SameActionsByName(string name, uint aId)
    {
        return actionsSheetDictionary[aId].Name.ToString().ToLower().Contains(name.ToLower());
    }

    public List<uint> NonRepeatedIdList()
    {
        return actionsSheet.Select(a => a.RowId).Where(id => id != 0).ToList();
    }

    public LuminaAction GetAction(uint id)
    {
        return actionsSheetDictionary[id];
    }

    public ushort GetActionIcon(uint id)
    {
        return actionsSheetDictionary[id].Icon;
    }

    public List<uint> GetNonRepeatedActionsByName(string name, Jobs job)
    {
        return actionsSheet
               .AsParallel()
               .Where(a =>
                          a.Name.ToString().ToLower().Contains(name.ToLower())
                          && (a.ClassJobCategory.Value!.Name.ToString().Contains(job.ToString()) || job == Jobs.ANY)
               )
               .Select(a => a.RowId)
               .Order()
               .ToList();
    }

    public static bool IsPvEAction(LuminaAction a)
    {
        return a.RowId == 0 ||                               // 0 is used as an catch-all action
               (a.ActionCategory.Row is 2 or 3 or 4          // GCD or Weaponskill or oGCD
                && a is { IsPvP: false, ClassJobLevel: > 0 } // not an old action
                && a.ClassJobCategory.Row != 0               // not an old action
               );
    }

    public static IDalamudTextureWrap GetTexture(string path)
    {
        var data = OpenerCreator.DataManager.GetFile<TexFile>(path)!;
        var pixels = new byte[data.Header.Width * data.Header.Height * 4];
        for (var i = 0; i < data.Header.Width * data.Header.Height; i++)
        {
            pixels[(i * 4) + 0] = data.ImageData[(i * 4) + 2];
            pixels[(i * 4) + 1] = data.ImageData[(i * 4) + 1];
            pixels[(i * 4) + 2] = data.ImageData[(i * 4) + 0];
            pixels[(i * 4) + 3] = data.ImageData[(i * 4) + 3];
        }

        return OpenerCreator.PluginInterface.UiBuilder.LoadImageRaw(pixels, data.Header.Width, data.Header.Height, 4);
    }

    public static IDalamudTextureWrap GetIconTexture(uint id)
    {
        var icon = Instance.GetActionIcon(id).ToString("D6");
        var path = $"ui/icon/{icon[0]}{icon[1]}{icon[2]}000/{icon}_hr1.tex";
        return GetTexture(path);
    }
}
