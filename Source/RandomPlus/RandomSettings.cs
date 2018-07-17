using Verse;
using RimWorld;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace RandomPlus
{
    public class RandomSettings
    {
        public static readonly int[] RANDOM_REROLL_LIMIT_OPTIONS = new int[] { 50, 200, 500, 1000 };

        private static PawnFilter pawnFilter;
        
        private static int randomRerollLimit = 200;
        private static int randomRerollCounter = 0;

        public static PawnFilter PawnFilter
        {
            get
            {
                return pawnFilter;
            }
        }

        public static int RandomRerollCounter()
        {
            return randomRerollCounter;
        }

        public static void SetRandomRerollLimit(int limit)
        {
            randomRerollLimit = limit;
        }

        public static int RandomRerollLimit()
        {
            return randomRerollLimit;
        }

        public static void Init()
        {
            pawnFilter = new PawnFilter();
        }

        public static void ResetRerollCounter()
        {
            randomRerollCounter = 0;
        }

        public static bool CheckPawnIsSatisfied(Pawn pawn)
        {
            if (RandomRerollCounter() >= RandomRerollLimit())
            {
                return true;
            }
            randomRerollCounter++;
            List<SkillRecord> skillList = pawn.skills.skills;

            var skillFilterList = PawnFilter.skillFilterList;
            foreach (var skillFilter in skillFilterList)
            {
                if (skillFilter.passion != Passion.None || 
                    skillFilter.MinValue > 0)
                {
                    var skillRecord = skillList.FirstOrDefault(i => i.def == skillFilter.skillDef);
                    if (skillRecord != null)
                    {
                        if (skillRecord.passion < skillFilter.passion || 
                            skillRecord.Level < skillFilter.MinValue)
                        {
                            return false;
                        }
                    }
                    else
                    {
                        Log.Error("Shouldn't reach here!");
                    }
                }
            }

            var traitFilterList = PawnFilter.Traits;
            foreach (var trait in traitFilterList)
            {
                if (!HasTrait(pawn, trait))
                {
                    return false;
                }
            }

            if (PawnFilter.NoHealthConditions &&
                pawn.health.hediffSet.hediffs.Count > 0)
                return false;

            if (PawnFilter.NoDumbLabor &&
                (pawn.story.CombinedDisabledWorkTags & WorkTags.ManualDumb) == WorkTags.ManualDumb)
                return false;

            if (PawnFilter.AgeRange.min != PawnFilter.MinAgeDefault || 
                PawnFilter.AgeRange.max != PawnFilter.MaxAgeDefault)
            {
                if (PawnFilter.AgeRange.min > pawn.ageTracker.AgeBiologicalYears || 
                    PawnFilter.AgeRange.max < pawn.ageTracker.AgeBiologicalYears)
                    return false;
            }

            //GC.Collect();
            return true;
        }

        public static bool HasTrait(Pawn pawn, Trait trait)
        {
            return pawn.story.traits.allTraits.Find((Trait t) => {
                if (t == null && trait == null)
                {
                    return true;
                }
                else if (trait == null || t == null)
                {
                    return false;
                }
                else if (trait.Label.Equals(t.Label))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }) != null;
        }
    }
}
