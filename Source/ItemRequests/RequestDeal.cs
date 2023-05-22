using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace ItemRequests;

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

    public void AdjustItemRequest(ThingType thingTypeFilter, ThingEntry entry, int numRequested, float price)
    {
        var key = entry.tradeable.GetHashCode();
        if (requestedItems[thingTypeFilter].dict.ContainsKey(key))
        {
            var amount = Mathf.Max(numRequested, 0);
            if (amount == 0)
            {
                // Log.Message("Requested: " + numRequested.ToString());
                // Log.Message(requestedItems[thingTypeFilter].Count.ToString() + " items in current filter");
                requestedItems[thingTypeFilter].dict.Remove(key);

                // Log.Message("Colony just removed request for " + entry.tradeable.ThingDef.LabelCap);
            }
            else if (amount == requestedItems[thingTypeFilter].dict[key].amount)
            {
            }
            else
            {
                requestedItems[thingTypeFilter].dict[key] = new RequestItem
                    { item = entry, amount = amount, pricePerItem = price, isPawn = entry.pawnDef != null };

                // Log.Message("Colony just adjusted request for " + entry.tradeable.ThingDef.LabelCap + " to " + numRequested);
            }
        }
        else if (numRequested > 0)
        {
            requestedItems[thingTypeFilter].dict[key] = new RequestItem
                { item = entry, amount = numRequested, pricePerItem = price, isPawn = entry.pawnDef != null };

            // Log.Message("Colony just requested " + entry.tradeable.ThingDef.LabelCap + " x" + numRequested + (entry.pawnDef != null ? " (" + entry.gender + ")" : ""));
        }
    }

    public int GetCountForItem(ThingType thingTypeFilter, Tradeable tradeable)
    {
        var key = tradeable.GetHashCode();
        if (requestedItems[thingTypeFilter].dict.TryGetValue(key, out var value))
        {
            return value.amount;
        }

        return 0;
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

    private class RequestedItemDict : IExposable
    {
        public Dictionary<int, RequestItem> dict = new Dictionary<int, RequestItem>();

        private List<int> ints;

        private List<RequestItem> items;

        public void ExposeData()
        {
            Scribe_Collections.Look(ref dict, "dict", LookMode.Value, LookMode.Deep, ref ints, ref items);
        }
    }
}