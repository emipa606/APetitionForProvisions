using System.Linq;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace ItemRequests
{
    internal class IncidentWorker_RequestCaravanArrival : IncidentWorker_NeutralGroup
    {
        protected override PawnGroupKindDef PawnGroupKindDef => PawnGroupKindDefOf.Trader;

        private new bool TryResolveParms(IncidentParms parms)
        {
            if (!TryResolveParmsGeneral(parms))
            {
                return false;
            }

            ResolveParmsPoints(parms);
            return true;
        }

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            var map = (Map) parms.target;
            return parms.faction != null || CandidateFactions(map).Any() ||
                   !NeutralGroupIncidentUtility.AnyBlockingHostileLord(map, parms.faction);
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            var map = (Map) parms.target;

            if (!TryResolveParms(parms))
            {
                return false;
            }

            if (parms.faction.HostileTo(Faction.OfPlayer))
            {
                return false;
            }

            var list = SpawnPawns(parms);
            if (list.Count == 0)
            {
                return false;
            }

            foreach (var pawn in list)
            {
                if (pawn.needs?.food != null)
                {
                    pawn.needs.food.CurLevel = pawn.needs.food.MaxLevel;
                }
            }

            var arrival = ItemRequestsDefOf.RequestCaravanArrival;
            Find.LetterStack.ReceiveLetter(arrival.letterLabel, arrival.letterText, arrival.letterDef, list[0],
                parms.faction);
            RCellFinder.TryFindRandomSpotJustOutsideColony(list[0], out var chillSpot);

            var lordJob = new LordJob_FulfillItemRequest(parms.faction, chillSpot);
            LordMaker.MakeNewLord(parms.faction, lordJob, map, list);
            return true;
        }

        protected override bool FactionCanBeGroupSource(Faction f, Map map, bool desperate = false)
        {
            return !f.IsPlayer && !f.defeated &&
                   (desperate || f.def.allowedArrivalTemperatureRange.Includes(map.mapTemperature.OutdoorTemp)
                       && f.def.allowedArrivalTemperatureRange.Includes(map.mapTemperature.SeasonalTemp)
                   ) &&
                   !f.def.hidden &&
                   !f.HostileTo(Faction.OfPlayer) &&
                   !NeutralGroupIncidentUtility.AnyBlockingHostileLord(map, f);
        }

        protected override bool TryResolveParmsGeneral(IncidentParms parms)
        {
            var map = (Map) parms.target;
            return (
                // Set valid spawn point
                parms.spawnCenter.IsValid ||
                RCellFinder.TryFindRandomPawnEntryCell(out parms.spawnCenter, map, CellFinder.EdgeRoadChance_Neutral)
            ) && parms.faction != null;
        }

        protected override void ResolveParmsPoints(IncidentParms parms)
        {
            parms.points = TraderCaravanUtility.GenerateGuardPoints();
        }
    }
}