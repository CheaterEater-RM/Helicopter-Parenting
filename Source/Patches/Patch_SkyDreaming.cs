using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace HelicopterParenting.Patches
{
    [HarmonyPatch(typeof(LearningGiver_Skydreaming), nameof(LearningGiver_Skydreaming.TryGiveJob))]
    public static class Patch_SkyDreaming
    {
        static void Postfix(Pawn pawn, ref Job __result)
        {
            if (__result == null || pawn?.Map == null || HelicopterParenting_Mod.Settings == null || !HelicopterParenting_Mod.Settings.enableSkyDreamFix)
            {
                return;
            }

            if (!JoyUtility.EnjoyableOutsideNow(pawn) || OutdoorActivityUtility.ShouldBlockOutdoorActivities(pawn.Map))
            {
                __result = null;
                return;
            }

            if (OutdoorActivityUtility.IsSkyDreamCellSafe(pawn, __result.targetA.Cell))
            {
                return;
            }

            if (OutdoorActivityUtility.TryFindSafeSkyDreamCell(pawn, pawn.Position, out IntVec3 saferCell))
            {
                __result.targetA = saferCell;
                return;
            }

            __result = null;
        }
    }
}