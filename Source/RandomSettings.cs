using Verse;
using RimWorld;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace RandomPlus
{
    public class RandomSettings
    {
        public static int randomRerollCounter = 0;

        public static List<PawnFilter> pawnFilterList = new List<PawnFilter>();

        private static PawnFilter pawnFilter;
        public static PawnFilter PawnFilter
        {
            get { return pawnFilter; }
            set { pawnFilter = value; }
        }

        public static int RandomRerollCounter()
        {
            return randomRerollCounter;
        }

        public static void SetRandomRerollLimit(int limit)
        {
            pawnFilter.randomRerollLimit = limit;
        }

        public static int RandomRerollLimit()
        {
            return pawnFilter.randomRerollLimit;
        }

        public static void Init()
        {
            PawnFilter = new PawnFilter();
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

            if (PawnFilter.gender != Gender.None && pawn.gender != Gender.None)
                if (PawnFilter.gender != pawn.gender)
                    return false;

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

            // handle total passion range
            if (RandomSettings.PawnFilter.totalPassionRange.min > PawnFilter.TotalPassionMinDefault ||
                RandomSettings.PawnFilter.totalPassionRange.max < PawnFilter.TotalPassionMaxDefault)
            {
                int totalPassions = skillList.Where(skill => skill.passion > 0).Count();
                if (totalPassions < RandomSettings.PawnFilter.totalPassionRange.min ||
                    totalPassions > RandomSettings.PawnFilter.totalPassionRange.max)
                {
                    return false;
                }
            }

            // handle required and exclude traits
            var traitFilterList = PawnFilter.Traits;
            foreach (var traitContainer in traitFilterList)
            {
                bool has = HasTrait(pawn, traitContainer.trait);

                switch (traitContainer.traitFilter)
                {
                    case TraitContainer.TraitFilterType.Required:
                        if (!has)
                            return false;
                        break;
                    case TraitContainer.TraitFilterType.Excluded:
                        if (has)
                            return false;
                        break;
                }
            }

            // handle trait pool (optional)
            if (PawnFilter.RequiredTraitsInPool > 0 && 
                PawnFilter.RequiredTraitsInPool <= PawnFilter.Traits.Count)
            {
                int pawnHasTraitCounter = 0;
                var traitPool = PawnFilter.Traits.Where(t => t.traitFilter == TraitContainer.TraitFilterType.Optional);
                foreach (var traitContainer in traitPool) {
                    if (HasTrait(pawn, traitContainer.trait))
                    {
                        pawnHasTraitCounter++;
                        if (PawnFilter.RequiredTraitsInPool == pawnHasTraitCounter)
                            break;
                    }
                }
                if (pawnHasTraitCounter < PawnFilter.RequiredTraitsInPool)
                    return false;
            }

            switch (PawnFilter.FilterHealthCondition) {
                case PawnFilter.HealthOptions.AllowAll:
                    break;
                case PawnFilter.HealthOptions.OnlyStartCondition:
                    var foundNotStartCondition = pawn.health.hediffSet.hediffs.FirstOrDefault((hediff) => hediff.def.defName != "CryptosleepSickness" && hediff.def.defName != "Malnutrition");
                    if (foundNotStartCondition != null)
                        return false;
                    break;
                case PawnFilter.HealthOptions.NoPain:
                    var foundPain = pawn.health.hediffSet.hediffs.FirstOrDefault((hediff) => hediff.PainOffset > 0f);
                    if (foundPain != null)
                        return false;
                    break;
                case PawnFilter.HealthOptions.AllowNone:
                    if (pawn.health.hediffSet.hediffs.Count > 0)
                        return false;
                    break;
            }

            switch (PawnFilter.FilterIncapable) {
                case PawnFilter.IncapableOptions.AllowAll:
                    break;
                case PawnFilter.IncapableOptions.NoDumbLabor:
                    if ((pawn.story.DisabledWorkTagsBackstoryAndTraits & WorkTags.ManualDumb) == WorkTags.ManualDumb)
                        return false;
                    break;
                case PawnFilter.IncapableOptions.AllowNone:
                    if (pawn.story.DisabledWorkTagsBackstoryAndTraits != WorkTags.None)
                        return false;
                    break;
            }

            if (PawnFilter.AgeRange.min != PawnFilter.MinAgeDefault || 
                PawnFilter.AgeRange.max != PawnFilter.MaxAgeDefault)
            {
                if (PawnFilter.AgeRange.min > pawn.ageTracker.AgeBiologicalYears || 
                    PawnFilter.AgeRange.max < pawn.ageTracker.AgeBiologicalYears)
                    return false;
            }

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

        public static void SetGenderFilter(Gender gender)
        {
            PawnFilter.gender = gender;
        }

        private static float _cacheTraitCommonalityMale;
        private static float _cacheTraitCommonalityFemale;

        private static float GetTotalTraitCommonality(Gender gender)
        {
            if (gender == Gender.Male && _cacheTraitCommonalityMale > 0)
                return _cacheTraitCommonalityMale;
            if (gender == Gender.Female && _cacheTraitCommonalityFemale > 0)
                return _cacheTraitCommonalityFemale;

            float total = 0;
            foreach (var trait in DefDatabase<TraitDef>.AllDefsListForReading)
            {
                total += trait.GetGenderSpecificCommonality(gender);
            }

            if (gender == Gender.Male)
                _cacheTraitCommonalityMale = total;
            if (gender == Gender.Female)
                _cacheTraitCommonalityFemale = total;

            return total;
        }

        public static float GetTraitRollChance(TraitDef traitDef, Gender gender = Gender.Male)
        {
            float total = GetTotalTraitCommonality(gender);
            return traitDef.GetGenderSpecificCommonality(gender) * 100 / total;
        }

        public static string GetTraitRollChanceText(TraitDef traitDef)
        {
            string pecentMale = GetTraitRollChance(traitDef, Gender.Male).ToString("0.0");
            string pecentFemale = GetTraitRollChance(traitDef, Gender.Female).ToString("0.0");

            if (traitDef.GetGenderSpecificCommonality(Gender.Male) == traitDef.GetGenderSpecificCommonality(Gender.Female))
                return $"({pecentMale}%)";
            return $"(♂:{pecentMale}%,♀:{pecentFemale}%)";
        }
    }
}
