using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace ItemRequests;

public class RequestSession(World world) : WorldComponent(world)
{
    public RequestDeal deal;

    private List<RequestDeal> deals;

    private List<RequestDeal> destinationTileDeals;

    private Dictionary<RequestDeal, int> destinationTiles = new();

    private List<int> destinationTileValues;

    private Faction faction;

    private Pawn negotiator;

    private Dictionary<RequestDeal, float> timeOfOccurences = new();

    private List<float> travelTimes;

    public IEnumerable<RequestDeal> openDeals
    {
        get
        {
            try
            {
                if (timeOfOccurences != null)
                {
                    destinationTiles ??= new Dictionary<RequestDeal, int>();
                    return timeOfOccurences.Keys;
                }

                Log.Warning("Trying to access request deals when they haven't been initialized! Fixing...");
                timeOfOccurences = new Dictionary<RequestDeal, float>();
                destinationTiles ??= new Dictionary<RequestDeal, int>();
                return timeOfOccurences.Keys;
            }
            catch
            {
                Log.ErrorOnce(
                    "Unable to access any existing deals with factions. It's possible the last time this game was saved a different version of this mod was running. Try saving and quitting, then coming back.",
                    "no_open_deals".GetHashCode());
                return new List<RequestDeal>();
            }
        }
    }

    public void CloseOpenDealWith(Faction dealFaction)
    {
        var openDealWith = GetOpenDealWith(dealFaction);
        if (openDealWith == null)
        {
            return;
        }

        timeOfOccurences.Remove(openDealWith);
        destinationTiles?.Remove(openDealWith);
    }

    public void CloseSession()
    {
        if (GetTimeOfOccurenceWithFaction(faction) == float.MaxValue)
        {
            CloseOpenDealWith(faction);
        }

        faction = null;
        negotiator = null;
        deal = null;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref timeOfOccurences, "timeOfOccurences", LookMode.Deep, LookMode.Value, ref deals,
            ref travelTimes);
        Scribe_Collections.Look(ref destinationTiles, "destinationTiles", LookMode.Deep, LookMode.Value,
            ref destinationTileDeals, ref destinationTileValues);
        Scribe_References.Look(ref negotiator, "negotiator");
        Scribe_References.Look(ref faction, "setupFaction");
    }

    public int GetDestinationTileWithFaction(Faction dealFaction)
    {
        var openDealWith = GetOpenDealWith(dealFaction);
        if (openDealWith == null || destinationTiles == null)
        {
            return -1;
        }

        return destinationTiles.GetValueOrDefault(openDealWith, -1);
    }

    public RequestDeal GetOpenDealWith(Faction dealFaction)
    {
        if (dealFaction == null)
        {
            return null;
        }

        foreach (var openDeal in openDeals)
        {
            if (openDeal == null)
            {
                continue;
            }

            if (openDeal.Faction.randomKey == dealFaction.randomKey)
            {
                return openDeal;
            }
        }

        return null;
    }

    public float GetTimeOfOccurenceWithFaction(Faction occuranceFaction)
    {
        var openDealWith = GetOpenDealWith(occuranceFaction);
        return openDealWith == null ? float.MaxValue : timeOfOccurences[openDealWith];
    }

    public bool HasOpenDealWith(Faction dealFaction)
    {
        return GetOpenDealWith(dealFaction) != null;
    }

    public void SetDestinationTile(Faction dealFaction, int tile)
    {
        var openDealWith = GetOpenDealWith(dealFaction);
        if (openDealWith == null)
        {
            Log.Warning(
                "Trying to set destination tile for requested setupFaction arrival, but no open deal with setupFaction exists!");
            return;
        }

        destinationTiles ??= new Dictionary<RequestDeal, int>();
        destinationTiles[openDealWith] = tile;
    }

    public void SetTimeOfOccurence(Faction occuranceFaction, float time)
    {
        var openDealWith = GetOpenDealWith(occuranceFaction);
        if (openDealWith == null)
        {
            Log.Warning(
                "Trying to set time of arrival for requested setupFaction arrival, but no open deal with setupFaction exists!");
            return;
        }

        timeOfOccurences[openDealWith] = time;
    }

    public void SetupWith(Faction setupFaction, Pawn playerNegotiator, out bool success)
    {
        if (HasOpenDealWith(setupFaction))
        {
            Messages.Message("IR.RequestSession.CannotRequestAgainYet".Translate(setupFaction.Name),
                MessageTypeDefOf.CautionInput, false);
            success = false;
            return;
        }

        faction = setupFaction;
        negotiator = playerNegotiator;
        deal = new RequestDeal(setupFaction);
        timeOfOccurences.Add(deal, float.MaxValue);
        destinationTiles ??= new Dictionary<RequestDeal, int>();
        destinationTiles[deal] = -1;
        success = true;
    }
}