using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace ItemRequests;

internal class JobDriver_FulfillItemRequestWithFaction : JobDriver
{
    private Pawn Trader => (Pawn)TargetThingA;

    protected override IEnumerable<Toil> MakeNewToils()
    {
        //Log.Message("Make fulfill trade request");
        this.FailOnDespawnedOrNull(TargetIndex.A);
        yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch).FailOn(() => !Trader.CanTradeNow);
        var trade = new Toil();
        trade.initAction = delegate
        {
            var actor = trade.actor;
            if (!Trader.CanTradeNow)
            {
                return;
            }

            var dialog = new FulfillItemRequestWindow(actor, Trader) { forcePause = true };
            Find.WindowStack.Add(dialog);
        };
        yield return trade;
    }

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        var toilPawn = pawn;
        LocalTargetInfo target = Trader;
        var toilJob = job;
        return toilPawn.Reserve(target, toilJob, 1, -1, null, errorOnFailed);
    }
}