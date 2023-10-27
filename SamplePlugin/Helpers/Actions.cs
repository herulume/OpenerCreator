using System.Collections.Generic;
using System.Linq;
using Dalamud.Plugin.Services;
using LuminaAction = Lumina.Excel.GeneratedSheets.Action;

namespace SamplePlugin.Helpers
{

    public readonly struct ActionUiData
    {
        public readonly string Name;
        public readonly uint Id;
        public readonly uint IconId;

        public ActionUiData(string name, uint id, uint iconId)
        {
            Name = name;
            Id = id;
            IconId = iconId;
        }
    }

    public class Actions
    {
        public List<ActionUiData> ActionList { get; init; }

        public Actions(IDataManager dataManager)
        {
            var sheet = dataManager.GetExcelSheet<LuminaAction>();
            if (sheet == null) { this.ActionList = new List<ActionUiData>(); }
            else
            {
                this.ActionList = sheet
                        .Where(a => (a.ActionCategory.Row == 2 || a.ActionCategory.Row == 4) && a.IsPlayerAction)
                        .Select(a => new ActionUiData(a.Name, a.RowId, a.Icon))
                        .ToList();
            }
        }
    }
}
