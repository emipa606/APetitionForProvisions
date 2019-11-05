using System;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using UnityEngine;

namespace ItemRequests
{
    class LordJob_FulfillItemRequest : LordJob
    {
        public static readonly string MemoOnFulfilled = "TriggerItemRequestFulfilled";
        public static readonly string MemoOnUnfulfilled = "TriggerItemRequestUnfulfilled";
        public static readonly string MemoOnPartiallyFulfilled_S = "TriggerItemRequestPartiallyFulfilled_S";
        public static readonly string MemoOnPartiallyFulfilled_M = "TriggerItemRequestPartiallyFulfilled_M";
        public static readonly string MemoOnPartiallyFulfilled_L = "TriggerItemRequestPartiallyFulfilled_L";

        private Faction faction;
        private IntVec3 chillSpot;
        private Faction playerFaction => Faction.OfPlayer;
        private bool isFactionNeutral => faction.PlayerRelationKind == FactionRelationKind.Neutral;

        public LordJob_FulfillItemRequest() { }

        public LordJob_FulfillItemRequest(Faction faction, IntVec3 chillSpot)
        {
            this.faction = faction;
            this.chillSpot = chillSpot;
        }

        public override StateGraph CreateGraph()
        {
            StateGraph stateGraph = new StateGraph();
            string noFulfilledTradeMsg = "IR.LordJobFulfillItemRequest.NoFulfillTrade".Translate();
            string partiallyFulfilledTradeMsg = "IR.LordJobFulfillItemRequest.NoFulfillAllTrade".Translate();
            string addedMessageText = faction.RelationKindWith(playerFaction) == FactionRelationKind.Neutral ? 
                "IR.LordJobFulfillItemRequest.AttackingOutOfAnger".Translate() :
                "IR.LordJobFulfillItemRequest.RelationsDropped".Translate();
            TransitionAction_Custom clearCaravanRequest = new TransitionAction_Custom(() => { RequestSession.CloseOpenDealWith(faction); });

            // ===================
            //       TOILS
            // ===================
            LordToil_Travel moving = new LordToil_Travel(chillSpot);
            stateGraph.StartingToil = moving;

            LordToil_DefendPoint defending = new LordToil_DefendTraderCaravan();
            stateGraph.AddToil(defending);

            LordToil_DefendPoint defendingChillPoint = new LordToil_DefendTraderCaravan(chillSpot, 60);
            stateGraph.AddToil(defendingChillPoint);

            LordToil_ExitMapAndEscortCarriers exitingAndEscorting = new LordToil_ExitMapAndEscortCarriers();
            stateGraph.AddToil(exitingAndEscorting);

            LordToil_ExitMap exiting = new LordToil_ExitMap();
            stateGraph.AddToil(exiting);

            LordToil_ExitMap urgentExiting = new LordToil_ExitMap(LocomotionUrgency.Walk, true);
            stateGraph.AddToil(urgentExiting);

            LordToil_ExitMapTraderFighting exitWhileFighting = new LordToil_ExitMapTraderFighting();
            stateGraph.AddToil(exitWhileFighting);

            // ===================
            //    TRANSITIONS
            // ===================
            Transition leaveIfDangerousTemp = new Transition(moving, exitingAndEscorting);
            leaveIfDangerousTemp.AddSources(new LordToil[]
            {
                defending,
                defendingChillPoint
            });
            leaveIfDangerousTemp.AddTrigger(new Trigger_PawnExperiencingDangerousTemperatures());
            leaveIfDangerousTemp.AddPreAction(new TransitionAction_Message("MessageVisitorsDangerousTemperature".Translate(faction.def.pawnsPlural.CapitalizeFirst(), faction.Name)));
            leaveIfDangerousTemp.AddPostAction(new TransitionAction_EndAllJobs());
            leaveIfDangerousTemp.AddPostAction(clearCaravanRequest);
            stateGraph.AddTransition(leaveIfDangerousTemp);

            Transition leaveIfTrapped = new Transition(moving, urgentExiting);
            leaveIfTrapped.AddSources(new LordToil[]
            {
                defending,
                defendingChillPoint,
                exitingAndEscorting,
                exiting,
                exitWhileFighting
            });
            leaveIfTrapped.AddTrigger(new Trigger_PawnCannotReachMapEdge());
            leaveIfTrapped.AddPostAction(new TransitionAction_Message("MessageVisitorsTrappedLeaving".Translate(faction.def.pawnsPlural.CapitalizeFirst(), faction.Name)));
            leaveIfTrapped.AddPostAction(new TransitionAction_WakeAll());
            leaveIfTrapped.AddPostAction(new TransitionAction_EndAllJobs());
            leaveIfTrapped.AddPostAction(clearCaravanRequest);
            stateGraph.AddTransition(leaveIfTrapped);

            Transition fightIfTrapped = new Transition(urgentExiting, exitWhileFighting);
            fightIfTrapped.AddTrigger(new Trigger_PawnCanReachMapEdge());
            fightIfTrapped.AddPostAction(new TransitionAction_EndAllJobs());
            fightIfTrapped.AddPostAction(clearCaravanRequest);
            stateGraph.AddTransition(fightIfTrapped);

            Transition fightIfFractionPawnsLost = new Transition(moving, exitWhileFighting);
            fightIfFractionPawnsLost.AddSources(new LordToil[]
            {
                defending,
                defendingChillPoint,
                exitingAndEscorting,
                exiting
            });
            fightIfFractionPawnsLost.AddTrigger(new Trigger_FractionPawnsLost(0.2f));
            fightIfFractionPawnsLost.AddPostAction(new TransitionAction_EndAllJobs());
            fightIfFractionPawnsLost.AddPostAction(clearCaravanRequest);
            stateGraph.AddTransition(fightIfFractionPawnsLost);

            Transition defendIfPawnHarmed = new Transition(moving, defending);
            defendIfPawnHarmed.AddTrigger(new Trigger_PawnHarmed());
            defendIfPawnHarmed.AddPreAction(new TransitionAction_SetDefendTrader());
            defendIfPawnHarmed.AddPostAction(new TransitionAction_WakeAll());
            defendIfPawnHarmed.AddPostAction(new TransitionAction_EndAllJobs());
            stateGraph.AddTransition(defendIfPawnHarmed);

            Transition returnToNormalAfterTimePasses = new Transition(defending, moving);
            returnToNormalAfterTimePasses.AddTrigger(new Trigger_TicksPassedWithoutHarm(1200));
            stateGraph.AddTransition(returnToNormalAfterTimePasses);

            Transition stayPutAtSpot = new Transition(moving, defendingChillPoint);
            stayPutAtSpot.AddTrigger(new Trigger_Memo("TravelArrived"));
            stateGraph.AddTransition(stayPutAtSpot);

            Transition leaveIfRequestFulfilled = new Transition(moving, exitingAndEscorting);
            leaveIfRequestFulfilled.AddSources(new LordToil[]
            {
                defending,
                defendingChillPoint,
            });
            leaveIfRequestFulfilled.AddTrigger(new Trigger_Memo(MemoOnFulfilled));
            leaveIfRequestFulfilled.AddPreAction(new TransitionAction_Message("IR.LordJobFulfillItemRequest.RequestedCaravanLeaving".Translate(faction.Name)));
            leaveIfRequestFulfilled.AddPostAction(new TransitionAction_WakeAll());
            stateGraph.AddTransition(leaveIfRequestFulfilled);


            int ticksUntilBadThings = Rand.Range(
                Mathf.RoundToInt(CaravanManager.fullDayInTicks / 2),                                   // 0.5  days
                CaravanManager.fullDayInTicks + Mathf.RoundToInt(CaravanManager.fullDayInTicks / 4));  // 1.25 days

            // Determine actions if request goes unfulfilled based on faction relation
            Trigger_TicksPassed ticksPassed = new Trigger_TicksPassed(ticksUntilBadThings);
            TransitionAction_Message actionMessage = new TransitionAction_Message("IR.LordJobFulfillItemRequest.FactionInsulted".Translate(faction.Name) + "\n" + addedMessageText);
            TransitionAction_Message leavingMessage = new TransitionAction_Message("IR.LordJobFulfillItemRequest.RequestedCaravanLeaving".Translate(faction.Name));
            if (isFactionNeutral)
            {
                Action setFactionToHostile = () => faction.TrySetRelationKind(playerFaction, FactionRelationKind.Hostile, true, noFulfilledTradeMsg);

                Transition attackIfNotEnoughSilver = new Transition(moving, defending, true);
                attackIfNotEnoughSilver.AddSources(new LordToil[]
                {
                    defending,
                    defendingChillPoint,
                    exiting,
                    exitingAndEscorting
                });
                attackIfNotEnoughSilver.AddTrigger(new Trigger_Memo(MemoOnUnfulfilled));
                attackIfNotEnoughSilver.AddPreAction(new TransitionAction_Message("IR.LordJobFulfillItemRequest.TradersAttacking".Translate(faction.Name)));
                attackIfNotEnoughSilver.AddPreAction(new TransitionAction_Custom(setFactionToHostile));
                attackIfNotEnoughSilver.AddPostAction(new TransitionAction_WakeAll());
                attackIfNotEnoughSilver.AddPostAction(new TransitionAction_SetDefendLocalGroup());
                stateGraph.AddTransition(attackIfNotEnoughSilver, true);

                Transition attackIfRequestUnfulfilled = new Transition(defendingChillPoint, defending);
                attackIfRequestUnfulfilled.AddTrigger(ticksPassed);
                attackIfRequestUnfulfilled.AddPreAction(actionMessage);
                attackIfRequestUnfulfilled.AddPreAction(new TransitionAction_Custom(setFactionToHostile));
                attackIfRequestUnfulfilled.AddPostAction(new TransitionAction_WakeAll());
                attackIfRequestUnfulfilled.AddPostAction(new TransitionAction_SetDefendLocalGroup());
                stateGraph.AddTransition(attackIfRequestUnfulfilled, true);

                Transition leaveAfterAttacking = new Transition(defendingChillPoint, exitingAndEscorting);
                leaveAfterAttacking.AddTrigger(new Trigger_TicksPassed(ticksUntilBadThings + 10000));
                leaveAfterAttacking.AddPreAction(leavingMessage);
                leaveAfterAttacking.AddPostAction(new TransitionAction_EndAllJobs());
                stateGraph.AddTransition(leaveAfterAttacking);
            }
            else
            {
                Transition leaveIfRequestUnfulfilled = new Transition(moving, exitingAndEscorting);
                leaveIfRequestUnfulfilled.AddSources(new LordToil[]
                {
                    defending,
                    defendingChillPoint
                });
                leaveIfRequestUnfulfilled.AddTrigger(new Trigger_Memo(MemoOnUnfulfilled));
                leaveIfRequestUnfulfilled.AddTrigger(ticksPassed);
                leaveIfRequestUnfulfilled.AddPreAction(actionMessage);
                leaveIfRequestUnfulfilled.AddPreAction(new TransitionAction_Custom(() =>
                {
                    faction.TryAffectGoodwillWith(playerFaction, -30, true, false, noFulfilledTradeMsg);
                }));
                leaveIfRequestUnfulfilled.AddPostAction(leavingMessage);
                leaveIfRequestUnfulfilled.AddPostAction(new TransitionAction_WakeAll());
                stateGraph.AddTransition(leaveIfRequestUnfulfilled);
            }

            // Partial fulfillment
            Transition leaveIfRequestMostlyFulfilled = new Transition(moving, exiting);
            leaveIfRequestMostlyFulfilled.AddSources(new LordToil[]
            {
                defending,
                defendingChillPoint
            });
            leaveIfRequestMostlyFulfilled.AddTrigger(new Trigger_Memo(MemoOnPartiallyFulfilled_S));
            leaveIfRequestMostlyFulfilled.AddPreAction(new TransitionAction_Message("IR.LordJobFulfillItemRequest.DisappointedTraders".Translate(faction.Name), MessageTypeDefOf.CautionInput));
            leaveIfRequestMostlyFulfilled.AddPreAction(new TransitionAction_Custom(() =>
            {
                faction.TryAffectGoodwillWith(playerFaction, -5, true, false, partiallyFulfilledTradeMsg);
            }));
            leaveIfRequestMostlyFulfilled.AddPostAction(leavingMessage);
            leaveIfRequestMostlyFulfilled.AddPostAction(new TransitionAction_WakeAll());
            stateGraph.AddTransition(leaveIfRequestMostlyFulfilled);


            Transition leaveIfRequestSomewhatFulfilled = new Transition(moving, exiting);
            leaveIfRequestSomewhatFulfilled.AddSources(new LordToil[]
            {
                defending,
                defendingChillPoint
            });
            leaveIfRequestSomewhatFulfilled.AddTrigger(new Trigger_Memo(MemoOnPartiallyFulfilled_M));
            leaveIfRequestSomewhatFulfilled.AddPreAction(new TransitionAction_Message("IR.LordJobFulfillItemRequest.AnnoyedTraders".Translate(faction.Name), MessageTypeDefOf.CautionInput));
            leaveIfRequestSomewhatFulfilled.AddPreAction(new TransitionAction_Custom(() =>
            {
                faction.TryAffectGoodwillWith(playerFaction, -10, true, false, partiallyFulfilledTradeMsg);
            }));
            leaveIfRequestSomewhatFulfilled.AddPostAction(leavingMessage);
            leaveIfRequestSomewhatFulfilled.AddPostAction(new TransitionAction_WakeAll());
            stateGraph.AddTransition(leaveIfRequestSomewhatFulfilled);

            Transition leaveIfRequestHardlyFulfilled = new Transition(moving, exiting);
            leaveIfRequestHardlyFulfilled.AddSources(new LordToil[]
            {
                defending,
                defendingChillPoint
            });
            leaveIfRequestHardlyFulfilled.AddTrigger(new Trigger_Memo(MemoOnPartiallyFulfilled_L));
            leaveIfRequestHardlyFulfilled.AddPreAction(new TransitionAction_Message("IR.LordJobFulfillItemRequest.OutragedTraders".Translate(faction.Name), MessageTypeDefOf.CautionInput));
            leaveIfRequestHardlyFulfilled.AddPreAction(new TransitionAction_Custom(() =>
            {
                faction.TryAffectGoodwillWith(playerFaction, -20, true, false, partiallyFulfilledTradeMsg);
            }));
            leaveIfRequestHardlyFulfilled.AddPostAction(leavingMessage);
            leaveIfRequestHardlyFulfilled.AddPostAction(new TransitionAction_WakeAll());
            stateGraph.AddTransition(leaveIfRequestHardlyFulfilled);

            Transition continueToLeaveMap = new Transition(exitingAndEscorting, exitingAndEscorting, true);
            continueToLeaveMap.AddTrigger(new Trigger_PawnLost());
            continueToLeaveMap.AddTrigger(new Trigger_TickCondition(() => LordToil_ExitMapAndEscortCarriers.IsAnyDefendingPosition(lord.ownedPawns) && !GenHostility.AnyHostileActiveThreatTo(Map, faction), 60));
            continueToLeaveMap.AddPostAction(clearCaravanRequest);
            stateGraph.AddTransition(continueToLeaveMap);

            Transition finishLeavingMap = new Transition(exitingAndEscorting, exiting);
            finishLeavingMap.AddTrigger(new Trigger_TicksPassed(60000));
            finishLeavingMap.AddPostAction(new TransitionAction_WakeAll());
            finishLeavingMap.AddPostAction(clearCaravanRequest);
            stateGraph.AddTransition(finishLeavingMap);

            Transition leaveIfBadThingsHappen = new Transition(defendingChillPoint, exitingAndEscorting);
            leaveIfBadThingsHappen.AddSources(new LordToil[]
            {
                moving,
                defending
            });
            leaveIfBadThingsHappen.AddTrigger(new Trigger_ImportantTraderCaravanPeopleLost());
            leaveIfBadThingsHappen.AddPostAction(new TransitionAction_WakeAll());
            leaveIfBadThingsHappen.AddPostAction(new TransitionAction_EndAllJobs());
            leaveIfBadThingsHappen.AddPostAction(clearCaravanRequest);
            stateGraph.AddTransition(leaveIfBadThingsHappen);

            return stateGraph;
        }
       
        public override void ExposeData()
        {
            Scribe_References.Look(ref lord, "lord");
            Scribe_References.Look(ref faction, "faction");
            Scribe_Values.Look(ref chillSpot, "chillSpot");
        }

        public override bool AddFleeToil => false;
    }

    public class LordToil_DefendTraderCaravan : LordToil_DefendPoint
    {
        public LordToil_DefendTraderCaravan() : base(true) { }
        public LordToil_DefendTraderCaravan(IntVec3 defendPoint, float defendRadius) : base(defendPoint, defendRadius) { }

        public override bool AllowSatisfyLongNeeds => false;
        public override float? CustomWakeThreshold => new float?(0.5f);

        public override void UpdateAllDuties()
        {
            LordToilData_DefendPoint data = Data;
            Pawn pawn = TraderCaravanUtility.FindTrader(lord);
            if (pawn != null)
            {
                pawn.mindState.duty = new PawnDuty(DutyDefOf.Defend, data.defendPoint, data.defendRadius);
                for (int i = 0; i < lord.ownedPawns.Count; i++)
                {
                    Pawn pawn2 = lord.ownedPawns[i];
                    switch (pawn2.GetTraderCaravanRole())
                    {
                        case TraderCaravanRole.Carrier:
                            pawn2.mindState.duty = new PawnDuty(DutyDefOf.Follow, pawn, 5f);
                            pawn2.mindState.duty.locomotion = LocomotionUrgency.Walk;
                            break;
                        case TraderCaravanRole.Guard:
                            pawn2.mindState.duty = new PawnDuty(DutyDefOf.Defend, data.defendPoint, data.defendRadius);
                            break;
                        case TraderCaravanRole.Chattel:
                            pawn2.mindState.duty = new PawnDuty(DutyDefOf.Escort, pawn, 5f);
                            pawn2.mindState.duty.locomotion = LocomotionUrgency.Walk;
                            break;
                    }
                }
                return;
            }
        }    
    }

}
