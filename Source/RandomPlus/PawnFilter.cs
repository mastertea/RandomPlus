using Verse;
using RimWorld;
using System.Collections.Generic;

namespace RandomPlus
{
    public class SkillContainer
    {
        public SkillDef skillDef;

        public Passion passion;

        private int minValue;
        public int MinValue {
            get { return minValue; }
            set {
                if (value > 20) minValue = 20;
                else if (value < 0) minValue = 0;
                else minValue = value;
            }
        }

        public SkillContainer(SkillDef skillDef)
        {
            this.skillDef = skillDef;
            passion = Passion.None;
            MinValue = 0;
        }
    }

    public class TraitContainer
    {
        public Trait trait;

        public enum TraitFilterType : byte
        {
            Required = 0,
            Optional = 1,
            Excluded = 2
        }

        public TraitFilterType traitFilter;

        public TraitContainer(Trait trait)
        {
            this.trait = trait;
        }

        static public bool operator ==(TraitContainer a, TraitContainer b)
        {
            bool aNull = a is null;
            bool bNull = b is null;

            if (aNull && bNull)
                return true;
            else if (!(aNull || bNull) && a.trait == b.trait)
                return true;

            return false;
        }

        static public bool operator !=(TraitContainer a, TraitContainer b)
        {
            return !(a == b);
        }

        public override bool Equals(object obj)
        {
            if (obj is null)
                return false;
            else if (obj is TraitContainer)
                return this.trait == (obj as TraitContainer).trait;

            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return this.trait.GetHashCode();
        }
    }
    

    public class PawnFilter
    {
        public List<SkillContainer> skillFilterList = new List<SkillContainer>();
        public List<TraitContainer> Traits = new List<TraitContainer>();
        public bool NoHealthConditions;
        public bool NoDumbLabor;

        public static readonly int MinAgeDefault = 15;
        public static readonly int MaxAgeDefault = 120;
        public IntRange AgeRange;
        public Gender gender;

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
            AgeRange = new IntRange(MinAgeDefault, MaxAgeDefault);
        }
    }
}
