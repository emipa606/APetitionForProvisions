using UnityEngine;
using Verse;
using System;
using System.Linq;
using System.Collections.Generic;
using Verse.Sound;

namespace ItemRequests
{

    // This class was adapted from the class WidgetTable
    // from the mod EdB Prepare Carefully by edbmods
    // https://github.com/edbmods/EdBPrepareCarefully/blob/develop/Source/WidgetTable.cs
    public class WidgetTable<T> where T : class
    {
        protected static Vector2 SizeSortIndicator = new Vector2(8, 4);
        protected Rect tableRect;
        protected ScrollViewVertical scrollView = new ScrollViewVertical();
        protected Action<T> doubleClickAction = null;
        protected Action<T> selectedAction = null;
        protected Func<T, bool> enabledFunc = (T) => { return true; };
        protected Column sortedColumn = null;
        protected int sortDirection = 1;
        protected T scrollTo;
        public class RowGroup
        {
            public string Label;
            public IEnumerable<T> Rows;
            public RowGroup()
            {
            }
            public RowGroup(string label, IEnumerable<T> rows)
            {
                this.Label = label;
                this.Rows = rows;
            }
        }
        public struct Metadata
        {
            public int groupIndex;
            public int rowIndex;
            public int columnIndex;
            public Metadata(int groupIndex, int rowIndex, int columnIndex)
            {
                this.groupIndex = groupIndex;
                this.rowIndex = rowIndex;
                this.columnIndex = columnIndex;
            }
        }
        public class Column
        {
            public float Width;
            public string Name;
            public string Label;
            public TextAnchor Alignment = TextAnchor.LowerLeft;
            public bool AdjustForScrollbars = false;
            public Action<T, Rect, Metadata> DrawAction = (T, Rect, Metadata) => { };
            public Func<T, float, Metadata, float> MeasureAction = null;
            public bool AllowSorting = false;
        }
        protected List<Column> columns = new List<Column>();
        protected List<float> columnHeights = new List<float>();
        public WidgetTable()
        {
            SupportSelection = false;
        }
        public Rect Rect
        {
            get
            {
                return tableRect;
            }
            set
            {
                tableRect = value;
            }
        }
        public bool ShowHeader
        {
            get;
            set;
        }
        public Color BackgroundColor
        {
            get;
            set;
        }
        public Color RowColor
        {
            get;
            set;
        }
        public Color AlternateRowColor
        {
            get;
            set;
        }
        public Color SelectedRowColor
        {
            get;
            set;
        }
        public List<T> Items
        {
            get;
            set;
        }
        public float RowHeight
        {
            get;
            set;
        }
        public float RowGroupHeaderHeight
        {
            get;
            set;
        }
        public bool SupportSelection
        {
            get;
            set;
        }
        public T Selected
        {
            get;
            set;
        }
        public Action<Column, int> SortAction
        {
            get;
            set;
        }
        public Action<RowGroup, int> DrawRowGroupHeaderAction
        {
            get;
            set;
        }
        public Func<RowGroup, int, float> MeasureRowGroupHeaderAction
        {
            get;
            set;
        }
        public ScrollViewVertical ScrollView
        {
            get
            {
                return scrollView;
            }
        }
        public Action<T> DoubleClickAction
        {
            get
            {
                return doubleClickAction;
            }
            set
            {
                doubleClickAction = value;
            }
        }
        public Action<T> SelectedAction
        {
            get
            {
                return selectedAction;
            }
            set
            {
                selectedAction = value;
            }
        }
        public Func<T, bool> RowEnabledFunc
        {
            get
            {
                return enabledFunc;
            }
            set
            {
                enabledFunc = value;
            }
        }
        public void ScrollTo(T row)
        {
            this.scrollTo = row;
        }
        public void SetSortState(string name, int direction)
        {
            sortDirection = direction;
            Column column = columns.FirstOrDefault((Column arg) => { return arg.Name == name; });
            sortedColumn = column;
        }
        public void Sort(int direction)
        {
            if (direction == -1 || direction == 1)
            {
                if (direction != sortDirection)
                {
                    sortDirection = direction;
                    if (sortedColumn != null)
                    {
                        DoSortAction();
                    }
                }
            }
        }
        public void Sort(Column column, int direction)
        {
            if (column != sortedColumn || direction != sortDirection)
            {
                sortedColumn = column;
                sortDirection = direction;
                DoSortAction();
            }
        }
        private void DoSortAction()
        {
            if (SortAction != null)
            {
                SortAction(sortedColumn, sortDirection);
            }
        }
        public void AddColumn(Column column)
        {
            columns.Add(column);
        }
        public void Draw(IEnumerable<T> rows)
        {
            Rect tableRect = this.tableRect;
            if (ShowHeader)
            {
                DrawHeader(new Rect(tableRect.x, tableRect.y, tableRect.width, 20));
                tableRect = tableRect.InsetBy(0, 20, 0, 0);
            }
            GUI.color = BackgroundColor;
            GUI.DrawTexture(tableRect, BaseContent.WhiteTex);
            GUI.color = Color.white;

            float cursor = 0;
            float? scrollToCursorTop = null;
            float? scrollToCursorBottom = null;
            GUI.BeginGroup(tableRect);
            scrollView.Begin(new Rect(0, 0, tableRect.width, tableRect.height));
            int index = 0;
            try
            {
                foreach (T row in rows)
                {
                    if (scrollTo != null && row == scrollTo)
                    {
                        scrollToCursorTop = cursor;
                    }
                    cursor = DrawRow(cursor, row, index);
                    if (scrollTo != null && row == scrollTo)
                    {
                        scrollToCursorBottom = cursor;
                    }
                    index++;
                }
            }
            finally
            {
                scrollView.End(cursor);
                GUI.EndGroup();
            }

            // Scroll to the specific row, if any.  Need to do this after all of the rows have been drawn.
            if (scrollTo != null)
            {
                ScrollTo(scrollToCursorTop.Value, scrollToCursorBottom.Value);
                scrollTo = null;
            }
        }
        public void Draw(IEnumerable<RowGroup> rowGroups)
        {
            Rect tableRect = this.tableRect;
            if (ShowHeader)
            {
                DrawHeader(new Rect(tableRect.x, tableRect.y, tableRect.width, 20));
                tableRect = tableRect.InsetBy(0, 20, 0, 0);
            }
            GUI.color = BackgroundColor;
            GUI.DrawTexture(tableRect, BaseContent.WhiteTex);
            GUI.color = Color.white;

            float cursor = 0;
            float? scrollToCursorTop = null;
            float? scrollToCursorBottom = null;
            GUI.BeginGroup(tableRect);
            scrollView.Begin(new Rect(0, 0, tableRect.width, tableRect.height));
            int index = 0;
            try
            {
                foreach (var group in rowGroups)
                {
                    if (group.Rows.DefaultIfEmpty() == null)
                    {
                        continue;
                    }
                    if (group.Label != null)
                    {
                        GUI.color = Color.white;
                        Text.Anchor = TextAnchor.LowerLeft;
                        Rect headerRect = new Rect(tableRect.x + 1, cursor - 2, tableRect.width - 4, RowGroupHeaderHeight);
                        if (scrollView.ScrollbarsVisible)
                        {
                            headerRect.width -= 16;
                        }
                        float labelHeight = Text.CalcHeight(group.Label, headerRect.width) + 16;
                        labelHeight = Mathf.Max(labelHeight, RowGroupHeaderHeight);
                        headerRect.height = labelHeight;
                        Widgets.Label(headerRect, group.Label);
                        Text.Anchor = TextAnchor.UpperLeft;
                        GUI.color = Color.white;
                        cursor += headerRect.height;
                        index = 0;
                    }

                    foreach (T row in group.Rows)
                    {
                        if (scrollTo != null && row == scrollTo)
                        {
                            scrollToCursorTop = cursor;
                        }
                        cursor = DrawRow(cursor, row, index);
                        if (scrollTo != null && row == scrollTo)
                        {
                            scrollToCursorBottom = cursor;
                        }
                        index++;
                    }
                }
            }
            finally
            {
                scrollView.End(cursor);
                GUI.EndGroup();
            }

            // Scroll to the specific row, if any.  Need to do this after all of the rows have been drawn.
            if (scrollTo != null)
            {
                ScrollTo(scrollToCursorTop.Value, scrollToCursorBottom.Value);
                scrollTo = null;
            }
        }
        protected void ScrollTo(float top, float bottom)
        {
            float contentHeight = bottom - top;
            float pos = top - (Mathf.Ceil(scrollView.ViewHeight * 0.25f) - Mathf.Floor(contentHeight * 0.5f));
            if (pos < scrollView.Position.y)
            {
                pos = scrollView.Position.y;
            }
            scrollView.ScrollTo(pos);
        }
        protected void ResizeColumnHeights()
        {
            // If the number of column heights don't match the number of columns, add enough to match.
            if (columnHeights.Count < columns.Count)
            {
                int diff = columns.Count - columnHeights.Count;
                for (int i = 0; i < diff; i++)
                {
                    columnHeights.Add(0);
                }
            }
        }
        protected float MeasureRow(T row, int index)
        {
            float rowHeight = 0;
            int columnIndex = 0;
            foreach (var column in columns)
            {
                float columnHeight = column.MeasureAction == null ? RowHeight : column.MeasureAction(row, column.Width, new Metadata(0, index, columnIndex));
                columnHeights[columnIndex] = columnHeight;
                rowHeight = Mathf.Max(rowHeight, columnHeight);
                columnIndex++;
            }
            return rowHeight;
        }
        protected float DrawRow(float cursor, T row, int index)
        {
            // Measure the columns and get the row height from the maximum column height.
            ResizeColumnHeights();
            float rowHeight = MeasureRow(row, index);

            // Set the row rectangle using the row height that we previously calculated.
            Rect rowRect = new Rect(0, cursor, tableRect.width, rowHeight);

            // Only draw the row if it's within the bounds of the content rect.
            if (cursor + rowRect.height >= scrollView.Position.y
                    && cursor <= scrollView.Position.y + scrollView.ViewHeight)
            {
                GUI.color = (index % 2 == 0) ? RowColor : AlternateRowColor;
                if (row == Selected && SelectedRowColor.a != 0)
                {
                    GUI.color = SelectedRowColor;
                }
                if (GUI.color.a != 0)
                {
                    GUI.DrawTexture(rowRect, BaseContent.WhiteTex);
                }
                GUI.color = Color.white;

                float columnCursor = 0;
                int columnIndex = 0;
                foreach (var column in columns)
                {
                    Rect columnRect = new Rect(columnCursor, rowRect.y, column.Width, rowRect.height);
                    if (column.AdjustForScrollbars && scrollView.ScrollbarsVisible)
                    {
                        columnRect.width = columnRect.width - 16;
                    }
                    column.DrawAction?.Invoke(row, columnRect, new Metadata(0, index, columnIndex));
                    columnCursor += columnRect.width;
                    columnIndex++;
                }

                if (SupportSelection)
                {
                    if (Event.current.type == EventType.MouseDown && rowRect.Contains(Event.current.mousePosition))
                    {
                        if (Event.current.button == 0)
                        {
                            if (Event.current.clickCount == 1)
                            {
                                Selected = row;
                                selectedAction?.Invoke(row);
                            }
                            else if (Event.current.clickCount == 2)
                            {
                                doubleClickAction?.Invoke(row);
                            }
                        }
                    }
                }
            }
            cursor += rowHeight;
            return cursor;
        }
        protected void DoScroll(IEnumerable<T> rows, T scrollTo)
        {
            ResizeColumnHeights();
            // Iterate the rows to try to find the one we're looking for and to determine the
            // row top and bottom positions.
            int index = -1;
            float rowTop = 0;
            float rowBottom = 0;
            bool foundRow = false;
            foreach (var row in rows)
            {
                index++;
                rowBottom = rowTop + MeasureRow(row, index);
                if (object.Equals(row, scrollTo))
                {
                    foundRow = true;
                    break;
                }
                rowTop = rowBottom;
            }
            if (index < 0 || !foundRow)
            {
                return;
            }

            float min = ScrollView.Position.y;
            float max = min + Rect.height;
            float pos = (float)index * RowHeight;
            if (rowTop < min)
            {
                float amount = min - rowTop;
                ScrollView.Position = new Vector2(ScrollView.Position.x, ScrollView.Position.y - amount);
            }
            else if (rowBottom > max)
            {
                float amount = rowBottom - max;
                ScrollView.Position = new Vector2(ScrollView.Position.x, ScrollView.Position.y + amount);
            }
        }
        public void DrawHeader(Rect rect)
        {
            Column clickedColumn = null;
            GUI.color = Style.ColorTableHeader;
            GUI.DrawTexture(rect, BaseContent.WhiteTex);
            GUI.color = Style.ColorTableHeaderBorder;
            GUI.DrawTexture(new Rect(rect.x, rect.y + rect.height - 1, rect.width, 1), BaseContent.WhiteTex);

            float cursor = rect.x;
            GUI.color = Style.ColorText;
            Text.Font = GameFont.Tiny;
            foreach (var column in columns)
            {
                if (column.Label != null)
                {
                    Text.Anchor = column.Alignment;
                    Rect labelRect = new Rect(cursor, rect.y, column.Width, rect.height);
                    if (column.AdjustForScrollbars && scrollView.ScrollbarsVisible)
                    {
                        labelRect.width -= 16;
                    }

                    if (column.AllowSorting)
                    {
                        float columnWidth = labelRect.width;
                        Vector2 textSize = Text.CalcSize(column.Label);
                        Rect textRect;
                        Rect sortRect;
                        if (column.Alignment == TextAnchor.LowerLeft)
                        {
                            textRect = new Rect(labelRect.x, labelRect.y, textSize.x, textSize.y);
                            sortRect = new Rect(labelRect.x + textSize.x + 2, labelRect.yMax - 11, SizeSortIndicator.x, SizeSortIndicator.y);
                        }
                        else
                        {
                            textRect = new Rect(labelRect.xMax - textSize.x - SizeSortIndicator.x - 2, labelRect.yMax - textSize.y, textSize.x, textSize.y);
                            sortRect = new Rect(labelRect.xMax - SizeSortIndicator.x, labelRect.yMax - 11, SizeSortIndicator.x, SizeSortIndicator.y);
                            labelRect = labelRect.InsetBy(0, 0, SizeSortIndicator.x + 2, 0);
                        }

                        //Rect highlightRect = textRect.Combined(sortRect);
                        //Style.SetGUIColorForButton(highlightRect);
                        //if (Widgets.ButtonInvisible(highlightRect, false))
                        //{
                        //    clickedColumn = column;
                        //}

                        //if (sortedColumn == column)
                        //{
                        //    if (sortDirection == 1)
                        //    {
                        //        GUI.DrawTexture(sortRect, Textures.TextureSortAscending);
                        //    }
                        //    else
                        //    {
                        //        GUI.DrawTexture(sortRect, Textures.TextureSortDescending);
                        //    }
                        //}

                        Widgets.Label(labelRect, column.Label);
                        GUI.color = Style.ColorText;
                        cursor += columnWidth;
                    }
                    else
                    {
                        Widgets.Label(labelRect, column.Label);
                        cursor += labelRect.width;
                    }
                }
                else
                {
                    cursor += column.Width;
                }
            }
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;

            if (clickedColumn != null)
            {
                if (sortedColumn != clickedColumn)
                {
                    Sort(clickedColumn, 1);
                }
                else
                {
                    Sort(-sortDirection);
                }
            }
        }
    }

    // This class was taken from the class Style
    // from the mod EdB Prepare Carefully by edbmods
    // https://github.com/edbmods/EdBPrepareCarefully/blob/develop/Source/Style.cs
    public class Style
    {
        public static Color ColorText = new Color(0.80f, 0.80f, 0.80f);
        public static Color ColorTextPanelHeader = new Color(207f / 255f, 207f / 255f, 207f / 255f);

        public static Color ColorPanelBackground = new Color(36f / 255f, 37f / 255f, 38f / 255f);
        public static Color ColorPanelBackgroundDeep = new Color(24f / 255f, 24f / 255f, 29f / 255f);
        public static Color ColorPanelBackgroundItem = new Color(43f / 255f, 44f / 255f, 45f / 255f);
        public static Color ColorPanelBackgroundScrollView = new Color(30f / 255f, 31f / 255f, 32f / 255f);

        public static Color ColorButton = new Color(0.623529f, 0.623529f, 0.623529f);
        public static Color ColorButtonHighlight = new Color(0.97647f, 0.97647f, 0.97647f);
        public static Color ColorButtonDisabled = new Color(0.27647f, 0.27647f, 0.27647f);
        public static Color ColorButtonSelected = new Color(1, 1, 1);

        public static Color ColorControlDisabled = new Color(1, 1, 1, 0.27647f);

        public static Color ColorTableHeader = new Color(30f / 255f, 31f / 255f, 32f / 255f);
        public static Color ColorTableHeaderBorder = new Color(63f / 255f, 64f / 255f, 65f / 255f);
        public static Color ColorTableRow1 = new Color(47f / 255f, 49f / 255f, 50f / 255f);
        public static Color ColorTableRow2 = new Color(54f / 255f, 56f / 255f, 57f / 255f);
        public static Color ColorTableRowSelected = new Color(12f / 255f, 12f / 255f, 12f / 255f);

        public static Color ColorWindowBackground = new Color(21f / 255f, 25f / 255f, 29f / 255f);

        public static Vector2 SizePanelMargin = new Vector2(12, 12);
        public static Vector2 SizePanelPadding = new Vector2(12, 12);

        public static Vector2 SizeTextFieldArrowMargin = new Vector2(4, 0);

        public static float FieldHeight = 22;

        public static void SetGUIColorForButton(Rect rect)
        {
            if (rect.Contains(Event.current.mousePosition))
            {
                GUI.color = Style.ColorButtonHighlight;
            }
            else
            {
                GUI.color = Style.ColorButton;
            }
        }
        public static void SetGUIColorForButton(Rect rect, bool selected)
        {
            if (selected)
            {
                GUI.color = Style.ColorButtonSelected;
            }
            else
            {
                if (rect.Contains(Event.current.mousePosition))
                {
                    GUI.color = Style.ColorButtonHighlight;
                }
                else
                {
                    GUI.color = Style.ColorButton;
                }
            }
        }
        public static void SetGUIColorForButton(Rect rect, bool selected, Color color, Color hoverColor, Color selectedColor)
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
    public static class WidgetDropdown
    {
        public static bool Button(Rect rect, string label)
        {
            return Button(rect, label, true, false, true);
        }
        public static bool Button(Rect rect, string label, bool drawBackground, bool doMouseoverSound, bool active)
        {
            TextAnchor anchor = Text.Anchor;
            Color color = GUI.color;

            //if (drawBackground)
            //{
            //    Texture2D atlas = Textures.TextureButtonBGAtlas;
            //    if (Mouse.IsOver(rect))
            //    {
            //        atlas = Textures.TextureButtonBGAtlasMouseover;
            //        if (Input.GetMouseButton(0))
            //        {
            //            atlas = Textures.TextureButtonBGAtlasClick;
            //        }
            //    }
            //    Widgets.DrawAtlas(rect, atlas);
            //    Rect indicator = new Rect(rect.xMax - 21, rect.MiddleY() - 4, 11, 8);
            //    GUI.DrawTexture(indicator, Textures.TextureDropdownIndicator);
            //}

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
            Rect textRect = new Rect(rect.x, rect.y, rect.width - 12, rect.height);
            Widgets.Label(textRect, label);
            Text.Anchor = anchor;
            GUI.color = color;
            return active && Widgets.ButtonInvisible(rect, false);
        }
    }

    // This class was taken from the class ScrollViewVertical
    // from the mod EdB Prepare Carefully by edbmods
    // https://github.com/edbmods/EdBPrepareCarefully/blob/develop/Source/ScrollViewVertical.cs
    public class ScrollViewVertical
    {
        public static readonly float ScrollbarSize = 15;
        private float contentHeight;
        private Vector2 position = Vector2.zero;
        private Rect viewRect;
        private Rect contentRect;
        private bool consumeScrollEvents = true;
        private Vector2? scrollTo = null;

        public float ViewHeight
        {
            get
            {
                return viewRect.height;
            }
        }

        public float ViewWidth
        {
            get
            {
                return viewRect.width;
            }
        }

        public float ContentWidth
        {
            get
            {
                return contentRect.width;
            }
        }

        public float ContentHeight
        {
            get
            {
                return contentHeight;
            }
        }

        public Vector2 Position
        {
            get
            {
                return position;
            }
            set
            {
                position = value;
            }
        }

        public bool ScrollbarsVisible
        {
            get
            {
                return ContentHeight > ViewHeight;
            }
        }

        public ScrollViewVertical()
        {

        }

        public ScrollViewVertical(bool consumeScrollEvents)
        {
            this.consumeScrollEvents = consumeScrollEvents;
        }

        public void Begin(Rect viewRect)
        {
            this.viewRect = viewRect;
            this.contentRect = new Rect(0, 0, viewRect.width - 16, contentHeight);
            if (consumeScrollEvents)
            {
                Widgets.BeginScrollView(viewRect, ref position, contentRect);
            }
            else
            {
                BeginScrollView(viewRect, ref position, contentRect);
            }
        }

        public void End(float yPosition)
        {
            contentHeight = yPosition;
            Widgets.EndScrollView();
            if (scrollTo != null)
            {
                Vector2 newPosition = scrollTo.Value;
                if (newPosition.y < 0)
                {
                    newPosition.y = 0;
                }
                else if (newPosition.y > ContentHeight - ViewHeight - 1)
                {
                    newPosition.y = ContentHeight - ViewHeight - 1;
                }
                Position = newPosition;
                scrollTo = null;
            }
        }

        public void ScrollToTop()
        {
            scrollTo = new Vector2(0, 0);
        }

        public void ScrollToBottom()
        {
            scrollTo = new Vector2(0, float.MaxValue);
        }

        public void ScrollTo(float y)
        {
            scrollTo = new Vector2(0, y);
        }

        protected static void BeginScrollView(Rect outRect, ref Vector2 scrollPosition, Rect viewRect)
        {
            Vector2 vector = scrollPosition;
            Vector2 vector2 = GUI.BeginScrollView(outRect, scrollPosition, viewRect);
            Vector2 vector3;
            if (Event.current.type == EventType.MouseDown)
            {
                vector3 = vector;
            }
            else
            {
                vector3 = vector2;
            }
            if (Event.current.type == EventType.ScrollWheel && Mouse.IsOver(outRect))
            {
                vector3 += Event.current.delta * 40;
            }
            scrollPosition = vector3;
        }
    }
}
