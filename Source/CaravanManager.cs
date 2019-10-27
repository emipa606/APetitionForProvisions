using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace ItemRequests
{
    public class CaravanManager
    {
        public static float fullDayInTicks = 60000;
        private static Dictionary<Faction, int> factionTravelTime = new Dictionary<Faction, int>();

        private static void DetermineCaravanTravelTimeFromFaction(Faction faction, Map playerMap)
        {
            int radius = 60;
            int playerBase = playerMap.Tile;
            WorldGrid grid = Find.World.grid;
            List<SettlementBase> bases = Find.WorldObjects.SettlementBases.FindAll((settlementBase) => settlementBase.Faction.Name == faction.Name);

            int closestFactionBase = 0;
            int ticksToArrive = int.MaxValue;
            bases.ForEach((fBase) =>
            {
                if (grid.ApproxDistanceInTiles(playerBase, fBase.Tile) < radius)
                {
                    int ticks = CaravanArrivalTimeEstimator.EstimatedTicksToArrive(
                        fBase.Tile,
                        playerBase,
                        Find.WorldPathFinder.FindPath(fBase.Tile, playerBase, null),
                        0f,
                        CaravanTicksPerMoveUtility.DefaultTicksPerMove,
                        GenTicks.TicksAbs
                    );
                    if (ticks < ticksToArrive)
                    {
                        ticksToArrive = ticks;
                        closestFactionBase = fBase.Tile;
                    }
                }
            });

            if (closestFactionBase == 0)
            {
                Log.Error("Couldn't find faction base within " + radius.ToString() + " tiles");
                // Fallback travel time 3.5 days
                factionTravelTime.Add(faction, Mathf.FloorToInt(3.5f * fullDayInTicks));
                return;
            }

            factionTravelTime.Add(faction, ticksToArrive);
        }


        public static void SendRequestedCaravan(Faction faction, Map playerMap)
        {
            IncidentParms incidentParms = new IncidentParms();
            incidentParms.faction = faction;
            incidentParms.target = playerMap;
            incidentParms.forced = true;
            incidentParms.traderKind = faction.def.caravanTraderKinds.RandomElement();

            int variableTravelTime = DetermineJourneyTime(faction, playerMap);
            Find.Storyteller.incidentQueue.Add(ItemRequestsDefOf.RequestCaravanArrival, Find.TickManager.TicksGame + variableTravelTime, incidentParms, 240000);
            faction.lastTraderRequestTick = Find.TickManager.TicksGame;
        }

        public static int DetermineJourneyTime(Faction faction, Map playerMap)
        {
            if (factionTravelTime.ContainsKey(faction)) return factionTravelTime[faction];
            DetermineCaravanTravelTimeFromFaction(faction, playerMap);
            return factionTravelTime[faction];
        }

        public static void ResetTravelTimeCache() => factionTravelTime.Clear();
    }
}
