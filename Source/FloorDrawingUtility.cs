using System.Linq;
using RimWorld;
using Verse;

namespace HelicopterParenting
{
    internal static class FloorDrawingUtility
    {
        private static readonly IntVec3[] Offsets =
        {
            IntVec3.East,
            IntVec3.South,
        };

        private readonly struct Candidate
        {
            public static readonly Candidate Invalid = new Candidate(IntVec3.Invalid, IntVec3.Invalid, 0, false, 0, float.MaxValue);

            public readonly IntVec3 DrawFrom;
            public readonly IntVec3 DrawOn;
            public readonly int TemperatureRank;
            public readonly bool SameComfortableRoom;
            public readonly int ColonistCount;
            public readonly float DistanceToPawn;

            public Candidate(IntVec3 drawFrom, IntVec3 drawOn, int temperatureRank, bool sameComfortableRoom, int colonistCount, float distanceToPawn)
            {
                DrawFrom = drawFrom;
                DrawOn = drawOn;
                TemperatureRank = temperatureRank;
                SameComfortableRoom = sameComfortableRoom;
                ColonistCount = colonistCount;
                DistanceToPawn = distanceToPawn;
            }

            public bool IsValid => DrawFrom.IsValid && DrawOn.IsValid;

            public bool IsBetterThan(Candidate other)
            {
                if (!IsValid)
                {
                    return false;
                }

                if (!other.IsValid)
                {
                    return true;
                }

                if (TemperatureRank != other.TemperatureRank)
                {
                    return TemperatureRank > other.TemperatureRank;
                }

                if (SameComfortableRoom != other.SameComfortableRoom)
                {
                    return SameComfortableRoom;
                }

                if (ColonistCount != other.ColonistCount)
                {
                    return ColonistCount > other.ColonistCount;
                }

                return DistanceToPawn < other.DistanceToPawn;
            }
        }

        public static bool TryImproveFloorDrawPair(Pawn pawn, IntVec3 currentDrawFrom, IntVec3 currentDrawOn, out IntVec3 betterDrawFrom, out IntVec3 betterDrawOn)
        {
            Candidate currentCandidate = EvaluateCandidate(pawn, currentDrawFrom, currentDrawOn);
            Candidate bestCandidate = FindBestCandidate(pawn);

            if (bestCandidate.IsBetterThan(currentCandidate))
            {
                betterDrawFrom = bestCandidate.DrawFrom;
                betterDrawOn = bestCandidate.DrawOn;
                return true;
            }

            betterDrawFrom = currentDrawFrom;
            betterDrawOn = currentDrawOn;
            return false;
        }

        private static Candidate FindBestCandidate(Pawn pawn)
        {
            if (TryFindBestCandidateForPass(pawn, comfortableOnly: true, desperate: false, out Candidate bestCandidate))
            {
                return bestCandidate;
            }

            if (TryFindBestCandidateForPass(pawn, comfortableOnly: true, desperate: true, out bestCandidate))
            {
                return bestCandidate;
            }

            if (TryFindBestCandidateForPass(pawn, comfortableOnly: false, desperate: false, out bestCandidate))
            {
                return bestCandidate;
            }

            if (TryFindBestCandidateForPass(pawn, comfortableOnly: false, desperate: true, out bestCandidate))
            {
                return bestCandidate;
            }

            return Candidate.Invalid;
        }

        private static bool TryFindBestCandidateForPass(Pawn pawn, bool comfortableOnly, bool desperate, out Candidate bestCandidate)
        {
            bestCandidate = Candidate.Invalid;

            foreach (IntVec3 root in pawn.Map.areaManager.Home.ActiveCells)
            {
                if (!LearningGiver_Floordrawing.CanFloorDrawFrom(root, pawn))
                {
                    continue;
                }

                bool canDrawOnRoot = CanDrawOn(root, pawn.Map, desperate);
                for (int index = 0; index < Offsets.Length; index++)
                {
                    IntVec3 companion = root + Offsets[index];
                    if (!LearningGiver_Floordrawing.CanFloorDrawFrom(companion, pawn))
                    {
                        continue;
                    }

                    if (!LearningGiver_Floordrawing.TryFloorDrawCellPair(companion, canDrawOnRoot, pawn, desperate, out bool shouldDrawOnRoot))
                    {
                        continue;
                    }

                    IntVec3 drawOn = shouldDrawOnRoot ? root : companion;
                    IntVec3 drawFrom = shouldDrawOnRoot ? companion : root;
                    Candidate candidate = EvaluateCandidate(pawn, drawFrom, drawOn);
                    if (!MatchesTemperaturePass(candidate, comfortableOnly))
                    {
                        continue;
                    }

                    if (candidate.IsBetterThan(bestCandidate))
                    {
                        bestCandidate = candidate;
                    }
                }
            }

            return bestCandidate.IsValid;
        }

        private static Candidate EvaluateCandidate(Pawn pawn, IntVec3 drawFrom, IntVec3 drawOn)
        {
            if (!drawFrom.IsValid || !drawOn.IsValid)
            {
                return Candidate.Invalid;
            }

            Map map = pawn.Map;
            Room currentRoom = pawn.GetRoom();
            bool currentRoomComfortable = currentRoom != null && pawn.ComfortableTemperatureRange().Includes(currentRoom.Temperature);
            Room candidateRoom = drawFrom.GetRoom(map);
            bool sameComfortableRoom = currentRoomComfortable && candidateRoom != null && candidateRoom == currentRoom;
            int colonistCount = 0;

            if (candidateRoom != null)
            {
                colonistCount = candidateRoom.ContainedThings<Pawn>().Count(otherPawn => otherPawn != pawn && otherPawn.Spawned && otherPawn.Faction == Faction.OfPlayer && otherPawn.RaceProps.Humanlike);
            }

            return new Candidate(
                drawFrom,
                drawOn,
                GetTemperatureRank(pawn, drawFrom, map),
                sameComfortableRoom,
                colonistCount,
                pawn.Position.DistanceTo(drawFrom));
        }

        private static int GetTemperatureRank(Pawn pawn, IntVec3 drawFrom, Map map)
        {
            if (pawn.ComfortableTemperatureAtCell(drawFrom, map))
            {
                return 2;
            }

            if (pawn.SafeTemperatureAtCell(drawFrom, map))
            {
                return 1;
            }

            return 0;
        }

        private static bool MatchesTemperaturePass(Candidate candidate, bool comfortableOnly)
        {
            if (!candidate.IsValid)
            {
                return false;
            }

            if (comfortableOnly)
            {
                return candidate.TemperatureRank == 2;
            }

            return candidate.TemperatureRank >= 1;
        }

        private static bool CanDrawOn(IntVec3 cell, Map map, bool desperate)
        {
            if (!FilthMaker.CanMakeFilth(cell, map, ThingDefOf.Filth_Floordrawing, FilthSourceFlags.Pawn))
            {
                return false;
            }

            if (desperate)
            {
                return true;
            }

            if (cell.GetFirstItem(map) != null || cell.GetFirstBuilding(map) != null)
            {
                return false;
            }

            return cell.GetFirstThing<Filth>(map) == null;
        }
    }
}