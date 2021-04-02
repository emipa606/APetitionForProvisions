using HarmonyLib;
using RimWorld;
using Verse;

namespace ItemRequests
{
    [HarmonyPatch(typeof(FactionDialogMaker))]
    [HarmonyPatch("FactionDialogFor")]
    public static class DialogWindow
    {
        [HarmonyPostfix]
        public static void AddOption(DiaNode __instance, ref DiaNode __result, Pawn negotiator, Faction faction)
        {
            if (faction.PlayerRelationKind != FactionRelationKind.Ally &&
                faction.PlayerRelationKind != FactionRelationKind.Neutral)
            {
                return;
            }

            var map = negotiator.Map;
            var newOption = RequestItemOption(map, faction, negotiator);

            // If there's a third option for requesting the AI Persona Core
            // then put it after that. Otherwise put it after first two
            // options (Request caravan & request military aid).
            var insertAtIndex = DefDatabase<ResearchProjectDef>.AllDefsListForReading.Any(rp =>
                rp.HasTag(ResearchProjectTagDefOf.ShipRelated) && rp.IsFinished)
                ? 3
                : 2;

            __result.options.Insert(insertAtIndex, newOption);
        }

        private static DiaOption RequestItemOption(Map map, Faction faction, Pawn negotiator)
        {
            string text = "IR.DialogWindow.RequestItems".Translate();
            var sociallyInept = negotiator.skills.GetSkill(SkillDefOf.Social).TotallyDisabled;

            if (sociallyInept)
            {
                var noSocial = new DiaOption(text);
                noSocial.Disable("WorkTypeDisablesOption".Translate(SkillDefOf.Social.LabelCap));
                return noSocial;
            }

            // Can't request more items from same faction
            // until x number of ticks have passed.
            var num = faction.lastTraderRequestTick + 240000 - Find.TickManager.TicksGame;
            if (num > 0)
            {
                var mustWaitOption = new DiaOption(text);
                mustWaitOption.Disable("WaitTime".Translate(num.ToStringTicksToPeriod()));
                return mustWaitOption;
            }

            if (faction == Faction.Empire)
            {
                var pawnHasRequiredTitle = false;
                var traderKind = faction.def.caravanTraderKinds[0];
                if (negotiator.royalty != null &&
                    negotiator.royalty.HasPermit(traderKind.permitRequiredForTrading, faction))
                {
                    pawnHasRequiredTitle = true;
                }

                //Find.ColonistBar.Entries.ForEach((ColonistBar.Entry entry) => {
                //    Pawn p = entry.pawn;
                //    if (p.royalty != null && p.royalty.HasPermit(traderKind.permitRequiredForTrading, faction))
                //    {
                //        hasPawnWithRequiredTitle = true;                        
                //        return;
                //    }
                //});

                if (!pawnHasRequiredTitle)
                {
                    var noTitle = new DiaOption(text);
                    var noTitleMessage = "CannotTradeMissingTitleAbility".Translate();
                    noTitle.Disable(noTitleMessage.RawText.Substring(noTitleMessage.RawText.IndexOf(':') + 1).Trim());
                    return noTitle;
                }
            }

            var tradeAcceptedOption = new DiaOption(text)
            {
                action = () =>
                {
                    Find.World.GetComponent<RequestSession>().SetupWith(faction, negotiator, out var success);
                    if (success)
                    {
                        Find.WindowStack.Add(new ItemRequestWindow(map, faction, negotiator));
                    }
                }
            };


            return tradeAcceptedOption;
        }
    }
}