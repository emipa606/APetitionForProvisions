using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace ItemRequests
{
    class IncidentWorker_RequestCaravanArrival : IncidentWorker
    {
        protected virtual PawnGroupKindDef PawnGroupKindDef => PawnGroupKindDefOf.Trader;

        protected bool TryResolveParms(IncidentParms parms)
        {
            if (!TryResolveParmsGeneral(parms))
            {
                return false;
            }
            ResolveParmsPoints(parms);
            return true;
        }

        protected void ResolveParmsPoints(IncidentParms parms)
        {
            parms.points = TraderCaravanUtility.GenerateGuardPoints();
        }

        private IEnumerable<Faction> CandidateFactions(Map map, bool desperate = false)
        {
            return from f in Find.FactionManager.AllFactions
                   where FactionCanBeGroupSource(f, map, desperate)
                   select f;
        }

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            return parms.faction != null || CandidateFactions(map, false).Any() || !NeutralGroupIncidentUtility.AnyBlockingHostileLord(map, parms.faction);
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;

            if (!TryResolveParms(parms))
            {
                return false;
            }

            if (parms.faction.HostileTo(Faction.OfPlayer))
            {
                return false;
            }

            List<Pawn> list = SpawnPawns(parms);
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

            IntVec3 chillSpot;
            IncidentDef arrival = ItemRequestsDefOf.RequestCaravanArrival;
            Find.LetterStack.ReceiveLetter(arrival.letterLabel, arrival.letterText, arrival.letterDef, list[0], parms.faction);
            RCellFinder.TryFindRandomSpotJustOutsideColony(list[0], out chillSpot);

            LordJob_FulfillItemRequest lordJob = new LordJob_FulfillItemRequest(parms.faction, chillSpot);
            LordMaker.MakeNewLord(parms.faction, lordJob, map, list);
            return true;
        }

        private List<Pawn> SpawnPawns(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            PawnGroupMakerParms defaultPawnGroupMakerParms = IncidentParmsUtility.GetDefaultPawnGroupMakerParms(PawnGroupKindDef, parms, true);
            List<Pawn> list = PawnGroupMakerUtility.GeneratePawns(defaultPawnGroupMakerParms, false).ToList();
            foreach (Pawn pawn in list)
            {
                IntVec3 loc = CellFinder.RandomClosewalkCellNear(parms.spawnCenter, map, 5);
                GenSpawn.Spawn(pawn, loc, map, WipeMode.Vanish);
            }
            return list;
        }

        private bool FactionCanBeGroupSource(Faction f, Map map, bool desperate = false)
        {
            return (
                !f.IsPlayer && !f.defeated &&
                    (desperate || (
                                f.def.allowedArrivalTemperatureRange.Includes(map.mapTemperature.OutdoorTemp)
                                && f.def.allowedArrivalTemperatureRange.Includes(map.mapTemperature.SeasonalTemp)
                        )
                    )
                ) &&
                !f.def.hidden &&
                !f.HostileTo(Faction.OfPlayer) &&
                !NeutralGroupIncidentUtility.AnyBlockingHostileLord(map, f);
        }

        protected virtual bool TryResolveParmsGeneral(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            return (
                // Set valid spawn point
                parms.spawnCenter.IsValid || 
                RCellFinder.TryFindRandomPawnEntryCell(out parms.spawnCenter, map, CellFinder.EdgeRoadChance_Neutral, false, null)
            ) && 
            (parms.faction != null || 
             CandidateFactions(map, false).TryRandomElement(out parms.faction) || 
             CandidateFactions(map, true).TryRandomElement(out parms.faction));
        }
    }
}

