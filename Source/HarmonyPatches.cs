using HarmonyLib;
using RimWorld;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using Verse;
using System.Collections;

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

    [HarmonyPatch(typeof(StartingPawnUtility), "RandomizePawn")]
    class Patch_RandomizeMethod
    {
        static bool rerolling;

        [HarmonyPrefix]
        static bool Prefix(int pawnIndex)
        {
            //if (rerolling)
            //    return false;

            //rerolling = true;
            RandomSettings.ResetRerollCounter();
            //return true;

            //var currentWindow = Find.WindowStack.currentlyDrawnWindow;
            //Log.Warning(pawnIndex.ToString());
            //Log.Warning(Find.GameInitData.startingAndOptionalPawns.Count.ToString());
            if (!TutorSystem.AllowAction((EventPack)nameof(StartingPawnUtility.RandomizePawn)))
                return false;
            int num = 0;
            do
            {
                RandomSettings.Reroll(pawnIndex);
                num++;
            }
            while (num <= 20 && !StartingPawnUtility.WorkTypeRequirementsSatisfied());
            TutorSystem.Notify_Event((EventPack) nameof(StartingPawnUtility.RandomizePawn));

            return false;
        }

        //[HarmonyPostfix]
        //static void Postfix()
        //{
        //    GC.Collect();
        //    rerolling = false;
        //}

        //[HarmonyTranspiler]
        //static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        //{
        //    int startIndex = -1;

        //    // load current pawn
        //    FieldInfo curPawnFieldInfo = typeof(Page_ConfigureStartingPawns)
        //        .GetField("curPawn", BindingFlags.NonPublic | BindingFlags.Instance);


        //    // remove pawn relationship
        //    MethodInfo notify_PawnRegeneratedMethodInfo = typeof(SpouseRelationUtility)
        //        .GetMethod("Notify_PawnRegenerated", BindingFlags.Public | BindingFlags.Static);

        //    var codes = new List<CodeInstruction>(instructions);

        //    for (int i = 0; i < codes.Count; i++)
        //    {
        //        if (codes[i].opcode == OpCodes.Ldarg_0 &&
        //            codes[i + 1].opcode == OpCodes.Ldfld &&
        //            codes[i + 1].operand == (object)curPawnFieldInfo &&
        //            codes[i + 2].opcode == OpCodes.Call &&
        //            codes[i + 2].operand == (object)notify_PawnRegeneratedMethodInfo)
        //        {
        //            startIndex = i;
        //            break;
        //        }
        //    }

        //    if (startIndex != -1)
        //    {
        //        var RerollMethodInfo = typeof(RandomSettings)
        //            .GetMethod("Reroll", BindingFlags.Public | BindingFlags.Static);

        //        var startLoopLocation = codes[startIndex + 16].operand;

        //        var newCode = new List<CodeInstruction>();
        //        newCode.Add(new CodeInstruction(OpCodes.Nop));
        //        newCode.Add(new CodeInstruction(OpCodes.Ldarg_0));
        //        newCode.Add(new CodeInstruction(OpCodes.Ldfld, curPawnFieldInfo));
        //        newCode.Add(new CodeInstruction(OpCodes.Call, RerollMethodInfo));
        //        newCode.Add(new CodeInstruction(OpCodes.Brfalse, startLoopLocation));
        //        codes.InsertRange(startIndex + 8, newCode);
        //    }

        //    return codes;
        //}
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
                if (codes[i].opcode == OpCodes.Ldloc_1 &&
                    codes[i + 1].opcode == OpCodes.Brfalse)
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
            Rect editButtonRect = new Rect(540f, 6f, 50f, 30f);
            if (ModsConfig.IsActive("hahkethomemah.simplepersonalities"))
                editButtonRect.x -= 130; 

            if (Widgets.ButtonText(editButtonRect, "RandomPlus.FilterButton".Translate(), true, false, true))
            {
                var page = new Page_RandomEditor();
                Find.WindowStack.Add(page);
            }

            Rect rerollLabelRect = new Rect(640f, 4f, 200f, 30f);
            if (ModsConfig.IdeologyActive)
                rerollLabelRect.y += 40;
            if (ModsConfig.BiotechActive)
                rerollLabelRect.y += 60;

            if (RandomSettings.PawnFilter == null)
                RandomSettings.Init();
                
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

            LongEventHandler.QueueLongEvent(() =>
            {
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
                        var page_ideo = (Page_ChooseIdeoPreset)page_select_site.next;
                        var allIdeo = DefDatabase<IdeoPresetDef>.AllDefs;
                        var page_ideo_select_field = typeof(Page_ChooseIdeoPreset).GetField("selectedIdeo", BindingFlags.NonPublic | BindingFlags.Instance);
                        page_ideo_select_field.SetValue(page_ideo, allIdeo.RandomElement());

                        var page_ideo_methodInfo0 = typeof(Page_ChooseIdeoPreset).GetMethod("CanDoNext", BindingFlags.NonPublic | BindingFlags.Instance);
                        page_ideo_methodInfo0.Invoke(page_ideo, new object[0]);
                        var page_ideo_methodInfo1 = typeof(Page_ChooseIdeoPreset).GetMethod("DoNext", BindingFlags.NonPublic | BindingFlags.Instance);
                        page_ideo_methodInfo1.Invoke(page_ideo, new object[0]);
                    }

                    var page = new Page_RandomEditor();
                    Find.WindowStack.Add(page);
                });
            }, null, true, null, false);


        }
    }
}