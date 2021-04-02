using UnityEngine;
using Verse;
using Verse.Sound;

namespace ItemRequests
{
    // This class was taken from the class Style
    // from the mod EdB Prepare Carefully by edbmods
    // https://github.com/edbmods/EdBPrepareCarefully/blob/develop/Source/Style.cs
    public class Style
    {
        public static Color ColorText = new(0.80f, 0.80f, 0.80f);
        public static Color ColorTextPanelHeader = new(207f / 255f, 207f / 255f, 207f / 255f);

        public static Color ColorPanelBackground = new(36f / 255f, 37f / 255f, 38f / 255f);
        public static Color ColorPanelBackgroundDeep = new(24f / 255f, 24f / 255f, 29f / 255f);
        public static Color ColorPanelBackgroundItem = new(43f / 255f, 44f / 255f, 45f / 255f);
        public static Color ColorPanelBackgroundScrollView = new(30f / 255f, 31f / 255f, 32f / 255f);

        public static Color ColorButton = new(0.623529f, 0.623529f, 0.623529f);
        public static Color ColorButtonHighlight = new(0.97647f, 0.97647f, 0.97647f);
        public static Color ColorButtonDisabled = new(0.27647f, 0.27647f, 0.27647f);
        public static Color ColorButtonSelected = new(1, 1, 1);

        public static Color ColorControlDisabled = new(1, 1, 1, 0.27647f);

        public static Color ColorTableHeader = new(30f / 255f, 31f / 255f, 32f / 255f);
        public static Color ColorTableHeaderBorder = new(63f / 255f, 64f / 255f, 65f / 255f);
        public static Color ColorTableRow1 = new(47f / 255f, 49f / 255f, 50f / 255f);
        public static Color ColorTableRow2 = new(54f / 255f, 56f / 255f, 57f / 255f);
        public static Color ColorTableRowSelected = new(12f / 255f, 12f / 255f, 12f / 255f);

        public static Color ColorWindowBackground = new(21f / 255f, 25f / 255f, 29f / 255f);

        public static Vector2 SizePanelMargin = new(12, 12);
        public static Vector2 SizePanelPadding = new(12, 12);

        public static Vector2 SizeTextFieldArrowMargin = new(4, 0);

        public static float FieldHeight = 22;

        public static void SetGUIColorForButton(Rect rect)
        {
            if (rect.Contains(Event.current.mousePosition))
            {
                GUI.color = ColorButtonHighlight;
            }
            else
            {
                GUI.color = ColorButton;
            }
        }

        public static void SetGUIColorForButton(Rect rect, bool selected)
        {
            if (selected)
            {
                GUI.color = ColorButtonSelected;
            }
            else
            {
                if (rect.Contains(Event.current.mousePosition))
                {
                    GUI.color = ColorButtonHighlight;
                }
                else
                {
                    GUI.color = ColorButton;
                }
            }
        }

        public static void SetGUIColorForButton(Rect rect, bool selected, Color color, Color hoverColor,
            Color selectedColor)
        {
            if (selected)
            {
                GUI.color = selectedColor;
            }
            else
            {
                if (rect.Contains(Event.current.mousePosition))
                {
                    GUI.color = hoverColor;
                }
                else
                {
                    GUI.color = color;
                }
            }
        }
    }


    // This class was taken from the class WidgetDropdown
    // from the mod EdB Prepare Carefully by edbmods
    // https://github.com/edbmods/EdBPrepareCarefully/blob/develop/Source/WidgetDropdown.cs

    [StaticConstructorOnStartup]
    public static class WidgetDropdown
    {
        private static Texture2D TextureButtonBGAtlas;
        private static Texture2D TextureButtonBGAtlasClick;
        private static Texture2D TextureButtonBGAtlasMouseover;
        private static Texture2D TextureDropdownIndicator;

        static WidgetDropdown()
        {
            LoadTextures();
        }

        private static void LoadTextures()
        {
            TextureButtonBGAtlas = ContentFinder<Texture2D>.Get("UI/Widgets/ButtonBG");
            TextureButtonBGAtlasMouseover = ContentFinder<Texture2D>.Get("UI/Widgets/ButtonBGMouseover");
            TextureButtonBGAtlasClick = ContentFinder<Texture2D>.Get("UI/Widgets/ButtonBGClick");
            TextureDropdownIndicator = ContentFinder<Texture2D>.Get("ItemRequests/DropdownIndicator");
        }


        public static bool Button(Rect rect, string label)
        {
            return Button(rect, label, true, false, true);
        }

        public static bool Button(Rect rect, string label, bool drawBackground, bool doMouseoverSound, bool active)
        {
            var anchor = Text.Anchor;
            var color = GUI.color;

            if (drawBackground)
            {
                var atlas = TextureButtonBGAtlas;
                if (Mouse.IsOver(rect))
                {
                    atlas = TextureButtonBGAtlasMouseover;
                    if (Input.GetMouseButton(0))
                    {
                        atlas = TextureButtonBGAtlasClick;
                    }
                }

                Widgets.DrawAtlas(rect, atlas);
                var indicator = new Rect(rect.xMax - 21, rect.MiddleY() - 4, 11, 8);
                GUI.DrawTexture(indicator, TextureDropdownIndicator);
            }

            if (doMouseoverSound)
            {
                MouseoverSounds.DoRegion(rect);
            }

            if (!drawBackground)
            {
                GUI.color = new Color(0.8f, 0.85f, 1f);
                if (Mouse.IsOver(rect))
                {
                    GUI.color = Widgets.MouseoverOptionColor;
                }
            }

            if (drawBackground)
            {
                Text.Anchor = TextAnchor.MiddleCenter;
            }
            else
            {
                Text.Anchor = TextAnchor.MiddleLeft;
            }

            var textRect = new Rect(rect.x, rect.y, rect.width - 12, rect.height);
            Widgets.Label(textRect, label);
            Text.Anchor = anchor;
            GUI.color = color;
            return active && Widgets.ButtonInvisible(rect, false);
        }
    }
}