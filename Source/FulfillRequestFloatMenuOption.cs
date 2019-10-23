using System;
using System.Collections.Generic;
using System.Linq;
using Harmony;
using RimWorld;
using Verse;
using UnityEngine;
using Verse.AI;
using Verse.AI.Group;

namespace ItemRequests
{
    [HarmonyPatch(typeof(FloatMenuMakerMap))]
    [HarmonyPatch("AddHumanlikeOrders")]
    public static class FulfillRequestFloatMenuOption
    {
        [HarmonyPostfix]
        public static void ModifyTradeOption(Vector3 clickPos, Pawn pawn, List<FloatMenuOption> opts)
        {
            List<LocalTargetInfo> localTradeTargets = GenUI.TargetsAt(clickPos, TargetingParameters.ForTrade(), true).ToList();
            if (localTradeTargets.Count == 0) return;

            FloatMenuOption optToRemove = opts.Find((option) =>
            {
                foreach (LocalTargetInfo localTarget in localTradeTargets)
                {
                    Pawn pTarg = (Pawn)localTarget.Thing;
                    // not foolproof, but should work in 99% of cases?
                    if (option.Label.Contains("TradeWith".Translate(pTarg.LabelShort + ", " + pTarg.TraderKind.label)) && RequestSession.HasOpenDealWith(pTarg.Faction))
                    {
                        Log.Message("Option found!");
                        return true;
                    }
                }
                return false;
            });

            if (optToRemove != null)
            {
                Log.Message("Option removed!");
                opts.Remove(optToRemove);

                foreach (LocalTargetInfo targetInfo in localTradeTargets)
                {
                    LocalTargetInfo localTargetInfo = targetInfo;
                    Pawn pTarg = (Pawn)localTargetInfo.Thing;

                    if (RequestSession.HasOpenDealWith(pTarg.Faction))
                    {
                        Action takeOrderedJob = delegate ()
                        {
                            Job job = new Job(ItemRequestsDefOf.FulfillItemRequestWithFaction, pTarg);
                            job.playerForced = true;
                            pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                            PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.InteractingWithTraders, KnowledgeAmount.Total);
                        };

                        Thing thing = localTargetInfo.Thing;
                        string label = "Pay for requested items from " + pTarg.Faction.Name;
                        MenuOptionPriority priority = MenuOptionPriority.InitiateSocial;
                        opts.Add(new FloatMenuOption(label, takeOrderedJob, priority, null, thing));
                    }
                }
            }
        }
    }
}
