using System.Collections.Generic;
using Verse;

namespace HelicopterParenting
{
    internal static class DiagnosticsUtility
    {
        private const string Prefix = "[Helicopter Parenting]";

        public static bool Enabled => HelicopterParenting_Mod.Settings?.enableDiagnostics ?? false;

        public static void LogIntervention(string action, Pawn pawn, string details)
        {
            if (!Enabled)
            {
                return;
            }

            string pawnLabel = pawn?.LabelShortCap ?? "Unknown pawn";
            Log.Message($"{Prefix} {action}: {pawnLabel} - {details}");
        }

        public static string DescribeCell(IntVec3 cell)
        {
            return cell.IsValid ? cell.ToString() : "Invalid";
        }

        public static string DescribeNatureRunningUnsafeReason(Pawn pawn, IntVec3 cell)
        {
            List<string> reasons = new List<string>();
            if (pawn?.Map == null || !cell.IsValid)
            {
                return "an unknown outdoor safety rule";
            }

            if (OutdoorActivityUtility.IsWithinEdgeBuffer(cell, pawn.Map))
            {
                reasons.Add("the map-edge buffer");
            }

            if (OutdoorActivityUtility.HasNearbyPredatorThreat(cell, pawn.Map, pawn))
            {
                reasons.Add("the predator buffer");
            }

            return DescribeReasons(reasons);
        }

        public static string DescribeSkyDreamUnsafeReason(Pawn pawn, IntVec3 cell)
        {
            List<string> reasons = new List<string>();
            if (pawn?.Map == null || !cell.IsValid)
            {
                return "an unknown outdoor safety rule";
            }

            if (OutdoorActivityUtility.IsWithinEdgeBuffer(cell, pawn.Map))
            {
                reasons.Add("the map-edge buffer");
            }

            if (OutdoorActivityUtility.HasNearbyPredatorThreat(cell, pawn.Map, pawn))
            {
                reasons.Add("the predator buffer");
            }

            if (!pawn.ComfortableTemperatureAtCell(cell, pawn.Map))
            {
                reasons.Add("comfortable temperature requirements");
            }

            return DescribeReasons(reasons);
        }

        private static string DescribeReasons(List<string> reasons)
        {
            if (reasons.Count == 0)
            {
                return "an unknown outdoor safety rule";
            }

            return string.Join(", ", reasons);
        }
    }
}