using System;
using System.Collections.Generic;
using System.Linq;
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
        private Faction playerFaction => Find.FactionManager.OfPlayer;

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

            LordToil_Travel lordToil_Travel = new LordToil_Travel(chillSpot);
            stateGraph.StartingToil = lordToil_Travel;

            LordToil_DefendPoint lordToil_DefendPoint = new LordToil_DefendPoint();
            stateGraph.AddToil(lordToil_DefendPoint);

            LordToil_DefendPoint lordToil_DefendTraderCaravanPoint = new LordToil_DefendPoint(chillSpot, 60);
            stateGraph.AddToil(lordToil_DefendTraderCaravanPoint);

            LordToil_ExitMapAndEscortCarriers lordToil_ExitMapAndEscortCarriers = new LordToil_ExitMapAndEscortCarriers();
            stateGraph.AddToil(lordToil_ExitMapAndEscortCarriers);

            LordToil_ExitMap lordToil_ExitMap = new LordToil_ExitMap();
            stateGraph.AddToil(lordToil_ExitMap);

            LordToil_ExitMap lordToil_ExitMapActively = new LordToil_ExitMap(LocomotionUrgency.Walk, true);
            stateGraph.AddToil(lordToil_ExitMapActively);

            LordToil_ExitMapTraderFighting lordToil_ExitMapTraderFighting = new LordToil_ExitMapTraderFighting();
            stateGraph.AddToil(lordToil_ExitMapTraderFighting);

            Transition leaveIfDangerousTemp = new Transition(lordToil_Travel, lordToil_ExitMapAndEscortCarriers);
            leaveIfDangerousTemp.AddSources(new LordToil[]
            {
                lordToil_DefendPoint,
                lordToil_DefendTraderCaravanPoint
            });
            leaveIfDangerousTemp.AddTrigger(new Trigger_PawnExperiencingDangerousTemperatures());
            leaveIfDangerousTemp.AddPreAction(new TransitionAction_Message("MessageVisitorsDangerousTemperature".Translate(faction.def.pawnsPlural.CapitalizeFirst(), faction.Name)));
            leaveIfDangerousTemp.AddPostAction(new TransitionAction_EndAllJobs());
            stateGraph.AddTransition(leaveIfDangerousTemp);

            Transition leaveIfTrapped = new Transition(lordToil_Travel, lordToil_ExitMapActively);
            leaveIfTrapped.AddSources(new LordToil[]
            {
                lordToil_DefendPoint,
                lordToil_DefendTraderCaravanPoint,
                lordToil_ExitMapAndEscortCarriers,
                lordToil_ExitMap,
                lordToil_ExitMapTraderFighting
            });
            leaveIfTrapped.AddTrigger(new Trigger_PawnCannotReachMapEdge());
            leaveIfTrapped.AddPostAction(new TransitionAction_Message("MessageVisitorsTrappedLeaving".Translate(faction.def.pawnsPlural.CapitalizeFirst(), faction.Name)));
            leaveIfTrapped.AddPostAction(new TransitionAction_WakeAll());
            leaveIfTrapped.AddPostAction(new TransitionAction_EndAllJobs());
            stateGraph.AddTransition(leaveIfTrapped);

            Transition fightIfTrapped = new Transition(lordToil_ExitMapActively, lordToil_ExitMapTraderFighting);
            fightIfTrapped.AddTrigger(new Trigger_PawnCanReachMapEdge());
            fightIfTrapped.AddPostAction(new TransitionAction_EndAllJobs());
            stateGraph.AddTransition(fightIfTrapped);

            Transition fightIfFractionPawnsLost = new Transition(lordToil_Travel, lordToil_ExitMapTraderFighting);
            fightIfFractionPawnsLost.AddSources(new LordToil[]
            {
                lordToil_DefendPoint,
                lordToil_DefendTraderCaravanPoint,
                lordToil_ExitMapAndEscortCarriers,
                lordToil_ExitMap
            });
            fightIfFractionPawnsLost.AddTrigger(new Trigger_FractionPawnsLost(0.2f));
            fightIfFractionPawnsLost.AddPostAction(new TransitionAction_EndAllJobs());
            stateGraph.AddTransition(fightIfFractionPawnsLost);

            Transition defendIfPawnHarmed = new Transition(lordToil_Travel, lordToil_DefendPoint);
            defendIfPawnHarmed.AddTrigger(new Trigger_PawnHarmed());
            defendIfPawnHarmed.AddPreAction(new TransitionAction_SetDefendTrader());
            defendIfPawnHarmed.AddPostAction(new TransitionAction_WakeAll());
            defendIfPawnHarmed.AddPostAction(new TransitionAction_EndAllJobs());
            stateGraph.AddTransition(defendIfPawnHarmed);

            Transition returnToNormalAfterTimePasses = new Transition(lordToil_DefendPoint, lordToil_Travel);
            returnToNormalAfterTimePasses.AddTrigger(new Trigger_TicksPassedWithoutHarm(1200));
            stateGraph.AddTransition(returnToNormalAfterTimePasses);

            Transition defendIfMemoReceived = new Transition(lordToil_Travel, lordToil_DefendTraderCaravanPoint);
            defendIfMemoReceived.AddTrigger(new Trigger_Memo("TravelArrived"));
            stateGraph.AddTransition(defendIfMemoReceived);

            Transition leaveIfRequestFulfilled = new Transition(lordToil_Travel, lordToil_ExitMapAndEscortCarriers);
            leaveIfRequestFulfilled.AddSources(new LordToil[]
            {
                lordToil_DefendPoint,
                lordToil_DefendTraderCaravanPoint
            });
            leaveIfRequestFulfilled.AddTrigger(new Trigger_Memo(MemoOnFulfilled));
            leaveIfRequestFulfilled.AddPreAction(new TransitionAction_Message("The requested caravan from " + faction.Name + " is leaving."));
            leaveIfRequestFulfilled.AddPostAction(new TransitionAction_WakeAll());
            stateGraph.AddTransition(leaveIfRequestFulfilled);

            Transition attackIfNotEnoughSilver = new Transition(lordToil_Travel, lordToil_DefendTraderCaravanPoint);
            attackIfNotEnoughSilver.AddSources(new LordToil[]
            {
                lordToil_DefendPoint,
                lordToil_DefendTraderCaravanPoint
            });
            attackIfNotEnoughSilver.AddTrigger(new Trigger_Memo(MemoOnUnfulfilled));
            attackIfNotEnoughSilver.AddPreAction(new TransitionAction_Message("The traders from " + faction.Name+ " are attacking your colonists!"));
            attackIfNotEnoughSilver.AddPostAction(new TransitionAction_WakeAll());
            stateGraph.AddTransition(attackIfNotEnoughSilver);
            
            Trigger_TicksPassed ticksPassed = new Trigger_TicksPassed(Rand.Range(25000, 35000));
            TransitionAction_Message actionMessage = new TransitionAction_Message(faction.Name + " has been insulted by your negligence to acknowledge their presence and purchase the items you requested." + addedMessageText);
            if (faction.PlayerRelationKind == FactionRelationKind.Neutral)
            {
                Transition attackIfRequestUnfulfilled = new Transition(lordToil_DefendTraderCaravanPoint, lordToil_ExitMapTraderFighting);
                attackIfRequestUnfulfilled.AddTrigger(ticksPassed);
                attackIfRequestUnfulfilled.AddPreAction(actionMessage);
                attackIfRequestUnfulfilled.AddPostAction(new TransitionAction_WakeAll());
                attackIfRequestUnfulfilled.AddPostAction(new TransitionAction_SetDefendTrader()); // will this work..?
                faction.TrySetRelationKind(playerFaction, FactionRelationKind.Hostile, true, noFulfilledTradeMsg);

                stateGraph.AddTransition(attackIfRequestUnfulfilled, true);
            }
            else
            {
                faction.TryAffectGoodwillWith(playerFaction, -30, true, false, noFulfilledTradeMsg);

                Transition leaveIfRequestUnfulfilled = new Transition(lordToil_Travel, lordToil_ExitMapAndEscortCarriers);
                leaveIfRequestUnfulfilled.AddSources(new LordToil[]
                {
                    lordToil_DefendPoint,
                    lordToil_DefendTraderCaravanPoint
                });
                leaveIfRequestUnfulfilled.AddTrigger(ticksPassed);
                leaveIfRequestUnfulfilled.AddPreAction(actionMessage);
                leaveIfRequestUnfulfilled.AddPostAction(new TransitionAction_Message("The requested caravan from " + faction.Name + " is leaving."));
                leaveIfRequestUnfulfilled.AddPostAction(new TransitionAction_WakeAll());

                stateGraph.AddTransition(leaveIfRequestUnfulfilled);
            }


            Transition continueToLeaveMap = new Transition(lordToil_ExitMapAndEscortCarriers, lordToil_ExitMapAndEscortCarriers, true, true);
            continueToLeaveMap.canMoveToSameState = true;
            continueToLeaveMap.AddTrigger(new Trigger_PawnLost());
            continueToLeaveMap.AddTrigger(new Trigger_TickCondition(() => LordToil_ExitMapAndEscortCarriers.IsAnyDefendingPosition(lord.ownedPawns) && !GenHostility.AnyHostileActiveThreatTo(base.Map, faction), 60));
            stateGraph.AddTransition(continueToLeaveMap);

            Transition finishLeavingMap = new Transition(lordToil_ExitMapAndEscortCarriers, lordToil_ExitMap);
            finishLeavingMap.AddTrigger(new Trigger_TicksPassed(60000));
            finishLeavingMap.AddPostAction(new TransitionAction_WakeAll());
            stateGraph.AddTransition(finishLeavingMap);

            Transition leaveIfBadThingsHappen = new Transition(lordToil_DefendTraderCaravanPoint, lordToil_ExitMapAndEscortCarriers);
            leaveIfBadThingsHappen.AddSources(new LordToil[]
            {
                lordToil_Travel,
                lordToil_DefendPoint
            });
            leaveIfBadThingsHappen.AddTrigger(new Trigger_ImportantTraderCaravanPeopleLost());
            //leaveIfBadThingsHappen.AddTrigger(new Trigger_BecamePlayerEnemy());
            leaveIfBadThingsHappen.AddPostAction(new TransitionAction_WakeAll());
            leaveIfBadThingsHappen.AddPostAction(new TransitionAction_EndAllJobs());
            stateGraph.AddTransition(leaveIfBadThingsHappen);

            return stateGraph;
        }
        public override void ExposeData()
        {
            Scribe_References.Look<Faction>(ref this.faction, "faction", false);
            Scribe_Values.Look<IntVec3>(ref this.chillSpot, "chillSpot", default(IntVec3), false);
        }
    }

}
