using HarmonyLib;
using RimWorld;
using Verse;

namespace HelicopterParenting.Patches
{
    [HarmonyPatch(typeof(LearningGiver_Floordrawing), nameof(LearningGiver_Floordrawing.TryFindFloordrawingSpots))]
    public static class Patch_FloorDrawing
    {
        static void Postfix(Pawn pawn, ref IntVec3 drawFrom, ref IntVec3 drawOn, ref bool __result)
        {
            if (!__result || pawn?.Map == null || HelicopterParenting_Mod.Settings == null || !HelicopterParenting_Mod.Settings.enableFloorDrawFix)
            {
                return;
            }

            if (pawn.ComfortableTemperatureAtCell(drawFrom, pawn.Map))
            {
                return;
            }

            if (FloorDrawingUtility.TryImproveFloorDrawPair(pawn, drawFrom, drawOn, out IntVec3 betterDrawFrom, out IntVec3 betterDrawOn))
            {
                DiagnosticsUtility.LogIntervention(
                    "Floor drawing redirect",
                    pawn,
                    $"redirected draw pair from {DiagnosticsUtility.DescribeCell(drawFrom)}->{DiagnosticsUtility.DescribeCell(drawOn)} to {DiagnosticsUtility.DescribeCell(betterDrawFrom)}->{DiagnosticsUtility.DescribeCell(betterDrawOn)} because the original room was outside the child's comfortable temperature range.");
                drawFrom = betterDrawFrom;
                drawOn = betterDrawOn;
            }
        }
    }
}