using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;
using UnityEngine;
using Verse.AI;

namespace ItemRequests
{
    [HarmonyPatch(typeof(FloatMenuMakerMap))]
    [HarmonyPatch("AddHumanlikeOrders")]
    public static class FulfillRequestFloatMenuOption
    {
        [HarmonyPostfix]
        public static void ModifyTradeOption(Vector3 clickPos, Pawn pawn, List<FloatMenuOption> opts)
        {
            RequestSession requestSession = Find.World.GetComponent<RequestSession>();            
            List<LocalTargetInfo> localTradeTargets = GenUI.TargetsAt_NewTemp(clickPos, TargetingParameters.ForTrade(), true).ToList();
            if (localTradeTargets.Count == 0) return;

            FloatMenuOption optToRemove = opts.Find((option) =>
            {
                foreach (LocalTargetInfo localTarget in localTradeTargets)
                {
                    Pawn pTarg = (Pawn)localTarget.Thing;
                    if (
                        option.Label.Contains("TradeWith".Translate(pTarg.LabelShort + ", " + pTarg.TraderKind.label)) && 
                        requestSession.HasOpenDealWith(pTarg.Faction) &&
                        Find.TickManager.TicksGame >= requestSession.GetTimeOfOccurenceWithFaction(pTarg.Faction) &&
                        pTarg.GetTraderCaravanRole() == TraderCaravanRole.Trader &&
                        pTarg.CanTradeNow
                       )
                    {
                        return true;
                    }
                }
                return false;
            });

            if (optToRemove != null)
            {
                foreach (LocalTargetInfo targetInfo in localTradeTargets)
                {
                    LocalTargetInfo localTargetInfo = targetInfo;
                    Pawn pTarg = (Pawn)localTargetInfo.Thing;

                    if (requestSession.HasOpenDealWith(pTarg.Faction) && Find.TickManager.TicksGame >= requestSession.GetTimeOfOccurenceWithFaction(pTarg.Faction))
                    {
                        Action takeOrderedJob = delegate ()
                        {
                            Job job = new Job(ItemRequestsDefOf.FulfillItemRequestWithFaction, pTarg);
                            job.playerForced = true;
                            pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                            PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.InteractingWithTraders, KnowledgeAmount.Total);
                        };
                        
                        Pawn reservedBy = pawn.Map.reservationManager.FirstRespectedReserver(pTarg, pawn);
                        string label = "IR.FulfillRequestFloatMenuOption.MenuText".Translate(pTarg.Faction.Name);
                        if (reservedBy != null)
                        {
                            label += "IR.FulfillRequestFloatMenuOption.ReservedBy".Translate(reservedBy.LabelShort);
                        }
                        Thing thing = localTargetInfo.Thing;
                        MenuOptionPriority priority = MenuOptionPriority.InitiateSocial;
                        opts.Add(new FloatMenuOption(label, takeOrderedJob, priority, null, thing));
                        opts.Remove(optToRemove);
                    }
                }
            }
        }
    }
}
