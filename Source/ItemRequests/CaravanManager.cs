using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace ItemRequests
{
    public class CaravanManager
    {
        public static readonly int fullDayInTicks = 60000;
        private static readonly Dictionary<Faction, int> factionTravelTime = new();

        public static void SendRequestedCaravan(Faction faction, Map playerMap)
        {
            var incidentParms = new IncidentParms
            {
                faction = faction,
                target = playerMap,
                forced = true,
                traderKind = faction.def.caravanTraderKinds.RandomElement()
            };

            var variableTravelTime = DetermineJourneyTime(faction, playerMap);
            Find.Storyteller.incidentQueue.Add(ItemRequestsDefOf.RequestCaravanArrival,
                Find.TickManager.TicksGame + variableTravelTime, incidentParms, 240000);
            faction.lastTraderRequestTick = Find.TickManager.TicksGame;
        }

        private static void DetermineCaravanTravelTimeFromFaction(Faction faction, Map playerMap)
        {
            try
            {
                var radius = 60;
                var playerBase = playerMap.Tile;
                var grid = Find.World.grid;
                var bases = Find.WorldObjects.SettlementBases.FindAll(settlementBase =>
                    settlementBase.Faction.Name == faction.Name);

                var closestFactionBase = 0;
                var ticksToArrive = int.MaxValue;
                bases.ForEach(fBase =>
                {
                    if (!(grid.ApproxDistanceInTiles(playerBase, fBase.Tile) < radius))
                    {
                        return;
                    }

                    var ticks = CaravanArrivalTimeEstimator.EstimatedTicksToArrive(
                        fBase.Tile,
                        playerBase,
                        Find.WorldPathFinder.FindPath(fBase.Tile, playerBase, null),
                        0f,
                        CaravanTicksPerMoveUtility.DefaultTicksPerMove,
                        GenTicks.TicksAbs
                    );
                    if (ticks >= ticksToArrive)
                    {
                        return;
                    }

                    ticksToArrive = ticks;
                    closestFactionBase = fBase.Tile;
                });

                if (closestFactionBase == 0)
                {
                    Log.Error("Couldn't find faction base within " + radius + " tiles");
                    // Fallback travel time 3.5 days
                    factionTravelTime.Add(faction, Mathf.FloorToInt(3.5f * fullDayInTicks));
                    return;
                }

                factionTravelTime.Add(faction, ticksToArrive);
            }
            catch
            {
                Log.Error("Error calculating dist to nearest settlement for " + faction.Name +
                          ". Defaulting travel time to 3.5 days");
                factionTravelTime.Add(faction, Mathf.FloorToInt(3.5f * fullDayInTicks));
            }
        }

        public static int DetermineJourneyTime(Faction faction, Map playerMap)
        {
            if (factionTravelTime.ContainsKey(faction))
            {
                return factionTravelTime[faction];
            }

            DetermineCaravanTravelTimeFromFaction(faction, playerMap);
            return factionTravelTime[faction];
        }

        public static void ResetTravelTimeCache()
        {
            factionTravelTime.Clear();
        }
    }
}