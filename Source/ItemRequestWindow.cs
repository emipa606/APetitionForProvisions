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
        public Vector2 WindowSize { get; protected set; }
        public Vector2 ContentMargin { get; protected set; }
        public Rect ContentRect { get; protected set; }
        public Rect ScrollRect { get; protected set; }
        protected WidgetTable<ThingEntry> table = new WidgetTable<ThingEntry>();
        public Map map { get; protected set; }

        protected static readonly Vector2 AcceptButtonSize = new Vector2(160, 40f);
        protected static readonly Vector2 OtherBottomButtonSize = new Vector2(160, 40f);
        private List<Tradeable> requestableItems = new List<Tradeable>();
        private int colonySilver;
        private Vector2 scrollPosition = Vector2.zero;
        private Faction faction;
        private Pawn negotiator;

        public ItemRequestWindow(Map map, Faction faction, Pawn negotiator)
        {
            this.map = map;
            this.faction = faction;
            this.negotiator = negotiator;
            DetermineRequestableItems();
            CalcCachedCurrency();
            Resize();
        }
        protected void Resize()
        {
            ContentMargin = new Vector2(10, 18);
            float HeaderHeight = 32;
            float FooterHeight = 40f;
            float WindowPadding = 18;
            WindowSize = new Vector2(700, 900);
            Vector2 ContentSize = new Vector2(WindowSize.x - WindowPadding * 2 - ContentMargin.x * 2,
                WindowSize.y - WindowPadding * 2 - ContentMargin.y * 2 - FooterHeight - HeaderHeight);

            ContentRect = new Rect(ContentMargin.x, ContentMargin.y + HeaderHeight, ContentSize.x, ContentSize.y);

            ScrollRect = new Rect(0, 0, ContentRect.width, ContentRect.height);
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

            inRect = inRect.AtZero();
            float x = ContentMargin.x;
            float headerRowHeight = 58f;
            Rect headerRowRect = new Rect(x, 0, inRect.width - x, headerRowHeight);

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
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            Rect negotiatorNameArea = new Rect(0, headerRowHeight / 2 - 2, headerRowRect.width / 2, headerRowRect.height / 2f);
            Widgets.Label(negotiatorNameArea, "Negotiator".Translate() + ": " + negotiator.LabelShort);

            // Draw just below trader name
            Text.Anchor = TextAnchor.UpperRight;
            Rect factionTechLevelArea = new Rect(headerRowRect.width / 2, headerRowHeight / 2 - 2, headerRowRect.width / 2, headerRowRect.height / 2f);
            Widgets.Label(factionTechLevelArea, "Tech Level: " + faction.def.techLevel.ToString());

            // End Header group
            GUI.EndGroup();

            x = headerRowRect.x;

            // Draws the $$ amount available
            float rowWidth = inRect.width - 16f;
            Rect rowRect = new Rect(x, headerRowHeight, rowWidth, 30f);

            DrawAvailableColonyCurrency(rowRect, colonySilver);

            GUI.color = Color.gray;
            Widgets.DrawLineHorizontal(x, 87, rowWidth);

            // ------------------------------------------------------------------------------------------------------ //
            // ------------------------------------------------------------------------------------------------------ //

            GUI.color = Color.white;
            float addedMainRectPadding = 30f;
            float buttonHeight = 38f;

            Rect mainRect = new Rect(x, headerRowHeight + addedMainRectPadding, inRect.width - x, inRect.height - headerRowHeight - buttonHeight - addedMainRectPadding - 20f);
            FillMainRect(mainRect);
            Rect confirmButtonRect = new Rect(inRect.width / 2f - AcceptButtonSize.x / 2, inRect.height - 55, AcceptButtonSize.x, AcceptButtonSize.y);

            if (Widgets.ButtonText(confirmButtonRect, "Confirm", true, false, true))
            {
                bool success;
                // TODO: need to make custom trade session class
                if (TradeSession.deal.TryExecute(out success))
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
                }

                Event.current.Use();
            }


            Rect resetButtonRect = new Rect(confirmButtonRect.x - 10f - OtherBottomButtonSize.x, confirmButtonRect.y, OtherBottomButtonSize.x, OtherBottomButtonSize.y);
            if (Widgets.ButtonText(resetButtonRect, "Reset", true, false, true))
            {
                SoundDefOf.Tick_Low.PlayOneShotOnCamera(null);
                //TradeSession.deal.Reset(); TODO: need to make custom trade session class
            }

            Rect cancelButtonRect = new Rect(confirmButtonRect.xMax + 10, confirmButtonRect.y, OtherBottomButtonSize.x, OtherBottomButtonSize.y);
            if (Widgets.ButtonText(cancelButtonRect, "Cancel", true, false, true))
            {
                Close(true);
                Event.current.Use();
            }

            // End Window group
            GUI.EndGroup();
        }

        private void FillMainRect(Rect mainRect)
        {


            // =====================
            //  THIS IS WHERE THE
            //  PLAYER WILL REQUEST
            //  WHICH ITEMS THEY
            //  WANT THE FACTION TO
            //  PROVIDE.
            // =====================

            // TODO: Make filter for requestable items

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
                    DrawTradeableRow(rect, requestableItems[i], counter, faction, negotiator);
                }
                y += 30f;
            }

            Widgets.EndScrollView();
        }

        public static void DrawAvailableColonyCurrency(Rect rowRect, int colonySilver)
        {
            // Begin row
            GUI.BeginGroup(rowRect);

            float rowWidth = rowRect.width;
            float silverIconWidth = 25;
            float silverCountWidth = 100;
            Text.Font = GameFont.Small;

            // Draw icon for silver
            Rect silverGraphicRect = new Rect(0, 0, silverIconWidth, rowRect.height);
            Widgets.ThingIcon(silverGraphicRect, ThingDefOf.Silver);

            // Draw the label "Silver"
            Text.Anchor = TextAnchor.MiddleLeft;
            Rect textRectPadded = new Rect(silverIconWidth + 10, 0, 75, rowRect.height);
            textRectPadded.xMin += 5;
            textRectPadded.xMax -= 5;
            Widgets.Label(textRectPadded, "Silver");

            // Draw the available silver for colony
            Text.Anchor = TextAnchor.MiddleCenter;
            Rect silverCountRect = new Rect(rowWidth - silverCountWidth, 0, silverCountWidth, rowRect.height);
            Widgets.Label(silverCountRect, colonySilver.ToString());
            GenUI.ResetLabelAlign();

            // Finish row
            GUI.EndGroup();
        }

        // Only draws first half of row (i.e. no interactable requesting on right side)
        public static void DrawTradeableRow(Rect rowRect, Tradeable trade, int index, Faction faction, Pawn negotiator)
        {
            float iconNameAreaWidth = 300;
            float priceTextAreaWidth = 100;
            float colonyItemCountAreaWidth = 75;

            if (index % 2 == 1)
            {
                Widgets.DrawLightHighlight(rowRect);
            }
            Text.Font = GameFont.Small;

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
            DrawPrice(priceTextArea, trade, faction, negotiator);
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
                TooltipHandler.TipRegion(colonyItemCountArea, "ColonyCount".Translate());
            }

            x += colonyItemCountAreaWidth;

            // End Row group
            GenUI.ResetLabelAlign();
            GUI.EndGroup();
        }

        private static void DrawPrice(Rect rect, Tradeable trad, Faction faction, Pawn negotiator)
        {
            rect = rect.Rounded();
            if (Mouse.IsOver(rect))
            {
                Widgets.DrawHighlight(rect);
            }

            PriceType priceType = GetPriceTypeFor(trad);
            float finalPrice = CalcRequestedItemPrice(trad, faction, negotiator, priceType);
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
        }

        private void CalcCachedCurrency()
        {
            colonySilver = map.resourceCounter.Silver;
        }

        private static PriceType GetPriceTypeFor(Tradeable trad)
        {
            ThingDef thingDef = trad.ThingDef;
            if (thingDef == ThingDefOf.Silver)
            {
                return PriceType.Undefined;
            }

            // PriceTypeFor() in TraderKindDef
            //for (int i = 0; i < this.stockGenerators.Count; i++)
            //{
            //    PriceType result;
            //    if (this.stockGenerators[i].TryGetPriceType(thingDef, action, out result))
            //    {
            //        return result;
            //    }
            //}

            return PriceType.Normal;
        }

        private static string GetPriceTooltip(Faction faction, Pawn negotiator, Tradeable trad, PriceType priceType, float priceFor)
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

        private static float CalcRequestedItemPrice(Tradeable item, Faction faction, Pawn negotiator, PriceType priceType)
        {
            if (item.IsCurrency)
            {
                return item.BaseMarketValue;
            }

            float basePrice = priceType.PriceMultiplier();
            float negotiatorBonus = negotiator.GetStatValue(StatDefOf.TradePriceImprovement, true);
            float settlementBonus = GetOfferPriceImprovementOffsetForFaction(faction, negotiator);
            float markupMultiplier = DetermineMarkupMultiplier();
            float total = TradeUtility.GetPricePlayerBuy(item.AnyThing, basePrice, negotiatorBonus, settlementBonus);

            return total * markupMultiplier;
        }

        private static float GetOfferPriceImprovementOffsetForFaction(Faction faction, Pawn negotiator)
        {
            // based on faction relations
            return 0;
        }

        private static float DetermineMarkupMultiplier()
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

            List<SlotGroup> slotGroups = new List<SlotGroup>(map.haulDestinationManager.AllGroups.ToList());
            Dictionary<string, int> thingCount = new Dictionary<string, int>();
            slotGroups.ForEach(group =>
            {
                Log.Message("Reading group " + group.ToString());
                // FALSE: this is a list of stacks, not amt in stack
                // Assuming this is a list of all instantiated things, instead
                // of just a list of the unique things stored
                group.HeldThings.ToList().ForEach(thing => 
                {
                    Log.Message("  - " + thing.LabelCap + " x" + thing.stackCount.ToString());
                    string key = thing.def.LabelCap;
                    if (thingCount.ContainsKey(key))
                    {
                        thingCount[key] += thing.stackCount;
                    } 
                    else
                    {
                        thingCount[key] = thing.stackCount;
                    }
                });
            });

            List<Thing> things = (from x in ThingDatabase.Instance.AllThings()
                                  where hasMaximumTechLevel(x, faction.def.techLevel)                                  
                                  select x.thing).ToList();

            Log.Message("There are " + things.Count.ToString() + " requestable items to show");
            things.ForEach(thing =>
            {
                Tradeable trad = new Tradeable(thing, thing);
                if (!trad.IsCurrency)
                {
                    trad.thingsColony = new List<Thing>();
                    string key = thing.def.LabelCap;
                    if (thingCount.ContainsKey(key))
                    {
                        Log.Message("The colony has " + thingCount[key].ToString() + " " + thing.LabelCapNoCount + "s");
                        // ???? will work ????
                        for (int i = 0; i < thingCount[key]; ++i)
                        {
                            trad.thingsColony.Add(ThingMaker.MakeThing(thing.def, thing.Stuff));
                        }
                    }
                    
                    requestableItems.Add(trad);
                }
            });
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
