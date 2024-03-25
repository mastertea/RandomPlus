using Verse;
using RimWorld;
using System.Collections.Generic;
using System.Linq;

namespace RandomPlus
{
    public class PawnFilter : IExposable
    {
        public static readonly int PassionMinDefault = 0;
        public static readonly int PassionMaxDefault = DefDatabase<SkillDef>.AllDefs.ToArray().Length;

        public static readonly int SkillMinDefault = 0;
        public static readonly int SkillMaxDefault = DefDatabase<SkillDef>.AllDefs.ToArray().Length * 8;

        public static readonly int MinAgeDefault = 0;
        public static readonly int MaxAgeDefault = 120;

        public static readonly int DefaultPoolSize = 0;
        public enum RerollAlgorithmOptions { Normal, Fast }
        public readonly static string[] _RerollAlgorithmOptionValues = new string[] {
            "RandomPlus.PanelOthers.RerollAlgorithmOptionValues.Normal",
            "RandomPlus.PanelOthers.RerollAlgorithmOptionValues.Fast", 
        };
        public static string[] RerollAlgorithmOptionValues { 
            get {
                if (ModsConfig.IsActive("erdelf.HumanoidAlienRaces"))
                {
                    return new string[] { "RandomPlus.PanelOthers.RerollAlgorithmOptionValues.Normal" };
                }
                return _RerollAlgorithmOptionValues;
            } 
        }
        public static RerollAlgorithmOptions DefaultRerollAlgorithm
        {
            get {
                if (ModsConfig.IsActive("erdelf.HumanoidAlienRaces"))
                    return RerollAlgorithmOptions.Normal;
                return RerollAlgorithmOptions.Normal;
            }
        } 

        public enum RerollLimitOptions { N100 = 100, N250 = 250, N500 = 500, N1000 = 1000, N2500 = 2500, N5000 = 5000, N10000 = 10000, N50000 = 50000 }
        public readonly static string[] RerollLimitOptionValues = new string[] { "100", "250", "500", "1000", "2500", "5000", "10000", "50000" };
        public static readonly RerollLimitOptions DefaultRerollLimit = RerollLimitOptions.N1000;

        public enum HealthOptions { AllowAll, OnlyStartCondition, NoPain, NoAddiction, AllowNone, 
            //OnlyPositiveImplants, 
        }
        public readonly static string[] HealthOptionValues = new string[] {
            "RandomPlus.PanelOthers.HealthOptions.AllowAll",
            "RandomPlus.PanelOthers.HealthOptions.OnlyStartConditions",
            "RandomPlus.PanelOthers.HealthOptions.NoPain",
            "RandomPlus.PanelOthers.HealthOptions.NoAddiction",
            "RandomPlus.PanelOthers.HealthOptions.AllowNone",
            //"RandomPlus.PanelOthers.HealthOptions.OnlyPositiveImplants",
        };

        public enum IncapableOptions { AllowAll, NoDumbLabor, AllowNone }
        public readonly static string[] IncapableOptionValues = new string[] {
            "RandomPlus.PanelOthers.IncapableOptions.AllowAll",
            "RandomPlus.PanelOthers.IncapableOptions.NoDumbLabor",
            "RandomPlus.PanelOthers.IncapableOptions.AllowNone"
        };

        public string name;

        private List<SkillContainer> skills = new List<SkillContainer>();
        public IEnumerable<SkillContainer> Skills
        {
            get
            {
                foreach (var skill in skills)
                    yield return skill;
            }
        }

        #region Traits
        private List<TraitContainer> traits = new List<TraitContainer>();
        public IEnumerable<TraitContainer> Traits
        {
            get
            {
                foreach (var trait in traits)
                    yield return trait;
            }
        }

        public void AddTrait(Trait trait)
        {
            traits.Add(new TraitContainer(trait, OnChange));
            OnChange();
        }

        public void TraitUpdated(int index, Trait trait)
        {
            traits[index].trait = trait;
            OnChange();
        }

        public void TraitRemoved(Trait trait)
        {
            var needToRemoveTC = traits.FirstOrDefault(tc => tc.trait == trait);
            traits.Remove(needToRemoveTC);
            OnChange();
        }
        #endregion

        private int _RequiredTraitsInPool = DefaultPoolSize;
        public int RequiredTraitsInPool { get => _RequiredTraitsInPool; set => _RequiredTraitsInPool = value; }

        public IntRange passionRange;
        public IntRange skillRange;
        public bool countOnlyHighestAttack;
        public bool countOnlyPassion;

        public IntRange ageRange;

        private Gender gender;
        public Gender Gender
        {
            get => gender;
            set
            {
                gender = value;
                OnChange();
            }
        }

        private RerollAlgorithmOptions rerollAlgorithm = DefaultRerollAlgorithm;
        public RerollAlgorithmOptions RerollAlgorithm
        {
            get => rerollAlgorithm;
            set
            {
                rerollAlgorithm = value;
                OnChange();
            }
        }

        private int rerollLimit = (int)DefaultRerollLimit;
        public int RerollLimit
        {
            get => rerollLimit;
            set
            {
                rerollLimit = value;
                OnChange();
            }
        }

        private HealthOptions filterHealthCondition;
        public HealthOptions FilterHealthCondition
        {
            get => filterHealthCondition;
            set
            {
                filterHealthCondition = value;
                OnChange();
            }
        }

        private IncapableOptions filterIncapable;
        public IncapableOptions FilterIncapable
        {
            get => filterIncapable;
            set
            {
                filterIncapable = value;
                OnChange();
            }
        }

        public PawnFilter()
        {
            ResetAll();
        }

        public void ResetSkills()
        {
            skills.Clear();
            foreach (var skilldef in DefDatabase<SkillDef>.AllDefs)
            {
                skills.Add(new SkillContainer(skilldef, OnChange));
            }
            passionRange = new IntRange(PassionMinDefault, PassionMaxDefault);
            skillRange = new IntRange(SkillMinDefault, SkillMaxDefault);
            countOnlyHighestAttack = false;
            countOnlyPassion = false;
            OnChange();
        }

        public void ResetTraits()
        {
            traits.Clear();
            RequiredTraitsInPool = DefaultPoolSize;
            OnChange();
        }

        public void ResetOther()
        {
            RerollAlgorithm = DefaultRerollAlgorithm;
            gender = Gender.None;
            rerollLimit = (int)DefaultRerollLimit;
            filterHealthCondition = HealthOptions.AllowAll;
            filterIncapable = IncapableOptions.AllowAll;

            ageRange = new IntRange(MinAgeDefault, MaxAgeDefault);
            OnChange();
        }

        public void ResetAll()
        {
            ResetSkills();
            ResetTraits();
            ResetOther();
        }

        public void OnChange()
        {

        }

        public void ExposeData()
        {
            int version = 1;
            Scribe_Values.Look(ref this.name, "name", "");
            Scribe_Values.Look(ref version, "version", 1);
            Scribe_Collections.Look(ref this.skills, "skills", LookMode.Deep, null);
            Scribe_Collections.Look(ref this.traits, "traits", LookMode.Deep, null);

            Scribe_Values.Look(ref _RequiredTraitsInPool, "poolSize", DefaultPoolSize);

            Scribe_Values.Look(ref passionRange.min, "passionRangeMin", PassionMinDefault);
            Scribe_Values.Look(ref passionRange.max, "passionRangeMax", PassionMaxDefault);

            Scribe_Values.Look(ref skillRange.min, "skillRangeMin", SkillMinDefault);
            Scribe_Values.Look(ref skillRange.max, "skillRangeMax", SkillMaxDefault);

            Scribe_Values.Look(ref countOnlyHighestAttack, "countOnlyHighestAttack", false);
            Scribe_Values.Look(ref countOnlyPassion, "countOnlyPassion", false);

            Scribe_Values.Look(ref ageRange.min, "ageRangeMin", MinAgeDefault);
            Scribe_Values.Look(ref ageRange.max, "ageRangeMax", MaxAgeDefault);

            Scribe_Values.Look(ref rerollAlgorithm, "rerollAlgorithm", DefaultRerollAlgorithm);
            Scribe_Values.Look(ref rerollLimit, "rerollLimit", (int)DefaultRerollLimit);
            Scribe_Values.Look(ref gender, "gender", Gender.None);
            Scribe_Values.Look(ref filterHealthCondition, "healthCondition", HealthOptions.AllowAll);
            Scribe_Values.Look(ref filterIncapable, "incapable", IncapableOptions.AllowAll);

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
