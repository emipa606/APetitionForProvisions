using RimWorld;
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

            // TODO: increase/decrease journey time based on closest faction base
            int variableTravelTime = 120000;
            Find.Storyteller.incidentQueue.Add(ItemRequestsDefOf.RequestCaravanArrival, Find.TickManager.TicksGame + variableTravelTime, incidentParms, 240000);
            faction.lastTraderRequestTick = Find.TickManager.TicksGame;
        }   
    }
}
