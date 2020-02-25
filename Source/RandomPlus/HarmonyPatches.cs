using Harmony;
using RimWorld;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using Verse;

namespace RandomPlus
{
    [StaticConstructorOnStartup]
    internal static class Main
    {
        static Main()
        {
            var harmony = HarmonyInstance.Create("RandomPlus");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
    }

    [HarmonyPatch(typeof(Page_ConfigureStartingPawns), "PreOpen")]
    class Patch_Init
    {
        static void Postfix()
        {
            RandomSettings.Init();
        }
    }

    [HarmonyPatch(typeof(Page_ConfigureStartingPawns), "RandomizeCurPawn")]
    class Patch_RandomizeMethod
    {
        static void Prefix()
        {
            RandomSettings.ResetRerollCounter();
        }

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
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            int startIndex = -1;

            var codes = new List<CodeInstruction>(instructions);
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Ldarg_2 &&
                    codes[i + 1].opcode == OpCodes.Brfalse)
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

            if (Widgets.ButtonText(editButtonRect, "Filter", true, false, true))
            {
                var page = new Page_RandomEditor();
                Find.WindowStack.Add(page);
            }

            Rect rerollLabelRect = new Rect(620f, 40f, 200f, 30f);
            string labelText = "Rerolls: " + RandomSettings.RandomRerollCounter() + "/" + RandomSettings.RandomRerollLimit();

            var tmpSave = GUI.color;
            if (RandomSettings.RandomRerollCounter() >= RandomSettings.RandomRerollLimit())
                GUI.color = Color.red;
            Widgets.Label(rerollLabelRect, labelText);
            GUI.color = tmpSave;
        }
    }
}