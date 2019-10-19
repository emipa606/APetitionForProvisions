using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;
using RimWorld;

namespace ItemRequests
{
    public class ItemRequestWindow : Window
    {
        // For being a sub class of Window
        public Vector2 WindowSize { get; protected set; }
        public Vector2 ContentMargin { get; protected set; }
        public Rect ContentRect { get; protected set; }
        public Rect ScrollRect { get; protected set; }
        private Vector2 scrollPosition = Vector2.zero;

        // For UI Items
        protected WidgetTable<ThingEntry> table = new WidgetTable<ThingEntry>();
        protected static readonly Vector2 AcceptButtonSize = new Vector2(160, 40f);
        protected static readonly Vector2 OtherBottomButtonSize = new Vector2(160, 40f);

        // For reference (set in constructor)
        private int colonySilver;
        private Faction faction;
        private Pawn negotiator;
        private Map map;

        // For listing the items
        private List<Tradeable> requestableItems = new List<Tradeable>();
        private HashSet<ThingDef> stuffFilterSet = new HashSet<ThingDef>();
        private Dictionary<string, int> colonyItemCount = new Dictionary<string, int>();
        private ThingType thingTypeFilter = ThingType.Resources;
        private ThingDef stuffTypeFilter = null;

        // For UI layout
        private float rightAlignOffset;
        private float rightContentSize;
        private const float iconNameAreaWidth = 350;
        private const float priceTextAreaWidth = 100;
        private const float colonyItemCountAreaWidth = 100;
        private const float resetItemCountAreaWidth = 40;
        private const float countAdjustInterfaceWidth = 200;
        private const string colonyCountTooltipText = "The amount your colony currently has stored.";


        public ItemRequestWindow(Map map, Faction faction, Pawn negotiator)
        {
            this.map = map;
            this.faction = faction;
            this.negotiator = negotiator;
            this.colonySilver = map.resourceCounter.Silver;

            // Find all items in stockpiles and store counts in dictionary
            List<SlotGroup> slotGroups = new List<SlotGroup>(map.haulDestinationManager.AllGroups.ToList());
            slotGroups.ForEach(group =>
            {
                //Log.Message("Reading " + group.ToString());
                group.HeldThings.ToList().ForEach(thing =>
                {
                    //Log.Message("  - " + thing.LabelCapNoCount + " x" + thing.stackCount.ToString());
                    string key = thing.def.label;
                    if (colonyItemCount.ContainsKey(key))
                    {
                        colonyItemCount[key] += thing.stackCount;
                    }
                    else
                    {
                        colonyItemCount[key] = thing.stackCount;
                    }
                });
            });

            RequestSession.SetupWith(faction, negotiator);
            DetermineRequestableItems();
            Resize();
        }
        protected void Resize()
        {
            ContentMargin = new Vector2(10, 18);
            float HeaderHeight = 32;
            float FooterHeight = 40f;
            float WindowPadding = 18;
            WindowSize = new Vector2(800, 1000);
            Vector2 ContentSize = new Vector2(WindowSize.x - WindowPadding * 2 - ContentMargin.x * 2,
                WindowSize.y - WindowPadding * 2 - ContentMargin.y * 2 - FooterHeight - HeaderHeight);

            ContentRect = new Rect(ContentMargin.x, ContentMargin.y + HeaderHeight, ContentSize.x, ContentSize.y);

            ScrollRect = new Rect(0, 0, ContentRect.width, ContentRect.height);

            rightContentSize = 200;
            rightAlignOffset = WindowSize.x - WindowPadding - ContentMargin.x - rightContentSize;
        }

        public override Vector2 InitialSize
        {
            get
            {
                return new Vector2(WindowSize.x, WindowSize.y);
            }
        }

        public override void DoWindowContents(Rect inRect)
        {
            // Begin Window group
            GUI.BeginGroup(inRect);

            // Draw the names of negotiator and factions
            inRect = inRect.AtZero();
            float x = ContentMargin.x;
            float headerRowHeight = 80f;
            Rect headerRowRect = new Rect(x, 0, inRect.width - x, headerRowHeight);
            DrawWindowHeader(headerRowRect, headerRowHeight);

            x = headerRowRect.x;

            // Draws the $$ amount available
            float rowWidth = inRect.width - 16f;
            Rect rowRect = new Rect(x, headerRowHeight, rowWidth, 30f);
            DrawAvailableColonyCurrency(rowRect, colonySilver);

            GUI.color = Color.gray;
            Widgets.DrawLineHorizontal(x, headerRowHeight + rowRect.height - 2, rowWidth);

            // Draw the main scroll view area
            GUI.color = Color.white;
            float addedMainRectPadding = 30f;
            float buttonHeight = 38f;
            Rect mainArea = new Rect(x, headerRowHeight + addedMainRectPadding, inRect.width - x, inRect.height - headerRowHeight - buttonHeight - addedMainRectPadding - 20f);
            DrawTradeableContent(mainArea);

            // Draw the buttons at bottom
            DrawButtons(inRect, rowRect);

            // End Window group
            GUI.EndGroup();
        }

        private void DrawWindowHeader(Rect headerRowRect, float headerRowHeight)
        {
            // Begin Header group
            GUI.BeginGroup(headerRowRect);
            Text.Font = GameFont.Medium;

            // Draw player faction name
            Rect playerFactionNameArea = new Rect(0, 0, headerRowRect.width / 2, headerRowRect.height);
            Text.Anchor = TextAnchor.UpperLeft;
            Widgets.Label(playerFactionNameArea, Faction.OfPlayer.Name.Truncate(playerFactionNameArea.width, null));

            // Draw trader name
            Rect tradingFactionNameArea = new Rect(headerRowRect.width / 2, 0, headerRowRect.width / 2, headerRowRect.height);
            Text.Anchor = TextAnchor.UpperRight;
            string tradingFactionName = faction.Name;
            if (Text.CalcSize(tradingFactionName).x > tradingFactionNameArea.width)
            {
                tradingFactionName = tradingFactionName.Truncate(tradingFactionNameArea.width, null);
            }
            Widgets.Label(tradingFactionNameArea, tradingFactionName);

            // Draw just below player faction name
            float amountRequestedTextHeight = 20;
            float secondRowY = (headerRowHeight - amountRequestedTextHeight) / 2;
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            Rect negotiatorNameArea = new Rect(0, secondRowY - 2, headerRowRect.width / 2, secondRowY);
            Widgets.Label(negotiatorNameArea, "Negotiator".Translate() + ": " + negotiator.LabelShort);

            // Draw just below trader name
            Text.Anchor = TextAnchor.UpperRight;
            Rect factionTechLevelArea = new Rect(headerRowRect.width / 2, secondRowY - 2, headerRowRect.width / 2, secondRowY);
            Widgets.Label(factionTechLevelArea, "Tech Level: " + faction.def.techLevel.ToString());


            // Draw the filter dropdowns
            Text.Anchor = TextAnchor.MiddleLeft;
            Rect filterDropdownArea = new Rect(0, headerRowHeight - amountRequestedTextHeight, headerRowRect.width - rightContentSize, amountRequestedTextHeight);
            DrawFilterDropdowns(filterDropdownArea);

            // Draw the amount requested text
            GUI.color = new Color(1f, 1f, 1f, 0.6f);
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.MiddleCenter;
            Rect clarificationTextArea = new Rect(rightAlignOffset, headerRowHeight - amountRequestedTextHeight, rightContentSize, amountRequestedTextHeight);
            Widgets.Label(clarificationTextArea, "Amount requested");


            // End Header group
            GUI.color = Color.white;
            GenUI.ResetLabelAlign();
            GUI.EndGroup();

        }

        public void DrawFilterDropdowns(Rect rectArea)
        {
            Rect filterLabelArea = rectArea;
            filterLabelArea.width = 80;
            Widgets.Label(filterLabelArea, "Filters:");

            // Draw the thing type filter
            float filterDropdownHeight = 27;
            float filterDropdownWidth = 130;
            Text.Anchor = TextAnchor.UpperLeft;
            Rect thingFilterDropdownArea = new Rect(filterLabelArea.width + 20, rectArea.y - 6, filterDropdownWidth, filterDropdownHeight);
            if (WidgetDropdown.Button(thingFilterDropdownArea, thingTypeFilter.ToString(), true, false, true))
            {
                var thingTypes = Enum.GetValues(typeof(ThingType));
                List<FloatMenuOption> filterOptions = new List<FloatMenuOption>();
                foreach (ThingType type in thingTypes)
                {
                    if (type == ThingType.Discard) continue;
                    filterOptions.Add(new FloatMenuOption(type.ToString(), () =>
                    {
                        if (thingTypeFilter != type)
                        {
                            thingTypeFilter = type;
                            DetermineRequestableItems();
                            UpdateAvailableMaterials();
                        }
                    }));
                }
                Find.WindowStack.Add(new FloatMenu(filterOptions, null, false));
            }

            // Draw the stuff filter
            Rect stuffFilterDropdownArea = thingFilterDropdownArea;
            stuffFilterDropdownArea.x += thingFilterDropdownArea.width + 10;
            string stuffFilterLabel = stuffTypeFilter == null ? "All" : stuffTypeFilter.LabelCap;
            if (WidgetDropdown.Button(stuffFilterDropdownArea, stuffFilterLabel, true, false, true))
            {
                List<FloatMenuOption> stuffFilterOptions = new List<FloatMenuOption>();
                stuffFilterOptions.Add(new FloatMenuOption("All", () =>
                {
                    if (stuffTypeFilter != null)
                    {
                        stuffTypeFilter = null;
                        DetermineRequestableItems();
                    }
                }));

                foreach (ThingDef item in stuffFilterSet.OrderBy((ThingDef def) => { return def.LabelCap; }))
                {
                    stuffFilterOptions.Add(new FloatMenuOption(item.LabelCap, () =>
                    {
                        if (stuffTypeFilter != item)
                        {
                            stuffTypeFilter = item;
                            DetermineRequestableItems();
                        }
                    }));
                }
                Find.WindowStack.Add(new FloatMenu(stuffFilterOptions, null, false));
            }
        }

        public void DrawAvailableColonyCurrency(Rect rowRect, int colonySilver)
        {
            // Begin row
            GUI.BeginGroup(rowRect);
            float silverIconWidth = 25;
            float iconNameItemWidth = iconNameAreaWidth - silverIconWidth;
            float x = 0;
            Text.Font = GameFont.Small;

            // Draw icon for silver
            Rect silverGraphicRect = new Rect(x, 0, silverIconWidth, rowRect.height);
            Widgets.ThingIcon(silverGraphicRect, ThingDefOf.Silver);

            x += silverIconWidth + 10;

            // Draw the label "Silver"
            Text.Anchor = TextAnchor.MiddleLeft;
            Rect textRectPadded = new Rect(x, 0, iconNameItemWidth, rowRect.height);
            textRectPadded.xMin += 5;
            textRectPadded.xMax -= 5;
            Widgets.Label(textRectPadded, "Silver");

            x += iconNameItemWidth + priceTextAreaWidth;

            // Draw the available silver for colony
            Rect silverCountRect = new Rect(x, 0, colonyItemCountAreaWidth, rowRect.height);
            Rect paddedRect = silverCountRect;
            paddedRect.xMin += 5f;
            paddedRect.xMax -= 5f;
            Widgets.Label(paddedRect, colonySilver.ToString());

            // Draw the amount currently requested by colony
            Text.Anchor = TextAnchor.MiddleCenter;
            string tooltipString = "This is the value of all the items you've requested.";
            if (RequestSession.deal.TotalRequestedValue > colonySilver)
            {
                GUI.color = Color.yellow;
                tooltipString += "\n\nCaution: You can still request these items, but you don't currently have enough silver to pay for them when they arrive.";
            }
            Rect requestedAmountArea = new Rect(rightAlignOffset, 0, rightContentSize, rowRect.height);
            Widgets.Label(requestedAmountArea, RequestSession.deal.TotalRequestedValue.ToStringMoney("F2"));
            TooltipHandler.TipRegion(requestedAmountArea, tooltipString);

            // Finish row
            GUI.color = Color.white;
            GenUI.ResetLabelAlign();
            GUI.EndGroup();
        }

        private void DrawTradeableContent(Rect mainRect)
        {
            Text.Font = GameFont.Small;
            float constScrollbarSize = 16;
            float cumulativeContentHeight = 6f + requestableItems.Count * 30f;
            Rect contentRect = new Rect(mainRect.x, 0, mainRect.width - constScrollbarSize, cumulativeContentHeight);
            float bottom = scrollPosition.y - 30f;
            float top = scrollPosition.y + mainRect.height;
            float y = 6f;
            int counter;

            Widgets.BeginScrollView(mainRect, ref scrollPosition, contentRect, true);

            for (int i = 0; i < requestableItems.Count; i++)
            {
                counter = i;
                if (y > bottom && y < top)
                {
                    Rect rect = new Rect(mainRect.x, y, contentRect.width, 30f);
                    DrawTradeableRow(rect, requestableItems[i], counter);
                }
                y += 30f;
            }

            Widgets.EndScrollView();
        }

        private void DrawButtons(Rect inRect, Rect rowRect)
        {
            Rect confirmButtonRect = new Rect(inRect.width - AcceptButtonSize.x, inRect.height - AcceptButtonSize.y, AcceptButtonSize.x, AcceptButtonSize.y);
            if (Widgets.ButtonText(confirmButtonRect, "Confirm", true, false, true))
            {
                bool success;
                if (RequestSession.deal.TryExecute(colonySilver, out success))
                {
                    if (success)
                    {
                        SoundDefOf.ExecuteTrade.PlayOneShotOnCamera(null);
                        Find.WindowStack.Add(new RequestAcknowledgedWindow());
                        Close(false);
                    }
                    else
                    {
                        Close(true);
                    }
                    RequestSession.CloseSession();
                }

                Event.current.Use();
            }


            Rect cancelButtonRect = new Rect(rowRect.x, confirmButtonRect.y, OtherBottomButtonSize.x, OtherBottomButtonSize.y);
            if (Widgets.ButtonText(cancelButtonRect, "Cancel", true, false, true))
            {
                Close(true);
                RequestSession.CloseOpenDealWith(faction);
                RequestSession.CloseSession();
                Event.current.Use();
            }

            Rect resetButtonRect = new Rect(rowRect.x + cancelButtonRect.width + 10, confirmButtonRect.y, OtherBottomButtonSize.x, OtherBottomButtonSize.y);
            if (Widgets.ButtonText(resetButtonRect, "Reset", true, false, true))
            {
                SoundDefOf.Tick_Low.PlayOneShotOnCamera(null);
                RequestSession.deal.Reset();
            }
        }

        public void DrawTradeableRow(Rect rowRect, Tradeable trade, int index)
        {
            if (index % 2 == 1)
            {
                Widgets.DrawLightHighlight(rowRect);
            }

            // Begin Row group
            GUI.BeginGroup(rowRect);
            float x = 0; // starting from left

            // Draw item icon and info icon
            Text.Anchor = TextAnchor.MiddleLeft;
            Rect idRect = new Rect(x, 0, iconNameAreaWidth, rowRect.height);
            TransferableUIUtility.DoExtraAnimalIcons(trade, rowRect, ref x);
            TransferableUIUtility.DrawTransferableInfo(trade, idRect, Color.white);

            x += iconNameAreaWidth;

            // Draw the price for requesting the item
            Rect priceTextArea = new Rect(x, 0, priceTextAreaWidth, rowRect.height);
            float price = DrawPrice(priceTextArea, trade);
            x += priceTextAreaWidth;

            // Draw the number the colony currently has, if any
            int colonyItemCount = trade.CountHeldBy(Transactor.Colony);
            if (colonyItemCount != 0)
            {
                Rect colonyItemCountArea = new Rect(x, 0, colonyItemCountAreaWidth, rowRect.height);
                if (Mouse.IsOver(colonyItemCountArea))
                {
                    Widgets.DrawHighlight(colonyItemCountArea);
                }
                Rect paddedRect = colonyItemCountArea;
                paddedRect.xMin += 5f;
                paddedRect.xMax -= 5f;
                Widgets.Label(paddedRect, colonyItemCount.ToStringCached());
                TooltipHandler.TipRegion(colonyItemCountArea, colonyCountTooltipText);
            }

            x += colonyItemCountAreaWidth;

            // Draw the input box to select number of requests
            Rect countAdjustInterfaceRect = new Rect(rightAlignOffset, 0, rightContentSize, rowRect.height);
            Rect interactiveNumericFieldArea = new Rect(countAdjustInterfaceRect.center.x - 45f, countAdjustInterfaceRect.center.y - 12.5f, 90f, 25f).Rounded();
            Rect paddedNumericFieldArea = interactiveNumericFieldArea.ContractedBy(2f);
            paddedNumericFieldArea.xMax -= 15f;
            paddedNumericFieldArea.xMin += 16f;
            int countToTransfer = RequestSession.deal.GetCountForItem(thingTypeFilter, trade);
            string editBuffer = trade.EditBuffer;
            Widgets.TextFieldNumeric(paddedNumericFieldArea, ref countToTransfer, ref editBuffer, 0, float.MaxValue);
            trade.AdjustTo(countToTransfer);
            RequestSession.deal.AdjustItemRequest(thingTypeFilter, trade, countToTransfer, price);

            // Draw the reset to zero button by input field
            if (trade.CountToTransfer > 0)
            {
                Rect resetToZeroButton = interactiveNumericFieldArea;
                resetToZeroButton.x -= resetItemCountAreaWidth - 5;
                resetToZeroButton.width = resetItemCountAreaWidth;
                if (Widgets.ButtonText(resetToZeroButton, "0"))
                {
                    trade.AdjustTo(0);
                    RequestSession.deal.AdjustItemRequest(thingTypeFilter, trade, 0, price);
                }
            }

            x += countAdjustInterfaceWidth;

            // End Row group
            GenUI.ResetLabelAlign();
            GUI.EndGroup();
        }

        private float DrawPrice(Rect rect, Tradeable trad)
        {
            rect = rect.Rounded();
            if (Mouse.IsOver(rect))
            {
                Widgets.DrawHighlight(rect);
            }

            PriceType priceType = GetPriceTypeFor(trad);
            float finalPrice = CalcRequestedItemPrice(trad, priceType);
            TooltipHandler.TipRegion(rect, new TipSignal(() => GetPriceTooltip(faction, negotiator, trad, priceType, finalPrice), trad.GetHashCode() * 297));
            switch (priceType)
            {
                case PriceType.VeryCheap:
                    GUI.color = new Color(0, 1, 0);
                    break;
                case PriceType.Cheap:
                    GUI.color = new Color(0.5f, 1, 0.5f);
                    break;
                case PriceType.Normal:
                    GUI.color = Color.white;
                    break;
                case PriceType.Expensive:
                    GUI.color = new Color(1, 0.5f, 0.5f);
                    break;
                case PriceType.Exorbitant:
                    GUI.color = new Color(1, 0, 0);
                    break;
            }


            string label = finalPrice.ToStringMoney("F2");
            Rect priceTextArea = new Rect(rect);
            priceTextArea.xMax -= 5f;
            priceTextArea.xMin += 5f;
            if (Text.Anchor == TextAnchor.MiddleLeft)
            {
                priceTextArea.xMax += 300f;
            }
            if (Text.Anchor == TextAnchor.MiddleRight)
            {
                priceTextArea.xMin -= 300f;
            }
            Widgets.Label(priceTextArea, label);
            GUI.color = Color.white;

            return finalPrice;
        }

        private PriceType GetPriceTypeFor(Tradeable trad)
        {
            ThingDef thingDef = trad.ThingDef;
            if (thingDef == ThingDefOf.Silver)
            {
                return PriceType.Undefined;
            }

            return PriceType.Normal;
        }

        private string GetPriceTooltip(Faction faction, Pawn negotiator, Tradeable trad, PriceType priceType, float priceFor)
        {
            if (!trad.HasAnyThing)
            {
                return string.Empty;
            }

            string text = "Price you'll pay upon delivery:";
            text += "\n\n";
            text = text + StatDefOf.MarketValue.LabelCap + ": " + trad.BaseMarketValue.ToStringMoney("F2");

            string text2 = text;

            // TODO: Breadown the markup multiplier here for
            // item rarity/dist to colony, etc.
            text = string.Concat(new string[]
            {
                    text2,
                    "\n  x ",
                    1.6f.ToString("F2"),
                    " (Requesting)"
            });


            if (priceType.PriceMultiplier() != 1f)
            {
                text2 = text;
                text = string.Concat(new string[]
                {
                        text2,
                        "\n  x ",
                        priceType.PriceMultiplier().ToString("F2"),
                        " (Faction tech level)"
                });
            }
            if (Find.Storyteller.difficulty.tradePriceFactorLoss != 0f)
            {
                text2 = text;
                text = string.Concat(new string[]
                {
                        text2,
                        "\n  x ",
                        (1f + Find.Storyteller.difficulty.tradePriceFactorLoss).ToString("F2"),
                        " (",
                        "DifficultyLevel".Translate(),
                        ")"
                });
            }
            text += "\n";
            text2 = text;
            text = string.Concat(new string[]
            {
                    text2,
                    "\n",
                    "YourNegotiatorBonus".Translate(),
                    ": -",
                    negotiator.GetStatValue(StatDefOf.TradePriceImprovement, true).ToStringPercent()
            });

            float priceGainSettlement = GetOfferPriceImprovementOffsetForFaction(faction, negotiator);
            if (priceGainSettlement != 0f)
            {
                text2 = text;
                text = string.Concat(new string[]
                {
                        text2,
                        "\n",
                        "TradeWithFactionBaseBonus".Translate(),
                        ": -",
                        priceGainSettlement.ToStringPercent()
                });
            }

            text += "\n\n";
            text = text + "FinalPrice".Translate() + ": " + priceFor.ToStringMoney("F2");
            if (priceFor <= 0.01f)
            {
                text = text + " (" + "minimum".Translate() + ")";
            }
            return text;
        }

        private float CalcRequestedItemPrice(Tradeable item, PriceType priceType)
        {
            if (item.IsCurrency)
            {
                return item.BaseMarketValue;
            }

            float basePrice = item.BaseMarketValue * priceType.PriceMultiplier();
            float negotiatorBonus = negotiator.GetStatValue(StatDefOf.TradePriceImprovement, true);
            float settlementBonus = GetOfferPriceImprovementOffsetForFaction(faction, negotiator);
            float markupMultiplier = DetermineMarkupMultiplier();
            float total = basePrice * markupMultiplier;
            total -= total * negotiatorBonus;
            total -= total * settlementBonus;
                
                //TradeUtility.GetPricePlayerBuy(item.AnyThing, basePrice, negotiatorBonus, settlementBonus);

            // Divide by 1.4 because that's the price multiplier for buying
            // and we want to have a 1.6 multiplier for buying
            return total;
        }

        private float GetOfferPriceImprovementOffsetForFaction(Faction faction, Pawn negotiator)
        {
            // based on faction relations
            return 0;
        }

        private float DetermineMarkupMultiplier()
        {
            // Price should be increased based on following factors:
            // - rarity of item
            // - distance of hailing colony from player colony

            // standard requesting markup 1.6 (buying markup is 1.4 so 1.6 makes sense for a specific request)
            return 1.6f;
        }

        private void DetermineRequestableItems()
        {
            requestableItems.Clear();
            List<Thing> things = (from x in ThingDatabase.Instance.AllThingsOfType(thingTypeFilter)
                                  where hasMaximumTechLevel(x, faction.def.techLevel)
                                  select x.thing).ToList();
            
            things.ForEach(thing =>
            {
                // Put no cap on quantity you can request of a single item
                thing.stackCount = int.MaxValue;
                Tradeable trad = new Tradeable(thing, thing);
                bool madeOfRightStuff = stuffTypeFilter == null || thing.Stuff == stuffTypeFilter;
                if (!trad.IsCurrency && madeOfRightStuff)
                {
                    trad.thingsColony = new List<Thing>();
                    string key = thing.def.label;
                    if (colonyItemCount.ContainsKey(key))
                    {
                        Thing colonyThing = ThingMaker.MakeThing(thing.def, thing.Stuff);
                        colonyThing.stackCount = colonyItemCount[key];
                        trad.thingsColony.Add(colonyThing);
                    }
                    requestableItems.Add(trad);
                }
            });

            Log.Message("There are " + requestableItems.Count.ToString() + " requestable items to show for filter " +
                thingTypeFilter.ToString() + " and for stuff " + (stuffTypeFilter == null ? " all" : stuffTypeFilter.LabelCap));
        }

        protected void UpdateAvailableMaterials()
        {
            stuffFilterSet.Clear();
            foreach (Tradeable item in requestableItems)
            {
                if (item.StuffDef != null)
                {
                    stuffFilterSet.Add(item.StuffDef);
                }
            }
            if (stuffTypeFilter != null && !stuffFilterSet.Contains(stuffTypeFilter))
            {
                stuffTypeFilter = null;
            }
        }

        private bool hasMaximumTechLevel(ThingEntry entry, TechLevel tLevel)
        {
            int lvl = (int)entry.def.techLevel;
            return lvl <= (int)tLevel;
        }

    }

    // TODO:
    // Create a window that appears after the request
    // has been confirmed and caravan is on
    // its way.
    public class RequestAcknowledgedWindow : Window
    {
        public override void DoWindowContents(Rect inRect)
        {
            throw new NotImplementedException();
        }
    }
}
