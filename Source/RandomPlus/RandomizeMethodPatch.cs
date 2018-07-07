using Harmony;
using RimWorld;
using Verse;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection.Emit;
using System;
using System.Reflection;

namespace RandomPlus
{
    [HarmonyPatch(typeof(Page_ConfigureStartingPawns), "RandomizeCurPawn")]
    class RandomizeMethodPatch
    {
        static void Prefix()
        {
            RandomSettings.ResetRerollCounter();
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            int startIndex = -1;

            var curPawnFieldInfo = typeof(Page_ConfigureStartingPawns)
                .GetField("curPawn", BindingFlags.NonPublic | BindingFlags.Instance);
            var randomizeInPlaceMethodInfo = typeof(StartingPawnUtility)
                .GetMethod("RandomizeInPlace", BindingFlags.Public | BindingFlags.Static);

            //Log.Message(curPawnFieldInfo.Name);
            //Log.Message(randomizeInPlaceMethodInfo.Name);

            var codes = new List<CodeInstruction>(instructions);

            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Ldfld &&
                    codes[i].operand == curPawnFieldInfo &&
                    codes[i + 1].opcode == OpCodes.Call &&
                    codes[i + 1].operand == randomizeInPlaceMethodInfo)
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

                codes[startIndex + 8] = new CodeInstruction(OpCodes.Call, randomLimitRerollMethodInfo);
                
                var startLoopLocation = codes[startIndex + 13].operand;

                var newCode = new List<CodeInstruction>();
                newCode.Add(new CodeInstruction(OpCodes.Nop));
                newCode.Add(new CodeInstruction(OpCodes.Ldarg_0));
                newCode.Add(new CodeInstruction(OpCodes.Ldfld, curPawnFieldInfo));
                newCode.Add(new CodeInstruction(OpCodes.Call, CheckPawnIsSatisfiedMethodInfo));
                newCode.Add(new CodeInstruction(OpCodes.Brfalse, startLoopLocation));
                codes.InsertRange(startIndex + 12, newCode);
            }

            return codes;
        }
    }
}
