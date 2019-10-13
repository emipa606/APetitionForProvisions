using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;
using RimWorld;
using RimWorld.Planet;

namespace ItemRequests
{
    public class ItemRequestWindow : Window
    {
        public Vector2 ContentMargin { get; protected set; }
        public Vector2 WindowSize { get; protected set; }
        public Vector2 ButtonSize { get; protected set; }
        public Vector2 ContentSize { get; protected set; }
        public Vector2 GenderSize { get; protected set; }
        public string HeaderLabel { get; protected set; }
        public float HeaderHeight { get; protected set; }
        public float RowGroupHeaderHeight { get; protected set; }
        public float FooterHeight { get; protected set; }
        public float WindowPadding { get; protected set; }
        public Rect ContentRect { get; protected set; }
        public Rect ScrollRect { get; protected set; }
        public Rect FooterRect { get; protected set; }
        public Rect HeaderRect { get; protected set; }
        public Rect CancelButtonRect { get; protected set; }
        public Rect ConfirmButtonRect { get; protected set; }
        public Rect SingleButtonRect { get; protected set; }
        protected WidgetTable<ThingEntry> table = new WidgetTable<ThingEntry>();

        protected static readonly Vector2 AcceptButtonSize = new Vector2(160f, 40f);
        protected static readonly Vector2 OtherBottomButtonSize = new Vector2(160f, 40f);
        private List<Tradeable> requestableItems = new List<Tradeable>();
        private int colonySilver;
        private Vector2 scrollPosition = Vector2.zero;
        public Map map { get; protected set; }

        public ItemRequestWindow(Map map, Faction faction)
        {
            this.map = map;
            DetermineAvailableItems(faction);
            CalcCachedCurrency();
            Resize();
        }
        protected void Resize()
        {
            float headerSize = 0;
            headerSize = HeaderHeight;
            if (HeaderLabel != null)
            {
                headerSize = HeaderHeight;
            }

            HeaderHeight = 32;
            FooterHeight = 40f;
            WindowPadding = 18;
            ContentMargin = new Vector2(10f, 18f);
            WindowSize = new Vector2(440f, 584f);
            ButtonSize = new Vector2(140f, 40f);

            ContentSize = new Vector2(WindowSize.x - WindowPadding * 2 - ContentMargin.x * 2,
                WindowSize.y - WindowPadding * 2 - ContentMargin.y * 2 - FooterHeight - headerSize);

            ContentRect = new Rect(ContentMargin.x, ContentMargin.y + headerSize, ContentSize.x, ContentSize.y);

            ScrollRect = new Rect(0, 0, ContentRect.width, ContentRect.height);

            HeaderRect = new Rect(ContentMargin.x, ContentMargin.y, ContentSize.x, HeaderHeight);

            FooterRect = new Rect(ContentMargin.x, ContentRect.y + ContentSize.y + 20,
                ContentSize.x, FooterHeight);

            GenderSize = new Vector2(48, 48);

            SingleButtonRect = new Rect(ContentSize.x / 2 - ButtonSize.x / 2,
                (FooterHeight / 2) - (ButtonSize.y / 2),
                ButtonSize.x, ButtonSize.y);

            CancelButtonRect = new Rect(0,
                (FooterHeight / 2) - (ButtonSize.y / 2),
                ButtonSize.x, ButtonSize.y);
            ConfirmButtonRect = new Rect(ContentSize.x - ButtonSize.x,
                (FooterHeight / 2) - (ButtonSize.y / 2),
                ButtonSize.x, ButtonSize.y);
        }

        public override void DoWindowContents(Rect inRect)
        {
            GUI.BeginGroup(inRect);

            inRect = inRect.AtZero();
            float x = inRect.width - 590f;
            Rect position = new Rect(x, 0f, inRect.width - x, 58f);

            // Start drawing top left (position)
            GUI.BeginGroup(position);
            Text.Font = GameFont.Medium;

            // Draw player faction name
            // Left half of rect
            Rect playerFactionNameArea = new Rect(0f, 0f, position.width / 2f, position.height);
            Text.Anchor = TextAnchor.UpperLeft;
            Widgets.Label(playerFactionNameArea, Faction.OfPlayer.Name.Truncate(playerFactionNameArea.width, null));

            // Draw trader name
            // Right half of rect
            Rect traderNameArea = new Rect(position.width / 2f, 0f, position.width / 2f, position.height);
            Text.Anchor = TextAnchor.UpperRight;
            string traderName = TradeSession.trader.TraderName;
            if (Text.CalcSize(traderName).x > traderNameArea.width)
            {
                Text.Font = GameFont.Small;
                traderName = traderName.Truncate(traderNameArea.width, null);
            }
            Widgets.Label(traderNameArea, traderName);


            Text.Font = GameFont.Small;

            // Draw just below player faction name
            Text.Anchor = TextAnchor.UpperLeft;
            Rect rect3 = new Rect(0f, 27f, position.width / 2f, position.height / 2f);
            Widgets.Label(rect3, "Negotiator".Translate() + ": " + TradeSession.playerNegotiator.LabelShort);

            // Draw just below trader name
            Text.Anchor = TextAnchor.UpperRight;
            Rect rect4 = new Rect(position.width / 2f, 27f, position.width / 2f, position.height / 2f);
            Widgets.Label(rect4, TradeSession.trader.TraderKind.LabelCap);

            GUI.EndGroup();

            x = 0f;

            // Draws the $$ amount for both sides
            float rowWidth = inRect.width - 16f;
            Rect rowRect = new Rect(x, 58f, rowWidth, 30f);

            DrawAvailableColonyCurrency(rowRect, colonySilver);

            GUI.color = Color.gray;
            Widgets.DrawLineHorizontal(x, 87f, rowWidth);

            GUI.color = Color.white;
            x = 30f;

            Rect mainRect = new Rect(0f, 58f + x, inRect.width, inRect.height - 58f - 38f - x - 20f);
            FillMainRect(mainRect);
            Rect rect7 = new Rect(inRect.width / 2f - AcceptButtonSize.x / 2f, inRect.height - 55f, AcceptButtonSize.x, AcceptButtonSize.y);
            if (Widgets.ButtonText(rect7, "Confirm", true, false, true))
            {
                //Action action = delegate ()
                //{
                bool success;
                if (TradeSession.deal.TryExecute(out success))
                {
                    if (success)
                    {
                        //SoundDefOf.ExecuteTrade.PlayOneShotOnCamera(null);
                        Find.WindowStack.Add(new RequestAcknowledgedWindow());
                        this.Close(false);
                    }
                    else
                    {
                        this.Close(true);
                    }
                }
                //};

                Event.current.Use();
            }


            Rect resetButtonArea = new Rect(rect7.x - 10f - OtherBottomButtonSize.x, rect7.y, OtherBottomButtonSize.x, OtherBottomButtonSize.y);
            if (Widgets.ButtonText(resetButtonArea, "Reset", true, false, true))
            {
                SoundDefOf.Tick_Low.PlayOneShotOnCamera(null);
                TradeSession.deal.Reset();
                //this.CountToTransferChanged();
            }

            Rect cancelButtonArea = new Rect(rect7.xMax + 10f, rect7.y, OtherBottomButtonSize.x, OtherBottomButtonSize.y);
            if (Widgets.ButtonText(cancelButtonArea, "Cancel", true, false, true))
            {
                this.Close(true);
                Event.current.Use();
            }

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


            // TODO:
            // requestableItems needs to become
            // a list of items available
            // to be requested.

            Text.Font = GameFont.Small;
            float height = 6f + requestableItems.Count * 30f;
            Rect viewRect = new Rect(0f, 0f, mainRect.width - 16f, height);
            float bottom = scrollPosition.y - 30f;
            float top = scrollPosition.y + mainRect.height;
            float y = 6f;
            int counter;

            Widgets.BeginScrollView(mainRect, ref scrollPosition, viewRect, true);

            for (int i = 0; i < requestableItems.Count; i++)
            {
                counter = i;
                if (y > bottom && y < top)
                {
                    Rect rect = new Rect(0f, y, viewRect.width, 30f);
                    DrawTradeableRow(rect, requestableItems[i], counter);
                }
                y += 30f;
            }
            Widgets.EndScrollView();
        }

        public static void DrawAvailableColonyCurrency(Rect rect, int colonySilver)
        {
            Text.Font = GameFont.Small;
            GUI.BeginGroup(rect);
            float num = rect.width;

            Rect rect6 = new Rect(num - 100f, 0f, 100f, rect.height);
            Rect rect7 = new Rect(rect6.x - 75f, 0f, 75f, rect.height);
            if (Mouse.IsOver(rect7))
            {
                Widgets.DrawHighlight(rect7);
            }
            Text.Anchor = TextAnchor.MiddleLeft;
            Rect rect8 = rect7;
            rect8.xMin += 5f;
            rect8.xMax -= 5f;
            Widgets.Label(rect8, colonySilver.ToString());
            TooltipHandler.TipRegion(rect7, "Colony Silver");
        }

        public static void DrawTradeableRow(Rect rect, Tradeable trade, int index)
        {
            if (index % 2 == 1)
            {
                Widgets.DrawLightHighlight(rect);
            }
            Text.Font = GameFont.Small;
            GUI.BeginGroup(rect);
            float num = rect.width;


            // The player is not selling anything, so this
            // area shouldn't be drawn

            //int traderItemCount = trade.CountHeldBy(Transactor.Trader);
            //if (traderItemCount != 0)
            //{
            //    Rect rect2 = new Rect(num - 75f, 0f, 75f, rect.height);
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
            //    Rect rect4 = new Rect(rect2.x - 100f, 0f, 100f, rect.height);
            //    Text.Anchor = TextAnchor.MiddleRight;
            //    DrawPrice(rect4, trade, TradeAction.PlayerBuys);
            //}


            num -= 175f;

            //Rect rect5 = new Rect(num - 240f, 0f, 240f, rect.height);
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
                Rect rect6 = new Rect(num - 100f, 0f, 100f, rect.height);
                Text.Anchor = TextAnchor.MiddleLeft;
                DrawPrice(rect6, trade, TradeAction.PlayerSells);
                Rect rect7 = new Rect(rect6.x - 75f, 0f, 75f, rect.height);
                if (Mouse.IsOver(rect7))
                {
                    Widgets.DrawHighlight(rect7);
                }
                Text.Anchor = TextAnchor.MiddleLeft;
                Rect rect8 = rect7;
                rect8.xMin += 5f;
                rect8.xMax -= 5f;
                Widgets.Label(rect8, colonyItemCount.ToStringCached());
                TooltipHandler.TipRegion(rect7, "ColonyCount".Translate());
            }

            num -= 175f;
            TransferableUIUtility.DoExtraAnimalIcons(trade, rect, ref num);
            Rect idRect = new Rect(0f, 0f, num, rect.height);
            TransferableUIUtility.DrawTransferableInfo(trade, idRect, (!trade.TraderWillTrade) ? TradeUI.NoTradeColor : Color.white);
            GenUI.ResetLabelAlign();
            GUI.EndGroup();
        }

        private static void DrawPrice(Rect rect, Tradeable trad, TradeAction action)
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
                    GUI.color = new Color(0f, 1f, 0f);
                    break;
                case PriceType.Cheap:
                    GUI.color = new Color(0.5f, 1f, 0.5f);
                    break;
                case PriceType.Normal:
                    GUI.color = Color.white;
                    break;
                case PriceType.Expensive:
                    GUI.color = new Color(1f, 0.5f, 0.5f);
                    break;
                case PriceType.Exorbitant:
                    GUI.color = new Color(1f, 0f, 0f);
                    break;
            }


            float priceFor = CalcRequestedItemPrice(trad);
            string label = priceFor.ToStringMoney("F2");
            Rect rect2 = new Rect(rect);
            rect2.xMax -= 5f;
            rect2.xMin += 5f;
            if (Text.Anchor == TextAnchor.MiddleLeft)
            {
                rect2.xMax += 300f;
            }
            if (Text.Anchor == TextAnchor.MiddleRight)
            {
                rect2.xMin -= 300f;
            }
            Widgets.Label(rect2, label);
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

        private static float CalcRequestedItemPrice(Tradeable item)
        {
            if (item.IsCurrency)
            {
                return item.BaseMarketValue;
            }

            float basePrice = item.PriceTypeFor(TradeAction.PlayerBuys).PriceMultiplier();
            float negotiatorBonus = TradeSession.playerNegotiator.GetStatValue(StatDefOf.TradePriceImprovement, true);
            float settlementBonus = TradeSession.trader.TradePriceImprovementOffsetForPlayer;
            float markupMultiplier = DetermineMarkupMultiplier();
            float total = TradeUtility.GetPricePlayerBuy(item.AnyThing, basePrice, negotiatorBonus, settlementBonus);

            return total + (total * markupMultiplier);
        }

        private static float DetermineMarkupMultiplier()
        {
            // Price should be increased based on following factors:
            // - rarity of item
            // - distance of hailing colony from player colony
            // - faction relation between colonies

            return 0.75f;
        }

        private void DetermineAvailableItems(Faction fromFaction)
        {
            requestableItems.Clear();
            switch (fromFaction.def.techLevel)
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
