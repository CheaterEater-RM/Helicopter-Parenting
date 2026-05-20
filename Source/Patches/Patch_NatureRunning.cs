using HarmonyLib;
using RimWorld;
using Verse;

namespace HelicopterParenting.Patches
{
    [HarmonyPatch(typeof(NatureRunningUtility), nameof(NatureRunningUtility.TryFindNatureInterestTarget))]
    public static class Patch_NatureRunning
    {
        static void Postfix(Pawn searcher, ref LocalTargetInfo interestTarget, ref bool __result)
        {
            if (searcher?.Map == null || HelicopterParenting_Mod.Settings == null || !HelicopterParenting_Mod.Settings.enableNatureRunFix)
            {
                return;
            }

            if (OutdoorActivityUtility.ShouldBlockOutdoorActivities(searcher.Map))
            {
                interestTarget = LocalTargetInfo.Invalid;
                __result = false;
                return;
            }

            if (!__result || OutdoorActivityUtility.IsNatureRunningCellSafe(searcher, interestTarget.Cell))
            {
                return;
            }

            if (OutdoorActivityUtility.TryFindSafeNatureRunningTarget(searcher, out LocalTargetInfo saferTarget))
            {
                interestTarget = saferTarget;
                return;
            }

            interestTarget = LocalTargetInfo.Invalid;
            __result = false;
        }
    }
}
