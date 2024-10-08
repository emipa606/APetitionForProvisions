using UnityEngine;
using Verse;
using Verse.Sound;

namespace ItemRequests;

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

    public static bool Button(Rect rect, string label, bool drawBackground = true, bool doMouseoverSound = false,
        bool active = true)
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

        Text.Anchor = drawBackground ? TextAnchor.MiddleCenter : TextAnchor.MiddleLeft;

        var textRect = new Rect(rect.x, rect.y, rect.width - 12, rect.height);
        Widgets.Label(textRect, label);
        Text.Anchor = anchor;
        GUI.color = color;
        return active && Widgets.ButtonInvisible(rect, false);
    }

    private static void LoadTextures()
    {
        TextureButtonBGAtlas = ContentFinder<Texture2D>.Get("UI/Widgets/ButtonBG");
        TextureButtonBGAtlasMouseover = ContentFinder<Texture2D>.Get("UI/Widgets/ButtonBGMouseover");
        TextureButtonBGAtlasClick = ContentFinder<Texture2D>.Get("UI/Widgets/ButtonBGClick");
        TextureDropdownIndicator = ContentFinder<Texture2D>.Get("ItemRequests/DropdownIndicator");
    }
}