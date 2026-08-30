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
        if (!requestedItems[thingTypeFilter].dict.ContainsKey(key))
        {
            if (numRequested > 0)
            {
                requestedItems[thingTypeFilter].dict[key] = new RequestItem
                    { item = entry, amount = numRequested, pricePerItem = price, isPawn = entry.pawnDef != null };
            }

            return;
        }

        var amount = Mathf.Max(numRequested, 0);
        if (amount == 0)
        {
            requestedItems[thingTypeFilter].dict.Remove(key);
            return;
        }

        if (amount != requestedItems[thingTypeFilter].dict[key].amount)
        {
            requestedItems[thingTypeFilter].dict[key] = new RequestItem
                { item = entry, amount = amount, pricePerItem = price, isPawn = entry.pawnDef != null };
        }
    }

    public int GetCountForItem(ThingType thingTypeFilter, Tradeable tradeable)
    {
        var key = tradeable.GetHashCode();
        return requestedItems[thingTypeFilter].dict.TryGetValue(key, out var value) ? value.amount : 0;
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

    private sealed class RequestedItemDict : IExposable
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