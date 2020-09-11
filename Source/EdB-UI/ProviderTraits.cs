using RimWorld;
using System.Collections.Generic;
using Verse;

namespace RandomPlus {
    public class ProviderTraits {
        protected List<Trait> traits = new List<Trait>();
        protected List<Trait> sortedTraits = new List<Trait>();

        public List<Trait> Traits {
            get {
                return sortedTraits;
            }
        }
        public ProviderTraits() {
            // Get all trait options.  If a traits has multiple degrees, create a separate trait for each degree.
            foreach (TraitDef def in DefDatabase<TraitDef>.AllDefs) {
                foreach (var trait in def.ToTraits())
                {
                    traits.Add(trait);
                }
            }

            // Create a sorted version of the trait list.
            sortedTraits = new List<Trait>(traits);
            sortedTraits.Sort((t1, t2) => t1.LabelCap.CompareTo(t2.LabelCap));
        }
    }
}
