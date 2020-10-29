using RimWorld;
using System.Collections.Generic;
using Verse;

namespace RandomPlus {
    public class ProviderTraits {
        private static ProviderTraits _instance;

        private List<Trait> traits = new List<Trait>();
        private List<Trait> sortedTraits = new List<Trait>();

        public static List<Trait> Traits {
            get {
                if (_instance == null)
                {
                    _instance = new ProviderTraits();
                }
                return _instance.sortedTraits;
            }
        }
        private ProviderTraits () {
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
