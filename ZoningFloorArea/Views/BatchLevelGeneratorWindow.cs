using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using Autodesk.Revit.DB;
using ZoningFloorArea.Models;
using ZoningFloorArea.Services;

namespace ZoningFloorArea.Views
{
    public class BatchLevelGeneratorWindow : Window
    {
        private readonly Document _doc;
        private readonly ObservableCollection<LevelCreationItem> _previewItems;

        // UI Controls
        private System.Windows.Controls.TextBox _txtFloorCount;
        private System.Windows.Controls.TextBox _txtTypicalHeight;
        private System.Windows.Controls.TextBox _txtBaseElevation;
        private System.Windows.Controls.TextBox _txtStartFloorNumber;

        private System.Windows.Controls.TextBox _txtCellarCount;
        private System.Windows.Controls.TextBox _txtCellarHeight;

        private System.Windows.Controls.CheckBox _chkIncludeRoof;
        private System.Windows.Controls.TextBox _txtRoofHeight;

        private System.Windows.Controls.CheckBox _chkIncludeBulkhead;
        private System.Windows.Controls.TextBox _txtBulkheadHeight;

        private System.Windows.Controls.CheckBox _chkTwoDigits;
        private System.Windows.Controls.CheckBox _chkCreateFloorPlans;
        private System.Windows.Controls.CheckBox _chkCreateCeilingPlans;

        private System.Windows.Controls.DataGrid _dataGrid;
        private System.Windows.Controls.TextBlock _statusSummary;

        // Color Palette matching BauTools
        private static readonly System.Windows.Media.Color COL_BG        = (System.Windows.Media.Color)ColorConverter.ConvertFromString("#F1F5F9");
        private static readonly System.Windows.Media.Color COL_CARD      = System.Windows.Media.Colors.White;
        private static readonly System.Windows.Media.Color COL_DARK      = (System.Windows.Media.Color)ColorConverter.ConvertFromString("#0F172A");
        private static readonly System.Windows.Media.Color COL_ACCENT    = (System.Windows.Media.Color)ColorConverter.ConvertFromString("#0071E3");
        private static readonly System.Windows.Media.Color COL_ACCENT2   = (System.Windows.Media.Color)ColorConverter.ConvertFromString("#0284C7");
        private static readonly System.Windows.Media.Color COL_MUTED     = (System.Windows.Media.Color)ColorConverter.ConvertFromString("#64748B");
        private static readonly System.Windows.Media.Color COL_BORDER    = (System.Windows.Media.Color)ColorConverter.ConvertFromString("#CBD5E1");
        private static readonly System.Windows.Media.Color COL_HEADER_BG = (System.Windows.Media.Color)ColorConverter.ConvertFromString("#1E293B");
        private static readonly System.Windows.Media.Color COL_SUCCESS   = (System.Windows.Media.Color)ColorConverter.ConvertFromString("#16A34A");

        public BatchLevelGeneratorWindow(Document doc)
        {
            _doc = doc;
            _previewItems = new ObservableCollection<LevelCreationItem>();

            Title = "BauTools — Batch Level Generator (Multi-Story Buildings)";
            Height = 840;
            Width = 1100;
            MinHeight = 650;
            MinWidth = 850;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Background = new SolidColorBrush(COL_BG);
            FontFamily = new System.Windows.Media.FontFamily("Segoe UI");
            FontSize = 13;

            BuildUI();
            RecalculateSchedule();
        }

        private void BuildUI()
        {
            SolidColorBrush cardBrush     = new SolidColorBrush(COL_CARD);
            SolidColorBrush darkBrush     = new SolidColorBrush(COL_DARK);
            SolidColorBrush accentBrush   = new SolidColorBrush(COL_ACCENT);
            SolidColorBrush accent2Brush  = new SolidColorBrush(COL_ACCENT2);
            SolidColorBrush mutedBrush    = new SolidColorBrush(COL_MUTED);
            SolidColorBrush borderBrush   = new SolidColorBrush(COL_BORDER);
            SolidColorBrush headerBgBrush = new SolidColorBrush(COL_HEADER_BG);

            System.Windows.Controls.Grid root = new System.Windows.Controls.Grid();
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 0: Header
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 1: Config Cards
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // 2: Preview Grid
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 3: Footer

            // ══════════════════════════════════════════════════════════
            // 0. HEADER
            // ══════════════════════════════════════════════════════════
            Border headerBar = new Border
            {
                Background = headerBgBrush,
                Padding = new Thickness(24, 14, 24, 14)
            };

            System.Windows.Controls.Grid hGrid = new System.Windows.Controls.Grid();
            hGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            hGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            StackPanel titlePanel = new StackPanel();
            StackPanel logoLine = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal };

            Border badge = new Border
            {
                Background = accent2Brush,
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(8, 3, 8, 3),
                Margin = new Thickness(0, 0, 10, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            badge.Child = new TextBlock { Text = "BUILDING LEVELS", FontWeight = FontWeights.ExtraBold, FontSize = 12, Foreground = System.Windows.Media.Brushes.White };
            logoLine.Children.Add(badge);

            logoLine.Children.Add(new TextBlock
            {
                Text = "BauTools — Multi-Story Batch Level Generator",
                FontSize = 17,
                FontWeight = FontWeights.Bold,
                Foreground = System.Windows.Media.Brushes.White,
                VerticalAlignment = VerticalAlignment.Center
            });
            titlePanel.Children.Add(logoLine);

            titlePanel.Children.Add(new TextBlock
            {
                Text = "Batch generate typical floors, underground cellars, roof, and bulkhead levels with automatic elevations and view plans.",
                FontSize = 11,
                Foreground = new SolidColorBrush((System.Windows.Media.Color)ColorConverter.ConvertFromString("#94A3B8")),
                Margin = new Thickness(0, 4, 0, 0)
            });
            hGrid.Children.Add(titlePanel);

            headerBar.Child = hGrid;
            System.Windows.Controls.Grid.SetRow(headerBar, 0);
            root.Children.Add(headerBar);

            // ══════════════════════════════════════════════════════════
            // 1. CONFIGURATION CARDS CONTAINER
            // ══════════════════════════════════════════════════════════
            Border configContainer = new Border
            {
                Background = cardBrush,
                BorderBrush = borderBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Margin = new Thickness(16, 12, 16, 8),
                Padding = new Thickness(16, 12, 16, 12)
            };

            System.Windows.Controls.Grid cfgGrid = new System.Windows.Controls.Grid();
            cfgGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.3, GridUnitType.Star) }); // Col 0: Typical Floors
            cfgGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.1, GridUnitType.Star) }); // Col 1: Cellars
            cfgGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.4, GridUnitType.Star) }); // Col 2: Roof, Bulkhead & Views

            // ── Section 1: Typical Floors ──
            StackPanel sec1 = new StackPanel { Margin = new Thickness(0, 0, 14, 0) };
            sec1.Children.Add(new TextBlock { Text = "🏢 TYPICAL FLOORS (SUPERSTRUCTURE)", FontWeight = FontWeights.Bold, FontSize = 11.5, Foreground = darkBrush, Margin = new Thickness(0, 0, 0, 8) });

            // Floor count
            sec1.Children.Add(new TextBlock { Text = "Number of Typical Floors:", FontSize = 11, Foreground = mutedBrush });
            StackPanel countRow = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 6) };
            _txtFloorCount = new System.Windows.Controls.TextBox { Text = "15", Width = 60, Height = 28, TextAlignment = TextAlignment.Center, FontWeight = FontWeights.Bold, VerticalContentAlignment = VerticalAlignment.Center };
            _txtFloorCount.TextChanged += (s, e) => RecalculateSchedule();

            System.Windows.Controls.Button btnMinusFloors = new System.Windows.Controls.Button { Content = "−", Width = 28, Height = 28, FontWeight = FontWeights.Bold, Margin = new Thickness(4, 0, 2, 0) };
            btnMinusFloors.Click += (s, e) => { int v; if (int.TryParse(_txtFloorCount.Text, out v) && v > 1) _txtFloorCount.Text = (v - 1).ToString(); };

            System.Windows.Controls.Button btnPlusFloors = new System.Windows.Controls.Button { Content = "+", Width = 28, Height = 28, FontWeight = FontWeights.Bold, Margin = new Thickness(2, 0, 0, 0) };
            btnPlusFloors.Click += (s, e) => { int v; if (int.TryParse(_txtFloorCount.Text, out v)) _txtFloorCount.Text = (v + 1).ToString(); };

            countRow.Children.Add(_txtFloorCount);
            countRow.Children.Add(btnMinusFloors);
            countRow.Children.Add(btnPlusFloors);
            sec1.Children.Add(countRow);

            // Floor-to-floor height
            sec1.Children.Add(new TextBlock { Text = "Floor-to-Floor Height (Typical):", FontSize = 11, Foreground = mutedBrush });
            _txtTypicalHeight = new System.Windows.Controls.TextBox { Text = "10'-0\"", Height = 28, Margin = new Thickness(0, 2, 0, 6), VerticalContentAlignment = VerticalAlignment.Center };
            _txtTypicalHeight.TextChanged += (s, e) => RecalculateSchedule();
            sec1.Children.Add(_txtTypicalHeight);

            // Base elevation & Start Floor number
            System.Windows.Controls.Grid baseRow = new System.Windows.Controls.Grid { Margin = new Thickness(0, 0, 0, 0) };
            baseRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            baseRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            StackPanel baseCol1 = new StackPanel { Margin = new Thickness(0, 0, 4, 0) };
            baseCol1.Children.Add(new TextBlock { Text = "Base Elevation (Ground Floor):", FontSize = 11, Foreground = mutedBrush });
            _txtBaseElevation = new System.Windows.Controls.TextBox { Text = "0'-0\"", Height = 28, Margin = new Thickness(0, 2, 0, 0), VerticalContentAlignment = VerticalAlignment.Center };
            _txtBaseElevation.TextChanged += (s, e) => RecalculateSchedule();
            baseCol1.Children.Add(_txtBaseElevation);
            System.Windows.Controls.Grid.SetColumn(baseCol1, 0);
            baseRow.Children.Add(baseCol1);

            StackPanel baseCol2 = new StackPanel { Margin = new Thickness(4, 0, 0, 0) };
            baseCol2.Children.Add(new TextBlock { Text = "Start Floor #:", FontSize = 11, Foreground = mutedBrush });
            _txtStartFloorNumber = new System.Windows.Controls.TextBox { Text = "1", Height = 28, Margin = new Thickness(0, 2, 0, 0), VerticalContentAlignment = VerticalAlignment.Center };
            _txtStartFloorNumber.TextChanged += (s, e) => RecalculateSchedule();
            baseCol2.Children.Add(_txtStartFloorNumber);
            System.Windows.Controls.Grid.SetColumn(baseCol2, 1);
            baseRow.Children.Add(baseCol2);

            sec1.Children.Add(baseRow);
            System.Windows.Controls.Grid.SetColumn(sec1, 0);
            cfgGrid.Children.Add(sec1);

            // ── Section 2: Cellars ──
            StackPanel sec2 = new StackPanel { Margin = new Thickness(6, 0, 14, 0) };
            sec2.Children.Add(new TextBlock { Text = "🚗 CELLARS (SUB-GRADE LEVELS)", FontWeight = FontWeights.Bold, FontSize = 11.5, Foreground = darkBrush, Margin = new Thickness(0, 0, 0, 8) });

            sec2.Children.Add(new TextBlock { Text = "Number of Cellars:", FontSize = 11, Foreground = mutedBrush });
            StackPanel cellarCountRow = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 6) };
            _txtCellarCount = new System.Windows.Controls.TextBox { Text = "2", Width = 60, Height = 28, TextAlignment = TextAlignment.Center, FontWeight = FontWeights.Bold, VerticalContentAlignment = VerticalAlignment.Center };
            _txtCellarCount.TextChanged += (s, e) => RecalculateSchedule();

            System.Windows.Controls.Button btnMinusCellars = new System.Windows.Controls.Button { Content = "−", Width = 28, Height = 28, FontWeight = FontWeights.Bold, Margin = new Thickness(4, 0, 2, 0) };
            btnMinusCellars.Click += (s, e) => { int v; if (int.TryParse(_txtCellarCount.Text, out v) && v > 0) _txtCellarCount.Text = (v - 1).ToString(); };

            System.Windows.Controls.Button btnPlusCellars = new System.Windows.Controls.Button { Content = "+", Width = 28, Height = 28, FontWeight = FontWeights.Bold, Margin = new Thickness(2, 0, 0, 0) };
            btnPlusCellars.Click += (s, e) => { int v; if (int.TryParse(_txtCellarCount.Text, out v)) _txtCellarCount.Text = (v + 1).ToString(); };

            cellarCountRow.Children.Add(_txtCellarCount);
            cellarCountRow.Children.Add(btnMinusCellars);
            cellarCountRow.Children.Add(btnPlusCellars);
            sec2.Children.Add(cellarCountRow);

            sec2.Children.Add(new TextBlock { Text = "Height per Cellar Level:", FontSize = 11, Foreground = mutedBrush });
            _txtCellarHeight = new System.Windows.Controls.TextBox { Text = "12'-0\"", Height = 28, Margin = new Thickness(0, 2, 0, 8), VerticalContentAlignment = VerticalAlignment.Center };
            _txtCellarHeight.TextChanged += (s, e) => RecalculateSchedule();
            sec2.Children.Add(_txtCellarHeight);

            _chkTwoDigits = new System.Windows.Controls.CheckBox { Content = "2-Digit Prefix (01 1ST FL., 00 CELLAR)", IsChecked = true, Margin = new Thickness(0, 4, 0, 0) };
            _chkTwoDigits.Checked += (s, e) => RecalculateSchedule();
            _chkTwoDigits.Unchecked += (s, e) => RecalculateSchedule();
            sec2.Children.Add(_chkTwoDigits);

            System.Windows.Controls.Grid.SetColumn(sec2, 1);
            cfgGrid.Children.Add(sec2);

            // ── Section 3: Roof, Bulkhead & Views ──
            StackPanel sec3 = new StackPanel { Margin = new Thickness(6, 0, 0, 0) };
            sec3.Children.Add(new TextBlock { Text = "🏗️ ROOF, BULKHEAD & VIEWS", FontWeight = FontWeights.Bold, FontSize = 11.5, Foreground = darkBrush, Margin = new Thickness(0, 0, 0, 8) });

            // Roof row
            StackPanel roofRow = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 4) };
            _chkIncludeRoof = new System.Windows.Controls.CheckBox { Content = "Create ROOF level  |  Height:", IsChecked = true, VerticalAlignment = VerticalAlignment.Center, Width = 180 };
            _chkIncludeRoof.Checked += (s, e) => RecalculateSchedule();
            _chkIncludeRoof.Unchecked += (s, e) => RecalculateSchedule();
            _txtRoofHeight = new System.Windows.Controls.TextBox { Text = "12'-0\"", Width = 75, Height = 26, VerticalContentAlignment = VerticalAlignment.Center };
            _txtRoofHeight.TextChanged += (s, e) => RecalculateSchedule();
            roofRow.Children.Add(_chkIncludeRoof);
            roofRow.Children.Add(_txtRoofHeight);
            sec3.Children.Add(roofRow);

            // Bulkhead row
            StackPanel bhRow = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 8) };
            _chkIncludeBulkhead = new System.Windows.Controls.CheckBox { Content = "Create BULKHEAD  |  Height:", IsChecked = true, VerticalAlignment = VerticalAlignment.Center, Width = 180 };
            _chkIncludeBulkhead.Checked += (s, e) => RecalculateSchedule();
            _chkIncludeBulkhead.Unchecked += (s, e) => RecalculateSchedule();
            _txtBulkheadHeight = new System.Windows.Controls.TextBox { Text = "10'-0\"", Width = 75, Height = 26, VerticalContentAlignment = VerticalAlignment.Center };
            _txtBulkheadHeight.TextChanged += (s, e) => RecalculateSchedule();
            bhRow.Children.Add(_chkIncludeBulkhead);
            bhRow.Children.Add(_txtBulkheadHeight);
            sec3.Children.Add(bhRow);

            // View checkboxes
            _chkCreateFloorPlans = new System.Windows.Controls.CheckBox { Content = "Create associated Floor Plan Views", IsChecked = true, Margin = new Thickness(0, 2, 0, 3) };
            _chkCreateFloorPlans.Checked += (s, e) => UpdateViewsFlag(true);
            _chkCreateFloorPlans.Unchecked += (s, e) => UpdateViewsFlag(false);

            _chkCreateCeilingPlans = new System.Windows.Controls.CheckBox { Content = "Create associated Reflected Ceiling Plans (RCP)", IsChecked = true, Margin = new Thickness(0, 2, 0, 0) };
            _chkCreateCeilingPlans.Checked += (s, e) => UpdateCeilingViewsFlag(true);
            _chkCreateCeilingPlans.Unchecked += (s, e) => UpdateCeilingViewsFlag(false);

            sec3.Children.Add(_chkCreateFloorPlans);
            sec3.Children.Add(_chkCreateCeilingPlans);

            System.Windows.Controls.Grid.SetColumn(sec3, 2);
            cfgGrid.Children.Add(sec3);

            configContainer.Child = cfgGrid;
            System.Windows.Controls.Grid.SetRow(configContainer, 1);
            root.Children.Add(configContainer);

            // ══════════════════════════════════════════════════════════
            // 2. LIVE PREVIEW DATAGRID
            // ══════════════════════════════════════════════════════════
            Border gridCard = new Border
            {
                Background = cardBrush,
                BorderBrush = borderBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Margin = new Thickness(16, 4, 16, 8),
                Padding = new Thickness(12)
            };

            System.Windows.Controls.Grid tableContainer = new System.Windows.Controls.Grid();
            tableContainer.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            tableContainer.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            TextBlock tableTitle = new TextBlock
            {
                Text = "LIVE PREVIEW — PLANNED LEVELS (Double-click any Level Name to edit before creation):",
                FontWeight = FontWeights.Bold,
                FontSize = 11,
                Foreground = mutedBrush,
                Margin = new Thickness(4, 0, 0, 8)
            };
            System.Windows.Controls.Grid.SetRow(tableTitle, 0);
            tableContainer.Children.Add(tableTitle);

            _dataGrid = new System.Windows.Controls.DataGrid
            {
                ItemsSource = _previewItems,
                AutoGenerateColumns = false,
                CanUserAddRows = false,
                CanUserDeleteRows = false,
                CanUserSortColumns = false,
                GridLinesVisibility = DataGridGridLinesVisibility.Horizontal,
                HorizontalGridLinesBrush = new SolidColorBrush((System.Windows.Media.Color)ColorConverter.ConvertFromString("#E2E8F0")),
                RowHeight = 30,
                FontSize = 12.5,
                BorderThickness = new Thickness(1),
                BorderBrush = borderBrush,
                Background = System.Windows.Media.Brushes.White,
                AlternatingRowBackground = new SolidColorBrush((System.Windows.Media.Color)ColorConverter.ConvertFromString("#F8FAFC"))
            };

            // Index
            _dataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "#",
                Binding = new System.Windows.Data.Binding("Index"),
                IsReadOnly = true,
                Width = 45
            });

            // Category Type
            _dataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Category",
                Binding = new System.Windows.Data.Binding("LevelType"),
                IsReadOnly = true,
                Width = 110
            });

            // Level Name (Editable)
            _dataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Level Name ✏️ (Editable)",
                Binding = new System.Windows.Data.Binding("LevelName") { UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged },
                IsReadOnly = false,
                Width = new DataGridLength(1.6, DataGridLengthUnitType.Star)
            });

            // Elevation
            _dataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Elevation",
                Binding = new System.Windows.Data.Binding("ElevationDisplay"),
                IsReadOnly = true,
                Width = 120
            });

            // Create Floor Plan View Checkbox
            _dataGrid.Columns.Add(new DataGridCheckBoxColumn
            {
                Header = "Floor Plan [✔]",
                Binding = new System.Windows.Data.Binding("CreateFloorPlan") { UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged },
                Width = 120
            });

            // Create Ceiling Plan (RCP) Checkbox
            _dataGrid.Columns.Add(new DataGridCheckBoxColumn
            {
                Header = "Ceiling Plan RCP [✔]",
                Binding = new System.Windows.Data.Binding("CreateCeilingPlan") { UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged },
                Width = 140
            });

            System.Windows.Controls.Grid.SetRow(_dataGrid, 1);
            tableContainer.Children.Add(_dataGrid);

            gridCard.Child = tableContainer;
            System.Windows.Controls.Grid.SetRow(gridCard, 2);
            root.Children.Add(gridCard);

            // ══════════════════════════════════════════════════════════
            // 3. FOOTER ACTIONS
            // ══════════════════════════════════════════════════════════
            Border footer = new Border
            {
                Background = new SolidColorBrush((System.Windows.Media.Color)ColorConverter.ConvertFromString("#E2E8F0")),
                Padding = new Thickness(20, 12, 20, 12)
            };

            System.Windows.Controls.Grid footGrid = new System.Windows.Controls.Grid();
            footGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            footGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            _statusSummary = new TextBlock
            {
                Text = "Calculating...",
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = darkBrush
            };
            footGrid.Children.Add(_statusSummary);

            StackPanel btnPanel = new StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Right
            };

            System.Windows.Controls.Button btnReset = new System.Windows.Controls.Button
            {
                Content = "Reset Defaults",
                Padding = new Thickness(14, 8, 14, 8),
                Margin = new Thickness(0, 0, 10, 0),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            btnReset.Click += (s, e) => {
                _txtFloorCount.Text = "15";
                _txtTypicalHeight.Text = "10'-0\"";
                _txtBaseElevation.Text = "0'-0\"";
                _txtStartFloorNumber.Text = "1";
                _txtCellarCount.Text = "2";
                _txtCellarHeight.Text = "12'-0\"";
                _chkIncludeRoof.IsChecked = true;
                _txtRoofHeight.Text = "12'-0\"";
                _chkIncludeBulkhead.IsChecked = true;
                _txtBulkheadHeight.Text = "10'-0\"";
                _chkTwoDigits.IsChecked = true;
                _chkCreateFloorPlans.IsChecked = true;
                RecalculateSchedule();
            };
            btnPanel.Children.Add(btnReset);

            System.Windows.Controls.Button btnCancel = new System.Windows.Controls.Button
            {
                Content = "Cancel",
                Padding = new Thickness(14, 8, 14, 8),
                Margin = new Thickness(0, 0, 10, 0),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            btnCancel.Click += (s, e) => Close();
            btnPanel.Children.Add(btnCancel);

            System.Windows.Controls.Button btnCreate = new System.Windows.Controls.Button
            {
                Content = "⚡ Create Levels in Revit",
                Padding = new Thickness(20, 8, 20, 8),
                Background = accentBrush,
                Foreground = System.Windows.Media.Brushes.White,
                FontWeight = FontWeights.Bold,
                Cursor = System.Windows.Input.Cursors.Hand,
                BorderThickness = new Thickness(0)
            };
            btnCreate.Click += (s, e) => ExecuteCreation();
            btnPanel.Children.Add(btnCreate);

            System.Windows.Controls.Grid.SetColumn(btnPanel, 1);
            footGrid.Children.Add(btnPanel);

            footer.Child = footGrid;
            System.Windows.Controls.Grid.SetRow(footer, 3);
            root.Children.Add(footer);

            Content = root;
        }

        private void UpdateViewsFlag(bool create)
        {
            foreach (var item in _previewItems)
            {
                item.CreateFloorPlan = create;
            }
            if (_dataGrid != null) _dataGrid.Items.Refresh();
            UpdateStatusSummary();
        }

        private void UpdateCeilingViewsFlag(bool create)
        {
            foreach (var item in _previewItems)
            {
                item.CreateCeilingPlan = create;
            }
            if (_dataGrid != null) _dataGrid.Items.Refresh();
            UpdateStatusSummary();
        }

        private void UpdateStatusSummary()
        {
            if (_statusSummary == null) return;
            double topElev = _previewItems.Count > 0 ? _previewItems.Max(x => x.ElevationFeet) : 0;
            double lowestElev = _previewItems.Count > 0 ? _previewItems.Min(x => x.ElevationFeet) : 0;
            double totalHeight = topElev - lowestElev;

            int floorViewsCount = _previewItems.Count(x => x.CreateFloorPlan);
            int rcpViewsCount = _previewItems.Count(x => x.CreateCeilingPlan);

            _statusSummary.Text = string.Format("⚡ Ready to create {0} levels ({1} total height). Generates {2} Floor Plan(s) and {3} RCP View(s).",
                _previewItems.Count,
                LevelCreatorService.FormatLength(_doc, totalHeight),
                floorViewsCount,
                rcpViewsCount);
        }

        private void RecalculateSchedule()
        {
            if (_txtFloorCount == null) return;

            int fc, sf, cc;
            int floorCount = int.TryParse(_txtFloorCount.Text, out fc) ? Math.Max(0, fc) : 10;
            int startFloor = int.TryParse(_txtStartFloorNumber.Text, out sf) ? Math.Max(1, sf) : 1;
            int cellarCount = int.TryParse(_txtCellarCount.Text, out cc) ? Math.Max(0, cc) : 0;

            double baseElev, typicalHeight, cellarHeight, roofHeight, bulkheadHeight;
            LevelCreatorService.TryParseLength(_doc, _txtBaseElevation.Text, out baseElev);
            LevelCreatorService.TryParseLength(_doc, _txtTypicalHeight.Text, out typicalHeight);
            if (typicalHeight <= 0) typicalHeight = 10.0;

            LevelCreatorService.TryParseLength(_doc, _txtCellarHeight.Text, out cellarHeight);
            if (cellarHeight <= 0) cellarHeight = 12.0;

            bool roof = _chkIncludeRoof != null ? (_chkIncludeRoof.IsChecked == true) : true;
            string roofTxt = _txtRoofHeight != null ? _txtRoofHeight.Text : "12'";
            LevelCreatorService.TryParseLength(_doc, roofTxt, out roofHeight);
            if (roofHeight <= 0) roofHeight = 12.0;

            bool bulkhead = _chkIncludeBulkhead != null ? (_chkIncludeBulkhead.IsChecked == true) : true;
            string bulkTxt = _txtBulkheadHeight != null ? _txtBulkheadHeight.Text : "10'";
            LevelCreatorService.TryParseLength(_doc, bulkTxt, out bulkheadHeight);
            if (bulkheadHeight <= 0) bulkheadHeight = 10.0;

            bool twoDigits = _chkTwoDigits != null ? (_chkTwoDigits.IsChecked == true) : true;
            bool createFloorViews = _chkCreateFloorPlans != null ? (_chkCreateFloorPlans.IsChecked == true) : true;
            bool createCeilingViews = _chkCreateCeilingPlans != null ? (_chkCreateCeilingPlans.IsChecked == true) : true;

            var planned = LevelCreatorService.BuildPlannedLevels(
                _doc,
                baseElev,
                startFloor,
                floorCount,
                typicalHeight,
                cellarCount,
                cellarHeight,
                roof,
                roofHeight,
                bulkhead,
                bulkheadHeight,
                createFloorViews,
                createCeilingViews,
                twoDigits);

            _previewItems.Clear();
            foreach (var item in planned)
            {
                _previewItems.Add(item);
            }

            UpdateStatusSummary();
        }

        private void ExecuteCreation()
        {
            if (_previewItems.Count == 0)
            {
                MessageBox.Show("No levels configured to create.", "BauTools", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int count = _previewItems.Count;
            int floorViewCount = _previewItems.Count(x => x.CreateFloorPlan);
            int rcpViewCount = _previewItems.Count(x => x.CreateCeilingPlan);
            int totalViews = floorViewCount + rcpViewCount;

            var confirm = MessageBox.Show(
                string.Format("Confirm creation of {0} new level(s), {1} Floor Plan(s), and {2} RCP Ceiling Plan(s) in Revit?", count, floorViewCount, rcpViewCount),
                "Confirm Batch Level Creation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes) return;

            bool createCeilings = rcpViewCount > 0;

            Tuple<int, int, List<string>> createResult = LevelCreatorService.CreateLevelsInRevit(
                _doc,
                _previewItems.ToList(),
                createCeilings);

            int levelsCreated = createResult.Item1;
            int viewsCreated = createResult.Item2;
            List<string> errors = createResult.Item3;

            if (errors.Count > 0)
            {
                string msg = string.Format("Created {0} levels and {1} views with observations:\n\n{2}", levelsCreated, viewsCreated, string.Join("\n", errors.Take(5).ToArray()));
                if (errors.Count > 5) msg += string.Format("\n...and {0} more.", errors.Count - 5);

                MessageBox.Show(msg, "BauTools - Completed with Warnings", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else
            {
                MessageBox.Show(string.Format("✅ Success!\n\n• Levels created: {0}\n• Plan & RCP views created: {1}", levelsCreated, viewsCreated),
                    "BauTools - Batch Level Generator", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            DialogResult = true;
            Close();
        }
    }
}
