using System;
using System.Linq;
using RimWorld;
using Verse;
using Harmony;

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
            string text = "Request specific items";

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
            //DiaNode requestItemNode = new DiaNode("TraderSent".Translate(faction.leader).CapitalizeFirst());
            //requestItemNode.options.Add(OKToRoot(faction,negotiator));
                        
            //DiaNode diaNode2 = new DiaNode("ChooseTraderKind".Translate(faction.leader));
            //foreach (TraderKindDef localTk2 in from x in faction.def.caravanTraderKinds
            //                                   where x.requestable
            //                                   select x)
            //{
            //    TraderKindDef localTk = localTk2;
            //    DiaOption diaOption5 = new DiaOption(localTk.LabelCap);
            //    diaOption5.action = delegate ()
            //    {
            //        IncidentParms incidentParms = new IncidentParms();
            //        incidentParms.target = map;
            //        incidentParms.faction = faction;
            //        incidentParms.traderKind = localTk;
            //        incidentParms.forced = true;
            //        Find.Storyteller.incidentQueue.Add(IncidentDefOf.TraderCaravanArrival, Find.TickManager.TicksGame + 120000, incidentParms, 240000);
            //        faction.lastTraderRequestTick = Find.TickManager.TicksGame;
            //        Faction faction2 = faction;
            //        Faction ofPlayer = Faction.OfPlayer;
            //        int goodwillChange = -15;
            //        bool canSendMessage = false;
            //        string reason = "GoodwillChangedReason_RequestedTrader".Translate();
            //        faction2.TryAffectGoodwillWith(ofPlayer, goodwillChange, canSendMessage, true, reason, null);
            //    };
            //    diaOption5.link = requestItemNode;
            //    diaNode2.options.Add(diaOption5);
            //}

            //DiaOption goBackOption = new DiaOption("GoBack".Translate());
            //goBackOption.linkLateBind = ResetToRoot(faction, negotiator);
            //diaNode2.options.Add(goBackOption);
            //tradeAcceptedOption.link = diaNode2;

            tradeAcceptedOption.action = () => {
                Find.WindowStack.Add(new ItemRequestWindow(map, faction, negotiator));
            };

            return tradeAcceptedOption;
        }


        // OK option -- return to root node
        private static DiaOption OKToRoot(Faction faction, Pawn negotiator)
        {
            return new DiaOption("OK".Translate())
            {
                linkLateBind = ResetToRoot(faction, negotiator)
            };
        }

        private static Func<DiaNode> ResetToRoot(Faction faction, Pawn negotiator)
        {
            return () => FactionDialogMaker.FactionDialogFor(negotiator, faction);
        }
    }
}