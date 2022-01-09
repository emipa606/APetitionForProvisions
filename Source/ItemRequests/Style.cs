using UnityEngine;

namespace ItemRequests;

// This class was taken from the class Style
// from the mod EdB Prepare Carefully by edbmods
// https://github.com/edbmods/EdBPrepareCarefully/blob/develop/Source/Style.cs
public class Style
{
    public static Color ColorButton = new Color(0.623529f, 0.623529f, 0.623529f);

    public static Color ColorButtonDisabled = new Color(0.27647f, 0.27647f, 0.27647f);

    public static Color ColorButtonHighlight = new Color(0.97647f, 0.97647f, 0.97647f);

    public static Color ColorButtonSelected = new Color(1, 1, 1);

    public static Color ColorControlDisabled = new Color(1, 1, 1, 0.27647f);

    public static Color ColorPanelBackground = new Color(36f / 255f, 37f / 255f, 38f / 255f);

    public static Color ColorPanelBackgroundDeep = new Color(24f / 255f, 24f / 255f, 29f / 255f);

    public static Color ColorPanelBackgroundItem = new Color(43f / 255f, 44f / 255f, 45f / 255f);

    public static Color ColorPanelBackgroundScrollView = new Color(30f / 255f, 31f / 255f, 32f / 255f);

    public static Color ColorTableHeader = new Color(30f / 255f, 31f / 255f, 32f / 255f);

    public static Color ColorTableHeaderBorder = new Color(63f / 255f, 64f / 255f, 65f / 255f);

    public static Color ColorTableRow1 = new Color(47f / 255f, 49f / 255f, 50f / 255f);

    public static Color ColorTableRow2 = new Color(54f / 255f, 56f / 255f, 57f / 255f);

    public static Color ColorTableRowSelected = new Color(12f / 255f, 12f / 255f, 12f / 255f);

    public static Color ColorText = new Color(0.80f, 0.80f, 0.80f);

    public static Color ColorTextPanelHeader = new Color(207f / 255f, 207f / 255f, 207f / 255f);

    public static Color ColorWindowBackground = new Color(21f / 255f, 25f / 255f, 29f / 255f);

    public static float FieldHeight = 22;

    public static Vector2 SizePanelMargin = new Vector2(12, 12);

    public static Vector2 SizePanelPadding = new Vector2(12, 12);

    public static Vector2 SizeTextFieldArrowMargin = new Vector2(4, 0);

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