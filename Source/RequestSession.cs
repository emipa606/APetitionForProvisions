using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        public static List<RequestDeal> openDeals;

        public static void SetupWith(Faction faction, Pawn playerNegotiator)
        {
            if (HasOpenDealWith(faction))
            {
                Messages.Message("You can't request more from " + faction.Name + " until they've satisfied your previous request.", 
                    MessageTypeDefOf.CautionInput, false);
                return;
            }

            RequestSession.faction = faction;
            negotiator = playerNegotiator;            
            deal = new RequestDeal(faction);
            openDeals.Add(deal);
        }

        public static RequestDeal GetOpenDealWith(Faction faction)
        {
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
                if (openDeals[i].Faction.randomKey == faction.randomKey)
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
                        val += item.price * item.amount;
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
                requestedItems.Add(type, new Dictionary<int, RequestItem>());
            }
        }

        public void Reset()
        {
            foreach (var value in requestedItems.Values)
            {
                value.Clear();
            }
        }

        public bool TryExecute(int colonySilver, out bool requestSucceeded)
        {
            requestSucceeded = true;



            // TODO: need to store the tradeable item
            // in some place for the caravan to bring
            // later on


            // Should have this part when the actual caravan arrives
            //if (RequestSession.faction != null)
            //{
            //    // Maybe shouldn't have this part
            //    RequestSession.faction.Notify_PlayerTraded(num, RequestSession.negotiator);
            //}

            //Pawn pawn = RequestSession.negotiator as Pawn;
            //if (pawn != null)
            //{
            //    TaleRecorder.RecordTale(TaleDefOf.TradedWith, new object[]
            //    {
            //        RequestSession.faction,
            //        pawn
            //    });
            //}

            RequestSession.negotiator.mindState.inspirationHandler.EndInspiration(InspirationDefOf.Inspired_Trade);
            return true;
        }

        public void AdjustRequestedItem(ThingType thingTypeFilter, int indexKey, Tradeable tradeable, int numRequested, float price)
        {
            if (requestedItems[thingTypeFilter].ContainsKey(indexKey))
            {
                int amount = Mathf.Max(numRequested, 0);
                if (amount == 0)
                {
                    requestedItems[thingTypeFilter].Remove(indexKey);
                    Log.Message("Colony just removed request for " + tradeable.ThingDef.LabelCap);
                }
                else if (amount == requestedItems[thingTypeFilter][indexKey].amount)
                {
                    return;
                }
                else
                {
                    requestedItems[thingTypeFilter][indexKey] = new RequestItem
                    {
                        item = tradeable,
                        amount = amount,
                        price = price
                    };
                    Log.Message("Colony just adjusted request for " + tradeable.ThingDef.LabelCap + " to " + numRequested + " for " + price.ToStringMoney("F2") + " each");
                }
            }
            else if (numRequested > 0)
            {
                requestedItems[thingTypeFilter][indexKey] = new RequestItem
                {
                    item = tradeable,
                    amount = numRequested,
                    price = price
                };
                Log.Message("Colony just requested " + tradeable.ThingDef.LabelCap + " x" + numRequested + " for " + price.ToStringMoney("F2") + " each");
            }
        }

        private class RequestItem
        {
            public Tradeable item;
            public int amount;
            public float price;
        }
    }
}
