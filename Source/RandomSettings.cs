using Verse;
using RimWorld;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using Verse.AI;

namespace RandomPlus
{
    public class RandomSettings
    {
        static MethodInfo randomAgeMethodInfo;
        static MethodInfo randomTraitMethodInfo;
        static MethodInfo randomSkillMethodInfo;
        static MethodInfo randomHealthMethodInfo;
        static MethodInfo randomBodyTypeMethodInfo;
        static MethodInfo randomGeneMethodInfo;

        static PropertyInfo startingAndOptionalPawnsPropertyInfo;

        public static int MinSkillRange;

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

        public static void Init()
        {
            pawnFilter = new PawnFilter();

            randomAgeMethodInfo = typeof(PawnGenerator)
                .GetMethod("GenerateRandomAge", BindingFlags.NonPublic | BindingFlags.Static);

            randomTraitMethodInfo = typeof(PawnGenerator)
                .GetMethod("GenerateTraits", BindingFlags.NonPublic | BindingFlags.Static);

            randomSkillMethodInfo = typeof(PawnGenerator)
                .GetMethod("GenerateSkills", BindingFlags.NonPublic | BindingFlags.Static);

            randomHealthMethodInfo = typeof(PawnGenerator)
                .GetMethod("GenerateInitialHediffs", BindingFlags.NonPublic | BindingFlags.Static);

            randomBodyTypeMethodInfo = typeof(PawnGenerator)
                .GetMethod("GenerateBodyType", BindingFlags.NonPublic | BindingFlags.Static);

            randomGeneMethodInfo = typeof(PawnGenerator)
                .GetMethod("GenerateGenes", BindingFlags.NonPublic | BindingFlags.Static);

            startingAndOptionalPawnsPropertyInfo = typeof(StartingPawnUtility)
                .GetProperty("StartingAndOptionalPawns", BindingFlags.NonPublic | BindingFlags.Static);
        }

        public static void ResetRerollCounter()
        {
            randomRerollCounter = 0;
        }

        public static void Reroll(int pawnIndex)
        {
            List<Pawn> pawnList = (List<Pawn>)startingAndOptionalPawnsPropertyInfo.GetValue(null);
            Pawn pawn = pawnList[pawnIndex];

            SpouseRelationUtility.Notify_PawnRegenerated(pawn);
            pawn = StartingPawnUtility.RandomizeInPlace(pawn);

            randomRerollCounter++;

            if (CheckPawnIsSatisfied(pawn))
                return;

            if (PawnFilter.RerollAlgorithm == PawnFilter.RerollAlgorithmOptions.Normal ||
                Find.WindowStack.currentlyDrawnWindow is Dialog_ChooseNewWanderers)
            {
                while (true)
                {
                    if (CheckPawnIsSatisfied(pawn))
                        break;

                    SpouseRelationUtility.Notify_PawnRegenerated(pawn);
                    pawn = StartingPawnUtility.RandomizeInPlace(pawn);

                    randomRerollCounter++;
                }
                return;
            }

            int index = StartingPawnUtility.PawnIndex(pawn);
            PawnGenerationRequest request = StartingPawnUtility.GetGenerationRequest(index);
            request.ValidateAndFix();

            Faction faction1;
            Faction faction2 = request.Faction == null ? (!Find.FactionManager.TryGetRandomNonColonyHumanlikeFaction(out faction1, false, true) ? Faction.OfAncients : faction1) : request.Faction;
            XenotypeDef xenotype = ModsConfig.BiotechActive ? PawnGenerator.GetXenotypeForGeneratedPawn(request) : null;

            while (randomRerollCounter < PawnFilter.RerollLimit)
            {
                try
                {
                    randomRerollCounter++;

                    PawnGenerator.RedressPawn(pawn, request);

                    pawn.ageTracker = new Pawn_AgeTracker(pawn);
                    randomAgeMethodInfo.Invoke(null, new object[] { pawn, request });
                    if (!CheckAgeIsSatisfied(pawn))
                        continue;

                    pawn.story.traits = new TraitSet(pawn);
                    pawn.skills = new Pawn_SkillTracker(pawn);

                    PawnBioAndNameGenerator.GiveAppropriateBioAndNameTo(pawn, faction2.def, request, xenotype);
                    randomTraitMethodInfo.Invoke(null, new object[] { pawn, request });
                    randomSkillMethodInfo.Invoke(null, new object[] { pawn, request });
                    if (!CheckSkillsIsSatisfied(pawn) || !CheckTraitsIsSatisfied(pawn))
                        continue;

                    for (int i = 0; i < 100; i++)
                    {
                        pawn.health.Reset();
                        try
                        {
                            // internally, this method only adds custom Scenario health (as of rimworld v1.3)
                            Find.Scenario.Notify_NewPawnGenerating(pawn, request.Context);
                            randomHealthMethodInfo.Invoke(null, new object[] { pawn, request });
                            if (!(pawn.Dead || pawn.Destroyed || pawn.Downed))
                            {
                                //pawn.health.Reset();
                                continue;
                            }
                        }
                        catch (Exception)
                        {
                            //SpouseRelationUtility.Notify_PawnRegenerated(pawn);
                            //pawn = StartingPawnUtility.RandomizeInPlace(pawn);
                            //Find.WorldPawns.RemoveAndDiscardPawnViaGC(pawn);
                            continue;
                        }
                    }
                    if (!CheckHealthIsSatisfied(pawn))
                        continue;

                    pawn.workSettings.EnableAndInitialize();
                    if (!CheckWorkIsSatisfied(pawn))
                        continue;

                    // Handle custom scenario e.g forced traits
                    Find.Scenario.Notify_PawnGenerated(pawn, request.Context, true);
                    if (!CheckPawnIsSatisfied(pawn))
                        continue;

                    // Generate Misc
                    if (ModsConfig.BiotechActive)
                    {
                        pawn.genes = new Pawn_GeneTracker(pawn);
                        randomGeneMethodInfo.Invoke(null, new object[] { pawn, xenotype, request });
                    }
                    randomBodyTypeMethodInfo.Invoke(null, new object[] { pawn, request });
                    GeneratePawnStyle(pawn);

                    return;
                }
                catch (Exception ex)
                {
                    Find.WorldPawns.RemoveAndDiscardPawnViaGC(pawn);
                    SpouseRelationUtility.Notify_PawnRegenerated(pawn);
                    pawn = StartingPawnUtility.RandomizeInPlace(pawn);

                    //Log.Error("Error while generating pawn. Rethrowing. Exception: \n" + (object)ex);
                    //return;
                    //throw;
                }

            }
        }

        public static bool CheckPawnIsSatisfied(Pawn pawn)
        {
            if (RandomRerollCounter() >= PawnFilter.RerollLimit)
            {
                return true;
            }
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

            // handle total skill range
            if (pawnFilter.skillRange.min != PawnFilter.SkillMinDefault ||
                pawnFilter.skillRange.max != PawnFilter.SkillMaxDefault)
            {
                int skillTotalCounter = 0;
                for (int i=0; i<skillList.Count;i++)
                {
                    var skill = skillList[i];
                    if (PawnFilter.countOnlyHighestAttack)
                    {
                        if (i == 0) // Shooting[i=0] Melee[i=1]
                        {
                            var meleeSkill = skillList[1];
                            skillTotalCounter += meleeSkill.Level > skill.Level ? meleeSkill.Level : skill.Level;
                            i = 1; // skip next loop (melee)
                            continue;
                        }
                    }
                    if (PawnFilter.countOnlyPassion)
                    {
                        if(skill.passion > 0)
                            skillTotalCounter += skill.Level;
                    }
                    else
                    {
                        skillTotalCounter += skill.Level;
                    }
                }
                
                if (pawnFilter.skillRange.min > skillTotalCounter ||
                pawnFilter.skillRange.max < skillTotalCounter)
                    return false;
            }

            return true;
        }

        public static bool CheckTraitsIsSatisfied(Pawn pawn)
        {
            if (Page_RandomEditor.MOD_WELL_MET)
                return true;

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

        private static bool IsGeneAffectedHealth(Hediff hediff)
        {
            if (!ModsConfig.BiotechActive)
                return false;

            if (hediff is Hediff_ChemicalDependency chemicalDependency && chemicalDependency.LinkedGene != null)
                return true;

            return false;
        }

        public static bool CheckHealthIsSatisfied(Pawn pawn)
        {

            // handle health options
            switch (pawnFilter.FilterHealthCondition)
            {
                case PawnFilter.HealthOptions.AllowAll:
                    break;
                case PawnFilter.HealthOptions.OnlyStartCondition:
                    var foundNotStartCondition = 
                        pawn.health.hediffSet.hediffs
                        .FirstOrDefault((hediff) => hediff.def.defName != "CryptosleepSickness" && hediff.def.defName != "Malnutrition" && !IsGeneAffectedHealth(hediff));
                    if (foundNotStartCondition != null)
                        return false;
                    break;
                case PawnFilter.HealthOptions.NoPain:
                    var foundPain = pawn.health.hediffSet.hediffs.FirstOrDefault((hediff) => hediff.PainOffset > 0f && !IsGeneAffectedHealth(hediff));
                    if (foundPain != null)
                        return false;
                    break;
                case PawnFilter.HealthOptions.NoAddiction:
                    var foundAddiction = pawn.health.hediffSet.hediffs.FirstOrDefault((hediff) => hediff is Hediff_Addiction && !IsGeneAffectedHealth(hediff));
                    if (foundAddiction != null)
                        return false;
                    break;
                case PawnFilter.HealthOptions.AllowNone:
                    if (ModsConfig.BiotechActive)
                    {
                        if (pawn.health.hediffSet.hediffs.Where(i => !IsGeneAffectedHealth(i)).Count() > 0)
                            return false;
                    }
                    else
                    {
                        if (pawn.health.hediffSet.hediffs.Count > 0)
                            return false;
                    }
                    
                    
                    break;
//                case PawnFilter.HealthOptions.OnlyPositiveImplants:
//                    var hediffs = pawn.health.hediffSet.hediffs;
//                    Hediff onlyPositiveImplants = null;
//                    for (int i = 0; i < hediffs.Count; i++)
//                    {
//                        var hediff = hediffs[i];
//                        if (hediff is Hediff_Implant)
//                        {
//                            onlyPositiveImplants = hediff;
//                            break;
//                        }
//;                    }
//                    if (onlyPositiveImplants == null)
//                        return false;
//                    break;
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

        public static void GeneratePawnStyle(Pawn pawn)
        {
            if (pawn.RaceProps.Humanlike)
            {
                pawn.story.hairDef = PawnStyleItemChooser.RandomHairFor(pawn);
                if (pawn.style != null)
                {
                pawn.style.beardDef = pawn.gender == Gender.Male? PawnStyleItemChooser.RandomBeardFor(pawn) : BeardDefOf.NoBeard;
                if (ModsConfig.IdeologyActive)
                {
                    pawn.style.FaceTattoo = PawnStyleItemChooser.RandomTattooFor(pawn, TattooType.Face);
                    pawn.style.BodyTattoo = PawnStyleItemChooser.RandomTattooFor(pawn, TattooType.Body);
                }
                else
                    pawn.style.SetupTattoos_NoIdeology();
                }
            }
        }
    }
}
