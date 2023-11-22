using System.Collections.Generic;
using System.Linq;
using Dalamud.Interface.Internal;
using LuminaAction = Lumina.Excel.GeneratedSheets.Action;


namespace OpenerCreator.Helpers
{
    public class Actions
    {
        private static Actions? SingletonInstance;
        private static readonly object LockObject = new();
        private readonly Dictionary<uint, LuminaAction> actionsSheet;
        private readonly IEnumerable<LuminaAction> nonRepeatedActions;

        private Actions()
        {
            var pve = OpenerCreator.DataManager.GetExcelSheet<LuminaAction>()!
                .Where(IsPvEAction);
            actionsSheet = pve.ToDictionary(a => a.RowId);
            nonRepeatedActions = pve
                .DistinctBy(a => a.Name.ToString()); // ToString needed since SeStrings are different
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

        public List<uint> NonRepeatedIdList() => nonRepeatedActions.Select(a => a.RowId).Where(id => id != 0).ToList();

        public string GetActionName(uint id) => actionsSheet[id].Name.ToString();

        public LuminaAction GetAction(uint id) => actionsSheet[id];

        public ushort GetActionIcon(uint id) => actionsSheet[id].Icon;

        public List<uint> GetNonRepeatedActionsByName(string name, Jobs job) => nonRepeatedActions
            .AsParallel()
            .Where(a =>
                a.Name.ToString().ToLower().Contains(name.ToLower())
                && (a.ClassJobCategory.Value!.Name.ToString().Contains(job.ToString()) || job == Jobs.ANY)
            )
            .Select(a => a.RowId)
            .Order()
            .ToList();

        public bool SameActionsByName(string name, uint aId) => actionsSheet[aId].Name.ToString().ToLower().Contains(name.ToLower());

        public static bool IsPvEAction(LuminaAction a) =>
            a.RowId == 0 || // 0 is used as an catch-all action
            ((a.ActionCategory.Row is 2 or 3 or 4) // GCD or Weaponskill or oGCD
                && !a.IsPvP
                && a.ClassJobLevel > 0 // not an old action
                && a.ClassJobCategory.Row != 0); // not an old action

        public static IDalamudTextureWrap GetTexture(string path)
        {
            var data = OpenerCreator.DataManager.GetFile<Lumina.Data.Files.TexFile>(path)!;
            var pixels = new byte[data.Header.Width * data.Header.Height * 4];
            for (var i = 0; i < data.Header.Width * data.Header.Height; i++)
            {
                pixels[i * 4 + 0] = data.ImageData[i * 4 + 2];
                pixels[i * 4 + 1] = data.ImageData[i * 4 + 1];
                pixels[i * 4 + 2] = data.ImageData[i * 4 + 0];
                pixels[i * 4 + 3] = data.ImageData[i * 4 + 3];
            }
            return OpenerCreator.PluginInterface.UiBuilder.LoadImageRaw(pixels, data.Header.Width, data.Header.Height, 4);
        }

        public IDalamudTextureWrap GetIconTexture(uint id)
        {
            var icon = Actions.Instance.GetActionIcon(id).ToString("D6");
            var path = $"ui/icon/{icon[0]}{icon[1]}{icon[2]}000/{icon}_hr1.tex";
            return GetTexture(path);
        }
    }
}
