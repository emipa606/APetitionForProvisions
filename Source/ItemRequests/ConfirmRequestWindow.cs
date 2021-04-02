using System;
using UnityEngine;
using Verse;

namespace ItemRequests
{
    public class ConfirmRequestWindow : Window
    {
        private readonly string cancelString;
        private readonly string confirmString;
        private readonly string message;
        private readonly Action onCancel;
        private readonly Action onConfirm;
        private readonly string title;

        public ConfirmRequestWindow(Action onConfirm, Action onCancel, string title, string message,
            string confirmString, string cancelString)
        {
            this.onConfirm = onConfirm;
            this.onCancel = onCancel;
            absorbInputAroundWindow = true;
            this.title = title;
            this.message = message;
            this.confirmString = confirmString;
            this.cancelString = cancelString;
        }

        public override Vector2 InitialSize => new(500, 500);

        public override void DoWindowContents(Rect inRect)
        {
            var contentMargin = new Vector2(10, 18);
            GUI.BeginGroup(inRect);

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
            var confirmButtonArea = new Rect(x, inRect.height - (contentMargin.y * 2),
                (inRect.width - contentMargin.x) / 2, closeButtonHeight);

            if (!string.IsNullOrEmpty(confirmString) && onConfirm != null)
            {
                if (Widgets.ButtonText(confirmButtonArea, confirmString, false))
                {
                    Close();
                    onConfirm();
                }
            }

            Text.Anchor = TextAnchor.MiddleRight;
            var cancelButtonArea = new Rect(confirmButtonArea.x + confirmButtonArea.width, confirmButtonArea.y,
                confirmButtonArea.width, closeButtonHeight);

            if (!string.IsNullOrEmpty(cancelString) && onCancel != null)
            {
                if (Widgets.ButtonText(cancelButtonArea, cancelString, false))
                {
                    Close(false);
                    onCancel();
                }
            }

            GenUI.ResetLabelAlign();
            GUI.EndGroup();

            GUI.EndGroup();
        }
    }
}