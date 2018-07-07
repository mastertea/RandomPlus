using Harmony;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace RandomPlus
{
    [StaticConstructorOnStartup]
    class Main
    {
        
    }

    [StaticConstructorOnStartup]
    public class HarmonyPatches
    {
        static HarmonyPatches()
        {
            var harmony = HarmonyInstance.Create("RandomPlus");
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            harmony.Patch(typeof(Page_ConfigureStartingPawns).GetMethod("PreOpen"),
                    new HarmonyMethod(null),
                    new HarmonyMethod(typeof(HarmonyPatches).GetMethod("PreOpenPostfix")));
        }

        //static bool showButton = false;

        //static HarmonyPatches()
        //{
        //    HarmonyInstance harmony = HarmonyInstance.Create("RandomPlus.HarmonyPatches");

        //    //harmony.Patch(typeof(Page_ConfigureStartingPawns).GetMethod("PreOpen"),
        //    //    new HarmonyMethod(null),
        //    //    new HarmonyMethod(typeof(HarmonyPatches).GetMethod("PreOpenPostfix")));

        //    //harmony.Patch(typeof(Page_ConfigureStartingPawns).GetMethod("DoWindowContents"),
        //    //    new HarmonyMethod(typeof(HarmonyPatches).GetMethod("DoWindowContentsPrefix")),
        //    //    new HarmonyMethod(null));

        //    // add a check condition after random button has been clicked on.

        //   harmony.Patch(typeof(Page_ConfigureStartingPawns).GetMethod("DoWindowContents"),
        //       new HarmonyMethod(typeof(HarmonyPatches).GetMethod("RandomizeCurPawnPrefix")),
        //       new HarmonyMethod(null));
        //}

        public static void PreOpenPostfix()
        {
            RandomSettings.Init();
        }
    }
}