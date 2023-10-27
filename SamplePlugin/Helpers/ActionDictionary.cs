using System.Collections.Generic;
using System.Linq;
using Dalamud.Plugin.Services;
using LuminaAction = Lumina.Excel.GeneratedSheets.Action;

namespace SamplePlugin.Helpers
{
    public class ActionDictionary
    {
        private readonly IReadOnlyDictionary<string, uint> actions;

        public ActionDictionary(IDataManager dataManager)
        {
            actions = LoadActions(dataManager);
        }

        public uint? GetIdByName(string name)
        {
            if (actions.TryGetValue(name, out var id))
            {
                return id;
            }
            return null;
        }
        private static IReadOnlyDictionary<string, uint> LoadActions(IDataManager dataManager)
        {
            var sheet = dataManager.GetExcelSheet<LuminaAction>();
            if (sheet == null)
            {
                return new Dictionary<string, uint>(); ;
            }

            return sheet
                // 2 gcd
                // 4 ogcd
                .Where(a => (a.ActionCategory.Row == 2 || a.ActionCategory.Row == 4) && a.IsPlayerAction)
                // DoH/DoL have actions with the same name
                // we pick the first and don't bother about it
                // the alternative is to use IDs to specify openers
                // that's cringe
                .GroupBy(a => a.Name.ToString(), a => a.RowId)
                .ToDictionary(a => a.Key, a => a.First());
        }
    }
}
