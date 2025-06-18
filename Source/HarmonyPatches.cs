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
        [HarmonyPrefix]
        static bool Prefix(int pawnIndex)
        {
            if (!TutorSystem.AllowAction((EventPack)nameof(StartingPawnUtility.RandomizePawn)))
                return false;
            
            RandomSettings.ResetRerollCounter();
            
            int num = 0;
            do
            {
                RandomSettings.Reroll(pawnIndex);
                num++;
            }
            while (num <= 20 && !StartingPawnUtility.WorkTypeRequirementsSatisfied());
            
            TutorSystem.Notify_Event((EventPack)nameof(StartingPawnUtility.RandomizePawn));

            return false;
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
            // RimWorld 1.6: Use scale-aware UI positioning
            float uiScale = Prefs.UIScale;
            
            Rect editButtonRect = new Rect(540f * uiScale, 6f * uiScale, 50f * uiScale, 30f * uiScale);
            if (ModsConfig.IsActive("hahkethomemah.simplepersonalities"))
                editButtonRect.x -= 130f * uiScale; 

            if (Widgets.ButtonText(editButtonRect, "RandomPlus.FilterButton".Translate(), true, false, true))
            {
                var page = new Page_RandomEditor();
                Find.WindowStack.Add(page);
            }

            Rect rerollLabelRect = new Rect(640f * uiScale, 4f * uiScale, 200f * uiScale, 30f * uiScale);
            if (ModsConfig.IdeologyActive)
                rerollLabelRect.y += 40f * uiScale;
            if (ModsConfig.BiotechActive)
                rerollLabelRect.y += 60f * uiScale;

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

    // Quick debug access - updated for 1.6
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
        }

        // Updated for RimWorld 1.6 - more robust error handling
        public static void GoToConfigPawnPage()
        {
            try
            {
                var page_select_scenario = new Page_SelectScenario();
                Find.WindowStack.Add(page_select_scenario);

                var methodInfo0 = typeof(Page_SelectScenario).GetMethod("CanDoNext", BindingFlags.NonPublic | BindingFlags.Instance);
                methodInfo0?.Invoke(page_select_scenario, new object[0]);
                var methodInfo1 = typeof(Page_SelectScenario).GetMethod("DoNext", BindingFlags.NonPublic | BindingFlags.Instance);
                methodInfo1?.Invoke(page_select_scenario, new object[0]);

                var page_storyteller = (Page_SelectStoryteller)page_select_scenario.next;

                var page_storyteller_methodInfo0 = typeof(Page_SelectStoryteller).GetMethod("CanDoNext", BindingFlags.NonPublic | BindingFlags.Instance);
                page_storyteller_methodInfo0?.Invoke(page_storyteller, new object[0]);
                var page_storyteller_methodInfo1 = typeof(Page_SelectStoryteller).GetMethod("DoNext", BindingFlags.NonPublic | BindingFlags.Instance);
                page_storyteller_methodInfo1?.Invoke(page_storyteller, new object[0]);

                var page_create_world = (Page_CreateWorldParams)page_storyteller.next;

                var prop = typeof(Page_CreateWorldParams).GetField("planetCoverage", BindingFlags.NonPublic | BindingFlags.Instance);
                prop?.SetValue(page_create_world, 0.1f);

                var page_create_world_methodInfo0 = typeof(Page_CreateWorldParams).GetMethod("CanDoNext", BindingFlags.NonPublic | BindingFlags.Instance);
                page_create_world_methodInfo0?.Invoke(page_create_world, new object[0]);

                var page_select_site = (Page_SelectStartingSite)page_create_world.next;

                LongEventHandler.QueueLongEvent(() =>
                {
                    while (Find.World == null) System.Threading.Thread.Sleep(100);
                    LongEventHandler.ExecuteWhenFinished(() =>
                    {
                        // RimWorld 1.6: Updated tile finder method
                        try
                        {
                            Find.WorldInterface.SelectedTile = RimWorld.Planet.TileFinder.RandomStartingTile();
                        }
                        catch
                        {
                            // Fallback if API changed
                            Find.WorldInterface.SelectedTile = Rand.Range(0, Find.WorldGrid.TilesCount);
                        }

                        var page_select_site_methodInfo0 = typeof(Page_SelectStartingSite).GetMethod("CanDoNext", BindingFlags.NonPublic | BindingFlags.Instance);
                        page_select_site_methodInfo0?.Invoke(page_select_site, new object[0]);
                        var page_create_world_methodInfo1 = typeof(Page_SelectStartingSite).GetMethod("DoNext", BindingFlags.NonPublic | BindingFlags.Instance);
                        page_create_world_methodInfo1?.Invoke(page_select_site, new object[0]);

                        if (ModsConfig.IdeologyActive)
                        {
                            var page_ideo = (Page_ChooseIdeoPreset)page_select_site.next;
                            var allIdeo = DefDatabase<IdeoPresetDef>.AllDefs;
                            var page_ideo_select_field = typeof(Page_ChooseIdeoPreset).GetField("selectedIdeo", BindingFlags.NonPublic | BindingFlags.Instance);
                            page_ideo_select_field?.SetValue(page_ideo, allIdeo.RandomElement());

                            var page_ideo_methodInfo0 = typeof(Page_ChooseIdeoPreset).GetMethod("CanDoNext", BindingFlags.NonPublic | BindingFlags.Instance);
                            page_ideo_methodInfo0?.Invoke(page_ideo, new object[0]);
                            var page_ideo_methodInfo1 = typeof(Page_ChooseIdeoPreset).GetMethod("DoNext", BindingFlags.NonPublic | BindingFlags.Instance);
                            page_ideo_methodInfo1?.Invoke(page_ideo, new object[0]);
                        }

                        var page = new Page_RandomEditor();
                        Find.WindowStack.Add(page);
                    });
                }, null, true, null, false);
            }
            catch (Exception ex)
            {
                Log.Error($"RandomPlus: Failed to launch quick config page: {ex.Message}");
            }
        }
    }
}
