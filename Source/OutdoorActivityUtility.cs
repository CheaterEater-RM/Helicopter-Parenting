using System.Collections.Generic;
using RimWorld;
using Verse;

namespace HelicopterParenting
{
    internal static class OutdoorActivityUtility
    {
        public static bool ShouldBlockOutdoorActivities(Map map)
        {
            if (map == null || HelicopterParenting_Mod.Settings == null || !HelicopterParenting_Mod.Settings.rejectDuringThreats)
            {
                return false;
            }

            return GenHostility.AnyHostileActiveThreatToPlayer(map);
        }

        public static bool IsWithinEdgeBuffer(IntVec3 cell, Map map)
        {
            if (map == null || HelicopterParenting_Mod.Settings == null)
            {
                return false;
            }

            int edgeBuffer = HelicopterParenting_Mod.Settings.mapEdgeBuffer;
            return cell.x < edgeBuffer || cell.z < edgeBuffer || cell.x >= map.Size.x - edgeBuffer || cell.z >= map.Size.z - edgeBuffer;
        }

        public static bool HasNearbyWildPredator(IntVec3 cell, Map map)
        {
            if (map == null || HelicopterParenting_Mod.Settings == null || !HelicopterParenting_Mod.Settings.avoidPredators)
            {
                return false;
            }

            float radius = HelicopterParenting_Mod.Settings.predatorScanRadius;
            IReadOnlyList<Pawn> allPawnsSpawned = map.mapPawns.AllPawnsSpawned;
            for (int index = 0; index < allPawnsSpawned.Count; index++)
            {
                Pawn predator = allPawnsSpawned[index];
                if (predator.Dead || predator.Downed || predator.Faction != null || predator.RaceProps == null || !predator.RaceProps.predator)
                {
                    continue;
                }

                if (predator.Position.DistanceTo(cell) <= radius)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsNatureRunningCellSafe(Pawn searcher, IntVec3 cell)
        {
            if (searcher?.Map == null || !cell.IsValid)
            {
                return false;
            }

            if (IsWithinEdgeBuffer(cell, searcher.Map))
            {
                return false;
            }

            return !HasNearbyWildPredator(cell, searcher.Map);
        }

        public static bool TryFindSafeNatureRunningTarget(Pawn searcher, out LocalTargetInfo interestTarget)
        {
            interestTarget = LocalTargetInfo.Invalid;
            if (searcher?.Map == null)
            {
                return false;
            }

            Dictionary<Region, bool> regionContainsOwnedBuildingCache = new Dictionary<Region, bool>();
            LocalTargetInfo bfsTarget = LocalTargetInfo.Invalid;
            TraverseParms traverseParms = TraverseParms.For(searcher);
            int additionalRegionsToCheck = 8;

            RegionTraverser.BreadthFirstTraverse(searcher.Position, searcher.Map, (Region from, Region region) => region.Allows(traverseParms, isDestination: false), delegate(Region region)
            {
                if (bfsTarget.IsValid && --additionalRegionsToCheck <= 0)
                {
                    return true;
                }

                if (region.IsForbiddenEntirely(searcher) || region.DangerFor(searcher) == Danger.Deadly || region.extentsClose.ClosestDistSquaredTo(searcher.Position) < 225f)
                {
                    return false;
                }

                CellRect nearbyArea = region.extentsClose.ExpandedBy(15);
                bool foundNearbyRegionWithBuildings = false;
                RegionTraverser.BreadthFirstTraverse(region, (Region from, Region to) => to.extentsClose.Overlaps(nearbyArea), delegate(Region nearbyRegion)
                {
                    if (regionContainsOwnedBuildingCache.TryGetValue(nearbyRegion, out bool containsOwnedBuilding))
                    {
                        if (containsOwnedBuilding)
                        {
                            foundNearbyRegionWithBuildings = true;
                            return true;
                        }

                        return false;
                    }

                    foreach (Thing thing in nearbyRegion.ListerThings.ThingsInGroup(ThingRequestGroup.BuildingArtificial))
                    {
                        if (thing.Faction != null)
                        {
                            foundNearbyRegionWithBuildings = true;
                            regionContainsOwnedBuildingCache[nearbyRegion] = true;
                            return true;
                        }
                    }

                    regionContainsOwnedBuildingCache[nearbyRegion] = false;
                    return false;
                });

                if (foundNearbyRegionWithBuildings)
                {
                    return false;
                }

                if (region.TryFindRandomCellInRegion(cell => NatureRunningCellValidator(searcher, cell), out IntVec3 result)
                    && (!bfsTarget.IsValid || searcher.Position.DistanceTo(bfsTarget.Cell) > searcher.Position.DistanceTo(result)))
                {
                    bfsTarget = result;
                }

                return false;
            }, 300);

            interestTarget = bfsTarget;
            return interestTarget.IsValid;
        }

        public static bool IsSkyDreamCellSafe(Pawn pawn, IntVec3 cell)
        {
            if (pawn?.Map == null || !cell.IsValid)
            {
                return false;
            }

            if (IsWithinEdgeBuffer(cell, pawn.Map) || HasNearbyWildPredator(cell, pawn.Map))
            {
                return false;
            }

            return pawn.ComfortableTemperatureAtCell(cell, pawn.Map);
        }

        public static bool TryFindSafeSkyDreamCell(Pawn pawn, IntVec3 root, out IntVec3 result)
        {
            result = IntVec3.Invalid;
            if (pawn?.Map == null)
            {
                return false;
            }

            List<Thing> nearbyColonyThings = new List<Thing>();
            TraverseParms traverseParms = TraverseParms.For(pawn);
            IntVec3 foundCell = IntVec3.Invalid;

            RegionTraverser.BreadthFirstTraverse(root, pawn.Map, (Region from, Region region) => region.Allows(traverseParms, isDestination: false), delegate(Region region)
            {
                if (region.IsForbiddenEntirely(pawn))
                {
                    return false;
                }

                List<Thing> pawns = region.ListerThings.ThingsInGroup(ThingRequestGroup.Pawn);
                for (int index = 0; index < pawns.Count; index++)
                {
                    if (pawns[index].Faction == Faction.OfPlayer)
                    {
                        nearbyColonyThings.Add(pawns[index]);
                    }
                }

                List<Thing> buildings = region.ListerThings.ThingsInGroup(ThingRequestGroup.BuildingArtificial);
                for (int index = 0; index < buildings.Count; index++)
                {
                    if (buildings[index].Faction == Faction.OfPlayer && !nearbyColonyThings.Contains(buildings[index]))
                    {
                        nearbyColonyThings.Add(buildings[index]);
                    }
                }

                if (region.TryFindRandomCellInRegion(cell => SkyDreamCellValidator(cell, pawn, nearbyColonyThings), out IntVec3 candidate))
                {
                    foundCell = candidate;
                    return true;
                }

                return false;
            }, 45);

            result = foundCell;
            return result.IsValid;
        }

        private static bool NatureRunningCellValidator(Pawn searcher, IntVec3 cell)
        {
            if (cell.IsForbidden(searcher))
            {
                return false;
            }

            TerrainDef terrain = cell.GetTerrain(searcher.Map);
            if (terrain.avoidWander || !terrain.natural)
            {
                return false;
            }

            return IsNatureRunningCellSafe(searcher, cell);
        }

        private static bool SkyDreamCellValidator(IntVec3 cell, Pawn pawn, List<Thing> nearbyColonyThings)
        {
            if (cell.Roofed(pawn.Map) || cell.GetTerrain(pawn.Map).avoidWander || !IsGoodDestinationFor(cell, pawn, careAboutDanger: true))
            {
                return false;
            }

            for (int index = 0; index < nearbyColonyThings.Count; index++)
            {
                float distance = nearbyColonyThings[index].Position.DistanceTo(cell);
                if (distance < 35f)
                {
                    return false;
                }
            }

            return IsSkyDreamCellSafe(pawn, cell);
        }

        private static bool IsGoodDestination(IntVec3 cell, Map map, bool careAboutDanger)
        {
            if (!cell.Standable(map))
            {
                return false;
            }

            if (careAboutDanger && cell.GetTerrain(map).dangerous)
            {
                return false;
            }

            return true;
        }

        private static bool IsGoodDestinationFor(IntVec3 cell, Pawn pawn, bool careAboutDanger)
        {
            Map map = pawn.Map;
            if (!IsGoodDestination(cell, map, careAboutDanger))
            {
                return false;
            }

            if (!cell.WalkableBy(map, pawn))
            {
                return false;
            }

            if (!cell.Standable(map))
            {
                Building_Door door = cell.GetDoor(map);
                if (door == null || !door.CanPhysicallyPass(pawn))
                {
                    return false;
                }
            }

            if (cell.IsForbidden(pawn))
            {
                return false;
            }

            if (careAboutDanger && cell.GetDangerFor(pawn, map) == Danger.Deadly)
            {
                return false;
            }

            if (careAboutDanger && PawnUtility.KnownDangerAt(cell, pawn.Map, pawn))
            {
                return false;
            }

            if (careAboutDanger && cell.VacuumConcernTo(pawn))
            {
                return false;
            }

            return true;
        }
    }
}
