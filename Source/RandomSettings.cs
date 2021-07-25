using Verse;
using RimWorld;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;

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

        //public static void SetRandomRerollLimit(int limit)
        //{
        //    pawnFilter.RerollLimit = limit;
        //}

        //public static int RandomRerollLimit()
        //{
        //    return pawnFilter.randomRerollLimit;
        //}

        public static void Init()
        {
            pawnFilter = new PawnFilter();
        }

        public static void ResetRerollCounter()
        {
            randomRerollCounter = 0;
        }

        static FieldInfo curPawnFieldInfo;
        static MethodInfo randomAgeMethodInfo;
        static MethodInfo randomTraitMethodInfo;
        static MethodInfo randomSkillMethodInfo;
        static MethodInfo randomHealthMethodInfo;
        public static bool Reroll(Pawn pawn)
        {
            if (PawnFilter.RerollAlgorithm == PawnFilter.RerollAlgorithmOptions.Normal
                || randomRerollCounter == 0)
            {
                return CheckPawnIsSatisfied(pawn);
            }

            if (curPawnFieldInfo == null)
                curPawnFieldInfo = typeof(Page_ConfigureStartingPawns)
                .GetField("curPawn", BindingFlags.NonPublic | BindingFlags.Instance);

            if (randomAgeMethodInfo == null)
                randomAgeMethodInfo = typeof(PawnGenerator)
                    .GetMethod("GenerateRandomAge", BindingFlags.NonPublic | BindingFlags.Static);

            if (randomTraitMethodInfo == null)
                randomTraitMethodInfo = typeof(PawnGenerator)
                    .GetMethod("GenerateTraits", BindingFlags.NonPublic | BindingFlags.Static);

            if (randomSkillMethodInfo == null)
                randomSkillMethodInfo = typeof(PawnGenerator)
                    .GetMethod("GenerateSkills", BindingFlags.NonPublic | BindingFlags.Static);

            if (randomHealthMethodInfo == null)
                randomHealthMethodInfo = typeof(PawnGenerator)
                    .GetMethod("GenerateInitialHediffs", BindingFlags.NonPublic | BindingFlags.Static);

            PawnGenerationRequest request = new PawnGenerationRequest(
                    Faction.OfPlayer.def.basicMemberKind,
                    Faction.OfPlayer,
                    PawnGenerationContext.PlayerStarter,
                    forceGenerateNewPawn: true,
                    mustBeCapableOfViolence: TutorSystem.TutorialMode,
                    colonistRelationChanceFactor: 20f);

            if (!CheckGenderIsSatisfied(pawn))
            {
                randomRerollCounter++;
                return false;
            }

            while (randomRerollCounter < PawnFilter.RerollLimit)
            {
                randomRerollCounter++;

                pawn.ageTracker = new Pawn_AgeTracker(pawn);
                randomAgeMethodInfo.Invoke(null, new object[] { pawn, request });
                if (!CheckAgeIsSatisfied(pawn))
                    continue;

                pawn.story.traits = new TraitSet(pawn);
                pawn.skills = new Pawn_SkillTracker(pawn);
                PawnBioAndNameGenerator.GiveAppropriateBioAndNameTo(pawn, pawn.story.birthLastName, request.Faction.def, request.ForceNoBackstory);
                randomTraitMethodInfo.Invoke(null, new object[] { pawn, request });
                randomSkillMethodInfo.Invoke(null, new object[] { pawn });
                if (!CheckSkillsIsSatisfied(pawn) || !CheckTraitsIsSatisfied(pawn))
                    continue;

                for (int i = 0; i < 100; i++)
                {
                    pawn.health = new Pawn_HealthTracker(pawn);
                    try
                    {
                        randomHealthMethodInfo.Invoke(null, new object[] { pawn, request });
                        if (!(pawn.Dead || pawn.Destroyed || pawn.Downed))
                            break;
                    }
                    catch (Exception)
                    {
                        Find.WorldPawns.RemoveAndDiscardPawnViaGC(pawn);
                        return false;
                    }
                }
                if (!CheckHealthIsSatisfied(pawn))
                    continue;

                pawn.workSettings.EnableAndInitialize();
                if (!CheckWorkIsSatisfied(pawn))
                    continue;

                return true;
            }

            if (RandomRerollCounter() >= PawnFilter.RerollLimit)
            {
                return true;
            }
            return false;
        }

        public static bool CheckPawnIsSatisfied(Pawn pawn)
        {
            if (RandomRerollCounter() >= PawnFilter.RerollLimit)
            {
                return true;
            }
            randomRerollCounter++;

            if (!CheckGenderIsSatisfied(pawn))
                return false;

            if (!CheckSkillsIsSatisfied(pawn))
                return false;

            if (!CheckTraitsIsSatisfied(pawn))
                return false;

            if (!CheckHealthIsSatisfied(pawn))
                return false;

            if (!CheckWorkIsSatisfied(pawn))
                return false;

            if (!CheckAgeIsSatisfied(pawn))
                return false;

            return true;
        }

        public static bool CheckAgeIsSatisfied(Pawn pawn)
        {
            if (pawnFilter.ageRange.min != PawnFilter.MinAgeDefault ||
                pawnFilter.ageRange.max != PawnFilter.MaxAgeDefault)
            {
                if (pawnFilter.ageRange.min > pawn.ageTracker.AgeBiologicalYears ||
                    (pawnFilter.ageRange.max != PawnFilter.MaxAgeDefault && pawnFilter.ageRange.max < pawn.ageTracker.AgeBiologicalYears))
                    return false;
            }
            return true;
        }

        public static bool CheckGenderIsSatisfied(Pawn pawn)
        {
            if (pawnFilter.Gender != Gender.None && pawn.gender != Gender.None)
                if (pawnFilter.Gender != pawn.gender)
                    return false;
            return true;
        }

        public static bool CheckSkillsIsSatisfied(Pawn pawn)
        {
            List<SkillRecord> skillList = pawn.skills.skills;
            foreach (var skillFilter in pawnFilter.Skills)
            {
                if (skillFilter.Passion != Passion.None ||
                    skillFilter.MinValue > 0)
                {
                    var skillRecord = skillList.FirstOrDefault(i => i.def == skillFilter.SkillDef);
                    if (skillRecord != null)
                    {
                        if (skillRecord.passion < skillFilter.Passion ||
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
            if (pawnFilter.passionRange.min > PawnFilter.PassionMinDefault ||
                pawnFilter.passionRange.max < PawnFilter.PassionMaxDefault)
            {
                int totalPassions = skillList.Where(skill => skill.passion > 0).Count();
                if (totalPassions < pawnFilter.passionRange.min ||
                    totalPassions > pawnFilter.passionRange.max)
                {
                    return false;
                }
            }

            return true;
        }

        public static bool CheckTraitsIsSatisfied(Pawn pawn)
        {
            // handle required and exclude traits
            var traitFilterList = pawnFilter.Traits;
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
            if (pawnFilter.RequiredTraitsInPool > 0 &&
                pawnFilter.RequiredTraitsInPool <= pawnFilter.Traits.Count())
            {
                int pawnHasTraitCounter = 0;
                var traitPool = pawnFilter.Traits.Where(t => t.traitFilter == TraitContainer.TraitFilterType.Optional);
                foreach (var traitContainer in traitPool)
                {
                    if (HasTrait(pawn, traitContainer.trait))
                    {
                        pawnHasTraitCounter++;
                        if (pawnFilter.RequiredTraitsInPool == pawnHasTraitCounter)
                            break;
                    }
                }
                if (pawnHasTraitCounter < pawnFilter.RequiredTraitsInPool)
                    return false;
            }

            return true;
        }

        public static bool CheckHealthIsSatisfied(Pawn pawn)
        {
            // handle health options
            switch (pawnFilter.FilterHealthCondition)
            {
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
                case PawnFilter.HealthOptions.NoAddiction:
                    var foundAddiction = pawn.health.hediffSet.hediffs.FirstOrDefault((hediff) => hediff is Hediff_Addiction);
                    if (foundAddiction != null)
                        return false;
                    break;
                case PawnFilter.HealthOptions.AllowNone:
                    if (pawn.health.hediffSet.hediffs.Count > 0)
                        return false;
                    break;
            }
            return true;
        }

        public static bool CheckWorkIsSatisfied(Pawn pawn)
        {
            // handle work options
            switch (pawnFilter.FilterIncapable)
            {
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
            pawnFilter.Gender = gender;
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
