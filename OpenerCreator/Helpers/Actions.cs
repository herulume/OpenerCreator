using System.Collections.Generic;
using System.Linq;
using Dalamud.Interface.Internal;
using Lumina.Excel.GeneratedSheets;
using LuminaAction = Lumina.Excel.GeneratedSheets.Action;


namespace OpenerCreator.Helpers
{
    public class Actions
    {
        private static Actions? instance;
        private static readonly object LockObject = new();
        private readonly Dictionary<uint, LuminaAction> actionsSheet;
        private readonly IEnumerable<LuminaAction> nonRepeatedActions;
        public List<string> Jobs { get; init; }


        private Actions()
        {
            var pve = OpenerCreator.DataManager.GetExcelSheet<LuminaAction>()!
                .Where(IsPvEAction);
            actionsSheet = pve.ToDictionary(a => a.RowId);
            nonRepeatedActions = pve
                .DistinctBy(a => a.Name.ToString()); // ToString needed since SeStrings are different

            var pveJobs = new List<string>() {
                "PLD",
                "WAR",
                "DRK",
                "GNB",
                "WHM",
                "SCH",
                "AST",
                "SGE",
                "MNK",
                "DRG",
                "NIN",
                "SAM",
                "RPR",
                "BRD",
                "MCH",
                "DNC",
                "BLM",
                "SMN",
                "RDM",
                "BLU"
            };

            Jobs = OpenerCreator.DataManager.GetExcelSheet<ClassJob>()!
                .Select(a => a.Abbreviation.ToString())
                .Where(a => pveJobs.Contains(a))
                .ToList();
            Jobs.Add("Any");
        }

        public static Actions Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (LockObject)
                    {
                        instance ??= new Actions();
                    }
                }
                return instance;
            }
        }

        public List<uint> NonRepeatedIdList() => nonRepeatedActions.Select(a => a.RowId).ToList();

        public string GetActionName(uint id) => actionsSheet[id].Name.ToString();

        public LuminaAction GetAction(uint id) => actionsSheet[id];

        public ushort GetActionIcon(uint id) => actionsSheet[id].Icon;

        public List<uint> GetNonRepeatedActionsByName(string name, string job) => nonRepeatedActions
            .AsParallel()
            .Where(a =>
                a.Name.ToString().ToLower().Contains(name.ToLower())
                && filterByJob(a, job)
            )
            .Select(a => a.RowId)
            .Order()
            .ToList();

        private bool filterByJob(LuminaAction action, string job)
        {
            var belongsToJob = job == "Any";
            switch (job)
            {
                // Tanks
                case "PLD": belongsToJob = action.ClassJobCategory.Value!.PLD; break;
                case "WAR": belongsToJob = action.ClassJobCategory.Value!.WAR; break;
                case "DRK": belongsToJob = action.ClassJobCategory.Value!.DRK; break;
                case "GNB": belongsToJob = action.ClassJobCategory.Value!.GNB; break;

                // Healers
                case "WHM": belongsToJob = action.ClassJobCategory.Value!.WHM; break;
                case "SCH": belongsToJob = action.ClassJobCategory.Value!.SCH; break;
                case "AST": belongsToJob = action.ClassJobCategory.Value!.AST; break;
                case "SGE": belongsToJob = action.ClassJobCategory.Value!.SGE; break;

                // Melee
                case "MNK": belongsToJob = action.ClassJobCategory.Value!.MNK; break;
                case "DRG": belongsToJob = action.ClassJobCategory.Value!.DRG; break;
                case "NIN": belongsToJob = action.ClassJobCategory.Value!.NIN; break;
                case "SAM": belongsToJob = action.ClassJobCategory.Value!.SAM; break;
                case "RPR": belongsToJob = action.ClassJobCategory.Value!.RPR; break;

                // Physical Ranged
                case "BRD": belongsToJob = action.ClassJobCategory.Value!.BRD; break;
                case "MCH": belongsToJob = action.ClassJobCategory.Value!.MCH; break;
                case "DNC": belongsToJob = action.ClassJobCategory.Value!.DNC; break;

                // Magical Ranged
                case "BLM": belongsToJob = action.ClassJobCategory.Value!.BLM; break;
                case "SMN": belongsToJob = action.ClassJobCategory.Value!.SMN; break;
                case "RDM": belongsToJob = action.ClassJobCategory.Value!.RDM; break;
                case "BLU": belongsToJob = action.ClassJobCategory.Value!.BLU; break;

                default: break;
            }
            return belongsToJob;
        }

        public bool SameActions(string name, uint aId) => actionsSheet[aId].Name.ToString().ToLower().Contains(name.ToLower());

        public static bool IsPvEAction(LuminaAction a) => (a.ActionCategory.Row is 2 or 3 or 4) // GCD or Weaponskill or oGCD
                                                                                                // && a.IsPlayerAction this will remove abilities like Paradox and Mudras and pet actions
                        && !a.IsPvP
                        && a.ClassJobLevel > 0 // not an old action
                        && a.ClassJobCategory.Row != 0; // not an old action

        public IDalamudTextureWrap GetTexture(string path)
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
