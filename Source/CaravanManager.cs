using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI.Group;

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
            incidentParms.traderKind = faction.def.caravanTraderKinds[faction.def.caravanTraderKinds.Count - 1];

            // TODO: increase/decrease journey time based on closest faction base
            Log.Message("Requested caravan sent");
            Find.Storyteller.incidentQueue.Add(IncidentWorker_RequestCaravanArrival.DefOf, Find.TickManager.TicksGame + 120000, incidentParms, 240000);
            faction.lastTraderRequestTick = Find.TickManager.TicksGame;
        }

        // faction.TryAffectGoodwillWith(ofPlayer, goodwillChange, true, true, reason, lookTarget);        
    }
}
