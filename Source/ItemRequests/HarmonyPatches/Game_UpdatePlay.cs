using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace ItemRequests.HarmonyPatches;

[HarmonyPatch(typeof(Game), nameof(Game.UpdatePlay))]
public static class Game_UpdatePlay
{
    private const int CheckIntervalTicks = 240;

    public static void Postfix()
    {
        ThingDatabase.Instance.LoadFrame();
        TryCancelRequestsWithLostDestination();
    }

    private static void TryCancelRequestsWithLostDestination()
    {
        if (Find.TickManager.TicksGame % CheckIntervalTicks != 0)
        {
            return;
        }

        var requestSession = Find.World.GetComponent<RequestSession>();
        if (requestSession == null)
        {
            return;
        }

        var dealsToCancel = requestSession.openDeals
            .Where(deal => deal?.Faction != null)
            .Where(deal =>
            {
                var destinationTile = requestSession.GetDestinationTileWithFaction(deal.Faction);
                return destinationTile >= 0 && !PlayerHasAccessToTile(destinationTile);
            })
            .Select(deal => deal.Faction)
            .ToList();

        foreach (var faction in dealsToCancel)
        {
            requestSession.CloseOpenDealWith(faction);
            Messages.Message("IR.RequestSession.RequestCancelledLostTile".Translate(faction.Name),
                MessageTypeDefOf.NeutralEvent, false);
        }
    }

    private static bool PlayerHasAccessToTile(int tile)
    {
        if (tile < 0)
        {
            return true;
        }

        var mapParent = Find.WorldObjects.MapParentAt(tile);
        if (mapParent == null)
        {
            return Find.WorldObjects.Caravans.Any(caravan => caravan is { Faction: not null } &&
                                                             caravan.Faction == Faction.OfPlayer &&
                                                             caravan.Tile == tile);
        }

        if (mapParent.Faction == Faction.OfPlayer && mapParent.HasMap)
        {
            return true;
        }

        if (mapParent.Map is { IsPlayerHome: true })
        {
            return true;
        }

        return Find.WorldObjects.Caravans.Any(caravan => caravan is { Faction: not null } &&
                                                         caravan.Faction == Faction.OfPlayer &&
                                                         caravan.Tile == tile);
    }
}