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

        // For reference (set in constructor)
        private int colonySilver;
        private Faction faction;
        private Pawn negotiator;
        private Map map;
        private RequestSession requestSession;

        // For listing the items
        private List<ThingEntry> allRequestableItems = new List<ThingEntry>();
        private List<ThingEntry> filteredRequestableItems = new List<ThingEntry>();
        private HashSet<ThingDef> stuffFilterSet = new HashSet<ThingDef>();
        private Dictionary<string, int> colonyItemCount = new Dictionary<string, int>();
        private ThingType thingTypeFilter = ThingType.Resources;
        private ThingDef stuffTypeFilter = null;

        // Regular buying markup is 1.4 so 1.5 makes sense for a specific request
        private const float requestingItemMarkupMultiplier = 1.5f;

        // For UI layout
        private float rightAlignOffset;
        private float rightContentSize;
        private const float iconNameAreaWidth = 350;
        private const float priceTextAreaWidth = 100;
        private const float resetItemCountAreaWidth = 40;
        private const float colonyItemCountAreaWidth = 100;
        protected static readonly Vector2 AcceptButtonSize = new Vector2(160, 40f);
        protected static readonly Vector2 OtherBottomButtonSize = new Vector2(160, 40f);
        private string colonyCountTooltipText = "IR.ItemRequestWindow.ColonyCountTooltip".Translate();

        public ItemRequestWindow(Map map, Faction faction, Pawn negotiator)
        {
            this.map = map;
            this.faction = faction;
            this.negotiator = negotiator;
            this.colonySilver = map.resourceCounter.Silver;
            this.absorbInputAroundWindow = true;
            this.forcePause = true;
            this.requestSession = Find.World.GetComponent<RequestSession>();

            // Find all items in stockpiles and store counts in dictionary
            List<SlotGroup> slotGroups = new List<SlotGroup>(map.haulDestinationManager.AllGroups.ToList());
            slotGroups.ForEach(group =>
            {
                group.HeldThings.ToList().ForEach(thing =>
                {
                    string key = thing.def.label;
                    if (thing.Stuff != null)
                    {
                        key += thing.Stuff.label;
                    }
                    if (colonyItemCount.ContainsKey(key))
                    {
                        colonyItemCount[key] += thing.stackCount;
                    }
                    else
                    {
                        colonyItemCount.Add(key, thing.stackCount);
                    }
                });
            });

            DetermineAllRequestableItems();
            FilterRequestableItems();
            Resize();
        }
        protected void Resize()
        {
            ContentMargin = new Vector2(10, 18);
            float HeaderHeight = 32;
            float FooterHeight = 40f;
            float WindowPadding = 18;
            WindowSize = new Vector2(800, 800);
            Vector2 ContentSize = new Vector2(WindowSize.x - WindowPadding * 2 - ContentMargin.x * 2,
                WindowSize.y - WindowPadding * 2 - ContentMargin.y * 2 - FooterHeight - HeaderHeight);

            ContentRect = new Rect(ContentMargin.x, ContentMargin.y + HeaderHeight, ContentSize.x, ContentSize.y);

            ScrollRect = new Rect(0, 0, ContentRect.width, ContentRect.height);

            rightContentSize = 200;
            rightAlignOffset = WindowSize.x - WindowPadding - ContentMargin.x - rightContentSize;
        }

        public override Vector2 InitialSize => new Vector2(WindowSize.x, WindowSize.y);

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

        // MAYBE TODO: request quality of items as well?
        // MAYBE TODO: request prisoners (may not always be available)
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
            Widgets.Label(negotiatorNameArea, "IR.ItemRequestWindow.NegotiatorLabel".Translate(negotiator.LabelShort));

            // Draw just below trader name
            Text.Anchor = TextAnchor.UpperRight;
            Rect factionTechLevelArea = new Rect(headerRowRect.width / 2, secondRowY - 2, headerRowRect.width / 2, secondRowY);
            Widgets.Label(factionTechLevelArea, "IR.ItemRequestWindow.TechLevelLabel".Translate(faction.def.techLevel.ToString()));


            // Draw the filter dropdowns
            Text.Anchor = TextAnchor.MiddleLeft;
            Rect filterDropdownArea = new Rect(0, headerRowHeight - amountRequestedTextHeight, headerRowRect.width - rightContentSize, amountRequestedTextHeight);
            DrawFilterDropdowns(filterDropdownArea);

            // Draw the amount requested text
            GUI.color = new Color(1f, 1f, 1f, 0.6f);
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.MiddleCenter;
            Rect clarificationTextArea = new Rect(rightAlignOffset, headerRowHeight - amountRequestedTextHeight, rightContentSize, amountRequestedTextHeight);
            Widgets.Label(clarificationTextArea, "IR.ItemRequestWindow.AmountRequested".Translate());


            // End Header group
            GUI.color = Color.white;
            GenUI.ResetLabelAlign();
            GUI.EndGroup();
        }

        private void DrawFilterDropdowns(Rect rectArea)
        {
            Rect filterLabelArea = rectArea;
            filterLabelArea.width = 80;
            Widgets.Label(filterLabelArea, "IR.ItemRequestWindow.Filters".Translate());

            // Draw the thing type filter
            float filterDropdownHeight = 27;
            float filterDropdownWidth = 130;
            Text.Anchor = TextAnchor.UpperLeft;
            Rect thingFilterDropdownArea = new Rect(filterLabelArea.width + 20, rectArea.y - 6, filterDropdownWidth, filterDropdownHeight);
            if (WidgetDropdown.Button(thingFilterDropdownArea, thingTypeFilter.Translate(), true, false, true))
            {
                var thingTypes = Enum.GetValues(typeof(ThingType));
                List<FloatMenuOption> filterOptions = new List<FloatMenuOption>();
                foreach (ThingType type in thingTypes)
                {
                    if (type == ThingType.Discard) continue;
                    filterOptions.Add(new FloatMenuOption(type.Translate(), () =>
                    {
                        if (thingTypeFilter != type)
                        {
                            thingTypeFilter = type;
                            FilterRequestableItems();
                            UpdateAvailableMaterials();
                            //FilterRequestableItems();
                        }
                    }));
                }
                Find.WindowStack.Add(new FloatMenu(filterOptions, null, false));
            }

            // Draw the stuff filter
            Rect stuffFilterDropdownArea = thingFilterDropdownArea;
            stuffFilterDropdownArea.x += thingFilterDropdownArea.width + 10;
            string stuffFilterLabel = stuffTypeFilter == null ? "IR.ItemRequestWindow.FilterAll".Translate() : stuffTypeFilter.LabelCap;
            if (WidgetDropdown.Button(stuffFilterDropdownArea, stuffFilterLabel, true, false, true))
            {
                List<FloatMenuOption> stuffFilterOptions = new List<FloatMenuOption>();
                stuffFilterOptions.Add(new FloatMenuOption("IR.ItemRequestWindow.FilterAll".Translate(), () =>
                {
                    if (stuffTypeFilter != null)
                    {
                        stuffTypeFilter = null;
                        FilterRequestableItems();
                    }
                }));

                foreach (ThingDef item in stuffFilterSet.OrderBy((ThingDef def) => { return def.LabelCap; }))
                {
                    stuffFilterOptions.Add(new FloatMenuOption(item.LabelCap, () =>
                    {
                        if (stuffTypeFilter != item)
                        {
                            stuffTypeFilter = item;
                            FilterRequestableItems();
                        }
                    }));
                }
                Find.WindowStack.Add(new FloatMenu(stuffFilterOptions, null, false));
            }
        }

        private void DrawAvailableColonyCurrency(Rect rowRect, int colonySilver)
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
            Widgets.Label(textRectPadded, "IR.ItemRequestWindow.Silver".Translate());

            x += iconNameItemWidth + priceTextAreaWidth;

            // Draw the available silver for colony
            Rect silverCountRect = new Rect(x, 0, colonyItemCountAreaWidth, rowRect.height);
            Rect paddedRect = silverCountRect;
            paddedRect.xMin += 5f;
            paddedRect.xMax -= 5f;
            Widgets.Label(paddedRect, colonySilver.ToString());

            // Draw the amount currently requested by colony
            Text.Anchor = TextAnchor.MiddleCenter;
            string tooltipString = "IR.ItemRequestWindow.TotalValueRequestedTooltip".Translate();
            if (requestSession.deal.TotalRequestedValue > colonySilver)
            {
                GUI.color = Color.yellow;
                tooltipString += "\n\n" + "IR.ItemRequestWindow.TotalValueRequestedCaution".Translate();
            }
            Rect requestedAmountArea = new Rect(rightAlignOffset, 0, rightContentSize, rowRect.height);
            Widgets.Label(requestedAmountArea, requestSession.deal.TotalRequestedValue.ToStringMoney("F2"));
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
            float cumulativeContentHeight = 6f + filteredRequestableItems.Count * 30f;
            Rect contentRect = new Rect(mainRect.x, 0, mainRect.width - constScrollbarSize, cumulativeContentHeight);
            float bottom = scrollPosition.y - 30f;
            float top = scrollPosition.y + mainRect.height;
            float y = 6f;
            int counter;

            Widgets.BeginScrollView(mainRect, ref scrollPosition, contentRect, true);

            for (int i = 0; i < filteredRequestableItems.Count; i++)
            {
                counter = i;
                if (y > bottom && y < top)
                {
                    Rect rect = new Rect(mainRect.x, y, contentRect.width, 30f);
                    DrawTradeableRow(rect, filteredRequestableItems[i], counter);
                }
                y += 30f;
            }

            Widgets.EndScrollView();
        }

        private void DrawButtons(Rect inRect, Rect rowRect)
        {
            Rect confirmButtonRect = new Rect(inRect.width - AcceptButtonSize.x, inRect.height - AcceptButtonSize.y, AcceptButtonSize.x, AcceptButtonSize.y);
            if (Widgets.ButtonText(confirmButtonRect, "IR.ItemRequestWindow.Confirm".Translate(), true, false, true))
            {
                Action onConfirmed = () =>
                {
                    SoundDefOf.ExecuteTrade.PlayOneShotOnCamera(null);
                    Find.WindowStack.Add(new RequestAcknowledgedWindow(faction, () =>
                    {
                        Close(false);
                        requestSession.CloseSession();
                        CaravanManager.SendRequestedCaravan(faction, map);
                    }));
                };
                Action onCancelled = () => { };


                if (colonySilver < requestSession.deal.TotalRequestedValue)
                {
                    Find.WindowStack.Add(new ConfirmRequestWindow(onConfirmed, onCancelled));
                }
                else
                {
                    onConfirmed();
                }
            }


            Rect cancelButtonRect = new Rect(rowRect.x, confirmButtonRect.y, OtherBottomButtonSize.x, OtherBottomButtonSize.y);
            if (Widgets.ButtonText(cancelButtonRect, "IR.ItemRequestWindow.Cancel".Translate(), true, false, true))
            {
                Close(true);
                requestSession.CloseOpenDealWith(faction);
                requestSession.CloseSession();
            }
        }

        private void DrawTradeableRow(Rect rowRect, ThingEntry entry, int index)
        {
            if (index % 2 == 1)
            {
                Widgets.DrawLightHighlight(rowRect);
            }
            Tradeable trade = entry.tradeable;

            // Begin Row group
            GUI.BeginGroup(rowRect);
            float x = 0; // starting from left

            // Draw item icon and info icon
            Text.Anchor = TextAnchor.MiddleLeft;
            Rect idRect = new Rect(x, 0, iconNameAreaWidth, rowRect.height);
            TransferableUIUtility.DoExtraAnimalIcons(trade, rowRect, ref x);
            DrawTradeableLabels(idRect, entry);

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

            // Draw the input box to select number of requests
            Rect countAdjustInterfaceRect = new Rect(rightAlignOffset, 0, rightContentSize, rowRect.height);
            Rect interactiveNumericFieldArea = new Rect(countAdjustInterfaceRect.center.x - 45f, countAdjustInterfaceRect.center.y - 12.5f, 90f, 25f).Rounded();
            Rect paddedNumericFieldArea = interactiveNumericFieldArea.ContractedBy(2f);
            paddedNumericFieldArea.xMax -= 15f;
            paddedNumericFieldArea.xMin += 16f;

            int amountRequested = requestSession.deal.GetCountForItem(thingTypeFilter, trade);
            string amountAsString = amountRequested.ToString();
            Widgets.TextFieldNumeric(paddedNumericFieldArea, ref amountRequested, ref amountAsString, 0, float.MaxValue);
            requestSession.deal.AdjustItemRequest(thingTypeFilter, entry, amountRequested, price);

            // Draw the reset to zero button by input field
            if (amountRequested > 0)
            {
                Rect resetToZeroButton = interactiveNumericFieldArea;
                resetToZeroButton.x -= resetItemCountAreaWidth - 5;
                resetToZeroButton.width = resetItemCountAreaWidth;
                if (Widgets.ButtonText(resetToZeroButton, "0"))
                {                    
                    requestSession.deal.AdjustItemRequest(thingTypeFilter, entry, 0, price);
                }
            }
            
            // End Row group
            GenUI.ResetLabelAlign();
            GUI.EndGroup();
        }

        private void DrawTradeableLabels(Rect rowRect, ThingEntry entry)
        {
            Tradeable trade = entry.tradeable;
            if (!trade.HasAnyThing)
            {
                return;
            }
            if (Mouse.IsOver(rowRect))
            {
                Widgets.DrawHighlight(rowRect);
            }

            Rect thingIconArea = new Rect(0f, 0f, 27f, 27f);
            Widgets.ThingIcon(thingIconArea, trade.AnyThing, 1f);
            Widgets.InfoCardButton(40f, 0f, trade.AnyThing);

            Text.Anchor = TextAnchor.MiddleLeft;
            Text.WordWrap = false;
            Rect itemLabelArea = new Rect(80f, 0f, rowRect.width - 80f, rowRect.height);

            string itemLabel = trade.LabelCap;
            if (entry.animal)
            {
                itemLabel += " (" + entry.GenderString() + ")";
            }
            else if (entry.type.HasQuality() && itemLabel.IndexOf("(normal)") != -1)
            {
                itemLabel = itemLabel.Substring(0, itemLabel.IndexOf("(normal)"));
            }
            Widgets.Label(itemLabelArea, itemLabel);
            
            Text.WordWrap = true;
            Transferable localTrad = trade;
            TooltipHandler.TipRegion(rowRect, new TipSignal(() =>
            {
                if (!localTrad.HasAnyThing)
                {
                    return string.Empty;
                }
                string text = trade.LabelCap;
                string tipDescription = localTrad.TipDescription;
                if (!tipDescription.NullOrEmpty())
                {
                    text = text + ": " + tipDescription;
                }
                return text;
            }, localTrad.GetHashCode()));
        }

        private float DrawPrice(Rect rect, Tradeable trad)
        {
            rect = rect.Rounded();
            if (Mouse.IsOver(rect))
            {
                Widgets.DrawHighlight(rect);
            }

            PriceType priceType = GetPriceTypeFor(trad);
            float finalPrice = CalcRequestedItemPrice(trad);
            TooltipHandler.TipRegion(rect, new TipSignal(() => GetPriceTooltip(faction, negotiator, trad, finalPrice), trad.GetHashCode() * 297));
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

        private string GetPriceTooltip(Faction faction, Pawn negotiator, Tradeable trad, float priceFor)
        {
            if (!trad.HasAnyThing)
            {
                return string.Empty;
            }

            string text = "IR.ItemRequestWindow.PriceUponDelivery".Translate();
            text += "\n\n";
            text = text + StatDefOf.MarketValue.LabelCap + ": " + trad.BaseMarketValue.ToStringMoney("F2");

            string text2 = text;

            text = string.Concat(new string[]
            {
                    text2,
                    "\n  x ",
                    requestingItemMarkupMultiplier.ToString("F2"),
                    "IR.ItemRequestWindow.Requesting".Translate()
            });

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

            float daysToTravel;
            float distPriceOffset = DetermineDistMultiplier(out daysToTravel);
            text += "\n";
            text2 = text;
            text = string.Concat(new string[]
            {
                text2,
                "\n",
                "IR.ItemRequestWindow.DeliveryCharge".Translate(daysToTravel.ToString("F1")),                
                " x",
                distPriceOffset.ToString("F2")
            });

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

            float priceGainSettlement = GetOfferPriceImprovementOffsetForFaction(faction);
            if (priceGainSettlement != 0f)
            {
                text2 = text;
                text = string.Concat(new string[]
                {
                        text2,
                        "\n",
                        "IR.ItemRequestWindow.FactionRelationOffset".Translate(),
                        " ",
                        Mathf.Sign(priceGainSettlement) >= 0 ? "-" : "+",
                        Mathf.Abs(priceGainSettlement).ToStringPercent()
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

        private float CalcRequestedItemPrice(Tradeable item)
        {
            if (item.IsCurrency)
            {
                return item.BaseMarketValue;
            }

            float daysToTravel;
            float basePrice = item.BaseMarketValue;
            float negotiatorBonus = negotiator.GetStatValue(StatDefOf.TradePriceImprovement, true);
            float settlementBonus = GetOfferPriceImprovementOffsetForFaction(faction);
            float distMultiplier = DetermineDistMultiplier(out daysToTravel);
            float total = basePrice * distMultiplier * requestingItemMarkupMultiplier;
            total -= total * negotiatorBonus;
            total -= total * settlementBonus;

            return total;
        }

        private float GetOfferPriceImprovementOffsetForFaction(Faction faction)
        {
            int goodwill = faction.RelationWith(Faction.OfPlayer).goodwill;
            int allyGoodwillThreshold = 75;
            float maxImprovementOffset = .60f;
            float priceImprovementRatio = (maxImprovementOffset * 100) / allyGoodwillThreshold;
            float priceImprovementOffset = (goodwill * priceImprovementRatio) / 100;
            return Mathf.Min(priceImprovementOffset, maxImprovementOffset);
        }

        private float DetermineDistMultiplier(out float daysToTravel)
        {
            int distToColonyInTicks = CaravanManager.DetermineJourneyTime(faction, map);
            daysToTravel = distToColonyInTicks / CaravanManager.fullDayInTicks;
            float distMultiplier = 1;

            if (daysToTravel >= 4)
            {
                distMultiplier = 1.5f;
            }
            else if (daysToTravel >= 2.5f)
            {
                distMultiplier = 1.25f;
            }
            else if (daysToTravel >= 1.5)
            {
                distMultiplier = 1.1f;
            }

            return distMultiplier;
        }

        // MAYBE TODO?: if goodwill > 80 should have change of having more advanced items (some chance 
        //   of containing any number of restricted item on the restricted list)
        private void DetermineAllRequestableItems()
        {
            List<ThingEntry> thingEntries = (from x in ThingDatabase.Instance.AllThings()
                                             where hasMaximumTechLevel(x, faction.def.techLevel)
                                             where isBuyableItem(x)
                                             select x).ToList();

            foreach (ThingEntry originalEntry in thingEntries)
            {
                ThingEntry thingEntry = originalEntry.Clone();
                if (thingEntry.pawnDef != null)
                {
                    Pawn pawn = thingEntry.thing as Pawn;
                    Tradeable trad = new Tradeable(pawn, pawn);
                    trad.thingsColony = new List<Thing>();
                    thingEntry.tradeable = trad;
                    allRequestableItems.Add(thingEntry);
                }
                else
                {
                    Thing thing = thingEntry.thing;
                    thing.stackCount = int.MaxValue;
                    Tradeable trad = new Tradeable(thing, thing);
                    if (!trad.IsCurrency)
                    {
                        trad.thingsColony = new List<Thing>();
                        string key = thing.def.label;
                        if (thing.Stuff != null)
                        {
                            key += thing.Stuff.label;
                        }
                        if (colonyItemCount.ContainsKey(key))
                        {
                            Thing colonyThing = ThingMaker.MakeThing(thing.def, thing.Stuff);
                            colonyThing.stackCount = colonyItemCount[key];
                            trad.thingsColony.Add(colonyThing);
                        }
                        thingEntry.tradeable = trad;
                        allRequestableItems.Add(thingEntry);
                    }
                }
            }
        }

        private ThingEntry GetTradeableThingEntry(Thing fromThing)
        {
            foreach (ThingEntry thingEntry in allRequestableItems)
            {
                if (thingEntry.tradeable.FirstThingTrader.ThingID == fromThing.ThingID) return thingEntry;
            }
            return null;
        }

        private void FilterRequestableItems()
        {
            filteredRequestableItems.Clear();
            List<ThingEntry> thingEntries = (from x in ThingDatabase.Instance.AllThingsOfType(thingTypeFilter)
                                             where hasMaximumTechLevel(x, faction.def.techLevel)
                                             select x).ToList();

            foreach (ThingEntry thingEntry in thingEntries)
            {
                if (thingEntry.def == ThingDefOf.Silver) continue;
                ThingEntry foundEntry = GetTradeableThingEntry(thingEntry.thing);
                if (foundEntry == null)
                {
                    //Log.Warning("Could not find matching TradeableThingEntry for " + thingEntry.Label);
                    continue;
                }
                else
                {
                    bool madeOfRightStuff = stuffTypeFilter == null || foundEntry.tradeable.FirstThingTrader.Stuff == stuffTypeFilter;
                    if (madeOfRightStuff)
                    {
                        filteredRequestableItems.Add(foundEntry);
                    }
                }
            }

            //Log.Message("There are " + filteredRequestableItems.Count.ToString() + " requestable items to show for filter " +
            //    thingTypeFilter.ToString() + " and for stuff " + (stuffTypeFilter == null ? " all" : stuffTypeFilter.LabelCap));
        }

        protected void UpdateAvailableMaterials()
        {
            stuffFilterSet.Clear();
            foreach (ThingEntry thingEntry in filteredRequestableItems)
            {
                if (thingEntry.stuffDef != null)
                {
                    stuffFilterSet.Add(thingEntry.stuffDef);
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

            // there's probably a better way to test for an advanced component but oh well
            if (entry.thing.def.label == "advanced component" && (int)tLevel < (int)TechLevel.Industrial)
            {
                return false;
            }

            if (lvl > 1)
            {
                // Current tech level or one level beneath
                return lvl <= (int)tLevel && lvl >= ((int)tLevel) - 1;
            }
            return lvl <= (int)tLevel;
        }

        private bool isBuyableItem(ThingEntry entry)
        {
            if (entry.animal)
            {
                return !RestrictedItems.Contains(entry.pawnDef);
            }
            return !RestrictedItems.Contains(entry.def);
        }

    }
}
