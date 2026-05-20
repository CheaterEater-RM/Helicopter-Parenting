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
        }
    }
}