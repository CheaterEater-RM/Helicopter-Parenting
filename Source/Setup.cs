using HarmonyLib;
using Verse;

namespace HelicopterParenting
{
    [StaticConstructorOnStartup]
    public static class HelicopterParenting_Init
    {
        static HelicopterParenting_Init()
        {
            Harmony harmony = new Harmony("com.cheatereater.helicopterparenting");
            harmony.PatchAll();

            if (Prefs.DevMode)
            {
                Log.Message("[Helicopter Parenting] Harmony patches applied.");
            }
        }
    }
}