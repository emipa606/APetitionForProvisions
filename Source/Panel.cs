using System;
using UnityEngine;
using Verse;

namespace ItemRequests
{
    // This class was originally created by edbmods
    // for the mod EdB Prepare Carefully.
    // https://github.com/edbmods/EdBPrepareCarefully
    public class Panel
    {
        public static Color ColorText = new Color(0.80f, 0.80f, 0.80f);
        public static Color ColorTextPanelHeader = new Color(207f / 255f, 207f / 255f, 207f / 255f);
        public static Color ColorPanelBackground = new Color(36f / 255f, 37f / 255f, 38f / 255f);
        public static Color ColorPanelBackgroundDarker = new Color(24f / 255f, 24f / 255f, 29f / 255f);

        public Rect HeaderLabelRect
        {
            get;
            private set;
        }
        public Rect PanelRect
        {
            get;
            protected set;
        }
        public Rect BodyRect
        {
            get;
            protected set;
        }
        public virtual string PanelHeader
        {
            get
            {
                return null;
            }
        }
        public string Warning
        {
            get;
            set;
        }
        public Panel()
        {
        }

        public virtual void Resize(Rect rect)
        {
            PanelRect = rect;
            BodyRect = new Rect(0, 0, rect.width, rect.height);
            if (PanelHeader != null)
            {
                BodyRect = new Rect(0, 36, rect.width, rect.height - 36);
            }
        }
        public virtual void Draw()
        {
            DrawPanelBackground();
            DrawPanelHeader();
            GUI.BeginGroup(PanelRect);
            try
            {
                DrawPanelContent();
            }
            finally
            {
                GUI.EndGroup();
            }
            GUI.color = Color.white;
        }
        protected virtual void DrawPanelBackground()
        {
            GUI.color = ColorPanelBackground;
            GUI.DrawTexture(PanelRect, BaseContent.WhiteTex);
            GUI.color = Color.white;
        }
        protected virtual void DrawPanelHeader()
        {
            if (PanelHeader == null)
            {
                return;
            }
            HeaderLabelRect = new Rect(10 + PanelRect.xMin, 3 + PanelRect.yMin, PanelRect.width - 30, 40);
            var fontValue = Text.Font;
            var anchorValue = Text.Anchor;
            var colorValue = GUI.color;
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.UpperLeft;
            Widgets.Label(HeaderLabelRect, PanelHeader);
            Text.Font = fontValue;
            Text.Anchor = anchorValue;
            GUI.color = colorValue;
        }
        protected virtual void DrawPanelContent()
        {
            GUI.color = ColorTextPanelHeader;

            GUI.color = Color.white;
        }
    }
}
