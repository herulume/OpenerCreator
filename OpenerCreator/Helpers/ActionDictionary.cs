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
        private readonly IEnumerable<LuminaAction> nonRepeatedActions;

        private ActionDictionary()
        {
            var pve = OpenerCreator.DataManager.GetExcelSheet<LuminaAction>()!
                .Where(IsPvEAction);
            actionsSheet = pve.ToDictionary(a => a.RowId);
            nonRepeatedActions = pve
                .DistinctBy(a => a.Name.ToString()); // ToString needed since SeStrings are different
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

        public List<uint> NonRepeatedIdList() => nonRepeatedActions.Select(a => a.RowId).ToList();

        public string GetActionName(uint id) => actionsSheet[id].Name.ToString();

        public LuminaAction GetAction(uint id) => actionsSheet[id];

        public ushort GetActionIcon(uint id) => actionsSheet[id].Icon;

        public List<uint> GetNonRepeatedActionsByName(string name) => nonRepeatedActions
            .Where(a => a.Name.ToString().ToLower().Contains(name.ToLower()))
            .Select(a => a.RowId)
            .ToList();

        public bool SameActions(string name, uint aId) => actionsSheet[aId].Name.ToString().ToLower().Contains(name.ToLower());

        public static bool IsPvEAction(LuminaAction a) => (a.ActionCategory.Row is 2 or 3 or 4) // GCD or Weaponskill or oGCD
                                                                                                // && a.IsPlayerAction this will remove abilities like Paradox and Mudras and pet actions
                        && !a.IsPvP
                        && a.ClassJobLevel > 0 // not an old action
                        && a.ClassJobCategory.Row != 0; // not an old action
    }
}