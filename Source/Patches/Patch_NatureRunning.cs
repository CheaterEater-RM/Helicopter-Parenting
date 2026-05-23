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
                DiagnosticsUtility.LogIntervention(
                    "Nature running canceled",
                    searcher,
                    "canceled because outdoor activities are blocked during an active hostile threat.");
                interestTarget = LocalTargetInfo.Invalid;
                __result = false;
                return;
            }

            if (!__result || OutdoorActivityUtility.IsNatureRunningCellSafe(searcher, interestTarget.Cell))
            {
                return;
            }

            IntVec3 originalCell = interestTarget.Cell;
            string unsafeReason = DiagnosticsUtility.Enabled ? DiagnosticsUtility.DescribeNatureRunningUnsafeReason(searcher, originalCell) : null;

            if (OutdoorActivityUtility.TryFindSafeNatureRunningTarget(searcher, out LocalTargetInfo saferTarget))
            {
                DiagnosticsUtility.LogIntervention(
                    "Nature running redirect",
                    searcher,
                    $"redirected target from {DiagnosticsUtility.DescribeCell(originalCell)} to {DiagnosticsUtility.DescribeCell(saferTarget.Cell)} because the original target violated {unsafeReason}.");
                interestTarget = saferTarget;
                return;
            }

            DiagnosticsUtility.LogIntervention(
                "Nature running canceled",
                searcher,
                $"canceled because target {DiagnosticsUtility.DescribeCell(originalCell)} violated {unsafeReason} and no safe replacement was found.");
            interestTarget = LocalTargetInfo.Invalid;
            __result = false;
        }
    }
}
