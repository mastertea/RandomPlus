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
    [HarmonyPatch(typeof(StartingPawnUtility), "WorkTypeRequirementsSatisfied")]
    class PawnSatisfiedMethodPatch
    {
        static int counter = 0;
        static void Prefix()
        {
            counter++;
            Log.Message(counter.ToString());
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            int startIndex = -1;

            var codes = new List<CodeInstruction>(instructions);

            //var newCode = new List<CodeInstruction>();
            //newCode.Add(new CodeInstruction(OpCodes.Nop));
            ////newCode.Add(new CodeInstruction(OpCodes.Ldstr, "test"));
            ////newCode.Add(new CodeInstruction(OpCodes.Call, typeof(Verse.Log).GetMethod("Message", new Type[] { typeof(string) })));
            //newCode.Add(new CodeInstruction(OpCodes.Call, typeof(RandomSettings).GetMethod("IsLimitEnabled")));
            //newCode.Add(new CodeInstruction(OpCodes.Ret));
            //codes.InsertRange(0, newCode);

            //for (int i = 0; i < codes.Count; i++)
            //{
            //    if (codes[i].opcode == OpCodes.Ldarg_2 &&
            //        codes[i + 1].opcode == OpCodes.Brfalse)
            //    {
            //        startIndex = i;
            //        break;
            //    }
            //}

            //if (startIndex != -1)
            //{
            //    var methodInfo = typeof(RandomEditButtonPatch)
            //        .GetMethod("DrawEditButton", BindingFlags.Public | BindingFlags.Static);
            //    codes.Insert(startIndex + 2, new CodeInstruction(OpCodes.Call, methodInfo));
            //}

            return codes;
        }

        //public static bool WorkTypeRequirementsSatisfied()
        //{
        //    if (StartingPawnUtility.StartingAndOptionalPawns.Count == 0)
        //        return false;
        //    List<WorkTypeDef> defsListForReading = DefDatabase<WorkTypeDef>.AllDefsListForReading;
        //    for (int index1 = 0; index1 < defsListForReading.Count; ++index1)
        //    {
        //        WorkTypeDef w = defsListForReading[index1];
        //        if (w.requireCapableColonist)
        //        {
        //            bool flag = false;
        //            for (int index2 = 0; index2 < Find.GameInitData.startingPawnCount; ++index2)
        //            {
        //                if (!StartingPawnUtility.StartingAndOptionalPawns[index2].story.WorkTypeIsDisabled(w))
        //                {
        //                    flag = true;
        //                    break;
        //                }
        //            }
        //            if (!flag)
        //                return false;
        //        }
        //    }
        //    return !TutorSystem.TutorialMode || !StartingPawnUtility.StartingAndOptionalPawns.Take<Pawn>(Find.GameInitData.startingPawnCount).Any<Pawn>((Func<Pawn, bool>)(p => p.story.WorkTagIsDisabled(WorkTags.Violent)));
        //}
    }
}
