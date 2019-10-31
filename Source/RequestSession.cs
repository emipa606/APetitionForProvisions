using System;
using System.Collections.Generic;
using UnityEngine;
using RimWorld;
using Verse;

namespace ItemRequests
{
    public static class RequestSession
    {
        public static Pawn negotiator;
        public static Faction faction;
        public static RequestDeal deal;
        public static List<RequestDeal> openDeals = new List<RequestDeal>();

        public static void SetupWith(Faction faction, Pawn playerNegotiator, out bool success)
        {
            if (HasOpenDealWith(faction))
            {
                Messages.Message("You can't request more items from " + faction.Name + " until they've satisfied your previous request.",
                    MessageTypeDefOf.CautionInput, false);
                success = false;
                return;
            }

            RequestSession.faction = faction;
            negotiator = playerNegotiator;
            deal = new RequestDeal(faction);
            openDeals.Add(deal);
            success = true;
        }

        public static RequestDeal GetOpenDealWith(Faction faction)
        {
            if (faction == null) return null;
            foreach (RequestDeal openDeal in openDeals)
            {
                if (openDeal.Faction.randomKey == faction.randomKey)
                {
                    return openDeal;
                }
            }
            return null;
        }

        public static bool HasOpenDealWith(Faction faction)
        {
            return GetOpenDealWith(faction) != null;
        }

        public static void CloseOpenDealWith(Faction faction)
        {
            for (int i = 0; i < openDeals.Count; ++i)
            {
                if (openDeals[i].Faction.Name == faction.Name)
                {
                    openDeals.RemoveAt(i);
                    return;
                }
            }
        }

        public static void CloseSession()
        {
            faction = null;
            negotiator = null;
            deal = null;
        }
    }

    public class RequestDeal
    {
        public Faction Faction { get; private set; }
        private Dictionary<ThingType, Dictionary<int, RequestItem>> requestedItems;

        public float TotalRequestedValue
        {
            get
            {
                float val = 0;
                foreach (var dictionary in requestedItems.Values)
                {
                    foreach (RequestItem item in dictionary.Values)
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
        public RequestDeal(Faction faction)
        {
            Faction = faction;
            requestedItems = new Dictionary<ThingType, Dictionary<int, RequestItem>>();

            foreach (ThingType type in Enum.GetValues(typeof(ThingType)))
            {
                if (type == ThingType.Discard) continue;
                requestedItems.Add(type, new Dictionary<int, RequestItem>());
            }
        }

        public List<RequestItem> GetRequestedItems()
        {
            List<RequestItem> things = new List<RequestItem>();
            foreach (ThingType type in requestedItems.Keys)
            {
                things.AddRange(requestedItems[type].Values);
            }
            return things;
        }

        public int GetCountForItem(ThingType thingTypeFilter, Tradeable tradeable)
        {
            int key = tradeable.GetHashCode();
            if (requestedItems[thingTypeFilter].ContainsKey(key))
            {
                return requestedItems[thingTypeFilter][key].amount;
            }
            return 0;
        }

        public void Reset()
        {
            foreach (var value in requestedItems.Values)
            {
                value.Clear();
            }
        }

        public void AdjustItemRequest(ThingType thingTypeFilter, ThingEntry entry, int numRequested, float price)
        {
            int key = entry.tradeable.GetHashCode();
            if (requestedItems[thingTypeFilter].ContainsKey(key))
            {
                int amount = Mathf.Max(numRequested, 0);
                if (amount == 0)
                {
                    //Log.Message("Requested: " + numRequested.ToString());
                    //Log.Message(requestedItems[thingTypeFilter].Count.ToString() + " items in current filter");
                    requestedItems[thingTypeFilter].Remove(key);
                    //Log.Message("Colony just removed request for " + entry.tradeable.ThingDef.LabelCap);
                }
                else if (amount == requestedItems[thingTypeFilter][key].amount)
                {
                    return;
                }
                else
                {
                    requestedItems[thingTypeFilter][key] = new RequestItem
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
                requestedItems[thingTypeFilter][key] = new RequestItem
                {
                    item = entry,
                    amount = numRequested,
                    pricePerItem = price,
                    isPawn = entry.pawnDef != null
                };
                //Log.Message("Colony just requested " + entry.tradeable.ThingDef.LabelCap + " x" + numRequested + (entry.pawnDef != null ? " (" + entry.gender + ")" : ""));
            }
        }
    }

    public class RequestItem
    {
        public ThingEntry item;
        public int amount;
        public float pricePerItem;
        public bool isPawn = false;
        public bool removed = false;
    }
}
