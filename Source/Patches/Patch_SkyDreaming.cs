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
        }
    }
}