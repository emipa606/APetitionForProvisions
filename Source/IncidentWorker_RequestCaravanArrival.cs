using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace ItemRequests
{
    class IncidentWorker_RequestCaravanArrival : IncidentWorker_NeutralGroup
    {
        public static IncidentDef DefOf
        {
            get
            {
                IncidentDef modifiedDef = IncidentDefOf.TraderCaravanArrival;
                modifiedDef.pawnMustBeCapableOfViolence = true;

                modifiedDef.defName = "Request Caravan Arrival";
                modifiedDef.letterLabel = "Requested Items Arrived";
                modifiedDef.letterText = "A trade caravan carrying the items you requested has arrived.\n\n" +
                    "Talk to the caravan leader to make your trade. Be careful though, if you don't have enough " +
                    "silver you'll anger the faction, which could possibly lead to... undesirable outcomes.";
                modifiedDef.description = "The caravan that was sent from the faction you requested specific items from.";
                
                return modifiedDef;
            }
        }

        protected override PawnGroupKindDef PawnGroupKindDef
        {
            get
            {
                return PawnGroupKindDefOf.Trader;
            }
        }

        protected override bool FactionCanBeGroupSource(Faction f, Map map, bool desperate = false)
        {
            return base.FactionCanBeGroupSource(f, map, desperate) && f.def.caravanTraderKinds.Any();
        }

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (!base.CanFireNowSub(parms))
            {
                return false;
            }
            Map map = (Map)parms.target;
            return parms.faction == null || !NeutralGroupIncidentUtility.AnyBlockingHostileLord(map, parms.faction);
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            if (!base.TryResolveParms(parms))
            {
                return false;
            }
            if (parms.faction.HostileTo(Faction.OfPlayer))
            {
                return false;
            }
            List<Pawn> list = base.SpawnPawns(parms);
            if (list.Count == 0)
            {
                return false;
            }
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].needs != null && list[i].needs.food != null)
                {
                    list[i].needs.food.CurLevel = list[i].needs.food.MaxLevel;
                }
            }

            Log.Message("Request caravan has arrived!");

            IntVec3 chillSpot;
            Find.LetterStack.ReceiveLetter(DefOf.letterLabel, DefOf.letterText, LetterDefOf.PositiveEvent, list[0], parms.faction, null);
            RCellFinder.TryFindRandomSpotJustOutsideColony(list[0], out chillSpot);

            // Specify actions that happen while caravan is on the map
            LordJob_FulfillItemRequest lordJob = new LordJob_FulfillItemRequest(parms.faction, chillSpot);
            LordMaker.MakeNewLord(parms.faction, lordJob, map, list);
            return true;
        }
    
        protected override void ResolveParmsPoints(IncidentParms parms)
        {
            parms.points = TraderCaravanUtility.GenerateGuardPoints();
        }
    }
}
