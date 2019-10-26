using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace ItemRequests
{
    class FulfillItemRequestWindow : Window
    {
        private Pawn playerPawn;
        private Pawn traderPawn;
        private Vector2 scrollPosition = Vector2.zero;
        private List<RequestItem> requestedItems;
        private List<Thing> colonySilver = new List<Thing>();
        private const float offsetFromRight = 100;
        private const float offsetFromBottom = 90;

        public FulfillItemRequestWindow(Pawn playerPawn, Pawn traderPawn)
        {
            this.playerPawn = playerPawn;
            this.traderPawn = traderPawn;
            this.requestedItems = RequestSession.GetOpenDealWith(traderFaction).GetRequestedItems();
        }

        private Faction traderFaction => traderPawn.Faction;

        public override Vector2 InitialSize => new Vector2(500, 700);

        public override void DoWindowContents(Rect inRect)
        {
            Vector2 contentMargin = new Vector2(12, 18);
            string title = "Review Requested Items";
            string closeString = "Trade";

            // Begin Window group
            GUI.BeginGroup(inRect);

            // Draw the names of negotiator and factions
            inRect = inRect.AtZero();
            float x = contentMargin.x;
            float headerRowHeight = 35f;
            Rect headerRowRect = new Rect(x, contentMargin.y, inRect.width - x, headerRowHeight);
            Rect titleArea = new Rect(x, 0, headerRowRect.width, headerRowRect.height);
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Medium;
            Widgets.Label(titleArea, title);

            Text.Font = GameFont.Small;
            float constScrollbarSize = 16;
            float rowHeight = 30;
            float cumulativeContentHeight = 6f + requestedItems.Count * rowHeight;
            Rect mainRect = new Rect(x, headerRowRect.y + headerRowRect.height + 10, inRect.width - x * 2, inRect.height - offsetFromBottom - headerRowRect.y - headerRowRect.height);
            Rect scrollRect = new Rect(mainRect.x, 0, mainRect.width - constScrollbarSize, cumulativeContentHeight);
            float bottom = scrollPosition.y - 30f;
            float top = scrollPosition.y + mainRect.height;
            float y = 6f;
            int counter;

            Widgets.BeginScrollView(mainRect, ref scrollPosition, scrollRect, true);

            for (int i = 0; i < requestedItems.Count; i++)
            {
                counter = i;
                if (y > bottom && y < top)
                {
                    Rect rect = new Rect(mainRect.x, y, scrollRect.width, 30f);
                    DrawRequestedItem(rect, requestedItems[i], counter);
                }
                y += 30f;
            }

            Widgets.EndScrollView();

            float horizontalLineY = inRect.height - offsetFromBottom;
            Widgets.DrawLineHorizontal(x, horizontalLineY, inRect.width - contentMargin.x * 2);

            // Draw total
            Text.Anchor = TextAnchor.MiddleRight;
            Rect totalStringRect = new Rect(scrollRect.width - offsetFromRight - 150, horizontalLineY, 140, rowHeight);
            Widgets.Label(totalStringRect, "Total");            
            Widgets.DrawLineVertical(scrollRect.width - offsetFromRight, horizontalLineY, rowHeight);
            Rect totalPriceRect = new Rect(scrollRect.width - offsetFromRight, horizontalLineY, offsetFromRight - 10, rowHeight);
            Widgets.Label(totalPriceRect, RequestSession.GetOpenDealWith(traderFaction).TotalRequestedValue.ToStringMoney("F2"));
                        
            Text.Anchor = TextAnchor.MiddleLeft;
            Rect closeButtonArea = new Rect(x, inRect.height - contentMargin.y * 2, 100, 50);
            if (Widgets.ButtonText(closeButtonArea, closeString, false))
            {
                CloseButtonPressed();
            }

            GenUI.ResetLabelAlign();
            GUI.EndGroup();
        }

        private void DrawRequestedItem(Rect rowRect, RequestItem requested, int index)
        {
            Text.Font = GameFont.Small;
            float price = requested.pricePerItem * requested.amount;
            if (index % 2 == 1)
            {
                Widgets.DrawLightHighlight(rowRect);
            }

            GUI.BeginGroup(rowRect);


            // Draw item icon
            float x = 0;
            float iconSize = 27;
            Text.Anchor = TextAnchor.MiddleLeft;
            Rect iconArea = new Rect(x, 0, iconSize, iconSize);

            // TODO: Doesn't draw the item with correct material
            Widgets.ThingIcon(iconArea, requested.item.FirstThingTrader);

            x += iconSize + (iconSize / 4);

            Rect itemNameArea = new Rect(x, 0, rowRect.width - offsetFromRight - x, rowRect.height);
            string itemTitle = requested.item.AnyThing.LabelCapNoCount + " x" + requested.amount;
            Widgets.Label(itemNameArea, itemTitle);

            x = rowRect.width - offsetFromRight;
            Widgets.DrawLineVertical(x, 0, rowRect.height);

            x += 10;
            Text.Anchor = TextAnchor.MiddleRight;
            Rect itemPriceArea = new Rect(x, 0, offsetFromRight - 10, rowRect.height);
            Widgets.Label(itemPriceArea, price.ToStringMoney("F2"));

            GUI.EndGroup();
        }
    
        private void CloseButtonPressed()
        {
            Close(true);            

            float totalRequestedValue = RequestSession.GetOpenDealWith(traderFaction).TotalRequestedValue;
            if (playerPawn.Map.resourceCounter.Silver < totalRequestedValue)
            {
                Messages.Message("Colony doesn't have enough silver to pay for the items!", MessageTypeDefOf.NegativeEvent, true);
                Lord lord = traderPawn.GetLord();
                lord.ReceiveMemo(LordJob_FulfillItemRequest.MemoOnUnfulfilled);
            }
            else
            {
                ITrader trader = traderPawn as ITrader;             
                if (trader == null)
                {
                    Log.Error("Trader pawn unable to be cast to ITrader!");
                    return;
                }

                foreach (RequestItem requested in requestedItems)
                {
                    Thing thing = ThingMaker.MakeThing(requested.item.ThingDef, requested.item.StuffDef);
                    thing.stackCount = requested.amount;

                    if (!GenPlace.TryPlaceThing(thing, traderPawn.Position, traderPawn.Map, ThingPlaceMode.Near))
                    {
                        Log.Error("Could not spawn " + thing.LabelCap + " near trader!");
                    }
                }

                traderFaction.Notify_PlayerTraded(totalRequestedValue, playerPawn);
                TaleRecorder.RecordTale(TaleDefOf.TradedWith, new object[]
                {
                        playerPawn,
                        traderPawn
                });

                Lord lord = traderPawn.GetLord();
                lord.ReceiveMemo(LordJob_FulfillItemRequest.MemoOnFulfilled);
                UpdateColonyCurrency(Mathf.RoundToInt(totalRequestedValue));
            }

            RequestSession.CloseOpenDealWith(traderFaction);
        }

        private void UpdateColonyCurrency(int amountToRemove)
        {
            List<SlotGroup> slotGroups = new List<SlotGroup>(playerPawn.Map.haulDestinationManager.AllGroups.ToList());
            slotGroups.ForEach(group =>
            {
                group.HeldThings.ToList().ForEach(thing =>
                {
                    if (thing.def == ThingDefOf.Silver)
                    {
                        colonySilver.Add(thing);
                    }
                });
            });

            foreach (Thing silver in colonySilver)
            {
                int stackCount = silver.stackCount;
                int remaining = amountToRemove - stackCount;
                if (remaining > 0)
                {                 
                    silver.Destroy();
                    amountToRemove = remaining;
                }
                else
                {
                    silver.stackCount -= amountToRemove;                 
                    break;
                }
            }
        }
    }
}
