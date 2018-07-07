using Harmony;
using RimWorld;
using System.Reflection;
using Verse;

namespace RandomPlus
{
    [StaticConstructorOnStartup]
    public class HarmonyPatches
    {
        static HarmonyPatches()
        {
            var harmony = HarmonyInstance.Create("RandomPlus");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
    }
}