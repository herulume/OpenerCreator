using System;

namespace OpenerCreator.Helpers
{
    public enum Jobs
    {
        ANY,

        // Tanks
        PLD,
        WAR,
        DRK,
        GNB,

        // Healers
        WHM,
        SCH,
        AST,
        SGE,

        // Melee
        MNK,
        DRG,
        NIN,
        SAM,
        RPR,

        // Physical Ranged
        BRD,
        MCH,
        DNC,

        // Magical Ranged
        BLM,
        SMN,
        RDM,
        BLU
    }

    [Flags]
    public enum JobCategory
    {
        None = 0,
        Tank = 1 << 0,
        Healer = 1 << 1,
        Melee = 1 << 2,
        PhysicalRanged = 1 << 3,
        MagicalRanged = 1 << 4
    }

    public static class JobsExtensions
    {
        public static JobCategory getCategory(this Jobs job)
        {
            return job switch
            {
                Jobs.PLD or Jobs.WAR or Jobs.DRK or Jobs.GNB => JobCategory.Tank,
                Jobs.WHM or Jobs.SCH or Jobs.AST or Jobs.SGE => JobCategory.Healer,
                Jobs.MNK or Jobs.DRG or Jobs.NIN or Jobs.SAM or Jobs.RPR => JobCategory.Melee,
                Jobs.BRD or Jobs.MCH or Jobs.DNC => JobCategory.PhysicalRanged,
                Jobs.BLM or Jobs.SMN or Jobs.RDM or Jobs.BLU => JobCategory.MagicalRanged,
                _ => JobCategory.None,
            };
        }

        public static bool filterBy(JobCategory filter, Jobs job)
        {
            return (filter & getCategory(job)) != 0;
        }

        public static JobCategory toggle(JobCategory filter, JobCategory category)
        {
            if ((filter & category) != 0)
                return filter & ~category; // remove
            else
                return filter | category; // add
        }
    }

}
