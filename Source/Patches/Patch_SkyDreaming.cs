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

            bool enjoyableOutside = JoyUtility.EnjoyableOutsideNow(pawn);
            bool blockedDuringThreats = OutdoorActivityUtility.ShouldBlockOutdoorActivities(pawn.Map);
            if (!enjoyableOutside || blockedDuringThreats)
            {
                string cancelReason = !enjoyableOutside && blockedDuringThreats
                    ? "canceled because outdoor joy is not enjoyable right now and outdoor activities are blocked during an active hostile threat."
                    : (!enjoyableOutside
                        ? "canceled because outdoor joy is not enjoyable right now."
                        : "canceled because outdoor activities are blocked during an active hostile threat.");
                DiagnosticsUtility.LogIntervention("Sky dreaming canceled", pawn, cancelReason);
                __result = null;
                return;
            }

            IntVec3 originalCell = __result.targetA.Cell;
            if (OutdoorActivityUtility.IsSkyDreamCellSafe(pawn, originalCell))
            {
                return;
            }

            string unsafeReason = DiagnosticsUtility.Enabled ? DiagnosticsUtility.DescribeSkyDreamUnsafeReason(pawn, originalCell) : null;
            if (OutdoorActivityUtility.TryFindSafeSkyDreamCell(pawn, pawn.Position, out IntVec3 saferCell))
            {
                DiagnosticsUtility.LogIntervention(
                    "Sky dreaming redirect",
                    pawn,
                    $"redirected target from {DiagnosticsUtility.DescribeCell(originalCell)} to {DiagnosticsUtility.DescribeCell(saferCell)} because the original target violated {unsafeReason}.");
                __result.targetA = saferCell;
                return;
            }

            DiagnosticsUtility.LogIntervention(
                "Sky dreaming canceled",
                pawn,
                $"canceled because target {DiagnosticsUtility.DescribeCell(originalCell)} violated {unsafeReason} and no safe replacement was found.");
            __result = null;
        }
    }
}