using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace ItemRequests;

public class ItemRequestWindow : Window
{
    private const float colonyItemCountAreaWidth = 100;

    private const float iconNameAreaWidth = 350;

    private const float priceTextAreaWidth = 100;

    // Regular buying markup is 1.4 so 1.5 makes sense for a specific request
    private const float requestingItemMarkupMultiplier = 1.5f;

    private const float resetItemCountAreaWidth = 40;

    private static readonly Vector2 AcceptButtonSize = new Vector2(160, 40f);

    private static readonly Vector2 OtherBottomButtonSize = new Vector2(160, 40f);

    private static string searchText = "";

    // For listing the items
    private readonly List<ThingEntry> allRequestableItems = [];

    private readonly string colonyCountTooltipText = "IR.ItemRequestWindow.ColonyCountTooltip".Translate();

    private readonly Dictionary<string, int> colonyItemCount = new Dictionary<string, int>();

    // For reference (set in constructor)
    private readonly int colonySilver;

    private readonly Faction faction;

    private readonly List<ThingEntry> filteredRequestableItems = [];

    private readonly Map map;

    private readonly Pawn negotiator;

    private readonly RequestSession requestSession;

    private readonly HashSet<ThingDef> stuffFilterSet = [];

    // For UI layout
    private float rightAlignOffset;

    private float rightContentSize;

    private Vector2 scrollPosition = Vector2.zero;

    private ThingDef stuffTypeFilter;

    private ThingType thingTypeFilter = ThingType.Resources;

    public ItemRequestWindow(Map map, Faction faction, Pawn negotiator)
    {
        this.map = map;
        this.faction = faction;
        this.negotiator = negotiator;
        colonySilver = map.resourceCounter.Silver;
        absorbInputAroundWindow = true;
        forcePause = true;
        requestSession = Find.World.GetComponent<RequestSession>();

        // Find all items in stockpiles and store counts in dictionary
        var slotGroups = new List<SlotGroup>(map.haulDestinationManager.AllGroups.ToList());
        slotGroups.ForEach(
            group =>
            {
                group.HeldThings.ToList().ForEach(
                    thing =>
                    {
                        var key = thing.def.label;
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

        AttemptDatabaseReload();
        Resize();
    }

    public override Vector2 InitialSize => new Vector2(WindowSize.x, WindowSize.y);

    private Vector2 ContentMargin { get; set; }

    private Rect ContentRect { get; set; }

    private Rect ScrollRect { [UsedImplicitly] get; set; }

    // For being a subclass of Window
    private Vector2 WindowSize { get; set; }

    public override void DoWindowContents(Rect inRect)
    {
        // Begin Window group
        GUI.BeginGroup(inRect);

        // Draw the names of priceNegotiator and factions
        inRect = inRect.AtZero();
        var x = ContentMargin.x;
        var headerRowHeight = 80f;
        var headerRowRect = new Rect(x, 0, inRect.width - x, headerRowHeight);
        DrawWindowHeader(headerRowRect, headerRowHeight);

        x = headerRowRect.x;

        // Draws the $$ amount available
        var rowWidth = inRect.width - 16f;
        var rowRect = new Rect(x, headerRowHeight, rowWidth, 30f);
        DrawAvailableColonyCurrency(rowRect, colonySilver);

        GUI.color = Color.gray;
        Widgets.DrawLineHorizontal(x, headerRowHeight + rowRect.height - 2, rowWidth);

        if (ThingDatabase.Instance.Loaded && allRequestableItems.Count > 0)
        {
            // Draw the main scroll view area
            GUI.color = Color.white;
            var addedMainRectPadding = 30f;
            var buttonHeight = 38f;
            var mainArea = new Rect(x, headerRowHeight + addedMainRectPadding, inRect.width - x,
                inRect.height - headerRowHeight - buttonHeight - addedMainRectPadding - 20f);
            DrawTradeableContent(mainArea);
        }
        else
        {
            string stillLoadingItems = "IR.ItemRequestWindow.ItemsStillLoading".Translate();
            var textSize = Text.CalcSize(stillLoadingItems);
            var middle = new Rect((inRect.width / 2) - (textSize.x / 2), inRect.height / 2, textSize.x, textSize.y);
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(middle, stillLoadingItems);
            GenUI.ResetLabelAlign();

            AttemptDatabaseReload();
        }

        // Draw the buttons at bottom
        DrawButtons(inRect, rowRect);

        // End Window group
        GUI.EndGroup();
    }

    public override void PostClose()
    {
        requestSession.CloseSession();
    }

    private void AttemptDatabaseReload()
    {
        DetermineAllRequestableItems();
        FilterRequestableItems();
    }

    private float CalcRequestedItemPrice(Tradeable item)
    {
        if (item.IsCurrency)
        {
            return item.BaseMarketValue;
        }

        var basePrice = item.BaseMarketValue;
        var negotiatorBonus = negotiator.GetStatValue(StatDefOf.TradePriceImprovement);
        var settlementBonus = GetOfferPriceImprovementOffsetForFaction(faction);
        var distMultiplier = DetermineDistMultiplier(out _);
        var total = basePrice * distMultiplier * requestingItemMarkupMultiplier;
        total -= total * negotiatorBonus;
        total -= total * settlementBonus;

        return total;
    }

    // MAYBE TODO?: if goodwill > 80 should have change of having more advanced items (some chance 
    // of containing any number of restricted item on the restricted list)
    private void DetermineAllRequestableItems()
    {
        if (!ThingDatabase.Instance.Loaded)
        {
            return;
        }

        var thingEntries = (from x in ThingDatabase.Instance.AllThings()
            where hasMaximumTechLevel(x, faction.def.techLevel)
            where isBuyableItem(x)
            select x).ToList();

        foreach (var originalEntry in thingEntries)
        {
            var thingEntry = originalEntry.Clone();
            if (thingEntry.pawnDef != null)
            {
                var pawn = thingEntry.thing as Pawn;
                var trad = new Tradeable(pawn, pawn) { thingsColony = [] };
                thingEntry.tradeable = trad;
                allRequestableItems.Add(thingEntry);
            }
            else
            {
                var thing = thingEntry.thing;
                thing.stackCount = int.MaxValue;
                var trad = new Tradeable(thing, thing);
                if (trad.IsCurrency)
                {
                    continue;
                }

                trad.thingsColony = [];
                var key = thing.def.label;
                if (key != null)
                {
                    if (thing.Stuff != null)
                    {
                        key += thing.Stuff.label;
                    }

                    if (colonyItemCount.TryGetValue(key, out var value))
                    {
                        var colonyThing = ThingMaker.MakeThing(thing.def, thing.Stuff);
                        colonyThing.stackCount = value;
                        trad.thingsColony.Add(colonyThing);
                    }
                }

                thingEntry.tradeable = trad;
                allRequestableItems.Add(thingEntry);
            }
        }
    }

    private float DetermineDistMultiplier(out float daysToTravel)
    {
        var distToColonyInTicks = CaravanManager.DetermineJourneyTime(faction, map);
        daysToTravel = (float)distToColonyInTicks / CaravanManager.fullDayInTicks;
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

    private void DrawAvailableColonyCurrency(Rect rowRect, int availableSilver)
    {
        // Begin row
        GUI.BeginGroup(rowRect);
        float silverIconWidth = 25;
        var iconNameItemWidth = iconNameAreaWidth - silverIconWidth;
        float x = 0;
        Text.Font = GameFont.Small;

        // Draw icon for silver
        var silverGraphicRect = new Rect(x, 0, silverIconWidth, rowRect.height);
        Widgets.ThingIcon(silverGraphicRect, ThingDefOf.Silver);

        x += silverIconWidth + 10;

        // Draw the label "Silver"
        Text.Anchor = TextAnchor.MiddleLeft;
        var textRectPadded = new Rect(x, 0, iconNameItemWidth, rowRect.height);
        textRectPadded.xMin += 5;
        textRectPadded.xMax -= 5;
        Widgets.Label(textRectPadded, "IR.ItemRequestWindow.Silver".Translate());

        x += iconNameItemWidth + priceTextAreaWidth;

        // Draw the available silver for colony
        var silverCountRect = new Rect(x, 0, colonyItemCountAreaWidth, rowRect.height);
        var paddedRect = silverCountRect;
        paddedRect.xMin += 5f;
        paddedRect.xMax -= 5f;
        Widgets.Label(paddedRect, availableSilver.ToString());

        // Draw the amount currently requested by colony
        Text.Anchor = TextAnchor.MiddleCenter;
        string tooltipString = "IR.ItemRequestWindow.TotalValueRequestedTooltip".Translate();
        if (requestSession?.deal?.TotalRequestedValue > availableSilver)
        {
            GUI.color = Color.yellow;
            tooltipString += "\n\n" + "IR.ItemRequestWindow.TotalValueRequestedCaution".Translate();
        }

        var requestedAmountArea = new Rect(rightAlignOffset, 0, rightContentSize, rowRect.height);
        Widgets.Label(requestedAmountArea, requestSession?.deal?.TotalRequestedValue.ToStringMoney("F2"));
        TooltipHandler.TipRegion(requestedAmountArea, tooltipString);

        // Finish row
        GUI.color = Color.white;
        GenUI.ResetLabelAlign();
        GUI.EndGroup();
    }

    private void DrawButtons(Rect inRect, Rect rowRect)
    {
        var confirmButtonRect = new Rect(inRect.width - AcceptButtonSize.x, inRect.height - AcceptButtonSize.y,
            AcceptButtonSize.x, AcceptButtonSize.y);
        if (Widgets.ButtonText(confirmButtonRect, "IR.ItemRequestWindow.Confirm".Translate(), true, false))
        {
            void OnConfirmed()
            {
                SoundDefOf.ExecuteTrade.PlayOneShotOnCamera();
                Find.WindowStack.Add(
                    new RequestAcknowledgedWindow(
                        faction,
                        () =>
                        {
                            requestSession.SetTimeOfOccurence(faction,
                                Find.TickManager.TicksGame + CaravanManager.DetermineJourneyTime(faction, map));
                            Close(false);
                            CaravanManager.SendRequestedCaravan(faction, map);
                        }));
            }

            void OnCancelled()
            {
            }

            if (colonySilver < requestSession.deal.TotalRequestedValue)
            {
                string title = "IR.ConfirmRequestWindow.WindowTitle".Translate();
                string message = "IR.ConfirmRequestWindow.WindowMessage".Translate();
                string confirmString = "IR.ConfirmRequestWindow.Confirm".Translate();
                string cancelString = "IR.ConfirmRequestWindow.Cancel".Translate();
                Find.WindowStack.Add(new ConfirmRequestWindow(OnConfirmed, OnCancelled, title, message,
                    confirmString, cancelString));
            }
            else if (requestSession.deal.GetRequestedItems().Count == 0)
            {
                string title = "IR.ConfirmEmptyRequestWindow.WindowTitle".Translate();
                string message = "IR.ConfirmEmptyRequestWindow.WindowMessage".Translate(faction.Name);
                string confirmString = "IR.ConfirmEmptyRequestWindow.Confirm".Translate();
                Find.WindowStack.Add(
                    new ConfirmRequestWindow(
                        () =>
                        {
                            Close(false);
                            requestSession.CloseOpenDealWith(faction);
                            requestSession.CloseSession();
                        },
                        null,
                        title,
                        message,
                        confirmString,
                        null));
            }
            else
            {
                OnConfirmed();
            }
        }

        var cancelButtonRect = new Rect(rowRect.x, confirmButtonRect.y, OtherBottomButtonSize.x,
            OtherBottomButtonSize.y);
        if (!Widgets.ButtonText(cancelButtonRect, "IR.ItemRequestWindow.Cancel".Translate(), true, false))
        {
            return;
        }

        requestSession.CloseOpenDealWith(faction);
        requestSession.CloseSession();
        Close();
    }

    private void DrawFilterDropdowns(Rect rectArea)
    {
        var filterLabelArea = rectArea;
        filterLabelArea.width = 80;
        Widgets.Label(filterLabelArea, "IR.ItemRequestWindow.Filters".Translate());

        // Draw the thing type filter
        float filterDropdownHeight = 27;
        float filterDropdownWidth = 130;
        Text.Anchor = TextAnchor.UpperLeft;
        Text.Font = GameFont.Small;

        searchText =
            Widgets.TextField(
                new Rect(filterLabelArea.width + 20, rectArea.y - 6, filterDropdownWidth * 1.6f,
                    filterDropdownHeight),
                searchText);
        TooltipHandler.TipRegion(new Rect(filterLabelArea.width + 20, rectArea.y - 6, filterDropdownWidth * 1.6f,
            filterDropdownHeight), "IR.search".Translate());

        var thingFilterDropdownArea = new Rect(filterLabelArea.width * 4, rectArea.y - 6, filterDropdownWidth,
            filterDropdownHeight);
        if (WidgetDropdown.Button(thingFilterDropdownArea, thingTypeFilter.Translate(), true, false,
                ThingDatabase.Instance.Loaded))
        {
            var thingTypes = Enum.GetValues(typeof(ThingType));
            var filterOptions = new List<FloatMenuOption>();
            foreach (ThingType type in thingTypes)
            {
                if (type == ThingType.Discard)
                {
                    continue;
                }

                filterOptions.Add(
                    new FloatMenuOption(
                        type.Translate(),
                        () =>
                        {
                            if (thingTypeFilter == type)
                            {
                                return;
                            }

                            thingTypeFilter = type;
                            stuffTypeFilter = null;
                            FilterRequestableItems();
                            UpdateAvailableMaterials();

                            // FilterRequestableItems();
                        }));
            }

            Find.WindowStack.Add(new FloatMenu(filterOptions, null));
        }

        if (!stuffFilterSet.Any())
        {
            return;
        }

        // Draw the stuff filter
        var stuffFilterDropdownArea = thingFilterDropdownArea;
        stuffFilterDropdownArea.x += thingFilterDropdownArea.width + 10;
        var stuffFilterLabel = stuffTypeFilter?.label ?? "IR.ItemRequestWindow.FilterAll".Translate();
        if (!WidgetDropdown.Button(stuffFilterDropdownArea, stuffFilterLabel, true, false,
                ThingDatabase.Instance.Loaded))
        {
            return;
        }

        var stuffFilterOptions = new List<FloatMenuOption>
        {
            new FloatMenuOption(
                "IR.ItemRequestWindow.FilterAll".Translate(),
                () =>
                {
                    if (stuffTypeFilter == null)
                    {
                        return;
                    }

                    stuffTypeFilter = null;
                    FilterRequestableItems();
                })
        };

        foreach (var item in stuffFilterSet.OrderBy(def => def.label))
        {
            stuffFilterOptions.Add(
                new FloatMenuOption(
                    item.label,
                    () =>
                    {
                        if (stuffTypeFilter == item)
                        {
                            return;
                        }

                        stuffTypeFilter = item;
                        FilterRequestableItems();
                    }));
        }

        Find.WindowStack.Add(new FloatMenu(stuffFilterOptions, null));
    }

    private float DrawPrice(Rect rect, Tradeable trad)
    {
        rect = rect.Rounded();
        if (Mouse.IsOver(rect))
        {
            Widgets.DrawHighlight(rect);
        }

        var priceType = GetPriceTypeFor(trad);
        var finalPrice = CalcRequestedItemPrice(trad);
        TooltipHandler.TipRegion(rect,
            new TipSignal(() => GetPriceTooltip(faction, negotiator, trad, finalPrice), trad.GetHashCode() * 297));
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

        var label = finalPrice.ToStringMoney("F2");
        var priceTextArea = new Rect(rect);
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

    private void DrawTradeableContent(Rect mainRect)
    {
        Text.Font = GameFont.Small;
        float constScrollbarSize = 16;
        var filteredItems = filteredRequestableItems;

        if (!string.IsNullOrEmpty(searchText))
        {
            filteredItems = filteredItems.Where(entry => entry.Label.ToLower().Contains(searchText.ToLower())).ToList();
        }

        var cumulativeContentHeight = 6f + (filteredItems.Count * 30f);
        var contentRect = new Rect(mainRect.x, 0, mainRect.width - constScrollbarSize, cumulativeContentHeight);
        var bottom = scrollPosition.y - 30f;
        var top = scrollPosition.y + mainRect.height;
        var y = 6f;

        Widgets.BeginScrollView(mainRect, ref scrollPosition, contentRect);

        for (var i = 0; i < filteredItems.Count; i++)
        {
            if (y > bottom && y < top)
            {
                var rect = new Rect(mainRect.x, y, contentRect.width, 30f);
                DrawTradeableRow(rect, filteredItems[i], i);
            }

            y += 30f;
        }

        Widgets.EndScrollView();
    }

    private void DrawTradeableLabels(Rect rowRect, ThingEntry entry)
    {
        var trade = entry.tradeable;
        if (!trade.HasAnyThing)
        {
            return;
        }

        if (Mouse.IsOver(rowRect))
        {
            Widgets.DrawHighlight(rowRect);
        }

        var thingIconArea = new Rect(0f, 0f, 27f, 27f);
        Widgets.ThingIcon(thingIconArea, trade.AnyThing);
        Widgets.InfoCardButton(40f, 0f, trade.AnyThing);

        Text.Anchor = TextAnchor.MiddleLeft;
        Text.WordWrap = false;
        var itemLabelArea = new Rect(80f, 0f, rowRect.width - 80f, rowRect.height);

        var itemLabel = trade.LabelCap;
        if (entry.animal)
        {
            itemLabel += $" ({entry.GenderString()})";
        }
        else if (entry.type.HasQuality() && itemLabel.IndexOf("(normal)", StringComparison.Ordinal) != -1)
        {
            itemLabel = itemLabel.Substring(0, itemLabel.IndexOf("(normal)", StringComparison.Ordinal));
        }

        Widgets.Label(itemLabelArea, itemLabel);

        Text.WordWrap = true;
        Transferable localTrad = trade;
        TooltipHandler.TipRegion(
            rowRect,
            new TipSignal(
                () =>
                {
                    if (!localTrad.HasAnyThing)
                    {
                        return string.Empty;
                    }

                    var text = trade.LabelCap;
                    var tipDescription = localTrad.TipDescription;
                    if (!tipDescription.NullOrEmpty())
                    {
                        text = $"{text}: {tipDescription}";
                    }

                    return text;
                },
                localTrad.GetHashCode()));
    }

    private void DrawTradeableRow(Rect rowRect, ThingEntry entry, int index)
    {
        if (index % 2 == 1)
        {
            Widgets.DrawLightHighlight(rowRect);
        }

        var trade = entry.tradeable;

        // Begin Row group
        GUI.BeginGroup(rowRect);
        float x = 0; // starting from left

        // Draw item icon and info icon
        Text.Anchor = TextAnchor.MiddleLeft;
        var idRect = new Rect(x, 0, iconNameAreaWidth, rowRect.height);
        TransferableUIUtility.DoExtraIcons(trade, rowRect, ref x);
        DrawTradeableLabels(idRect, entry);

        x += iconNameAreaWidth;

        // Draw the price for requesting the item
        var priceTextArea = new Rect(x, 0, priceTextAreaWidth, rowRect.height);
        var price = DrawPrice(priceTextArea, trade);
        x += priceTextAreaWidth;

        // Draw the number the colony currently has, if any
        var countHeldBy = trade.CountHeldBy(Transactor.Colony);
        if (countHeldBy != 0)
        {
            var colonyItemCountArea = new Rect(x, 0, colonyItemCountAreaWidth, rowRect.height);
            if (Mouse.IsOver(colonyItemCountArea))
            {
                Widgets.DrawHighlight(colonyItemCountArea);
            }

            var paddedRect = colonyItemCountArea;
            paddedRect.xMin += 5f;
            paddedRect.xMax -= 5f;
            Widgets.Label(paddedRect, countHeldBy.ToStringCached());
            TooltipHandler.TipRegion(colonyItemCountArea, colonyCountTooltipText);
        }

        // Draw the input box to select number of requests
        var countAdjustInterfaceRect = new Rect(rightAlignOffset, 0, rightContentSize, rowRect.height);
        var interactiveNumericFieldArea = new Rect(countAdjustInterfaceRect.center.x - 45f,
            countAdjustInterfaceRect.center.y - 12.5f, 90f, 25f).Rounded();
        var paddedNumericFieldArea = interactiveNumericFieldArea.ContractedBy(2f);
        paddedNumericFieldArea.xMax -= 15f;
        paddedNumericFieldArea.xMin += 16f;

        if (requestSession?.deal != null)
        {
            var amountRequested = requestSession.deal.GetCountForItem(thingTypeFilter, trade);
            var amountAsString = amountRequested.ToString();
            Widgets.TextFieldNumeric(paddedNumericFieldArea, ref amountRequested, ref amountAsString, 0,
                float.MaxValue);
            requestSession.deal.AdjustItemRequest(thingTypeFilter, entry, amountRequested, price);

            // Draw the reset to zero button by input field
            if (amountRequested > 0)
            {
                var resetToZeroButton = interactiveNumericFieldArea;
                resetToZeroButton.x -= resetItemCountAreaWidth - 5;
                resetToZeroButton.width = resetItemCountAreaWidth;
                if (Widgets.ButtonText(resetToZeroButton, "0"))
                {
                    requestSession.deal.AdjustItemRequest(thingTypeFilter, entry, 0, price);
                }
            }
        }

        // End Row group
        GenUI.ResetLabelAlign();
        GUI.EndGroup();
    }

    // MAYBE TODO: request quality of items as well?
    // MAYBE TODO: request prisoners (may not always be available)
    private void DrawWindowHeader(Rect headerRowRect, float headerRowHeight)
    {
        // Begin Header group
        GUI.BeginGroup(headerRowRect);
        Text.Font = GameFont.Medium;

        // Draw player priceFaction name
        var playerFactionNameArea = new Rect(0, 0, headerRowRect.width / 2, headerRowRect.height);
        Text.Anchor = TextAnchor.UpperLeft;
        Widgets.Label(playerFactionNameArea, Faction.OfPlayer.Name.Truncate(playerFactionNameArea.width));

        // Draw trader name
        var tradingFactionNameArea =
            new Rect(headerRowRect.width / 2, 0, headerRowRect.width / 2, headerRowRect.height);
        Text.Anchor = TextAnchor.UpperRight;
        var tradingFactionName = faction.Name;
        if (Text.CalcSize(tradingFactionName).x > tradingFactionNameArea.width)
        {
            tradingFactionName = tradingFactionName.Truncate(tradingFactionNameArea.width);
        }

        Widgets.Label(tradingFactionNameArea, tradingFactionName);

        // Draw just below player priceFaction name
        float amountRequestedTextHeight = 20;
        var secondRowY = (headerRowHeight - amountRequestedTextHeight) / 2;
        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.UpperLeft;
        var negotiatorNameArea = new Rect(0, secondRowY - 2, headerRowRect.width / 2, secondRowY);
        Widgets.Label(negotiatorNameArea, "IR.ItemRequestWindow.NegotiatorLabel".Translate(negotiator.LabelShort));

        // Draw just below trader name
        Text.Anchor = TextAnchor.UpperRight;
        var factionTechLevelArea =
            new Rect(headerRowRect.width / 2, secondRowY - 2, headerRowRect.width / 2, secondRowY);
        Widgets.Label(factionTechLevelArea,
            "IR.ItemRequestWindow.TechLevelLabel".Translate(faction.def.techLevel.ToString()));

        // Draw the filter dropdowns
        Text.Anchor = TextAnchor.MiddleLeft;
        var filterDropdownArea = new Rect(0, headerRowHeight - amountRequestedTextHeight,
            headerRowRect.width - rightContentSize, amountRequestedTextHeight);
        DrawFilterDropdowns(filterDropdownArea);

        // Draw the amount requested text
        GUI.color = new Color(1f, 1f, 1f, 0.6f);
        Text.Font = GameFont.Tiny;
        Text.Anchor = TextAnchor.MiddleCenter;
        var clarificationTextArea = new Rect(rightAlignOffset, headerRowHeight - amountRequestedTextHeight,
            rightContentSize, amountRequestedTextHeight);
        Widgets.Label(clarificationTextArea, "IR.ItemRequestWindow.AmountRequested".Translate());

        // End Header group
        GUI.color = Color.white;
        GenUI.ResetLabelAlign();
        GUI.EndGroup();
    }

    private void FilterRequestableItems()
    {
        if (!ThingDatabase.Instance.Loaded)
        {
            return;
        }

        filteredRequestableItems.Clear();
        var thingEntries = (from x in ThingDatabase.Instance.AllThingsOfType(thingTypeFilter)
            where hasMaximumTechLevel(x, faction.def.techLevel)
            select x).ToList();

        foreach (var thingEntry in thingEntries)
        {
            if (thingEntry.def == ThingDefOf.Silver)
            {
                continue;
            }

            var foundEntry = GetTradeableThingEntry(thingEntry.thing);
            if (foundEntry == null)
            {
                // Log.Warning("Could not find matching TradeableThingEntry for " + thingEntry.Label);
            }
            else
            {
                var madeOfRightStuff = stuffTypeFilter == null ||
                                       foundEntry.tradeable.FirstThingTrader.Stuff == stuffTypeFilter;
                if (madeOfRightStuff)
                {
                    filteredRequestableItems.Add(foundEntry);
                }
            }
        }

        // Log.Message("There are " + filteredRequestableItems.Count.ToString() + " requestable items to show for filter " +
        // thingTypeFilter.ToString() + " and for stuff " + (stuffTypeFilter == null ? " all" : stuffTypeFilter.LabelCap));
    }

    private float GetOfferPriceImprovementOffsetForFaction(Faction factionForOffset)
    {
        var goodwill = factionForOffset.RelationWith(Faction.OfPlayer).baseGoodwill;
        var allyGoodwillThreshold = 75;
        var maxImprovementOffset = .60f;
        var priceImprovementRatio = maxImprovementOffset * 100 / allyGoodwillThreshold;
        var priceImprovementOffset = goodwill * priceImprovementRatio / 100;
        return Mathf.Min(priceImprovementOffset, maxImprovementOffset);
    }

    private string GetPriceTooltip(Faction priceFaction, Pawn priceNegotiator, Tradeable trad, float priceFor)
    {
        if (!trad.HasAnyThing)
        {
            return string.Empty;
        }

        string text = "IR.ItemRequestWindow.PriceUponDelivery".Translate();
        text += "\n\n";
        text = text + StatDefOf.MarketValue.LabelCap + ": " + trad.BaseMarketValue.ToStringMoney("F2");

        var text2 = text;

        text = string.Concat([
            text2, "\n  x ", requestingItemMarkupMultiplier.ToString("F2"),
            "IR.ItemRequestWindow.Requesting".Translate()
        ]);

        if (Find.Storyteller.difficulty.tradePriceFactorLoss != 0f)
        {
            text2 = text;
            text =
                $"{text2}\n  x {1f + Find.Storyteller.difficulty.tradePriceFactorLoss:F2} ({"DifficultyLevel".Translate()})";
        }

        var distPriceOffset = DetermineDistMultiplier(out var daysToTravel);
        text += "\n";
        text2 = text;
        text =
            $"{text2}\n{"IR.ItemRequestWindow.DeliveryCharge".Translate(daysToTravel.ToString("F1"))} x{distPriceOffset:F2}";

        text += "\n";
        text2 = text;
        text =
            $"{text2}\n{"YourNegotiatorBonus".Translate()}: -{priceNegotiator.GetStatValue(StatDefOf.TradePriceImprovement).ToStringPercent()}";

        var priceGainSettlement = GetOfferPriceImprovementOffsetForFaction(priceFaction);
        if (priceGainSettlement != 0f)
        {
            text2 = text;
            text =
                $"{text2}\n{"IR.ItemRequestWindow.FactionRelationOffset".Translate()} {(Mathf.Sign(priceGainSettlement) >= 0 ? "-" : "+")}{Mathf.Abs(priceGainSettlement).ToStringPercent()}";
        }

        text += "\n\n";
        text = text + "FinalPrice".Translate() + ": " + priceFor.ToStringMoney("F2");
        if (priceFor <= 0.01f)
        {
            text = $"{text} (" + "minimum".Translate() + ")";
        }

        return text;
    }

    private PriceType GetPriceTypeFor(Tradeable trad)
    {
        var thingDef = trad.ThingDef;
        return thingDef == ThingDefOf.Silver ? PriceType.Undefined : PriceType.Normal;
    }

    private ThingEntry GetTradeableThingEntry(Thing fromThing)
    {
        foreach (var thingEntry in allRequestableItems)
        {
            if (thingEntry.tradeable.FirstThingTrader.ThingID == fromThing.ThingID)
            {
                return thingEntry;
            }
        }

        return null;
    }

    private bool hasMaximumTechLevel(ThingEntry entry, TechLevel tLevel)
    {
        if (entry.def.techLevel > tLevel)
        {
            return false;
        }

        if (RestrictedItems.researchTechCache.ContainsKey(entry.def) &&
            RestrictedItems.researchTechCache[entry.def] > tLevel)
        {
            return false;
        }

        if ((entry.def.intricate || entry.def.thingCategories?.Contains(ThingCategoryDefOf.Techprints) == true) &&
            tLevel < TechLevel.Industrial)
        {
            return false;
        }

        if (entry.def.thingClass == typeof(Building) &&
            (!entry.def.Minifiable || entry.def.designationCategory == null))
        {
            return false;
        }

        return !entry.def.destroyOnDrop;
    }

    private bool isBuyableItem(ThingEntry entry)
    {
        if (entry.animal)
        {
            return !RestrictedItems.Contains(entry.pawnDef);
        }

        return !RestrictedItems.Contains(entry.def);
    }

    private void Resize()
    {
        ContentMargin = new Vector2(10, 18);
        float HeaderHeight = 32;
        var FooterHeight = 40f;
        float WindowPadding = 18;
        WindowSize = new Vector2(800, 800);
        var ContentSize = new Vector2(WindowSize.x - (WindowPadding * 2) - (ContentMargin.x * 2),
            WindowSize.y - (WindowPadding * 2) - (ContentMargin.y * 2) - FooterHeight - HeaderHeight);

        ContentRect = new Rect(ContentMargin.x, ContentMargin.y + HeaderHeight, ContentSize.x, ContentSize.y);

        ScrollRect = new Rect(0, 0, ContentRect.width, ContentRect.height);

        rightContentSize = 200;
        rightAlignOffset = WindowSize.x - WindowPadding - ContentMargin.x - rightContentSize;
    }

    private void UpdateAvailableMaterials()
    {
        stuffFilterSet.Clear();
        foreach (var thingEntry in filteredRequestableItems)
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
}