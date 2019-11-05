using System;
using System.Collections.Generic;
using UnityEngine;
using RimWorld;
using Verse;

namespace ItemRequests
{
    public class RequestSession : IExposable
    {
        public static Pawn negotiator  = null;
        public static Faction faction  = null;
        public static RequestDeal deal = null;
        public static List<RequestDeal> openDeals;

        public static void SetupWith(Faction faction, Pawn playerNegotiator, out bool success)
        {
            if (HasOpenDealWith(faction))
            {
                Messages.Message("IR.RequestSession.CannotRequestAgainYet".Translate(faction.Name),
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
            if (openDeals == null) openDeals = new List<RequestDeal>();
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

        public void ExposeData()
        {
            Log.Message("scribe requestsession");
            Scribe_Collections.Look(ref openDeals, "openDeals", LookMode.Deep);
        }
    }

    public class RequestDeal : IExposable
    {
        public Faction Faction { get { return faction; } }
        private Faction faction;
        private Dictionary<ThingType, RequestedItemDict> requestedItems;

        private List<ThingType> thingTypes;
        private List<RequestedItemDict> requestedItemsDicts;

        public float TotalRequestedValue
        {
            get
            {
                float val = 0;
                foreach (var dictionary in requestedItems.Values)
                {
                    foreach (RequestItem item in dictionary.dict.Values)
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
            this.faction = faction;
            requestedItems = new Dictionary<ThingType, RequestedItemDict>();

            foreach (ThingType type in Enum.GetValues(typeof(ThingType)))
            {
                if (type == ThingType.Discard) continue;
                requestedItems.Add(type, new RequestedItemDict());
            }
        }

        public List<RequestItem> GetRequestedItems()
        {
            List<RequestItem> things = new List<RequestItem>();
            foreach (ThingType type in requestedItems.Keys)
            {
                things.AddRange(requestedItems[type].dict.Values);
            }
            return things;
        }

        public int GetCountForItem(ThingType thingTypeFilter, Tradeable tradeable)
        {
            int key = tradeable.GetHashCode();
            if (requestedItems[thingTypeFilter].dict.ContainsKey(key))
            {
                return requestedItems[thingTypeFilter].dict[key].amount;
            }
            return 0;
        }

        public void AdjustItemRequest(ThingType thingTypeFilter, ThingEntry entry, int numRequested, float price)
        {
            int key = entry.tradeable.GetHashCode();
            if (requestedItems[thingTypeFilter].dict.ContainsKey(key))
            {
                int amount = Mathf.Max(numRequested, 0);
                if (amount == 0)
                {
                    //Log.Message("Requested: " + numRequested.ToString());
                    //Log.Message(requestedItems[thingTypeFilter].Count.ToString() + " items in current filter");
                    requestedItems[thingTypeFilter].dict.Remove(key);
                    //Log.Message("Colony just removed request for " + entry.tradeable.ThingDef.LabelCap);
                }
                else if (amount == requestedItems[thingTypeFilter].dict[key].amount)
                {
                    return;
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

        public void ExposeData()
        {
            Log.Message("Scribe request deal");
            Scribe_References.Look(ref faction, "faction");
            Scribe_Collections.Look(ref requestedItems, "requestedItems", LookMode.Value, LookMode.Deep, ref thingTypes, ref requestedItemsDicts);            
        }

        private class RequestedItemDict : IExposable
        {
            public Dictionary<int, RequestItem> dict = new Dictionary<int, RequestItem>();

            private List<int> ints;
            private List<RequestItem> items;

            public void ExposeData()
            {
                Log.Message("Scribing requesteditemdict");
                Scribe_Collections.Look(ref dict, "dict", LookMode.Value, LookMode.Deep, ref ints, ref items);
            }
        }
    }

    public class RequestItem : IExposable
    {
        public ThingEntry item;
        public int amount;
        public float pricePerItem;
        public bool isPawn = false;
        public bool removed = false;

        public void ExposeData()
        {
            Scribe_Deep.Look(ref item, "item");
            Scribe_Values.Look(ref amount, "amount", 0);
            Scribe_Values.Look(ref pricePerItem, "pricePerItem", 0);
            Scribe_Values.Look(ref isPawn, "isPawn", false);
            Scribe_Values.Look(ref removed, "removed", false);
        }
    }
}
