using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace ItemRequests
{
    class FulfillItemRequestWindow : Window
    {
        public static readonly float PartialFulfillmentCutoff_S = 400;
        public static readonly float PartialFulfillmentCutoff_M = 1200;

        private Pawn playerPawn;
        private Pawn traderPawn;
        private Vector2 scrollPosition = Vector2.zero;
        private List<RequestItem> requestedItems;
        private List<Thing> colonySilverStacks = new List<Thing>();
        private float colonySilver = 0;
        private const float offsetFromRight = 120;
        private const float offsetFromBottom = 90;

        private RequestDeal deal => Find.World.GetComponent<RequestSession>().GetOpenDealWith(traderFaction);

        public FulfillItemRequestWindow(Pawn playerPawn, Pawn traderPawn)
        {
            this.playerPawn = playerPawn;
            this.traderPawn = traderPawn;
            this.requestedItems = Find.World.GetComponent<RequestSession>().GetOpenDealWith(traderFaction).GetRequestedItems();
            UpdateColonyCurrency(0);
        }

        private Faction traderFaction => traderPawn.Faction;

        public override Vector2 InitialSize => new Vector2(500, 700);

        public override void DoWindowContents(Rect inRect)
        {
            Vector2 contentMargin = new Vector2(12, 18);
            string title = "IR.FulfillItemRequestWindow.ReviewItems".Translate();
            string closeString = "IR.FulfillItemRequestWindow.Trade".Translate();
            string cancelString = "IR.FulfillItemRequestWindow.Postpone".Translate();
            float totalValue = deal.TotalRequestedValue;

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

            float horizontalLineY = mainRect.y + mainRect.height;
            Widgets.DrawLineHorizontal(x, horizontalLineY, inRect.width - contentMargin.x * 2);

            // Draw total
            Text.Anchor = TextAnchor.MiddleRight;
            Rect totalStringRect = new Rect(mainRect.width - offsetFromRight - 155, horizontalLineY, 140, rowHeight);
            Widgets.Label(totalStringRect, "IR.FulfillItemRequestWindow.Total".Translate());
            Widgets.DrawLineVertical(mainRect.width - offsetFromRight, horizontalLineY, rowHeight);
            Rect totalPriceRect = new Rect(mainRect.width - offsetFromRight, horizontalLineY, offsetFromRight, rowHeight);
            GUI.color = totalValue > colonySilver ? Color.red : Color.white;
            Widgets.Label(totalPriceRect, totalValue.ToStringMoney("F2"));
            if (totalValue > colonySilver)
            {
                TooltipHandler.TipRegion(totalPriceRect, "IR.FulfillItemRequestWindow.NotEnoughSilverStored".Translate());
            }

            GUI.color = Color.white;
            Text.Anchor = TextAnchor.MiddleLeft;
            Rect closeButtonArea = new Rect(x, inRect.height - contentMargin.y * 2, 100, 50);
            if (Widgets.ButtonText(closeButtonArea, closeString, false))
            {
                TradeButtonPressed();
            }

            Text.Anchor = TextAnchor.MiddleRight;
            Rect cancelButtonArea = new Rect(totalPriceRect.x, closeButtonArea.y, totalPriceRect.width + 10, closeButtonArea.height);
            if (Widgets.ButtonText(cancelButtonArea, cancelString, false))
            {
                Close(true);
            }

            GenUI.ResetLabelAlign();
            GUI.EndGroup();
        }

        private void DrawRequestedItem(Rect rowRect, RequestItem requested, int index)
        {
            float removeItemButtonSize = 24;
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

            Widgets.ThingIcon(iconArea, requested.item.tradeable.FirstThingTrader);

            x += iconSize + (iconSize / 4);

            Rect itemNameArea = new Rect(x, 0, rowRect.width - offsetFromRight - x, rowRect.height);
            string itemTitle = requested.item.thing.LabelCapNoCount;
            if (requested.isPawn)
            {
                itemTitle += " (" + requested.item.GenderString() + ")";
            }
            itemTitle += " x" + requested.amount;
            Widgets.Label(itemNameArea, itemTitle);

            x = rowRect.width - offsetFromRight;
            Widgets.DrawLineVertical(x, 0, rowRect.height);

            x += 10;
            Rect itemPriceArea = new Rect(x, 0, offsetFromRight - 10 - removeItemButtonSize, rowRect.height);
            Widgets.Label(itemPriceArea, price.ToStringMoney("F2"));

            Rect removeItemArea = new Rect(rowRect.width - removeItemButtonSize, 0, removeItemButtonSize, removeItemButtonSize);
            bool isBeingTraded = !requested.removed;
            Widgets.Checkbox(removeItemArea.position, ref isBeingTraded, removeItemButtonSize);
            requested.removed = !isBeingTraded;

            if (requested.removed)
            {                
                Widgets.DrawLineHorizontal(itemNameArea.x, rowRect.height / 2, Text.CalcSize(itemTitle).x);
                Widgets.DrawLineHorizontal(itemPriceArea.x, rowRect.height / 2, Text.CalcSize(price.ToStringMoney("F2")).x);
            }

            GUI.color = Color.white;
            GUI.EndGroup();
        }

        private void TradeButtonPressed()
        {
            Close(true);

            bool fulfilledFullRequest = true;
            float totalRequestedValue = deal.TotalRequestedValue;
            Lord lord = traderPawn.GetLord();

            if (playerPawn.Map.resourceCounter.Silver < totalRequestedValue)
            {
                Messages.Message("IR.FulfillItemRequestWindow.NotEnoughSilverMessage".Translate(), MessageTypeDefOf.NegativeEvent, true);
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
                    if (requested.removed)
                    {
                        fulfilledFullRequest = false;
                    }
                    else
                    {
                        if (requested.isPawn)
                        {
                            for (int i = 0; i < requested.amount; ++i)
                            {
                                Pawn pawn = PawnGenerator.GeneratePawn(requested.item.pawnDef, Faction.OfPlayer);
                                pawn.gender = requested.item.gender;
                                IntVec3 spawnSpot = CellFinder.RandomSpawnCellForPawnNear(traderPawn.Position, traderPawn.Map);
                                GenSpawn.Spawn(pawn, spawnSpot, traderPawn.Map);
                                //Log.Message("Spawned " + pawn.LabelCap + " the " + pawn.KindLabel);
                            }
                        }
                        else
                        {
                            Thing thing = ThingMaker.MakeThing(requested.item.def, requested.item.stuffDef);
                            thing.stackCount = requested.amount;

                            if (!GenPlace.TryPlaceThing(thing, traderPawn.Position, traderPawn.Map, ThingPlaceMode.Near))
                            {
                                Log.Error("Could not spawn " + thing.LabelCap + " near trader!");
                            }
                        }
                    }
                }


                if (fulfilledFullRequest)
                {
                    traderFaction.Notify_PlayerTraded(totalRequestedValue, playerPawn);
                    TaleRecorder.RecordTale(TaleDefOf.TradedWith, new object[]
                    {
                            playerPawn,
                            traderPawn
                    });

                    lord.ReceiveMemo(LordJob_FulfillItemRequest.MemoOnFulfilled);
                }
                else
                {
                    lord.ReceiveMemo(DetermineUnfulfilledValue());
                }
                UpdateColonyCurrency(Mathf.RoundToInt(totalRequestedValue));
            }

            Find.World.GetComponent<RequestSession>().CloseOpenDealWith(traderFaction);
        }

        private string DetermineUnfulfilledValue()
        {
            float removedItemsValue = 0;
            float totalItemsValue = 0;
            foreach (RequestItem item in requestedItems)
            {
                if (item.removed)
                {
                    removedItemsValue += item.pricePerItem * item.amount;
                }
                totalItemsValue += item.pricePerItem * item.amount;
            }

            if (removedItemsValue == totalItemsValue) return LordJob_FulfillItemRequest.MemoOnUnfulfilled;
            if (removedItemsValue < PartialFulfillmentCutoff_S) return LordJob_FulfillItemRequest.MemoOnPartiallyFulfilled_S;
            if (removedItemsValue < PartialFulfillmentCutoff_M) return LordJob_FulfillItemRequest.MemoOnPartiallyFulfilled_M;
            return LordJob_FulfillItemRequest.MemoOnPartiallyFulfilled_L;
            
        }

        private void UpdateColonyCurrency(int amountToRemove)
        {
            colonySilver = 0;
            List<SlotGroup> slotGroups = new List<SlotGroup>(playerPawn.Map.haulDestinationManager.AllGroups.ToList());
            slotGroups.ForEach(group =>
            {
                group.HeldThings.ToList().ForEach(thing =>
                {
                    if (thing.def == ThingDefOf.Silver)
                    {
                        colonySilverStacks.Add(thing);
                        colonySilver += thing.stackCount;
                    }
                });
            });

            foreach (Thing silver in colonySilverStacks)
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
