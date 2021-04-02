using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
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
            var requestSession = Find.World.GetComponent<RequestSession>();
            var localTradeTargets = GenUI.TargetsAt_NewTemp(clickPos, TargetingParameters.ForTrade(), true).ToList();
            if (localTradeTargets.Count == 0)
            {
                return;
            }

            var optToRemove = opts.Find(option =>
            {
                foreach (var localTarget in localTradeTargets)
                {
                    var pTarg = (Pawn) localTarget.Thing;
                    if (
                        option.Label.Contains(
                            "TradeWith".Translate(pTarg.LabelShort + ", " + pTarg.TraderKind.label)) &&
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

            if (optToRemove == null)
            {
                return;
            }

            {
                foreach (var targetInfo in localTradeTargets)
                {
                    var localTargetInfo = targetInfo;
                    var pTarg = (Pawn) localTargetInfo.Thing;

                    if (!requestSession.HasOpenDealWith(pTarg.Faction) || !(Find.TickManager.TicksGame >=
                                                                            requestSession
                                                                                .GetTimeOfOccurenceWithFaction(
                                                                                    pTarg.Faction)))
                    {
                        continue;
                    }

                    void TakeOrderedJob()
                    {
                        var job = new Job(ItemRequestsDefOf.FulfillItemRequestWithFaction, pTarg) {playerForced = true};
                        pawn.jobs.TryTakeOrderedJob(job);
                        PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.InteractingWithTraders,
                            KnowledgeAmount.Total);
                    }

                    var reservedBy = pawn.Map.reservationManager.FirstRespectedReserver(pTarg, pawn);
                    string label = "IR.FulfillRequestFloatMenuOption.MenuText".Translate(pTarg.Faction.Name);
                    if (reservedBy != null)
                    {
                        label += "IR.FulfillRequestFloatMenuOption.ReservedBy".Translate(reservedBy.LabelShort);
                    }

                    var thing = localTargetInfo.Thing;
                    var priority = MenuOptionPriority.InitiateSocial;
                    opts.Add(new FloatMenuOption(label, TakeOrderedJob, priority, null, thing));
                    opts.Remove(optToRemove);
                }
            }
        }
    }
}