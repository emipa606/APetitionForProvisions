using System;
using RimWorld;
using UnityEngine;
using Verse;

namespace ItemRequests
{
    public class RequestAcknowledgedWindow : Window
    {
        private Faction faction;
        private Action onClose;

        public RequestAcknowledgedWindow(Faction faction, Action doOnClose)
        {
            this.faction = faction;
            this.onClose = doOnClose;
        }

        public override void DoWindowContents(Rect inRect)
        {
            Vector2 contentMargin = new Vector2(10, 18);
            string title = "Request Acknowledged";
            string message = faction.Name + " has agreed to the exchange and will arrive within a few days.\n\n" +
                "Be sure to have the silver for the amount you agreed upon when they arrive. You may have enough " +
                "currently, but life is notoriously perilous on the Rim and you never know what misfortunes await " +
                "you in the next few days.";
            string closeString = "OK";

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
            Rect messageAreaRect = new Rect(x, headerRowRect.y + headerRowRect.height + 30, inRect.width - x * 2, inRect.height - contentMargin.y * 2 - headerRowRect.height);
            Widgets.Label(messageAreaRect, message);

            float closeButtonHeight = 30;
            Text.Anchor = TextAnchor.MiddleLeft;
            Rect closeButtonArea = new Rect(x, inRect.height - contentMargin.y * 2, 100, closeButtonHeight);
            if (Widgets.ButtonText(closeButtonArea, closeString, false))
            {
                Close(true);
                onClose();
            }

            GenUI.ResetLabelAlign();
            GUI.EndGroup();
        }

        public override Vector2 InitialSize => new Vector2(450, 475);
    }
}
