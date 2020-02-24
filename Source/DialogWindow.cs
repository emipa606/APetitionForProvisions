using System;
using System.Linq;
using RimWorld;
using Verse;
using HarmonyLib;

namespace ItemRequests
{
    [HarmonyPatch(typeof(FactionDialogMaker))]
    [HarmonyPatch("FactionDialogFor")]
    public static class DialogWindow
    {
        
        [HarmonyPostfix]
        public static void AddOption(DiaNode __instance, ref DiaNode __result, Pawn negotiator, Faction faction)
        {
            if (faction.PlayerRelationKind == FactionRelationKind.Ally ||
                faction.PlayerRelationKind == FactionRelationKind.Neutral)
            {
                Map map = negotiator.Map;
                DiaOption newOption = RequestItemOption(map, faction, negotiator);

                // If there's a third option for requesting the AI Persona Core
                // then put it after that. Otherwise put it after first two
                // options (Request caravan & request military aid).
                int insertAtIndex = DefDatabase<ResearchProjectDef>.AllDefsListForReading.Any((ResearchProjectDef rp) => 
                    rp.HasTag(ResearchProjectTagDefOf.ShipRelated) && rp.IsFinished) ? 
                    3 :
                    2;
                
                __result.options.Insert(insertAtIndex, newOption);
            }
        }

        private static DiaOption RequestItemOption(Map map, Faction faction, Pawn negotiator)
        {
            string text = "IR.DialogWindow.RequestItems".Translate();

            // Can't request more items from same faction
            // until x number of ticks have passed.
            int num = faction.lastTraderRequestTick + 240000 - Find.TickManager.TicksGame;
            if (num > 0)
            {
                DiaOption mustWaitOption = new DiaOption(text);
                mustWaitOption.Disable("WaitTime".Translate(num.ToStringTicksToPeriod()));
                return mustWaitOption;
            }

            DiaOption tradeAcceptedOption = new DiaOption(text);

            tradeAcceptedOption.action = () =>
            {
                bool success;
                Find.World.GetComponent<RequestSession>().SetupWith(faction, negotiator, out success);
                if (success)
                {
                    Find.WindowStack.Add(new ItemRequestWindow(map, faction, negotiator));
                }
            };

            return tradeAcceptedOption;
        }
        
    }
}