using System.Collections.Generic;
using System.Linq;
using LuminaAction = Lumina.Excel.GeneratedSheets.Action;


namespace SamplePlugin.Helpers
{
    public class ActionDictionary
    {
        private static ActionDictionary? instance;
        private static readonly object LockObject = new();
        private readonly Dictionary<uint, LuminaAction> actionsSheet;


        private ActionDictionary()
        {
            actionsSheet = Plugin.DataManager.GetExcelSheet<LuminaAction>()!
                .Where(a =>
                        (a.ActionCategory.Row == 2 || a.ActionCategory.Row == 4) // GCD or oGCD
                        && a.IsPlayerAction
                        && !a.IsPvP
                        && a.ClassJobLevel > 0 // not an old action
                    )
            .ToDictionary(a => a.RowId);
        }

        public static ActionDictionary Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (LockObject)
                    {
                        instance ??= new ActionDictionary();
                    }
                }
                return instance;
            }
        }

        public List<uint> ToIdList() => actionsSheet.Select(a => a.Key).ToList();

        public string GetActionName(uint id) => actionsSheet[id].Name.ToString();

        public LuminaAction GetAction(uint id) => actionsSheet[id];

        public ushort GetActionIcon(uint id) => actionsSheet[id].Icon;

        public List<uint> GetActionsByName(string name) => actionsSheet
                    .Where(a => a.Value.Name.ToString().ToLower().Contains(name.ToLower()))
                    .Select(a => a.Key)
                    .ToList();
    }
}
