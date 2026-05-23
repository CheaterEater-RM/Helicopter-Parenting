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
                    toil.AddEndCondition(() =>
                    {
                        if (!pawn.IsHashIntervalTick(250) || !OutdoorActivityUtility.ShouldInterruptSkyDreamingForPredator(pawn))
                        {
                            return JobCondition.Ongoing;
                        }

                        int predatorScanRadius = HelicopterParenting_Mod.Settings?.predatorScanRadius ?? 0;
                        DiagnosticsUtility.LogIntervention(
                            "Sky dreaming interrupted",
                            pawn,
                            $"interrupted at {DiagnosticsUtility.DescribeCell(pawn.Position)} because a predator entered the {predatorScanRadius}-tile buffer during active sky dreaming.");
                        return JobCondition.Incompletable;
                    });
                }

                yield return toil;
            }
        }
    }
}