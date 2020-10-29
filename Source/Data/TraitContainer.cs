using Verse;
using RimWorld;
using System;

namespace RandomPlus
{
    public class TraitContainer : IExposable
    {
        public Trait trait;

        private string traitDefName;
        private int traitDegree;

        public enum TraitFilterType : byte
        {
            Required = 0,
            Optional = 1,
            Excluded = 2
        }

        public TraitFilterType traitFilter;

        private Action OnChange;

        public TraitContainer()
        {
            
        }

        public TraitContainer(Trait trait, Action OnChange)
        {
            this.trait = trait;
            this.OnChange = OnChange;
        }

        public void ExposeData()
        {
            switch(Scribe.mode)
            {
                case LoadSaveMode.Saving:
                    traitDefName = trait.def.defName;
                    traitDegree = trait.Degree;
                    Scribe_Values.Look(ref traitDefName, "traitDef", null, false);
                    Scribe_Values.Look(ref traitDegree, "traitDegree", 0, false);
                    break;
                case LoadSaveMode.LoadingVars:
                    Scribe_Values.Look(ref traitDefName, "traitDef", null, false);
                    Scribe_Values.Look(ref traitDegree, "traitDegree", 0, false);
                    trait = new Trait(DefDatabase<TraitDef>.GetNamed(traitDefName), traitDegree, true);
                    break;
            }
            
            Scribe_Values.Look(ref traitFilter, "filterType", TraitFilterType.Required, false);
        }
    }
}
