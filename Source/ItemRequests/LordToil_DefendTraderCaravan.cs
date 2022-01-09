using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace ItemRequests;

public class LordToil_DefendTraderCaravan : LordToil_DefendPoint
{
    public LordToil_DefendTraderCaravan()
    {
    }

    public LordToil_DefendTraderCaravan(IntVec3 defendPoint, float defendRadius)
        : base(defendPoint, defendRadius)
    {
    }

    public override bool AllowSatisfyLongNeeds => false;

    public override float? CustomWakeThreshold => 0.5f;

    public override void UpdateAllDuties()
    {
        var toilData = Data;
        var pawn = TraderCaravanUtility.FindTrader(lord);
        if (pawn == null)
        {
            return;
        }

        pawn.mindState.duty = new PawnDuty(DutyDefOf.Defend, toilData.defendPoint, toilData.defendRadius);
        foreach (var pawn2 in lord.ownedPawns)
        {
            switch (pawn2.GetTraderCaravanRole())
            {
                case TraderCaravanRole.Carrier:
                    pawn2.mindState.duty = new PawnDuty(DutyDefOf.Follow, pawn, 5f);
                    pawn2.mindState.duty.locomotion = LocomotionUrgency.Walk;
                    break;
                case TraderCaravanRole.Guard:
                    pawn2.mindState.duty =
                        new PawnDuty(DutyDefOf.Defend, toilData.defendPoint, toilData.defendRadius);
                    break;
                case TraderCaravanRole.Chattel:
                    pawn2.mindState.duty = new PawnDuty(DutyDefOf.Escort, pawn, 5f);
                    pawn2.mindState.duty.locomotion = LocomotionUrgency.Walk;
                    break;
            }
        }
    }
}