using System;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace ItemRequests
{
    class LordJob_FulfillItemRequest : LordJob
    {
        public static readonly string MemoOnFulfilled = "TriggerItemRequestFulfilled";
        public static readonly string MemoOnUnfulfilled = "TriggerItemRequestUnfulfilled";
        private Faction faction;
        private IntVec3 chillSpot;
        private Faction playerFaction => Faction.OfPlayer;
        private bool isFactionNeutral => faction.PlayerRelationKind == FactionRelationKind.Neutral;

        public LordJob_FulfillItemRequest(Faction faction, IntVec3 chillSpot)
        {
            this.faction = faction;
            this.chillSpot = chillSpot;
        }


        // TODO: need to close the open request if caravan leaves map from other method than fulfilling/attacking
        public override StateGraph CreateGraph()
        {
            StateGraph stateGraph = new StateGraph();
            string noFulfilledTradeMsg = "Didn't fulfill trade agreement.";
            string addedMessageText = faction.RelationKindWith(playerFaction) == FactionRelationKind.Neutral ? " They're attacking your colonists out of anger!" : " Relations with your faction have dropped.";

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
            stateGraph.AddTransition(leaveIfTrapped);

            Transition fightIfTrapped = new Transition(urgentExiting, exitWhileFighting);
            fightIfTrapped.AddTrigger(new Trigger_PawnCanReachMapEdge());
            fightIfTrapped.AddPostAction(new TransitionAction_EndAllJobs());
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
                defendingChillPoint
            });
            leaveIfRequestFulfilled.AddTrigger(new Trigger_Memo(MemoOnFulfilled));
            leaveIfRequestFulfilled.AddPreAction(new TransitionAction_Message("The requested caravan from " + faction.Name + " is leaving."));
            leaveIfRequestFulfilled.AddPostAction(new TransitionAction_WakeAll());
            stateGraph.AddTransition(leaveIfRequestFulfilled);
                        
            // Determine actions if request goes unfulfilled based on faction relation
            Trigger_TicksPassed ticksPassed = new Trigger_TicksPassed(Rand.Range(25000, 35000));
            TransitionAction_Message actionMessage = new TransitionAction_Message(faction.Name + " has been insulted by your negligence to acknowledge their presence.\n" + addedMessageText);
            if (isFactionNeutral)
            {
                Action setFactionToHostile = () => faction.TrySetRelationKind(playerFaction, FactionRelationKind.Hostile, true, noFulfilledTradeMsg);

                Transition attackIfNotEnoughSilver = new Transition(moving, defendingChillPoint, true);
                attackIfNotEnoughSilver.AddSources(new LordToil[]
                {
                    defending,
                    defendingChillPoint,
                    exiting,
                    exitingAndEscorting
                });
                attackIfNotEnoughSilver.AddTrigger(new Trigger_Memo(MemoOnUnfulfilled));
                attackIfNotEnoughSilver.AddPreAction(new TransitionAction_Message("The traders from " + faction.Name + " are attacking your colonists!"));
                attackIfNotEnoughSilver.AddPreAction(new TransitionAction_Custom(setFactionToHostile));
                attackIfNotEnoughSilver.AddPostAction(new TransitionAction_WakeAll());
                attackIfNotEnoughSilver.AddPostAction(new TransitionAction_SetDefendLocalGroup());
                stateGraph.AddTransition(attackIfNotEnoughSilver);

                Transition attackIfRequestUnfulfilled = new Transition(defendingChillPoint, exitWhileFighting);
                attackIfRequestUnfulfilled.AddTrigger(ticksPassed);
                attackIfRequestUnfulfilled.AddPreAction(actionMessage);
                attackIfRequestUnfulfilled.AddPreAction(new TransitionAction_Custom(setFactionToHostile));
                attackIfRequestUnfulfilled.AddPostAction(new TransitionAction_WakeAll());                
                attackIfRequestUnfulfilled.AddPostAction(new TransitionAction_SetDefendLocalGroup());

                stateGraph.AddTransition(attackIfRequestUnfulfilled, true);
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
                leaveIfRequestUnfulfilled.AddPostAction(new TransitionAction_Message("The requested caravan from " + faction.Name + " is leaving."));
                leaveIfRequestUnfulfilled.AddPostAction(new TransitionAction_WakeAll());

                stateGraph.AddTransition(leaveIfRequestUnfulfilled);
            }

            Transition continueToLeaveMap = new Transition(exitingAndEscorting, exitingAndEscorting, true);                        
            continueToLeaveMap.AddTrigger(new Trigger_PawnLost());
            continueToLeaveMap.AddTrigger(new Trigger_TickCondition(() => LordToil_ExitMapAndEscortCarriers.IsAnyDefendingPosition(lord.ownedPawns) && !GenHostility.AnyHostileActiveThreatTo(Map, faction), 60));
            stateGraph.AddTransition(continueToLeaveMap);

            Transition finishLeavingMap = new Transition(exitingAndEscorting, exiting);
            finishLeavingMap.AddTrigger(new Trigger_TicksPassed(60000));
            finishLeavingMap.AddPostAction(new TransitionAction_WakeAll());
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
            stateGraph.AddTransition(leaveIfBadThingsHappen);

            return stateGraph;
        }
        public override void ExposeData()
        {
            Scribe_References.Look(ref faction, "faction", false);
            Scribe_Values.Look(ref chillSpot, "chillSpot", default(IntVec3), false);
        }

        public override bool AddFleeToil => false;
    }

    public class LordToil_DefendTraderCaravan : LordToil_DefendPoint
    {
        public LordToil_DefendTraderCaravan() : base(true) {}
        public LordToil_DefendTraderCaravan(IntVec3 defendPoint, float defendRadius) : base(defendPoint, defendRadius) {}

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
