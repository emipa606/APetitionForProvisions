using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        public float TotalRequestedValue { get; private set; }
        private List<Tradeable> requestedItems;
        public RequestDeal()
        {
            TotalRequestedValue = 0;
            requestedItems = new List<Tradeable>();
        }

        public void Reset()
        {
            TotalRequestedValue = 0;
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
            foreach (Tradeable tradeable in requestedItems)
            {
                if (tradeable.ActionToDo != TradeAction.None)
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
    
        public void AddRequestedItem(Tradeable tradeable, float forPrice)
        {
            requestedItems.Add(tradeable);
            TotalRequestedValue += forPrice;
        }
    }
}
