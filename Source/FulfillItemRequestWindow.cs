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
        private float iconNameAreaWidth = ItemRequestWindow.iconNameAreaWidth;
        private const float offsetFromRight = 200;

        public FulfillItemRequestWindow(Pawn playerPawn, Pawn traderPawn)
        {
            this.playerPawn = playerPawn;
            this.traderPawn = traderPawn;
            this.requestedItems = RequestSession.GetOpenDealWith(traderPawn.Faction).GetRequestedItems();
        }

        public override Vector2 InitialSize => new Vector2(800, 1000);

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
            Rect mainRect = new Rect(x, headerRowRect.y + headerRowRect.height + 30, inRect.width - x * 2, inRect.height - contentMargin.y * 2 - headerRowRect.height - rowHeight);
            Rect contentRect = new Rect(mainRect.x, 0, mainRect.width - constScrollbarSize, cumulativeContentHeight);
            float bottom = scrollPosition.y - 30f;
            float top = scrollPosition.y + mainRect.height;
            float y = 6f;
            int counter;

            Widgets.BeginScrollView(mainRect, ref scrollPosition, contentRect, true);

            for (int i = 0; i < requestedItems.Count; i++)
            {
                counter = i;
                if (y > bottom && y < top)
                {
                    Rect rect = new Rect(mainRect.x, y, contentRect.width, 30f);
                    DrawRequestedItem(rect, requestedItems[i], counter);
                }
                y += 30f;
            }

            Widgets.EndScrollView();

            float horizontalLineY = inRect.height - contentMargin.y * 2 - headerRowRect.height - rowHeight;
            Widgets.DrawLineHorizontal(x, horizontalLineY, inRect.width - contentMargin.x * 2);

            // Draw total
            Text.Anchor = TextAnchor.MiddleRight;
            Rect totalStringRect = new Rect(x, horizontalLineY, contentRect.width - offsetFromRight - contentMargin.x, rowHeight);
            Widgets.Label(totalStringRect, "Total");
            Rect totalPriceRect = new Rect(contentRect.width - offsetFromRight, horizontalLineY, offsetFromRight, rowHeight);
            Widgets.Label(totalPriceRect, RequestSession.GetOpenDealWith(traderPawn.Faction).TotalRequestedValue.ToStringMoney("F2"));
                        
            Text.Anchor = TextAnchor.MiddleLeft;
            Rect closeButtonArea = new Rect(x, inRect.height - contentMargin.y * 2, 100, rowHeight);
            if (Widgets.ButtonText(closeButtonArea, closeString, false))
            {
                CloseButtonPressed();
            }

            GenUI.ResetLabelAlign();
            GUI.EndGroup();
        }

        private void DrawRequestedItem(Rect rowRect, RequestItem requested, int index)
        {
            float price = requested.pricePerItem * requested.amount;
            if (index % 2 == 1)
            {
                Widgets.DrawLightHighlight(rowRect);
            }

            GUI.BeginGroup(rowRect);


            // Draw item icon
            float x = 0;
            float iconWidth = iconNameAreaWidth / 4;
            Text.Anchor = TextAnchor.MiddleLeft;
            Rect iconArea = new Rect(x, 0, iconWidth, rowRect.height);
            Widgets.ThingIcon(iconArea, requested.item.AnyThing.def);

            x += iconWidth * 2;

            // Draw item name
            Rect itemNameArea = new Rect(x, 0, iconWidth * 2, rowRect.height);
            Widgets.Label(itemNameArea, requested.item.AnyThing.LabelCap);

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

            float totalRequestedValue = RequestSession.GetOpenDealWith(traderPawn.Faction).TotalRequestedValue;
            if (playerPawn.Map.resourceCounter.Silver < totalRequestedValue)
            {
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
                    trader.GiveSoldThingToPlayer(thing, requested.amount, playerPawn);
                }

                Log.Message("Trade successful!");
                traderPawn.Faction.Notify_PlayerTraded(totalRequestedValue, playerPawn);
                TaleRecorder.RecordTale(TaleDefOf.TradedWith, new object[]
                {
                        playerPawn,
                        traderPawn
                });

                Lord lord = traderPawn.GetLord();
                lord.ReceiveMemo(LordJob_FulfillItemRequest.MemoOnFulfilled);
            }
        }
    }
}
