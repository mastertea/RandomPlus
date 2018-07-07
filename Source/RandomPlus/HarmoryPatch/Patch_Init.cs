using Harmony;
using RimWorld;
using Verse;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection.Emit;
using System;
using System.Reflection;

namespace RandomPlus.HarmoryPatch
{
    [HarmonyPatch(typeof(Page_ConfigureStartingPawns), "PreOpen")]
    class Patch_Init
    {
        static void Postfix()
        {
            RandomSettings.Init();
        }
    }
}
