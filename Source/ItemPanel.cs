using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RimWorld;
using Verse;

namespace ItemRequests
{
    class ItemPanel : Panel
    {
        public delegate void AddEquipmentHandler(ThingDef entry);

        public event AddEquipmentHandler EquipmentAdded;

        public class ViewItems
        {
            public ItemTable Table;
            public List<ThingDef> List;
        }
        public static readonly string ColumnNameInfo = "Info";
        public static readonly string ColumnNameIcon = "Icon";
        public static readonly string ColumnNameName = "Name";
        public static readonly string ColumnNameCost = "Cost";
        public static readonly string ColumnNameAvailable = "Num Available";

        protected Rect RectDropdownTypes;
        protected Rect RectDropdownMaterials;
        protected Rect RectDropdownQuality;
        protected Rect RectListHeader;
        protected Rect RectListBody;
        protected Rect RectInfoButton;
        protected Rect RectColumnHeaderName;
        protected Rect RectColumnHeaderCost;
        protected Rect RectScrollFrame;
        protected Rect RectScrollView;
        protected Rect RectRow;
        protected Rect RectItem;
        protected Rect RectAddButton;
        protected ThingDef selectedType;
        protected static Vector2 SizeTextureSortIndicator = new Vector2(8, 4);
        //protected ProviderThingDefs providerEquipment;
        //protected ScrollViewVertical scrollView = new ScrollViewVertical();
        protected Dictionary<ThingDef, ViewItems> equipmentViews =
            new Dictionary<ThingDef, ViewItems>();
        private HashSet<ThingDef> filteredItems = new HashSet<ThingDef>();
        private ThingDef filterStuff = null;
        private bool filterMadeFromStuff = true;
        private bool loading = true;

        public override void Resize(Rect rect)
        {
            //base.Resize(rect);

            //Vector2 padding = new Vector2(12, 12);

            //RectDropdownTypes = new Rect(padding.x, padding.y, 140, 28);
            //RectDropdownMaterials = new Rect(RectDropdownTypes.xMax + 8, RectDropdownTypes.yMin, 160, 28);

            //Vector2 sizeInfoButton = new Vector2(24, 24);
            //Vector2 sizeAddButton = new Vector2(160, 34);
            //RectAddButton = new Rect(PanelRect.HalfWidth() - sizeAddButton.HalfX(),
            //    PanelRect.height - padding.y - sizeAddButton.y, sizeAddButton.x, sizeAddButton.y);

            //Vector2 listSize = new Vector2();
            //listSize.x = rect.width - padding.x * 2;
            //listSize.y = rect.height - RectDropdownTypes.yMax - (padding.y * 3) - RectAddButton.height;
            //float listHeaderHeight = 20;
            //float listBodyHeight = listSize.y - listHeaderHeight;

            //Rect rectTable = new Rect(padding.x, padding.y + RectDropdownTypes.yMax, listSize.x, listSize.y);

            //RectListHeader = new Rect(padding.x, RectDropdownTypes.yMax + 4, listSize.x, listHeaderHeight);
            //RectListBody = new Rect(padding.x, RectListHeader.yMax, listSize.x, listBodyHeight);

            //RectColumnHeaderName = new Rect(RectListHeader.x + 64, RectListHeader.y, 240, RectListHeader.height);
            //RectColumnHeaderCost = new Rect(RectListHeader.xMax - 100, RectListHeader.y, 100, RectListHeader.height);

            //RectScrollFrame = RectListBody;
            //RectScrollView = new Rect(0, 0, RectScrollFrame.width, RectScrollFrame.height);

            //RectRow = new Rect(0, 0, RectScrollView.width, 42);
            //RectItem = new Rect(10, 2, 38, 38);

            //Vector2 nameOffset = new Vector2(10, 0);
            //float columnWidthInfo = 36;
            //float columnWidthIcon = 42;
            //float columnWidthCost = 100;
            //float columnWidthName = RectRow.width - columnWidthInfo - columnWidthIcon - columnWidthCost - 10;

            //if (providerEquipment == null)
            //{
            //    providerEquipment = new ProviderThingDefs();
            //}
            //if (!providerEquipment.DatabaseReady)
            //{
            //    return;
            //}
            //foreach (var type in providerEquipment.Types)
            //{
            //    if (!equipmentViews.ContainsKey(type))
            //    {
            //        WidgetTable<ThingDef> table = new WidgetTable<ThingDef>();
            //        table.Rect = rectTable;
            //        table.BackgroundColor = Style.ColorPanelBackgroundDeep;
            //        table.RowColor = Style.ColorTableRow1;
            //        table.AlternateRowColor = Style.ColorTableRow2;
            //        table.SelectedRowColor = Style.ColorTableRowSelected;
            //        table.SupportSelection = true;
            //        table.RowHeight = 42;
            //        table.ShowHeader = true;
            //        table.SortAction = DoSort;
            //        table.SelectedAction = (ThingDef entry) =>
            //        {
            //            SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
            //        };
            //        table.DoubleClickAction = (ThingDef entry) =>
            //        {
            //            SoundDefOf.Tick_High.PlayOneShotOnCamera();
            //            EquipmentAdded(entry);
            //        };
            //        table.AddColumn(new WidgetTable<ThingDef>.Column()
            //        {
            //            Width = columnWidthInfo,
            //            Name = ColumnNameInfo,
            //            DrawAction = (ThingDef entry, Rect columnRect, WidgetTable<ThingDef>.Metadata metadata) =>
            //            {
            //                Rect infoRect = new Rect(columnRect.MiddleX() - sizeInfoButton.HalfX(), columnRect.MiddleY() - sizeInfoButton.HalfY(), sizeInfoButton.x, sizeInfoButton.y);
            //                Style.SetGUIColorForButton(infoRect);
            //                GUI.DrawTexture(infoRect, Textures.TextureButtonInfo);
            //                if (Widgets.ButtonInvisible(infoRect))
            //                {
            //                    if (entry.animal)
            //                    {
            //                        Find.WindowStack.Add((Window)new Dialog_InfoCard(entry.thing));
            //                    }
            //                    else if (entry.stuffDef != null)
            //                    {
            //                        Find.WindowStack.Add((Window)new Dialog_InfoCard(entry.def, entry.stuffDef));
            //                    }
            //                    else
            //                    {
            //                        Find.WindowStack.Add((Window)new Dialog_InfoCard(entry.def));
            //                    }
            //                }
            //                GUI.color = Color.white;
            //            }
            //        });
            //        table.AddColumn(new WidgetTable<ThingDef>.Column()
            //        {
            //            Width = columnWidthIcon,
            //            Name = ColumnNameIcon,
            //            DrawAction = (ThingDef entry, Rect columnRect, WidgetTable<ThingDef>.Metadata metadata) =>
            //            {
            //                WidgetEquipmentIcon.Draw(columnRect, entry);
            //            }
            //        });
            //        table.AddColumn(new WidgetTable<ThingDef>.Column()
            //        {
            //            Width = columnWidthName,
            //            Name = ColumnNameName,
            //            Label = "Name",
            //            AdjustForScrollbars = true,
            //            AllowSorting = true,
            //            DrawAction = (ThingDef entry, Rect columnRect, WidgetTable<ThingDef>.Metadata metadata) =>
            //            {
            //                columnRect = columnRect.InsetBy(nameOffset.x, 0, 0, 0);
            //                GUI.color = Style.ColorText;
            //                Text.Font = GameFont.Small;
            //                Text.Anchor = TextAnchor.MiddleLeft;
            //                Widgets.Label(columnRect, entry.Label);
            //                GUI.color = Color.white;
            //                Text.Anchor = TextAnchor.UpperLeft;
            //            }
            //        });
            //        table.AddColumn(new WidgetTable<ThingDef>.Column()
            //        {
            //            Width = columnWidthCost,
            //            Name = ColumnNameCost,
            //            Label = "Cost",
            //            AdjustForScrollbars = false,
            //            AllowSorting = true,
            //            DrawAction = (ThingDef entry, Rect columnRect, WidgetTable<ThingDef>.Metadata metadata) =>
            //            {
            //                GUI.color = Style.ColorText;
            //                Text.Font = GameFont.Small;
            //                Text.Anchor = TextAnchor.MiddleRight;
            //                Widgets.Label(new Rect(columnRect.x, columnRect.y, columnRect.width, columnRect.height),
            //                              "" + entry.cost);
            //                GUI.color = Color.white;
            //                Text.Anchor = TextAnchor.UpperLeft;
            //            },
            //            Alignment = TextAnchor.LowerRight
            //        });
            //        table.SetSortState(ColumnNameName, 1);
            //        ViewEquipmentList view = new ViewEquipmentList()
            //        {
            //            Table = table,
            //            List = providerEquipment.AllEquipmentOfType(type).ToList()
            //        };
            //        SortByName(view, 1);
            //        equipmentViews.Add(type, view);
            //    }
            //}
        }
        protected override void DrawPanelContent()
        {
            base.DrawPanelContent();

            //if (loading)
            //{
            //    if (providerEquipment != null && providerEquipment.DatabaseReady)
            //    {
            //        loading = false;
            //        Resize(this.PanelRect);
            //    }
            //    else
            //    {
            //        DrawLoadingProgress();
            //        return;
            //    }
            //}

            // Find the view.  Select the first row in the equipment list if none is selected.
            var view = CurrentView;
            if (view.Table.Selected == null)
            {
                view.Table.Selected = view.List.FirstOrDefault();
            }

            //DrawFilters(view);
            DrawEquipmentList(view);

            if (Widgets.ButtonText(RectAddButton, "Request", true, false, view.Table.Selected != null))
            {
                //SoundDefOf.Tick_High.PlayOneShotOnCamera();
                EquipmentAdded(view.Table.Selected);
            }
        }

        protected void UpdateAvailableMaterials()
        {
            ViewItems view = CurrentView;
            filteredItems.Clear();
            foreach (var item in view.List)
            {
                if (item != null)
                {
                    filteredItems.Add(item);
                }
            }
            if (filterStuff != null && !filteredItems.Contains(filterStuff))
            {
                filterStuff = null;
            }
        }

        //protected readonly Vector2 ProgressBarSize = new Vector2(250, 18);
        //protected void DrawLoadingProgress()
        //{
        //    Rect progressBarRect = new Rect(PanelRect.width / 2 - ProgressBarSize.x * 0.5f, PanelRect.height / 2 - ProgressBarSize.y * 0.5f,
        //        ProgressBarSize.x, ProgressBarSize.y);
        //    var progress = providerEquipment.LoadingProgress;
        //    GUI.color = Color.gray;
        //    Widgets.DrawBox(progressBarRect);
        //    if (progress.defCount > 0)
        //    {
        //        int totalCount = progress.defCount * 2;
        //        int processed = progress.stuffProcessed + progress.thingsProcessed;
        //        float percent = (float)processed / (float)totalCount;
        //        float barWidth = progressBarRect.width * percent;
        //        Widgets.DrawRectFast(new Rect(progressBarRect.x, progressBarRect.y, barWidth, progressBarRect.height), Color.green);
        //    }
        //    GUI.color = Style.ColorText;
        //    Text.Font = GameFont.Tiny;
        //    string label = "EdB.PC.Equipment.LoadingProgress.Initializing".Translate();
        //    if (progress.phase == EquipmentDatabase.LoadingPhase.ProcessingStuff)
        //    {
        //        label = "EdB.PC.Equipment.LoadingProgress.StuffDefs".Translate();
        //    }
        //    else if (progress.phase == EquipmentDatabase.LoadingPhase.ProcessingThings)
        //    {
        //        label = "EdB.PC.Equipment.LoadingProgress.ThingDefs".Translate();
        //    }
        //    else if (progress.phase == EquipmentDatabase.LoadingPhase.Loaded)
        //    {
        //        label = "EdB.PC.Equipment.LoadingProgress.Finished".Translate();
        //    }
        //    Widgets.Label(new Rect(progressBarRect.x, progressBarRect.yMax + 2, progressBarRect.width, 20), label);
        //    Text.Font = GameFont.Small;
        //    GUI.color = Color.white;
        //}

        //protected void DrawFilters(ViewItems view)
        //{
        //    string label = selectedType.Label.Translate();
        //    if (WidgetDropdown.Button(RectDropdownTypes, label, true, false, true))
        //    {
        //        List<FloatMenuOption> list = new List<FloatMenuOption>();
        //        foreach (var type in providerEquipment.Types)
        //        {
        //            ThingDef localType = type;
        //            list.Add(new FloatMenuOption(type.Label.Translate(), () =>
        //            {
        //                this.selectedType = localType;
        //                this.UpdateAvailableMaterials();
        //            }, MenuOptionPriority.Default, null, null, 0, null, null));
        //        }
        //        Find.WindowStack.Add(new FloatMenu(list, null, false));
        //    }

        //    if (StuffFilterVisible)
        //    {
        //        string stuffLabel = null;
        //        if (!filterMadeFromStuff)
        //        {
        //            stuffLabel = "EdB.PC.Panel.AvailableEquipment.Materials.None".Translate();
        //        }
        //        else if (filterStuff == null)
        //        {
        //            stuffLabel = "EdB.PC.Panel.AvailableEquipment.Materials.All".Translate();
        //        }
        //        else
        //        {
        //            stuffLabel = filterStuff.LabelCap;
        //        }
        //        if (WidgetDropdown.Button(RectDropdownMaterials, stuffLabel, true, false, true))
        //        {
        //            List<FloatMenuOption> stuffFilterOptions = new List<FloatMenuOption>();
        //            stuffFilterOptions.Add(new FloatMenuOption("EdB.PC.Panel.AvailableEquipment.Materials.All".Translate(), () =>
        //            {
        //                UpdateStuffFilter(true, null);
        //            }, MenuOptionPriority.Default, null, null, 0, null, null));
        //            stuffFilterOptions.Add(new FloatMenuOption("EdB.PC.Panel.AvailableEquipment.Materials.None".Translate(), () =>
        //            {
        //                UpdateStuffFilter(false, null);
        //            }, MenuOptionPriority.Default, null, null, 0, null, null));
        //            foreach (var item in filteredItems.OrderBy((ThingDef def) => { return def.LabelCap; }))
        //            {
        //                stuffFilterOptions.Add(new FloatMenuOption(item.LabelCap, () =>
        //                {
        //                    UpdateStuffFilter(true, item);
        //                }, MenuOptionPriority.Default, null, null, 0, null, null));
        //            }
        //            Find.WindowStack.Add(new FloatMenu(stuffFilterOptions, null, false));
        //        }
        //    }
        //}

        protected ViewItems CurrentView
        {
            get
            {
                if (selectedType == null)
                {
                    //selectedType = providerEquipment.Types.First();
                    UpdateAvailableMaterials();
                }
                return equipmentViews[selectedType];
            }
        }

        protected void UpdateStuffFilter(bool madeFromStuff, ThingDef stuff)
        {
            this.filterMadeFromStuff = madeFromStuff;
            this.filterStuff = stuff;
            ViewItems view = CurrentView;
            IEnumerable<ThingDef> entries = FilterEquipmentList(view);
            if (!entries.Any((ThingDef e) =>
            {
                return e == view.Table.Selected;
            }))
            {
                view.Table.Selected = entries.FirstOrDefault();
            }
        }

        protected bool StuffFilterVisible
        {
            get
            {
                return filteredItems.Count > 0;
            }
        }

        protected void DrawEquipmentList(ViewItems view)
        {
            //SortField sortField = PrepareCarefully.Instance.SortField;
            view.Table.Draw(FilterEquipmentList(view));
            view.Table.BackgroundColor = ColorPanelBackgroundDarker;
        }

        protected IEnumerable<ThingDef> FilterEquipmentList(ViewItems view)
        {
            if (StuffFilterVisible)
            {
                return view.List.FindAll((ThingDef entry) =>
                {
                    if (filterMadeFromStuff)
                    {
                        return filterStuff == null || filterStuff == entry;
                    }
                    else
                    {
                        return !MadeFromStuff(entry);
                    }
                });
            }
            return view.List;
        }

        protected bool MadeFromStuff(ThingDef thing) { return false; }

        protected void DoSort(ItemTable.Column column, int direction)
        {
            var view = equipmentViews[selectedType];
            if (view == null)
            {
                return;
            }
            if (column != null)
            {
                if (column.Name == ColumnNameName)
                {
                    SortByName(view, direction);
                    //SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
                }
                else if (column.Name == ColumnNameCost)
                {
                    SortByCost(view, direction);
                    //SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
                }
            }
        }

        protected void SortByName(ViewItems view, int direction)
        {
            if (direction == 1)
            {
                view.List.SortBy((ThingDef arg) => arg.label);
            }
            else
            {
                view.List.SortByDescending((ThingDef arg) =>arg.label);
            }
        }
        protected void SortByCost(ViewItems view, int direction)
        {
            view.List.Sort((ThingDef x, ThingDef y) =>
            {
                if (direction == 1)
                {
                    int result = x.costStuffCount.CompareTo(y.costStuffCount);
                    if (result != 0)
                    {
                        return result;
                    }
                }
                else
                {
                    int result = y.costStuffCount.CompareTo(x.costStuffCount);
                    if (result != 0)
                    {
                        return result;
                    }
                }
                return x.label.CompareTo(y.label);
            });
        }
    }


}
