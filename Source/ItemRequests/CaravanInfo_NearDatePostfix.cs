using System;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace ItemRequests;

[HarmonyPatch(typeof(GlobalControlsUtility), nameof(GlobalControlsUtility.DoDate))]
internal class CaravanInfo_NearDatePostfix
{
    private static TaggedString currentDealMessage = string.Empty;

    private static string TicksToHumanTime(float ticks)
    {
        if (ticks < 2500)
        {
            return "IR.LessThanOneHour".Translate();
        }

        var hours = Math.Ceiling(ticks / 2500);
        switch (hours)
        {
            case 1:
                return "IR.AboutOneHour".Translate();
            case < 24:
                return $"{hours} {"IR.Hours".Translate()}";
            default:
            {
                var days = Math.Round(hours / 24, 1);
                return $"{days} {"IR.Days".Translate()}";
            }
        }
    }

    private static void Postfix(ref float curBaseY)
    {
        var map = Find.CurrentMap;

        if (map is not { IsPlayerHome: true })
        {
            return;
        }

        if (Find.TickManager.TicksGame % 400 == 0)
        {
            var requestSession = Find.World.GetComponent<RequestSession>();
            currentDealMessage = string.Empty;
            if (requestSession == null || !requestSession.openDeals.Any())
            {
                return;
            }

            var maxItemsToShow = 3;
            switch (requestSession.openDeals.Count())
            {
                case 1:
                    maxItemsToShow = 10;
                    break;
                case 2:
                    maxItemsToShow = 5;
                    break;
            }

            var currentTime = Find.TickManager.TicksGame;
            foreach (var requestSessionOpenDeal in requestSession.openDeals.OrderBy(deal =>
                         requestSession.GetTimeOfOccurenceWithFaction(deal.Faction)))
            {
                var timeOfOccurance = requestSession.GetTimeOfOccurenceWithFaction(requestSessionOpenDeal.Faction);
                if (currentDealMessage != string.Empty)
                {
                    currentDealMessage += "\n";
                }

                currentDealMessage +=
                    $"{"IR.NearDatePatch.CaravanInfo".Translate(requestSessionOpenDeal.Faction.NameColored, TicksToHumanTime(timeOfOccurance - currentTime))}";
                var counter = maxItemsToShow;
                foreach (var requestedItem in requestSessionOpenDeal.GetRequestedItems())
                {
                    if (counter == 0)
                    {
                        currentDealMessage +=
                            $"\n...{"IR.NearDatePatch.ItemInfo".Translate(requestSessionOpenDeal.GetRequestedItems().Count - maxItemsToShow)}";
                        break;
                    }

                    currentDealMessage += $"\n- {requestedItem.amount} {requestedItem.item.def.label}";
                    counter--;
                }

                currentDealMessage +=
                    $"\n{"IR.Value".Translate(Math.Round(requestSessionOpenDeal.TotalRequestedValue))}\n";
            }
        }

        if (string.IsNullOrEmpty(currentDealMessage))
        {
            return;
        }

        var rightMargin = 7f;
        var zlRect = new Rect(UI.screenWidth - Alert.Width, curBaseY - 24f, Alert.Width, 24f);
        Text.Font = GameFont.Small;

        if (Mouse.IsOver(zlRect))
        {
            Widgets.DrawHighlight(zlRect);
        }

        GUI.BeginGroup(zlRect);
        Text.Anchor = TextAnchor.UpperRight;
        var rect = zlRect.AtZero();
        rect.xMax -= rightMargin;

        Widgets.Label(rect, "IR.NearDatePatch.Title".Translate());
        Text.Anchor = TextAnchor.UpperLeft;
        GUI.EndGroup();

        TooltipHandler.TipRegion(zlRect, new TipSignal(currentDealMessage));

        curBaseY -= zlRect.height;
    }
}