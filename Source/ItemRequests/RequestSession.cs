using System;
using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace ItemRequests
{
    public class RequestSession : WorldComponent
    {
        public RequestDeal deal;
        private List<RequestDeal> deals;
        private Faction faction;
        private Pawn negotiator;

        private Dictionary<RequestDeal, float> timeOfOccurences;
        private List<float> travelTimes;

        public RequestSession(World world) : base(world)
        {
            timeOfOccurences = new Dictionary<RequestDeal, float>();
        }

        private IEnumerable<RequestDeal> openDeals
        {
            get
            {
                try
                {
                    if (timeOfOccurences != null)
                    {
                        return timeOfOccurences.Keys;
                    }

                    Log.Warning("Trying to access request deals when they haven't been initialized! Fixing...");
                    timeOfOccurences = new Dictionary<RequestDeal, float>();
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
            success = true;
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
            if (openDealWith == null)
            {
                return float.MaxValue;
            }

            return timeOfOccurences[openDealWith];
        }

        public bool HasOpenDealWith(Faction dealFaction)
        {
            return GetOpenDealWith(dealFaction) != null;
        }

        public void CloseOpenDealWith(Faction dealFaction)
        {
            var openDealWith = GetOpenDealWith(dealFaction);
            if (openDealWith != null)
            {
                timeOfOccurences.Remove(openDealWith);
            }
        }

        public void CloseSession()
        {
            faction = null;
            negotiator = null;
            deal = null;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref timeOfOccurences, "timeOfOccurences", LookMode.Deep, LookMode.Value, ref deals,
                ref travelTimes);
            Scribe_References.Look(ref negotiator, "negotiator");
            Scribe_References.Look(ref faction, "setupFaction");
        }
    }

    public class RequestDeal : IExposable
    {
        private Faction faction;
        private Dictionary<ThingType, RequestedItemDict> requestedItems;
        private List<RequestedItemDict> requestedItemsDicts;

        private List<ThingType> thingTypes;

        public RequestDeal()
        {
            SetupRequestedItemsContainer();
        }

        public RequestDeal(Faction faction)
        {
            this.faction = faction;
            SetupRequestedItemsContainer();
        }

        public Faction Faction => faction;

        public float TotalRequestedValue
        {
            get
            {
                float val = 0;
                foreach (var dictionary in requestedItems.Values)
                {
                    foreach (var item in dictionary.dict.Values)
                    {
                        if (!item.removed)
                        {
                            val += item.pricePerItem * item.amount;
                        }
                    }
                }

                return val;
            }
        }

        public void ExposeData()
        {
            Scribe_References.Look(ref faction, "setupFaction");
            Scribe_Collections.Look(ref requestedItems, "requestedItems", LookMode.Value, LookMode.Deep, ref thingTypes,
                ref requestedItemsDicts);
        }

        private void SetupRequestedItemsContainer()
        {
            requestedItems = new Dictionary<ThingType, RequestedItemDict>();
            foreach (ThingType type in Enum.GetValues(typeof(ThingType)))
            {
                if (type == ThingType.Discard)
                {
                    continue;
                }

                requestedItems.Add(type, new RequestedItemDict());
            }
        }

        public List<RequestItem> GetRequestedItems()
        {
            var things = new List<RequestItem>();
            foreach (var type in requestedItems.Keys)
            {
                things.AddRange(requestedItems[type].dict.Values);
            }

            return things;
        }

        public int GetCountForItem(ThingType thingTypeFilter, Tradeable tradeable)
        {
            var key = tradeable.GetHashCode();
            if (requestedItems[thingTypeFilter].dict.ContainsKey(key))
            {
                return requestedItems[thingTypeFilter].dict[key].amount;
            }

            return 0;
        }

        public void AdjustItemRequest(ThingType thingTypeFilter, ThingEntry entry, int numRequested, float price)
        {
            var key = entry.tradeable.GetHashCode();
            if (requestedItems[thingTypeFilter].dict.ContainsKey(key))
            {
                var amount = Mathf.Max(numRequested, 0);
                if (amount == 0)
                {
                    //Log.Message("Requested: " + numRequested.ToString());
                    //Log.Message(requestedItems[thingTypeFilter].Count.ToString() + " items in current filter");
                    requestedItems[thingTypeFilter].dict.Remove(key);
                    //Log.Message("Colony just removed request for " + entry.tradeable.ThingDef.LabelCap);
                }
                else if (amount == requestedItems[thingTypeFilter].dict[key].amount)
                {
                }
                else
                {
                    requestedItems[thingTypeFilter].dict[key] = new RequestItem
                    {
                        item = entry,
                        amount = amount,
                        pricePerItem = price,
                        isPawn = entry.pawnDef != null
                    };
                    //Log.Message("Colony just adjusted request for " + entry.tradeable.ThingDef.LabelCap + " to " + numRequested);
                }
            }
            else if (numRequested > 0)
            {
                requestedItems[thingTypeFilter].dict[key] = new RequestItem
                {
                    item = entry,
                    amount = numRequested,
                    pricePerItem = price,
                    isPawn = entry.pawnDef != null
                };
                //Log.Message("Colony just requested " + entry.tradeable.ThingDef.LabelCap + " x" + numRequested + (entry.pawnDef != null ? " (" + entry.gender + ")" : ""));
            }
        }

        private class RequestedItemDict : IExposable
        {
            public Dictionary<int, RequestItem> dict = new();

            private List<int> ints;
            private List<RequestItem> items;

            public void ExposeData()
            {
                Scribe_Collections.Look(ref dict, "dict", LookMode.Value, LookMode.Deep, ref ints, ref items);
            }
        }
    }

    public class RequestItem : IExposable
    {
        public int amount;
        public bool isPawn;
        public ThingEntry item;
        public float pricePerItem;
        public bool removed;

        public void ExposeData()
        {
            Scribe_Deep.Look(ref item, "item");
            Scribe_Values.Look(ref amount, "amount");
            Scribe_Values.Look(ref pricePerItem, "pricePerItem");
            Scribe_Values.Look(ref isPawn, "isPawn");
            Scribe_Values.Look(ref removed, "removed");
        }
    }
}