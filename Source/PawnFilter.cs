using Verse;
using RimWorld;
using System.Collections.Generic;
using System.Linq;

namespace RandomPlus
{
    public class PawnFilter : IExposable
    {
        public static readonly int TotalPassionMinDefault = 0;
        public static readonly int TotalPassionMaxDefault = DefDatabase<SkillDef>.AllDefs.ToArray().Length;

        public static readonly int MinAgeDefault = 0;
        public static readonly int MaxAgeDefault = 120;

        public static readonly int DefaultPoolSize = 0;

        public enum RerollLimitOptions { N100 = 100, N250 = 250, N500 = 500, N1000 = 1000, N2500 = 2500, N5000 = 5000, N10000 = 10000, N50000 = 50000 }
        public readonly static string[] RerollLimitOptionValues = new string[] { "100", "250", "500", "1000", "2500", "5000", "10000", "50000" };
        public static readonly RerollLimitOptions DefaultRerollLimit = RerollLimitOptions.N1000;

        public enum HealthOptions { AllowAll, OnlyStartCondition, NoPain, AllowNone }
        public readonly static string[] HealthOptionValues = new string[] {
            "RandomPlus.PanelOthers.HealthOptions.AllowAll",
            "RandomPlus.PanelOthers.HealthOptions.OnlyStartConditions",
            "RandomPlus.PanelOthers.HealthOptions.NoPain",
            "RandomPlus.PanelOthers.HealthOptions.AllowNone"
        };

        public enum IncapableOptions { AllowAll, NoDumbLabor, AllowNone }
        public readonly static string[] IncapableOptionValues = new string[] {
            "RandomPlus.PanelOthers.IncapableOptions.AllowAll",
            "RandomPlus.PanelOthers.IncapableOptions.NoDumbLabor",
            "RandomPlus.PanelOthers.IncapableOptions.AllowNone"
        };

        public string name;

        public List<SkillContainer> skillFilterList = new List<SkillContainer>();
        public List<TraitContainer> Traits = new List<TraitContainer>();

        public int RequiredTraitsInPool = DefaultPoolSize;

        public IntRange totalPassionRange;
        public IntRange AgeRange;
        public Gender gender;
        
        public int randomRerollLimit = (int)DefaultRerollLimit;
        public HealthOptions FilterHealthCondition = HealthOptions.AllowAll;
        public IncapableOptions FilterIncapable = IncapableOptions.AllowAll;

        public PawnFilter()
        {
            ResetAll();
        }

        public void ResetSkills()
        {
            skillFilterList.Clear();
            foreach (var skilldef in DefDatabase<SkillDef>.AllDefs)
            {
                skillFilterList.Add(new SkillContainer(skilldef));
            }
        }

        public void ResetTraits()
        {
            Traits.Clear();
        }

        public void ResetAll()
        {
            gender = Gender.None;
            ResetSkills();
            ResetTraits();
            totalPassionRange = new IntRange(TotalPassionMinDefault, TotalPassionMaxDefault);
            AgeRange = new IntRange(MinAgeDefault, MaxAgeDefault);
        }

        public void ExposeData()
        {
            int version = 1;
            Scribe_Values.Look(ref this.name, "name", "");
            Scribe_Values.Look(ref version, "version", 1);
            Scribe_Collections.Look(ref this.skillFilterList, "skills", LookMode.Deep, null);
            Scribe_Collections.Look(ref this.Traits, "traits", LookMode.Deep, null);

            Scribe_Values.Look(ref RequiredTraitsInPool, "poolSize", DefaultPoolSize);

            Scribe_Values.Look(ref totalPassionRange.min, "passionRangeMin", TotalPassionMinDefault);
            Scribe_Values.Look(ref totalPassionRange.max, "passionRangeMax", TotalPassionMaxDefault);

            Scribe_Values.Look(ref AgeRange.min, "ageRangeMin", MinAgeDefault);
            Scribe_Values.Look(ref AgeRange.max, "ageRangeMax", MaxAgeDefault);

            Scribe_Values.Look(ref randomRerollLimit, "rerollLimit", (int)DefaultRerollLimit);
            Scribe_Values.Look(ref gender, "gender", Gender.None);
            Scribe_Values.Look(ref FilterHealthCondition, "healthCondition", HealthOptions.AllowAll);
            Scribe_Values.Look(ref FilterIncapable, "incapable", IncapableOptions.AllowAll);

            switch (Scribe.mode)
            {
                case LoadSaveMode.Saving:
                    
                    break;
                case LoadSaveMode.LoadingVars:
                    
                    break;
            }
        }
    }
}
