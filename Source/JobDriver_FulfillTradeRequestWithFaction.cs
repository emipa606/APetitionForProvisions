using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace ItemRequests
{
    class JobDriver_FulfillTradeRequestWithFaction : JobDriver_TradeWithPawn
    {
        public static JobDef Def
        {
            get
            {
                JobDef def = JobDefOf.TradeWithPawn;
                def.defName = "Fulfill Trade Request With Faction";
                def.description = "Talk to the faction you requested items from to pay for the items.";
                def.label = def.defName;
                
                return def;
            }
        }

        private Pawn Trader
        {
            get
            {
                return (Pawn)base.TargetThingA;
            }
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            Log.Message("Make fulfill trade request");
            this.FailOnDespawnedOrNull(TargetIndex.A);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch).FailOn(() => !Trader.CanTradeNow);
            Toil trade = new Toil();
            trade.initAction = delegate ()
            {
                Pawn actor = trade.actor;
                if (Trader.CanTradeNow)
                {
                    // TODO: make custom dialog trade window
                    FulfillItemRequestWindow dialog = new FulfillItemRequestWindow(actor, Trader);
                    Find.WindowStack.Add(dialog);
                }
            };
            yield return trade;
            yield break;
        }
    }
}
