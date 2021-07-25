using HarmonyLib;
using RimWorld;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using Verse;

namespace RandomPlus
{
    [StaticConstructorOnStartup]
    class HarmonyPatches
    {
        static HarmonyPatches()
        {
            var harmony = new Harmony("RandomPlus");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
    }

    [HarmonyPatch(typeof(Page_ConfigureStartingPawns), "PreOpen")]
    class Patch_InitRandomSettings
    {
        [HarmonyPostfix]
        static void Postfix()
        {
            RandomSettings.Init();
            RandomSettings.ResetRerollCounter();
        }
    }

    [HarmonyPatch(typeof(Page_ConfigureStartingPawns), "RandomizeCurPawn")]
    class Patch_RandomizeMethod
    {
        static FieldInfo curPawnFieldInfo;
        static MethodInfo randomAgeMethodInfo;
        static MethodInfo randomTraitMethodInfo;
        static MethodInfo randomSkillMethodInfo;
        static MethodInfo randomHealthMethodInfo;

        static bool rerolling;

        [HarmonyPrefix]
        static bool Prefix()
        {
            if (rerolling)
                return false;

            rerolling = true;
            RandomSettings.ResetRerollCounter();
            return true;
        }

        [HarmonyPostfix]
        static void Postfix()
        {
            rerolling = false;
        }

        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            int startIndex = -1;

            // load current pawn
            FieldInfo curPawnFieldInfo = typeof(Page_ConfigureStartingPawns)
                .GetField("curPawn", BindingFlags.NonPublic | BindingFlags.Instance);
            // remove pawn relationship
            MethodInfo notify_PawnRegeneratedMethodInfo = typeof(SpouseRelationUtility)
                .GetMethod("Notify_PawnRegenerated", BindingFlags.Public | BindingFlags.Static);

            var codes = new List<CodeInstruction>(instructions);

            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Ldarg_0 &&
                    codes[i + 1].opcode == OpCodes.Ldfld &&
                    codes[i + 1].operand == (object)curPawnFieldInfo &&
                    codes[i + 2].opcode == OpCodes.Call &&
                    codes[i + 2].operand == (object)notify_PawnRegeneratedMethodInfo)
                {
                    startIndex = i;
                    break;
                }
            }

            if (startIndex != -1)
            {
                var randomLimitRerollMethodInfo = typeof(RandomSettings)
                    .GetMethod("RandomRerollLimit", BindingFlags.Public | BindingFlags.Static);

                var CheckPawnIsSatisfiedMethodInfo = typeof(RandomSettings)
                    .GetMethod("CheckPawnIsSatisfied", BindingFlags.Public | BindingFlags.Static);

                var RerollMethodInfo = typeof(Patch_RandomizeMethod)
                    .GetMethod("Reroll", BindingFlags.Public | BindingFlags.Static);

                var startLoopLocation = codes[startIndex + 16].operand;

                var newCode = new List<CodeInstruction>();
                newCode.Add(new CodeInstruction(OpCodes.Nop));
                newCode.Add(new CodeInstruction(OpCodes.Ldarg_0));
                newCode.Add(new CodeInstruction(OpCodes.Ldfld, curPawnFieldInfo));
                newCode.Add(new CodeInstruction(OpCodes.Call, RerollMethodInfo));
                newCode.Add(new CodeInstruction(OpCodes.Brfalse, startLoopLocation));
                codes.InsertRange(startIndex + 8, newCode);
            }

            return codes;
        }

        public static bool Reroll(Pawn pawn)
        {
            if (RandomSettings.PawnFilter.RerollAlgorithm == PawnFilter.RerollAlgorithmOptions.Normal
                || RandomSettings.randomRerollCounter == 0)
            {
                return RandomSettings.CheckPawnIsSatisfied(pawn);
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

            if(randomSkillMethodInfo == null)
                randomSkillMethodInfo = typeof(PawnGenerator)
                    .GetMethod("GenerateSkills", BindingFlags.NonPublic | BindingFlags.Static);

            if(randomHealthMethodInfo == null)
                randomHealthMethodInfo = typeof(PawnGenerator)
                    .GetMethod("GenerateInitialHediffs", BindingFlags.NonPublic | BindingFlags.Static);

            PawnGenerationRequest request = new PawnGenerationRequest(
                    Faction.OfPlayer.def.basicMemberKind,
                    Faction.OfPlayer,
                    PawnGenerationContext.PlayerStarter,
                    forceGenerateNewPawn: true,
                    mustBeCapableOfViolence: TutorSystem.TutorialMode,
                    colonistRelationChanceFactor: 20f);

            if (!RandomSettings.CheckGenderIsSatisfied(pawn))
                return false;

            while (RandomSettings.randomRerollCounter <= RandomSettings.PawnFilter.RerollLimit)
            {
                pawn.ageTracker = new Pawn_AgeTracker(pawn);
                pawn.story.traits = new TraitSet(pawn);
                pawn.skills = new Pawn_SkillTracker(pawn);

                randomAgeMethodInfo.Invoke(null, new object[] { pawn, request });
                PawnBioAndNameGenerator.GiveAppropriateBioAndNameTo(pawn, pawn.story.birthLastName, request.Faction.def, request.ForceNoBackstory);
                randomTraitMethodInfo.Invoke(null, new object[] { pawn, request });
                randomSkillMethodInfo.Invoke(null, new object[] { pawn });

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

                pawn.workSettings.EnableAndInitialize();

                //RandomSettings.randomRerollCounter++;

                if (RandomSettings.CheckPawnIsSatisfied(pawn))
                    return true;
            }
            return false;
        }
    }

    // working progress
    //[HarmonyPatch(typeof(Page_ConfigureStartingPawns), "DrawPortraitArea")]
    class Patch_MenuAlignment
    {
        [HarmonyTranspiler]
        [HarmonyDebug]
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            int startIndex = -1;

            var codes = new List<CodeInstruction>(instructions);

            for (int i = 0; i < codes.Count - 1; i++)
            {
                if (codes[i].opcode == OpCodes.Ldloca_S &&
                    codes[i + 1].opcode == OpCodes.Ldarga_S &&
                    codes[i + 2].opcode == OpCodes.Call &&
                    codes[i + 3].opcode == OpCodes.Ldloc_S)
                {
                    startIndex = i;
                    break;
                }
            }

            if (startIndex != -1)
            {
                var modifyRectMethodInfo = typeof(Patch_MenuAlignment)
                        .GetMethod("ModifyRect", BindingFlags.Public | BindingFlags.Static);

                //Log.Message(codes[startIndex + 8].operand.ToString());

                codes[startIndex + 8].operand = 100;

                //var rect1 = codes[startIndex].operand;

                //Log.Message(rect1.GetType().ToString());

                //var newCode = new List<CodeInstruction>();

                //newCode.Add(new CodeInstruction(OpCodes.Ldloca_S, rect1));
                //newCode.Add(new CodeInstruction(OpCodes.Call, modifyRectMethodInfo));

                //codes.InsertRange(startIndex + 14, newCode);
                //codes[startIndex + 14] = new CodeInstruction(OpCodes.Call, modifyRectMethodInfo);
            }

            return codes;
        }

        public static Rect ModifyRect()
        {
            return new Rect(0, 0, 100, 100);
        }
    }

    [HarmonyPatch(typeof(CharacterCardUtility), "DrawCharacterCard")]
    class Patch_RandomEditButton
    {
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            int startIndex = -1;

            var codes = new List<CodeInstruction>(instructions);
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Ldarg_2 &&
                    codes[i + 1].opcode == OpCodes.Brfalse_S)
                {
                    startIndex = i;
                    break;
                }
            }

            if (startIndex != -1)
            {
                var methodInfo = typeof(Patch_RandomEditButton)
                    .GetMethod("InjectCustomUI", BindingFlags.Public | BindingFlags.Static);
                codes.Insert(startIndex + 2, new CodeInstruction(OpCodes.Call, methodInfo));
            }

            return codes;
        }

        public static void InjectCustomUI()
        {
            Rect editButtonRect = new Rect(620f, 0.0f, 50f, 30f);
            if (ModsConfig.IsActive("hahkethomemah.simplepersonalities"))
                editButtonRect.x -= 130; 

            if (Widgets.ButtonText(editButtonRect, "RandomPlus.FilterButton".Translate(), true, false, true))
            {
                var page = new Page_RandomEditor();
                Find.WindowStack.Add(page);
            }

            Rect rerollLabelRect = new Rect(620f, 40f, 200f, 30f);
            if (ModsConfig.IdeologyActive)
                rerollLabelRect.y += 40;

            string labelText = "RandomPlus.RerollLabel".Translate() + RandomSettings.RandomRerollCounter() + "/" + RandomSettings.PawnFilter.RerollLimit;

            var tmpSave = GUI.color;
            if (RandomSettings.RandomRerollCounter() >= RandomSettings.PawnFilter.RerollLimit)
                GUI.color = Color.red;
            Widgets.Label(rerollLabelRect, labelText);
            GUI.color = tmpSave;
        }
    }

    //For testing only
    [HarmonyPatch(typeof(MainMenuDrawer), "DoMainMenuControls")]
    class Patch_DoMainMenuControls
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            int startIndex = -1;

            var codes = new List<CodeInstruction>(instructions);
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Newobj &&
                    codes[i + 1].opcode == OpCodes.Stloc_2)
                {
                    startIndex = i;
                    break;
                }

            }

            if (startIndex != -1)
            {
                var newCode = new List<CodeInstruction>();
                newCode.Add(new CodeInstruction(OpCodes.Ldloc_2));
                var methodInfo = typeof(Patch_DoMainMenuControls)
                    .GetMethod("AddQuickGoToConfigPawnPage", BindingFlags.Public | BindingFlags.Static);
                newCode.Add(new CodeInstruction(OpCodes.Call, methodInfo));
                codes.InsertRange(startIndex + 2, newCode);
            }

            return codes;
        }

        public static void AddQuickGoToConfigPawnPage(List<ListableOption> optList)
        {
            if (Event.current.type == EventType.KeyDown) {
                KeyBindingDef quickKey = DefDatabase<KeyBindingDef>.GetNamed("Dev_QuickGoToConfigPawnPage");
                if (quickKey.JustPressed)
                {
                    Patch_DoMainMenuControls.GoToConfigPawnPage();
                }
            }
            
            //optList.Add(new ListableOption((string)"RandomPlus.Debug.QuickStartButton".Translate(), () => {
            //    GoToConfigPawnPage();
            //}, (string)null));
        }

        // Macro function go automaticially go straight to pawn select page
        public static void GoToConfigPawnPage()
        {
            var page_select_scenario = new Page_SelectScenario();
            Find.WindowStack.Add(page_select_scenario);

            var methodInfo0 = typeof(Page_SelectScenario).GetMethod("CanDoNext", BindingFlags.NonPublic | BindingFlags.Instance);
            methodInfo0.Invoke(page_select_scenario, new object[0]);
            var methodInfo1 = typeof(Page_SelectScenario).GetMethod("DoNext", BindingFlags.NonPublic | BindingFlags.Instance);
            methodInfo1.Invoke(page_select_scenario, new object[0]);

            var page_storyteller = (Page_SelectStoryteller)page_select_scenario.next;
            
            var page_storyteller_methodInfo0 = typeof(Page_SelectStoryteller).GetMethod("CanDoNext", BindingFlags.NonPublic | BindingFlags.Instance);
            page_storyteller_methodInfo0.Invoke(page_storyteller, new object[0]);
            var page_storyteller_methodInfo1 = typeof(Page_SelectStoryteller).GetMethod("DoNext", BindingFlags.NonPublic | BindingFlags.Instance);
            page_storyteller_methodInfo1.Invoke(page_storyteller, new object[0]);

            var page_create_world = (Page_CreateWorldParams)page_storyteller.next;

            var prop = typeof(Page_CreateWorldParams).GetField("planetCoverage", BindingFlags.NonPublic | BindingFlags.Instance);
            prop.SetValue(page_create_world, 0.1f);

            var page_create_world_methodInfo0 = typeof(Page_CreateWorldParams).GetMethod("CanDoNext", BindingFlags.NonPublic | BindingFlags.Instance);
            page_create_world_methodInfo0.Invoke(page_create_world, new object[0]);

            var page_select_site = (Page_SelectStartingSite)page_create_world.next;

            LongEventHandler.QueueLongEvent(() => {
                while (Find.World == null) System.Threading.Thread.Sleep(100);
                LongEventHandler.ExecuteWhenFinished(() =>
                {
                    Find.WorldInterface.SelectedTile = RimWorld.Planet.TileFinder.RandomStartingTile();

                    var page_select_site_methodInfo0 = typeof(Page_SelectStartingSite).GetMethod("CanDoNext", BindingFlags.NonPublic | BindingFlags.Instance);
                    page_select_site_methodInfo0.Invoke(page_select_site, new object[0]);
                    var page_create_world_methodInfo1 = typeof(Page_SelectStartingSite).GetMethod("DoNext", BindingFlags.NonPublic | BindingFlags.Instance);
                    page_create_world_methodInfo1.Invoke(page_select_site, new object[0]);


                    if (ModsConfig.IdeologyActive)
                    {
                        var page_ideo = (Page_ConfigureIdeo)page_select_site.next;
                        page_ideo.SelectOrMakeNewIdeo(Find.IdeoManager.IdeosInViewOrder.RandomElement());

                        var page_ideo_methodInfo0 = typeof(Page_ConfigureIdeo).GetMethod("CanDoNext", BindingFlags.NonPublic | BindingFlags.Instance);
                        page_ideo_methodInfo0.Invoke(page_ideo, new object[0]);
                        var page_ideo_methodInfo1 = typeof(Page_ConfigureIdeo).GetMethod("DoNext", BindingFlags.NonPublic | BindingFlags.Instance);
                        page_ideo_methodInfo1.Invoke(page_ideo, new object[0]);
                    }
                });
            }, "wait", true, null, false);

            
        }
    }
}