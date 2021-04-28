using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace ItemRequests
{
    internal class FulfillItemRequestWindow : Window
    {
        private const float offsetFromRight = 120;
        private const float offsetFromBottom = 90;
        private static readonly float PartialFulfillmentCutoff_S = 400;
        private static readonly float PartialFulfillmentCutoff_M = 1200;
        private readonly List<Thing> colonySilverStacks = new List<Thing>();

        private readonly Pawn playerPawn;
        private readonly List<RequestItem> requestedItems;
        private readonly Pawn traderPawn;
        private float colonySilver;
        private Vector2 scrollPosition = Vector2.zero;

        public FulfillItemRequestWindow(Pawn playerPawn, Pawn traderPawn)
        {
            this.playerPawn = playerPawn;
            this.traderPawn = traderPawn;
            requestedItems = Find.World.GetComponent<RequestSession>().GetOpenDealWith(traderFaction)
                .GetRequestedItems();
            UpdateColonyCurrency(0);
        }

        private RequestDeal deal => Find.World.GetComponent<RequestSession>().GetOpenDealWith(traderFaction);

        private Faction traderFaction => traderPawn.Faction;

        public override Vector2 InitialSize => new Vector2(500, 700);

        public override void DoWindowContents(Rect inRect)
        {
            var contentMargin = new Vector2(12, 18);
            string title = "IR.FulfillItemRequestWindow.ReviewItems".Translate();
            string closeString = "IR.FulfillItemRequestWindow.Trade".Translate();
            string cancelString = "IR.FulfillItemRequestWindow.Postpone".Translate();
            var totalValue = deal.TotalRequestedValue;

            // Begin Window group
            GUI.BeginGroup(inRect);

            // Draw the names of negotiator and factions
            inRect = inRect.AtZero();
            var x = contentMargin.x;
            var headerRowHeight = 35f;
            var headerRowRect = new Rect(x, contentMargin.y, inRect.width - x, headerRowHeight);
            var titleArea = new Rect(x, 0, headerRowRect.width, headerRowRect.height);
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Medium;
            Widgets.Label(titleArea, title);

            Text.Font = GameFont.Small;
            float constScrollbarSize = 16;
            float rowHeight = 30;
            var cumulativeContentHeight = 6f + (requestedItems.Count * rowHeight);
            var mainRect = new Rect(x, headerRowRect.y + headerRowRect.height + 10, inRect.width - (x * 2),
                inRect.height - offsetFromBottom - headerRowRect.y - headerRowRect.height);
            var scrollRect = new Rect(mainRect.x, 0, mainRect.width - constScrollbarSize, cumulativeContentHeight);
            var bottom = scrollPosition.y - 30f;
            var top = scrollPosition.y + mainRect.height;
            var y = 6f;

            Widgets.BeginScrollView(mainRect, ref scrollPosition, scrollRect);

            for (var i = 0; i < requestedItems.Count; i++)
            {
                var counter = i;
                if (y > bottom && y < top)
                {
                    var rect = new Rect(mainRect.x, y, scrollRect.width, 30f);
                    DrawRequestedItem(rect, requestedItems[i], counter);
                }

                y += 30f;
            }

            Widgets.EndScrollView();

            var horizontalLineY = mainRect.y + mainRect.height;
            Widgets.DrawLineHorizontal(x, horizontalLineY, inRect.width - (contentMargin.x * 2));

            // Draw total
            Text.Anchor = TextAnchor.MiddleRight;
            var totalStringRect = new Rect(mainRect.width - offsetFromRight - 155, horizontalLineY, 140, rowHeight);
            Widgets.Label(totalStringRect, "IR.FulfillItemRequestWindow.Total".Translate());
            Widgets.DrawLineVertical(mainRect.width - offsetFromRight, horizontalLineY, rowHeight);
            var totalPriceRect =
                new Rect(mainRect.width - offsetFromRight, horizontalLineY, offsetFromRight, rowHeight);
            GUI.color = totalValue > colonySilver ? Color.red : Color.white;
            Widgets.Label(totalPriceRect, totalValue.ToStringMoney("F2"));
            if (totalValue > colonySilver)
            {
                TooltipHandler.TipRegion(totalPriceRect,
                    "IR.FulfillItemRequestWindow.NotEnoughSilverStored".Translate());
            }

            GUI.color = Color.white;
            Text.Anchor = TextAnchor.MiddleLeft;
            var closeButtonArea = new Rect(x, inRect.height - (contentMargin.y * 2), 100, 50);
            if (Widgets.ButtonText(closeButtonArea, closeString, false))
            {
                TradeButtonPressed();
            }

            Text.Anchor = TextAnchor.MiddleRight;
            var cancelButtonArea = new Rect(totalPriceRect.x, closeButtonArea.y, totalPriceRect.width + 10,
                closeButtonArea.height);
            if (Widgets.ButtonText(cancelButtonArea, cancelString, false))
            {
                Close();
            }

            GenUI.ResetLabelAlign();
            GUI.EndGroup();
        }

        private void DrawRequestedItem(Rect rowRect, RequestItem requested, int index)
        {
            float removeItemButtonSize = 24;
            Text.Font = GameFont.Small;
            var price = requested.pricePerItem * requested.amount;
            if (index % 2 == 1)
            {
                Widgets.DrawLightHighlight(rowRect);
            }

            GUI.BeginGroup(rowRect);


            // Draw item icon
            float x = 0;
            float iconSize = 27;
            Text.Anchor = TextAnchor.MiddleLeft;
            var iconArea = new Rect(x, 0, iconSize, iconSize);

            Widgets.ThingIcon(iconArea, requested.item.tradeable.FirstThingTrader);

            x += iconSize + (iconSize / 4);

            var itemNameArea = new Rect(x, 0, rowRect.width - offsetFromRight - x, rowRect.height);
            var itemTitle = requested.item.thing.LabelCapNoCount;
            if (requested.isPawn)
            {
                itemTitle += " (" + requested.item.GenderString() + ")";
            }
            else if (requested.item.type.HasQuality() && itemTitle.IndexOf("(normal", StringComparison.Ordinal) != -1)
            {
                itemTitle = itemTitle.Substring(0, itemTitle.IndexOf("(normal)", StringComparison.Ordinal));
            }

            itemTitle += " x" + requested.amount;
            Widgets.Label(itemNameArea, itemTitle);

            x = rowRect.width - offsetFromRight;
            Widgets.DrawLineVertical(x, 0, rowRect.height);

            x += 10;
            var itemPriceArea = new Rect(x, 0, offsetFromRight - 10 - removeItemButtonSize, rowRect.height);
            Widgets.Label(itemPriceArea, price.ToStringMoney("F2"));

            var removeItemArea = new Rect(rowRect.width - removeItemButtonSize, 0, removeItemButtonSize,
                removeItemButtonSize);
            var isBeingTraded = !requested.removed;
            Widgets.Checkbox(removeItemArea.position, ref isBeingTraded, removeItemButtonSize);
            requested.removed = !isBeingTraded;

            if (requested.removed)
            {
                Widgets.DrawLineHorizontal(itemNameArea.x, rowRect.height / 2, Text.CalcSize(itemTitle).x);
                Widgets.DrawLineHorizontal(itemPriceArea.x, rowRect.height / 2,
                    Text.CalcSize(price.ToStringMoney("F2")).x);
            }

            GUI.color = Color.white;
            GUI.EndGroup();
        }

        private void TradeButtonPressed()
        {
            Close();

            var fulfilledFullRequest = true;
            var totalRequestedValue = deal.TotalRequestedValue;
            var lord = traderPawn.GetLord();

            if (playerPawn.Map.resourceCounter.Silver < totalRequestedValue)
            {
                Messages.Message("IR.FulfillItemRequestWindow.NotEnoughSilverMessage".Translate(),
                    MessageTypeDefOf.NegativeEvent);
                lord.ReceiveMemo(LordJob_FulfillItemRequest.MemoOnUnfulfilled);
            }
            else
            {
                if (traderPawn == null)
                {
                    Log.Error("Trader pawn unable to be cast to ITrader!");
                    return;
                }

                foreach (var requested in requestedItems)
                {
                    if (requested.removed)
                    {
                        fulfilledFullRequest = false;
                    }
                    else
                    {
                        SpawnItem(requested);
                    }
                }


                if (fulfilledFullRequest)
                {
                    traderFaction.Notify_PlayerTraded(totalRequestedValue, playerPawn);
                    TaleRecorder.RecordTale(TaleDefOf.TradedWith, playerPawn, traderPawn);

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

        private void SpawnItem(RequestItem requested)
        {
            if (requested.isPawn)
            {
                for (var i = 0; i < requested.amount; ++i)
                {
                    var pawn = PawnGenerator.GeneratePawn(requested.item.pawnDef, Faction.OfPlayer);
                    pawn.gender = requested.item.gender;
                    var spawnSpot = CellFinder.RandomSpawnCellForPawnNear(traderPawn.Position, traderPawn.Map);
                    GenSpawn.Spawn(pawn, spawnSpot, traderPawn.Map);
                }
            }
            else if (requested.item.type.HasQuality())
            {
                for (var i = 0; i < requested.amount; ++i)
                {
                    var thing = ThingMaker.MakeThing(requested.item.def, requested.item.stuffDef);
                    thing.SetRandomQualityWeighted();

                    if (requested.item.type == ThingType.Buildings)
                    {
                        var minifiedThing = thing.MakeMinified();
                        if (minifiedThing.Stuff != requested.item.stuffDef)
                        {
                            minifiedThing.SetStuffDirect(requested.item.stuffDef);
                        }

                        if (!GenPlace.TryPlaceThing(minifiedThing, traderPawn.Position, traderPawn.Map,
                            ThingPlaceMode.Near))
                        {
                            Log.Error("Could not spawn " + thing.LabelCap + " near trader!");
                        }
                    }
                    else
                    {
                        if (!GenPlace.TryPlaceThing(thing, traderPawn.Position, traderPawn.Map, ThingPlaceMode.Near))
                        {
                            Log.Error("Could not spawn " + thing.LabelCap + " near trader!");
                        }
                    }
                }
            }
            else
            {
                var thing = ThingMaker.MakeThing(requested.item.def, requested.item.stuffDef);
                thing.stackCount = requested.amount;
                if (!GenPlace.TryPlaceThing(thing, traderPawn.Position, traderPawn.Map, ThingPlaceMode.Near))
                {
                    Log.Error("Could not spawn " + thing.LabelCap + " near trader!");
                }
            }
        }

        private string DetermineUnfulfilledValue()
        {
            float removedItemsValue = 0;
            float totalItemsValue = 0;
            foreach (var item in requestedItems)
            {
                if (item.removed)
                {
                    removedItemsValue += item.pricePerItem * item.amount;
                }

                totalItemsValue += item.pricePerItem * item.amount;
            }

            if (removedItemsValue == totalItemsValue)
            {
                return LordJob_FulfillItemRequest.MemoOnUnfulfilled;
            }

            if (removedItemsValue < PartialFulfillmentCutoff_S)
            {
                return LordJob_FulfillItemRequest.MemoOnPartiallyFulfilled_S;
            }

            if (removedItemsValue < PartialFulfillmentCutoff_M)
            {
                return LordJob_FulfillItemRequest.MemoOnPartiallyFulfilled_M;
            }

            return LordJob_FulfillItemRequest.MemoOnPartiallyFulfilled_L;
        }

        private void UpdateColonyCurrency(int amountToRemove)
        {
            colonySilver = 0;
            var slotGroups = new List<SlotGroup>(playerPawn.Map.haulDestinationManager.AllGroups.ToList());
            slotGroups.ForEach(group =>
            {
                group.HeldThings.ToList().ForEach(thing =>
                {
                    if (thing.def != ThingDefOf.Silver)
                    {
                        return;
                    }

                    colonySilverStacks.Add(thing);
                    colonySilver += thing.stackCount;
                });
            });

            foreach (var silver in colonySilverStacks)
            {
                var stackCount = silver.stackCount;
                var remaining = amountToRemove - stackCount;
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