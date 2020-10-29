using Verse;
using RimWorld;
using UnityEngine;
using System.Collections.Generic;

namespace RandomPlus
{
    public static class ExtensionMethods
    {
        public static IEnumerable<Trait> ToTraits(this TraitDef traitDef)
        {
            List<TraitDegreeData> degreeData = traitDef.degreeDatas;
            int count = degreeData.Count;
            if (count > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    Trait trait = new Trait(traitDef, degreeData[i].degree, true);
                    yield return trait;
                }
            }
            else
            {
                yield return new Trait(traitDef, 0, true);
            }
        }

        public static bool ContainsTrait(this IEnumerable<TraitContainer> traits, Trait trait)
        {
            return true;
        }
    }
}
