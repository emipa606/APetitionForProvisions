using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace ItemRequests
{
    public class CaravanManager
    {
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

        private static int DetermineJourneyTime(Faction faction, Map playerMap)
        {
            float fullDay = 60000;
            int playerBase = playerMap.Tile;
            int radius = 60;

            // Pick closest of two factions, if there's a choice
            int factionBase1 = TileFinder.RandomSettlementTileFor(faction, false, (factionBaseTile) => {
                float tileDist = Find.World.grid.ApproxDistanceInTiles(playerBase, factionBaseTile);
                return tileDist < radius;                               
            });
            int factionBase2 = TileFinder.RandomSettlementTileFor(faction, false, (factionBaseTile) => {
                float tileDist = Find.World.grid.ApproxDistanceInTiles(playerBase, factionBaseTile);
                return factionBaseTile != factionBase1 && factionBaseTile < radius;
            });

            int factionBase = Mathf.Min(factionBase1, factionBase2);
            if (factionBase == 0)
            {
                factionBase = Mathf.Max(factionBase1, factionBase2);
                if (factionBase == 0)
                {
                    Log.Error("Couldn't find faction base within " + radius.ToString() + " tiles");
                    // Default travel time 3.5 days
                    return Mathf.FloorToInt(3.5f * fullDay);
                }
            }

            int ticks = CaravanArrivalTimeEstimator.EstimatedTicksToArrive(
                factionBase, 
                playerBase, 
                Find.WorldPathFinder.FindPath(factionBase, playerBase, null), 
                0f, 
                CaravanTicksPerMoveUtility.DefaultTicksPerMove, 
                GenTicks.TicksAbs
            );

            //Log.Message("It will take " + (ticks/fullDay).ToString() + " days to reach you");

            return ticks;
        }
    }
}
