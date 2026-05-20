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
        }
    }
}