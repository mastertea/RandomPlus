using HarmonyLib;
using RimWorld;
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
        }
    }

    [HarmonyPatch(typeof(Page_ConfigureStartingPawns), "RandomizeCurPawn")]
    class Patch_RandomizeMethod
    {
        [HarmonyPrefix]
        static void Prefix()
        {
            RandomSettings.ResetRerollCounter();
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
                    codes[i + 1].operand == curPawnFieldInfo &&
                    codes[i + 2].opcode == OpCodes.Call &&
                    codes[i + 2].operand == notify_PawnRegeneratedMethodInfo)
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

                //codes[startIndex + 8] = new CodeInstruction(OpCodes.Call, randomLimitRerollMethodInfo);

                var startLoopLocation = codes[startIndex + 16].operand;

                var newCode = new List<CodeInstruction>();
                newCode.Add(new CodeInstruction(OpCodes.Nop));
                newCode.Add(new CodeInstruction(OpCodes.Ldarg_0));
                newCode.Add(new CodeInstruction(OpCodes.Ldfld, curPawnFieldInfo));
                newCode.Add(new CodeInstruction(OpCodes.Call, CheckPawnIsSatisfiedMethodInfo));
                newCode.Add(new CodeInstruction(OpCodes.Brfalse, startLoopLocation));
                codes.InsertRange(startIndex + 8, newCode);
            }

            return codes;
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
                    .GetMethod("DrawEditButton", BindingFlags.Public | BindingFlags.Static);
                codes.Insert(startIndex + 2, new CodeInstruction(OpCodes.Call, methodInfo));
            }

            return codes;
        }

        public static void DrawEditButton()
        {
            Rect editButtonRect = new Rect(620f, 0.0f, 50f, 30f);

            if (Widgets.ButtonText(editButtonRect, "RandomPlus.FilterButton".Translate(), true, false, true))
            {
                var page = new Page_RandomEditor();
                Find.WindowStack.Add(page);
            }

            Rect rerollLabelRect = new Rect(620f, 40f, 200f, 30f);
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
                    .GetMethod("AddTestButton", BindingFlags.Public | BindingFlags.Static);
                newCode.Add(new CodeInstruction(OpCodes.Call, methodInfo));
                codes.InsertRange(startIndex + 2, newCode);
            }

            return codes;
        }

        public static void AddTestButton(List<ListableOption> optList)
        {
            optList.Add(new ListableOption((string)"RandomPlus.Debug.QuickStartButton".Translate(), () => {
                var page_select_scenario = new Page_SelectScenario();
                Find.WindowStack.Add(page_select_scenario);

                var methodInfo0 = typeof(Page_SelectScenario).GetMethod("CanDoNext", BindingFlags.NonPublic | BindingFlags.Instance);
                methodInfo0.Invoke(page_select_scenario, new object[0]);
                var methodInfo1 = typeof(Page_SelectScenario).GetMethod("DoNext", BindingFlags.NonPublic | BindingFlags.Instance);
                methodInfo1.Invoke(page_select_scenario, new object[0]);

                var page_storyteller = (Page_SelectStoryteller)page_select_scenario.next;
                Log.Message(page_storyteller.ToString());
                var page_storyteller_methodInfo0 = typeof(Page_SelectStoryteller).GetMethod("CanDoNext", BindingFlags.NonPublic | BindingFlags.Instance);
                page_storyteller_methodInfo0.Invoke(page_storyteller, new object[0]);
                var page_storyteller_methodInfo1 = typeof(Page_SelectStoryteller).GetMethod("DoNext", BindingFlags.NonPublic | BindingFlags.Instance);
                page_storyteller_methodInfo1.Invoke(page_storyteller, new object[0]);

                var page_create_world = (Page_CreateWorldParams)page_storyteller.next;

                var prop = typeof(Page_CreateWorldParams).GetField("planetCoverage", BindingFlags.NonPublic | BindingFlags.Instance);
                prop.SetValue(page_create_world, 0.01f);

                var page_create_world_methodInfo0 = typeof(Page_CreateWorldParams).GetMethod("CanDoNext", BindingFlags.NonPublic | BindingFlags.Instance);
                page_create_world_methodInfo0.Invoke(page_create_world, new object[0]);

                page_create_world.next = new Page_ConfigureStartingPawns();

            }, (string)null));
        }
    }
}