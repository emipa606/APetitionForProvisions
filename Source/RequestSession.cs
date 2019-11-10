using System;
using System.Collections.Generic;
using UnityEngine;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace ItemRequests
{
    public class RequestSession : WorldComponent
    {
        public Pawn negotiator  = null;
        public Faction faction  = null;
        public RequestDeal deal = null;
        public List<RequestDeal> openDeals;


        public RequestSession(World world) : base(world) 
        {
            openDeals = new List<RequestDeal>();
        }

        public void SetupWith(Faction faction, Pawn playerNegotiator, out bool success)
        {
            if (HasOpenDealWith(faction))
            {
                Messages.Message("IR.RequestSession.CannotRequestAgainYet".Translate(faction.Name),
                    MessageTypeDefOf.CautionInput, false);
                success = false;
                return;
            }

            this.faction = faction;
            negotiator = playerNegotiator;
            deal = new RequestDeal(faction);
            openDeals.Add(deal);
            success = true;
        }

        public RequestDeal GetOpenDealWith(Faction faction)
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

        public bool HasOpenDealWith(Faction faction)
        {
            return GetOpenDealWith(faction) != null;
        }

        public void CloseOpenDealWith(Faction faction)
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

        public void CloseSession()
        {
            faction = null;
            negotiator = null;
            deal = null;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref openDeals, "openDeals", LookMode.Deep);
            Scribe_References.Look(ref negotiator, "negotiator");
            Scribe_References.Look(ref faction, "faction");            
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

        public RequestDeal()
        {
            SetupRequestedItemsContainer();
        }

        public RequestDeal(Faction faction)
        {
            this.faction = faction;
            SetupRequestedItemsContainer();
        }

        private void SetupRequestedItemsContainer()
        {
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
