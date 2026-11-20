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
        var dictionary = requestedItems[thingTypeFilter].dict;
        var existingKey = FindItemKey(dictionary, entry);
        var amount = Mathf.Max(numRequested, 0);
        if (existingKey == null)
        {
            if (amount > 0)
            {
                var key = 0;
                while (dictionary.ContainsKey(key))
                {
                    key++;
                }

                dictionary.Add(key, new RequestItem
                    { item = entry, amount = amount, pricePerItem = price, isPawn = entry.pawnDef != null });
            }

            return;
        }

        if (amount == 0)
        {
            dictionary.Remove(existingKey.Value);
        }
        else if (amount != dictionary[existingKey.Value].amount)
        {
            dictionary[existingKey.Value] = new RequestItem
                { item = entry, amount = amount, pricePerItem = price, isPawn = entry.pawnDef != null };
        }
    }

    public int GetCountForItem(ThingType thingTypeFilter, ThingEntry entry)
    {
        var dictionary = requestedItems[thingTypeFilter].dict;
        var key = FindItemKey(dictionary, entry);
        return key.HasValue ? dictionary[key.Value].amount : 0;
    }

    private static int? FindItemKey(Dictionary<int, RequestItem> dictionary, ThingEntry entry)
    {
        foreach (var pair in dictionary)
        {
            if (pair.Value?.item != null && pair.Value.item.ThingKey.Equals(entry.ThingKey))
            {
                return pair.Key;
            }
        }

        return null;
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