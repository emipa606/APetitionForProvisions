using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace ItemRequests.HarmonyPatches;

public class FloatMenuOptionProvider_FulfillRequest : FloatMenuOptionProvider
{
    protected override bool Drafted => true;

    protected override bool Undrafted => true;

    protected override bool Multiselect => false;

    public override IEnumerable<FloatMenuOption> GetOptionsFor(Pawn clickedPawn, FloatMenuContext context)
    {
        if (clickedPawn?.Faction == null || !((ITrader)clickedPawn).CanTradeNow)
        {
            yield break;
        }

        if (clickedPawn.GetTraderCaravanRole() != TraderCaravanRole.Trader)
        {
            yield break;
        }

        var requestSession = Find.World.GetComponent<RequestSession>();
        if (requestSession == null || !requestSession.HasOpenDealWith(clickedPawn.Faction))
        {
            yield break;
        }

        if (Find.TickManager.TicksGame < requestSession.GetTimeOfOccurenceWithFaction(clickedPawn.Faction))
        {
            yield break;
        }

        if (!context.FirstSelectedPawn.CanReach(clickedPawn, PathEndMode.OnCell, Danger.Deadly))
        {
            yield return new FloatMenuOption("CannotTrade".Translate() + ": " + "NoPath".Translate().CapitalizeFirst(),
                null);
            yield break;
        }

        var reservedBy =
            context.FirstSelectedPawn.Map.reservationManager.FirstRespectedReserver(clickedPawn,
                context.FirstSelectedPawn);
        if (reservedBy != null)
        {
            yield return new FloatMenuOption(
                "CannotTrade".Translate() + ": " +
                "IR.FulfillRequestFloatMenuOption.ReservedBy".Translate(reservedBy.LabelShort),
                null);
            yield break;
        }

        if (context.FirstSelectedPawn.skills.GetSkill(SkillDefOf.Social).TotallyDisabled)
        {
            yield return new FloatMenuOption("CannotPrioritizeWorkTypeDisabled".Translate(SkillDefOf.Social.LabelCap),
                null);
            yield break;
        }

        yield return FloatMenuUtility.DecoratePrioritizedTask(
            new FloatMenuOption(
                "IR.FulfillRequestFloatMenuOption.MenuText".Translate(clickedPawn.Faction.Name), takeOrderedJob,
                MenuOptionPriority.InitiateSocial, null, clickedPawn), context.FirstSelectedPawn, clickedPawn);
        yield break;

        void takeOrderedJob()
        {
            var job = new Job(ItemRequestsDefOf.FulfillItemRequestWithFaction, clickedPawn) { playerForced = true };
            context.FirstSelectedPawn.jobs.TryTakeOrderedJob(job);
            PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.InteractingWithTraders, KnowledgeAmount.Total);
        }
    }
}