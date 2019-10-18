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

        public static void SetupWith(Faction faction, Pawn playerNegotiator)
        {
            RequestSession.faction = faction;
            RequestSession.negotiator = playerNegotiator;
            RequestSession.deal = new RequestDeal();
        }

        public static void Close()
        {
            RequestSession.faction = null;
            RequestSession.negotiator = null;
            RequestSession.deal = null;
        }
    }

    public class RequestDeal
    {
        // TODO: This may need to be looked at once filters are implemented
        private Dictionary<int, RequestItem> requestedItems;
        public float TotalRequestedValue
        {
            get
            {
                float val = 0;
                foreach (RequestItem item in requestedItems.Values)
                {
                    val += item.price * item.amount;
                }
                return val;
            }
        }
        public RequestDeal()
        {
            requestedItems = new Dictionary<int, RequestItem>();
        }

        public void Reset()
        {
            requestedItems.Clear();
        }

        public bool TryExecute(int colonySilver, out bool requestSucceeded)
        {
            if (colonySilver < TotalRequestedValue)
            {
                // Show window asking if colony really wants to request
                // items with less silver than needed
                Messages.Message("The colony does not currently have enough silver to pay for requested items", MessageTypeDefOf.CautionInput, false);

                /*
                Find.WindowStack.WindowOfType<Dialog_Trade>().FlashSilver();
                Messages.Message("MessageColonyCannotAfford".Translate(), MessageTypeDefOf.RejectInput, false);
                actuallyTraded = false;
                return false;
                */
            }

            requestSucceeded = false;
            float num = 0f;
            foreach (RequestItem item in requestedItems.Values)
            {
                if (item.tradeable.ActionToDo != TradeAction.None)
                {
                    requestSucceeded = true;
                }

                // TODO: need to store the tradeable item
                // in some place for the caravan to bring
                // later on
            }

            if (RequestSession.faction != null)
            {
                // Maybe shouldn't have this part
                RequestSession.faction.Notify_PlayerTraded(num, RequestSession.negotiator);
            }

            Pawn pawn = RequestSession.negotiator as Pawn;
            if (pawn != null)
            {
                TaleRecorder.RecordTale(TaleDefOf.TradedWith, new object[]
                {
                    RequestSession.faction,
                    pawn
                });
            }

            RequestSession.negotiator.mindState.inspirationHandler.EndInspiration(InspirationDefOf.Inspired_Trade);
            return true;
        }

        public void AdjustRequestedItem(int indexKey, Tradeable tradeable, int numRequested, float price)
        {
            if (requestedItems.ContainsKey(indexKey))
            {
                int amount = Mathf.Max(numRequested, 0);
                if (amount == 0)
                {
                    requestedItems.Remove(indexKey);
                    Log.Message("Colony just removed request for " + tradeable.ThingDef.LabelCap);
                }
                else if (amount == requestedItems[indexKey].amount)
                {
                    return;
                }
                else
                {
                    requestedItems[indexKey] = new RequestItem
                    {
                        tradeable = tradeable,
                        amount = amount,
                        price = price
                    };
                    Log.Message("Colony just adjusted request for " + tradeable.ThingDef.LabelCap + " to " + numRequested + " for " + price.ToStringMoney("F2") + " each");
                }
            }
            else if (numRequested > 0)
            {
                requestedItems[indexKey] = new RequestItem
                {
                    tradeable = tradeable,
                    amount = numRequested,
                    price = price
                };
                Log.Message("Colony just requested " + tradeable.ThingDef.LabelCap + " x" + numRequested + " for " + price.ToStringMoney("F2") + " each");
            }
        }

        private class RequestItem
        {
            public Tradeable tradeable;
            public int amount;
            public float price;
        }
    }
}
