using System.Collections.Generic;
using System.Linq;
using LuminaAction = Lumina.Excel.GeneratedSheets.Action;


namespace OpenerCreator.Helpers
{
    public class ActionDictionary
    {
        private static ActionDictionary? instance;
        private static readonly object LockObject = new();
        private readonly Dictionary<uint, LuminaAction> actionsSheet;


        private ActionDictionary()
        {
            actionsSheet = OpenerCreator.DataManager.GetExcelSheet<LuminaAction>()!
                .Where(IsPvEAction)
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

        public static bool IsPvEAction(LuminaAction a) => (a.ActionCategory.Row is 2 or 3 or 4) // GCD or Weaponskill or oGCD
                        && a.IsPlayerAction
                        && !a.IsPvP
                        && a.ClassJobLevel > 0; // not an old action
    }
}
