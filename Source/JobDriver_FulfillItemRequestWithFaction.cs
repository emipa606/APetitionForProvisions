using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace ItemRequests
{
    class JobDriver_FulfillItemRequestWithFaction : JobDriver
    {
        private Pawn Trader
        {
            get
            {
                return (Pawn)base.TargetThingA;
            }
        }        

        protected override IEnumerable<Toil> MakeNewToils()
        {
            //Log.Message("Make fulfill trade request");
            this.FailOnDespawnedOrNull(TargetIndex.A);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch).FailOn(() => !Trader.CanTradeNow);
            Toil trade = new Toil();
            trade.initAction = delegate ()
            {
                Pawn actor = trade.actor;
                if (Trader.CanTradeNow)
                {
                    FulfillItemRequestWindow dialog = new FulfillItemRequestWindow(actor, Trader);
                    dialog.forcePause = true;
                    Find.WindowStack.Add(dialog);
                }
            };
            yield return trade;
            yield break;
        }
                
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            Pawn pawn = this.pawn;
            LocalTargetInfo target = this.Trader;
            Job job = this.job;
            return pawn.Reserve(target, job, 1, -1, null, errorOnFailed);
        }
    }
}
