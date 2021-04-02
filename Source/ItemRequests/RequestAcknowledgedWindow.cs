using System;
using RimWorld;
using UnityEngine;
using Verse;

namespace ItemRequests
{
    public class RequestAcknowledgedWindow : Window
    {
        private readonly Faction faction;
        private readonly Action onClose;

        public RequestAcknowledgedWindow(Faction faction, Action doOnClose)
        {
            this.faction = faction;
            onClose = doOnClose;
            absorbInputAroundWindow = true;
        }

        public override Vector2 InitialSize => new(500, 500);

        public override void DoWindowContents(Rect inRect)
        {
            var contentMargin = new Vector2(10, 18);
            string title = "IR.RequestAcknowledgedWindow.WindowTitle".Translate();
            string message = "IR.RequestAcknowledgedWindow.WindowMessage".Translate(faction.Name);
            string closeString = "IR.RequestAcknowledgedWindow.CloseText".Translate();

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
            var messageAreaRect = new Rect(x, headerRowRect.y + headerRowRect.height + 30, inRect.width - (x * 2),
                inRect.height - (contentMargin.y * 2) - headerRowRect.height);
            Widgets.Label(messageAreaRect, message);

            float closeButtonHeight = 30;
            Text.Anchor = TextAnchor.MiddleLeft;
            var closeButtonArea = new Rect(x, inRect.height - (contentMargin.y * 2), 100, closeButtonHeight);
            if (Widgets.ButtonText(closeButtonArea, closeString, false))
            {
                Close();
                onClose();
            }

            GenUI.ResetLabelAlign();
            GUI.EndGroup();
        }
    }
}