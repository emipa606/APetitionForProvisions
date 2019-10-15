using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Sound;
using RimWorld;

namespace ItemRequests
{
    public class ItemRequestWindow : Window
    {
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
            DetermineAvailableItems();
            CalcCachedCurrency();
            Resize();
        }
        protected void Resize()
        {
            ContentMargin = new Vector2(10, 18);
            float HeaderHeight = 32;
            float FooterHeight = 40f;
            float WindowPadding = 18;
            Vector2 WindowSize = new Vector2(700, 1000);
            Vector2 ContentSize = new Vector2(WindowSize.x - WindowPadding * 2 - ContentMargin.x * 2,
                WindowSize.y - WindowPadding * 2 - ContentMargin.y * 2 - FooterHeight - HeaderHeight);

            ContentRect = new Rect(ContentMargin.x, ContentMargin.y + HeaderHeight, ContentSize.x, ContentSize.y);

            ScrollRect = new Rect(0, 0, ContentRect.width, ContentRect.height);
        }

        public override void DoWindowContents(Rect inRect)
        {
            // Begin Window group
            GUI.BeginGroup(inRect);

            inRect = inRect.AtZero();
            float x = ContentMargin.x;
            float rowHeight = 58f;
            Rect headerRowRect = new Rect(x, 0, inRect.width - x, rowHeight);

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
            Text.Font = GameFont.Small;

            // Draw just below player faction name
            Text.Anchor = TextAnchor.UpperLeft;
            Rect negotiatorNameArea = new Rect(0, rowHeight / 2 - 2, headerRowRect.width / 2, headerRowRect.height / 2f);
            Widgets.Label(negotiatorNameArea, "Negotiator".Translate() + ": " + negotiator.LabelShort);

            // Draw just below trader name
            Text.Anchor = TextAnchor.UpperRight;
            Rect factionTechLevelArea = new Rect(headerRowRect.width / 2, rowHeight / 2 - 2, headerRowRect.width / 2, headerRowRect.height / 2f);
            Widgets.Label(factionTechLevelArea, "Tech Level: " + faction.def.techLevel.ToString());
                    
            // End Header group
            GUI.EndGroup();

            x = headerRowRect.x;

            // Draws the $$ amount available
            float rowWidth = inRect.width - 16f;
            Rect rowRect = new Rect(x, rowHeight, rowWidth, 30f);

            DrawAvailableColonyCurrency(rowRect, colonySilver);

            GUI.color = Color.gray;
            Widgets.DrawLineHorizontal(x, 87, rowWidth);

            // ------------------------------------------------------------------------------------------------------ //
            // ------------------------------------------------------------------------------------------------------ //
            
            GUI.color = Color.white;
            x = 30f;

            Rect mainRect = new Rect(0, rowHeight + x, inRect.width, inRect.height - rowHeight - 38f - x - 20f);
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

            Text.Font = GameFont.Small;
            float height = 6f + requestableItems.Count * 30f;
            Rect contentRect = new Rect(0, 0, mainRect.width - 16, height);
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
                    Rect rect = new Rect(0, y, contentRect.width, 30f);
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

            // Draw highlight if mouse is over the label "Silver"
            Rect textRect = new Rect(silverIconWidth, 0, rowWidth - silverIconWidth - silverCountWidth, rowRect.height);
            if (Mouse.IsOver(textRect))
            {
                Widgets.DrawHighlight(textRect);
            }

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

        public static void DrawTradeableRow(Rect rowRect, Tradeable trade, int index, Faction faction, Pawn negotiator)
        {
            if (index % 2 == 1)
            {
                Widgets.DrawLightHighlight(rowRect);
            }
            Text.Font = GameFont.Small;

            // Begin Row group
            GUI.BeginGroup(rowRect);
            float num = rowRect.width;


            // The player is not selling anything, so this
            // area shouldn't be drawn

            //int traderItemCount = trade.CountHeldBy(Transactor.Trader);
            //if (traderItemCount != 0)
            //{
            //    Rect rect2 = new Rect(num - 75, 0, 75, rect.height);
            //    if (Mouse.IsOver(rect2))
            //    {
            //        Widgets.DrawHighlight(rect2);
            //    }
            //    Text.Anchor = TextAnchor.MiddleRight;
            //    Rect rect3 = rect2;
            //    rect3.xMin += 5f;
            //    rect3.xMax -= 5f;
            //    Widgets.Label(rect3, traderItemCount.ToStringCached());
            //    TooltipHandler.TipRegion(rect2, "TraderCount".Translate());
            //    Rect rect4 = new Rect(rect2.x - 100, 0, 100, rect.height);
            //    Text.Anchor = TextAnchor.MiddleRight;
            //    DrawPrice(rect4, trade, TradeAction.PlayerBuys);
            //}


            num -= 175f;

            //Rect rect5 = new Rect(num - 240, 0, 240, rect.height);
            //if (trade.TraderWillTrade)
            //{
            //    bool flash = Time.time - Dialog_Trade.lastCurrencyFlashTime < 1f && trade.IsCurrency;
            //    TransferableUIUtility.DoCountAdjustInterface(rect5, trade, index, trade.GetMinimumToTransfer(), trade.GetMaximumToTransfer(), flash, null, false);
            //}
            //else
            //{
            //    TradeUI.DrawWillNotTradeIndication(rect5, trade);
            //}

            num -= 240f;
            int colonyItemCount = trade.CountHeldBy(Transactor.Colony);
            if (colonyItemCount != 0)
            {
                Rect priceTextArea = new Rect(num - 100, 0, 100, rowRect.height);
                Text.Anchor = TextAnchor.MiddleLeft;
                DrawPrice(priceTextArea, trade, TradeAction.PlayerSells, faction, negotiator);
                Rect colonyItemCountArea = new Rect(priceTextArea.x - 75, 0, 75, rowRect.height);
                if (Mouse.IsOver(colonyItemCountArea))
                {
                    Widgets.DrawHighlight(colonyItemCountArea);
                }
                Text.Anchor = TextAnchor.MiddleLeft;
                Rect rect8 = colonyItemCountArea;
                rect8.xMin += 5f;
                rect8.xMax -= 5f;
                Widgets.Label(rect8, colonyItemCount.ToStringCached());
                TooltipHandler.TipRegion(colonyItemCountArea, "ColonyCount".Translate());
            }

            num -= 175f;
            TransferableUIUtility.DoExtraAnimalIcons(trade, rowRect, ref num);
            Rect idRect = new Rect(0, 0, num, rowRect.height);
            TransferableUIUtility.DrawTransferableInfo(trade, idRect, (!trade.TraderWillTrade) ? TradeUI.NoTradeColor : Color.white);
            GenUI.ResetLabelAlign();

            // End Row group
            GUI.EndGroup();
        }

        private static void DrawPrice(Rect rect, Tradeable trad, TradeAction action, Faction faction, Pawn negotiator)
        {
            if (trad.IsCurrency || !trad.TraderWillTrade)
            {
                return;
            }

            rect = rect.Rounded();
            if (Mouse.IsOver(rect))
            {
                Widgets.DrawHighlight(rect);
            }

            TooltipHandler.TipRegion(rect, new TipSignal(() => trad.GetPriceTooltip(action), trad.GetHashCode() * 297));
            switch (trad.PriceTypeFor(action))
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


            float priceFor = CalcRequestedItemPrice(trad, faction, negotiator);
            string label = priceFor.ToStringMoney("F2");
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

            //List<WorldObject> allWorldObjects = Find.WorldObjects.AllWorldObjects;
            //for (int l = 0; l < allWorldObjects.Count; l++)
            //{
            //    if (allWorldObjects[l].Faction == Faction.OfPlayer)
            //    {
            //        TradeRequestComp component2 = allWorldObjects[l].GetComponent<TradeRequestComp>();
            //        if (component2 != null && component2.ActiveRequest)
            //        {
            //            component2.Disable();
            //        }
            //    }
            //}

            colonySilver = map.resourceCounter.Silver;
        }

        private static float CalcRequestedItemPrice(Tradeable item, Faction faction, Pawn negotiator)
        {
            if (item.IsCurrency)
            {
                return item.BaseMarketValue;
            }

            float basePrice = item.PriceTypeFor(TradeAction.PlayerBuys).PriceMultiplier();
            float negotiatorBonus = negotiator.GetStatValue(StatDefOf.TradePriceImprovement, true);
            float settlementBonus = GetOfferPriceImprovementOffsetForFaction(faction, negotiator);
            float markupMultiplier = DetermineMarkupMultiplier();
            float total = TradeUtility.GetPricePlayerBuy(item.AnyThing, basePrice, negotiatorBonus, settlementBonus);

            return total + (total * markupMultiplier);
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

            return 0.75f;
        }

        private void DetermineAvailableItems()
        {
            requestableItems.Clear();
            switch (faction.def.techLevel)
            {
                case TechLevel.Animal:
                case TechLevel.Neolithic:
                    // for each item of this tech level, add to list
                    // requestableItems.Add()
                    break;

                case TechLevel.Medieval:

                    break;

                case TechLevel.Industrial:

                    break;

                case TechLevel.Spacer:

                    break;

                case TechLevel.Ultra:
                case TechLevel.Archotech:

                    break;

                default:
                    break;
            }
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
