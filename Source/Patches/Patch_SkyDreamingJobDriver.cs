using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace HelicopterParenting.Patches
{
    [HarmonyPatch(typeof(JobDriver_Skydreaming), "MakeNewToils")]
    public static class Patch_SkyDreamingJobDriver
    {
        static void Postfix(JobDriver_Skydreaming __instance, ref IEnumerable<Toil> __result)
        {
            if (__instance?.pawn == null || __result == null)
            {
                return;
            }

            __result = AddPredatorInterrupt(__result, __instance.pawn);
        }

        private static IEnumerable<Toil> AddPredatorInterrupt(IEnumerable<Toil> toils, Pawn pawn)
        {
            foreach (Toil toil in toils)
            {
                if (toil.defaultCompleteMode == ToilCompleteMode.Delay)
                {
                    toil.FailOn(() => pawn.IsHashIntervalTick(250) && OutdoorActivityUtility.ShouldInterruptSkyDreamingForPredator(pawn));
                }

                yield return toil;
            }
        }
    }
}