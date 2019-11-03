using System;
using Verse;
using UnityEngine;

namespace ItemRequests
{
    public class ConfirmRequestWindow : Window
    {
        private Action onConfirm;
        private Action onCancel;
        public override Vector2 InitialSize => new Vector2(500, 500);

        public ConfirmRequestWindow(Action onConfirm, Action onCancel)
        {
            this.onConfirm = onConfirm;
            this.onCancel = onCancel;
            this.absorbInputAroundWindow = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            Vector2 contentMargin = new Vector2(10, 18);
            string title = "IR.ConfirmRequestWindow.WindowTitle".Translate();
            string message = "IR.ConfirmRequestWindow.WindowMessage".Translate();
            string confirmString = "IR.ConfirmRequestWindow.Confirm".Translate();
            string cancelString = "IR.ConfirmRequestWindow.Cancel".Translate();

            GUI.BeginGroup(inRect);

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
            Rect confirmButtonArea = new Rect(x, inRect.height - contentMargin.y * 2, (inRect.width - contentMargin.x) / 2, closeButtonHeight);

            if (Widgets.ButtonText(confirmButtonArea, confirmString, false))
            {
                Close(true);
                onConfirm();
            }

            Text.Anchor = TextAnchor.MiddleRight;
            Rect cancelButtonArea = new Rect(confirmButtonArea.x + confirmButtonArea.width, confirmButtonArea.y, confirmButtonArea.width, closeButtonHeight);
            if (Widgets.ButtonText(cancelButtonArea, cancelString, false))
            {
                Close(false);
                onCancel();
            }

            GenUI.ResetLabelAlign();
            GUI.EndGroup();

            GUI.EndGroup();
        }
    }
}
