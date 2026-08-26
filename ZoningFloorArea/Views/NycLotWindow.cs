using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using Autodesk.Revit.DB;
using ZoningFloorArea.Models;
using ZoningFloorArea.Services;

// Aliases to avoid ambiguity between System.Windows and Autodesk.Revit.DB
using WpfGrid = System.Windows.Controls.Grid;
using WpfButton = System.Windows.Controls.Button;
using WpfTextBox = System.Windows.Controls.TextBox;
using WpfTextBlock = System.Windows.Controls.TextBlock;
using WpfComboBox = System.Windows.Controls.ComboBox;
using WpfCheckBox = System.Windows.Controls.CheckBox;
using WpfRadioButton = System.Windows.Controls.RadioButton;
using WpfListBox = System.Windows.Controls.ListBox;
using WpfProgressBar = System.Windows.Controls.ProgressBar;
using WpfOrientation = System.Windows.Controls.Orientation;
using WpfVisibility = System.Windows.Visibility;
using WpfColor = System.Windows.Media.Color;
using WpfColors = System.Windows.Media.Colors;
using WpfBrushes = System.Windows.Media.Brushes;

namespace ZoningFloorArea.Views
{
    public class NycLotWindow : Window
    {
        private readonly Document _doc;
        private readonly NycPlutoService _plutoService;
        private readonly RevitLotDrawerService _drawerService;
        private readonly List<Level> _levels;
        private readonly List<string> _availableLineStyles;

        private NycLotInfo? _currentLot;
        private NycBlockContext? _currentBlockContext;
        private readonly ObservableCollection<NycSearchResult> _searchResults;

        // UI Controls - Search
        private WpfRadioButton _rbSearchAddress = null!;
        private WpfRadioButton _rbSearchBbl = null!;
        private StackPanel _panelAddressSearch = null!;
        private StackPanel _panelBblSearch = null!;
        private WpfTextBox _txtAddressQuery = null!;
        private WpfListBox _listSearchResults = null!;
        private WpfComboBox _comboBorough = null!;
        private WpfTextBox _txtBlock = null!;
        private WpfTextBox _txtLot = null!;
        private WpfButton _btnSearch = null!;
        private WpfProgressBar _progressBar = null!;

        // UI Controls - Drawing, Level & Grouping Options
        private WpfComboBox _comboElementType = null!;
        private WpfComboBox _comboAnchorCorner = null!;
        private WpfCheckBox _chkAlignPbp = null!;
        private WpfCheckBox _chkCreatePropLineLvl1 = null!;
        private WpfComboBox _comboLevels = null!;

        // Proposal C: Grouping Mode Selectors
        private WpfRadioButton _rbGroupSingle = null!;
        private WpfRadioButton _rbGroupSplit = null!;
        private WpfRadioButton _rbGroupNone = null!;
        private WpfCheckBox _chkPinGroup = null!;

        // Proposal B: Zoning Drafting View Table
        private WpfCheckBox _chkGenerateZoningTable = null!;

        // UI Controls - Granular Line Style Selectors
        private WpfCheckBox _chkDrawSubjectLot = null!;
        private WpfComboBox _comboSubjectLineStyle = null!;
        private WpfCheckBox _chkDrawAdjacentLots = null!;
        private WpfComboBox _comboAdjacentLineStyle = null!;
        private WpfCheckBox _chkDrawBlockContext = null!;
        private WpfComboBox _comboBlockContextLineStyle = null!;
        private WpfCheckBox _chkDrawSidewalk = null!;
        private WpfComboBox _comboSidewalkLineStyle = null!;
        private WpfCheckBox _chkPlaceStreetNotes = null!;

        // UI Controls - 3D Building Masses
        private WpfCheckBox _chkCreate3DBuildingMasses = null!;
        private WpfCheckBox _chkExtrudeSubjectLotBuilding = null!;

        // UI Controls - Info Card
        private Border _infoCardContainer = null!;
        private WpfTextBlock _txtPlaceholderInfo = null!;
        private StackPanel _panelLotDetails = null!;
        private WpfTextBlock _lblLotAddress = null!;
        private WpfTextBlock _lblLotBbl = null!;
        private WpfTextBlock _lblBlockContextSummary = null!;
        private WpfTextBlock _lblZoningSummary = null!;
        private WpfTextBlock _lblLotArea = null!;
        private WpfTextBlock _lblBldgArea = null!;
        private WpfTextBlock _lblResFar = null!;
        private WpfTextBlock _lblCommFar = null!;
        private WpfTextBlock _lblFacilFar = null!;
        private WpfTextBlock _lblBuiltFar = null!;
        private WpfTextBlock _lblDimensions = null!;
        private WpfTextBlock _lblExtraDetails = null!;

        // Action Buttons
        private WpfButton _btnDrawInRevit = null!;
        private WpfTextBlock _txtStatusMsg = null!;

        // Theme Colors matching BauTools
        private static readonly WpfColor COL_BG        = (WpfColor)ColorConverter.ConvertFromString("#F8FAFC");
        private static readonly WpfColor COL_CARD      = WpfColors.White;
        private static readonly WpfColor COL_DARK      = (WpfColor)ColorConverter.ConvertFromString("#0F172A");
        private static readonly WpfColor COL_ACCENT    = (WpfColor)ColorConverter.ConvertFromString("#2563EB");
        private static readonly WpfColor COL_ACCENT2   = (WpfColor)ColorConverter.ConvertFromString("#0284C7");
        private static readonly WpfColor COL_MUTED     = (WpfColor)ColorConverter.ConvertFromString("#64748B");
        private static readonly WpfColor COL_BORDER    = (WpfColor)ColorConverter.ConvertFromString("#E2E8F0");
        private static readonly WpfColor COL_HEADER_BG = (WpfColor)ColorConverter.ConvertFromString("#1E293B");

        public NycLotWindow(Document doc)
        {
            _doc = doc;
            _plutoService = new NycPlutoService();
            _drawerService = new RevitLotDrawerService(doc);
            _searchResults = new ObservableCollection<NycSearchResult>();

            _levels = new FilteredElementCollector(doc)
                .OfClass(typeof(Level))
                .Cast<Level>()
                .OrderBy(l => l.Elevation)
                .ToList();

            _availableLineStyles = _drawerService.GetAvailableLineStyles();

            Title = "BauTools — NYC Lot Boundary, 3D Context Masses & Zoning Table";
            Height = 910;
            Width = 1180;
            MinHeight = 780;
            MinWidth = 1000;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Background = new SolidColorBrush(COL_BG);
            FontFamily = new System.Windows.Media.FontFamily("Segoe UI");
            FontSize = 13;

            BuildUI();
        }

        private void BuildUI()
        {
            var mainGrid = new WpfGrid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Header
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Content
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Footer

            // ── 1. HEADER ──
            var header = CreateHeader();
            WpfGrid.SetRow(header, 0);
            mainGrid.Children.Add(header);

            // ── 2. CONTENT (2 Columns) ──
            var contentGrid = new WpfGrid { Margin = new Thickness(24, 14, 24, 14) };
            contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(490) }); // Left: Search & Options
            contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(20) });  // Gap
            contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Right: Preview Card

            var leftPanel = CreateLeftPanel();
            WpfGrid.SetColumn(leftPanel, 0);
            contentGrid.Children.Add(leftPanel);

            var rightPanel = CreateRightPanel();
            WpfGrid.SetColumn(rightPanel, 2);
            contentGrid.Children.Add(rightPanel);

            WpfGrid.SetRow(contentGrid, 1);
            mainGrid.Children.Add(contentGrid);

            // ── 3. FOOTER ──
            var footer = CreateFooter();
            WpfGrid.SetRow(footer, 2);
            mainGrid.Children.Add(footer);

            Content = mainGrid;
        }

        private UIElement CreateHeader()
        {
            var headerBorder = new Border
            {
                Background = new SolidColorBrush(COL_HEADER_BG),
                Padding = new Thickness(24, 14, 24, 14)
            };

            var stack = new StackPanel();

            var titleRow = new StackPanel { Orientation = WpfOrientation.Horizontal };
            var badge = new Border
            {
                Background = new SolidColorBrush(COL_ACCENT),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(8, 2, 8, 2),
                Margin = new Thickness(0, 0, 10, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            badge.Child = new WpfTextBlock
            {
                Text = "NYC GIS 3D",
                Foreground = WpfBrushes.White,
                FontWeight = FontWeights.Bold,
                FontSize = 11
            };
            titleRow.Children.Add(badge);

            titleRow.Children.Add(new WpfTextBlock
            {
                Text = "NYC Lot Boundary, 3D Context Masses & Zoning Schedule",
                Foreground = WpfBrushes.White,
                FontWeight = FontWeights.Bold,
                FontSize = 17,
                VerticalAlignment = VerticalAlignment.Center
            });
            stack.Children.Add(titleRow);

            stack.Children.Add(new WpfTextBlock
            {
                Text = "Official NYC MapPLUTO & Building Footprints. Named Model Groups, Level 1 boundaries, 3D building masses & Native Revit Zoning Drafting View.",
                Foreground = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#94A3B8")),
                FontSize = 12,
                Margin = new Thickness(0, 3, 0, 0)
            });

            headerBorder.Child = stack;
            return headerBorder;
        }

        private UIElement CreateLeftPanel()
        {
            var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Padding = new Thickness(0, 0, 8, 0) };
            var stack = new StackPanel();

            // ── Card 1: Search Lot ──
            var searchCard = CreateCard("1. Search NYC Tax Lot");
            var searchContent = new StackPanel();

            var modePanel = new StackPanel { Orientation = WpfOrientation.Horizontal, Margin = new Thickness(0, 0, 0, 10) };
            _rbSearchAddress = new WpfRadioButton
            {
                Content = "By Address",
                IsChecked = true,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 16, 0)
            };
            _rbSearchAddress.Checked += (s, e) => ToggleSearchMode(true);

            _rbSearchBbl = new WpfRadioButton
            {
                Content = "By BBL (Boro-Block-Lot)",
                FontWeight = FontWeights.SemiBold
            };
            _rbSearchBbl.Checked += (s, e) => ToggleSearchMode(false);

            modePanel.Children.Add(_rbSearchAddress);
            modePanel.Children.Add(_rbSearchBbl);
            searchContent.Children.Add(modePanel);

            // Address Search
            _panelAddressSearch = new StackPanel();
            _panelAddressSearch.Children.Add(new WpfTextBlock
            {
                Text = "Street Address or Building Name:",
                FontSize = 11,
                Foreground = new SolidColorBrush(COL_MUTED),
                Margin = new Thickness(0, 0, 0, 4)
            });

            var searchRow = new WpfGrid();
            searchRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            searchRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
            searchRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            _txtAddressQuery = new WpfTextBox
            {
                Height = 30,
                Padding = new Thickness(8, 4, 8, 4),
                FontSize = 12,
                VerticalContentAlignment = VerticalAlignment.Center,
                BorderBrush = new SolidColorBrush(COL_BORDER)
            };
            _txtAddressQuery.KeyDown += async (s, e) =>
            {
                if (e.Key == System.Windows.Input.Key.Enter)
                {
                    await PerformAddressSearchAsync();
                }
            };
            WpfGrid.SetColumn(_txtAddressQuery, 0);
            searchRow.Children.Add(_txtAddressQuery);

            _btnSearch = CreateStyledButton("Search", COL_ACCENT, WpfBrushes.White);
            _btnSearch.Height = 30;
            _btnSearch.Padding = new Thickness(14, 0, 14, 0);
            _btnSearch.Click += async (s, e) => await PerformAddressSearchAsync();
            WpfGrid.SetColumn(_btnSearch, 2);
            searchRow.Children.Add(_btnSearch);

            _panelAddressSearch.Children.Add(searchRow);

            _listSearchResults = new WpfListBox
            {
                Height = 85,
                Margin = new Thickness(0, 6, 0, 0),
                ItemsSource = _searchResults,
                DisplayMemberPath = "Label",
                BorderBrush = new SolidColorBrush(COL_BORDER),
                Visibility = WpfVisibility.Collapsed
            };
            _listSearchResults.SelectionChanged += async (s, e) =>
            {
                if (_listSearchResults.SelectedItem is NycSearchResult selected && !string.IsNullOrEmpty(selected.Bbl))
                {
                    await LoadLotByBblAsync(selected.Bbl);
                }
            };
            _panelAddressSearch.Children.Add(_listSearchResults);

            searchContent.Children.Add(_panelAddressSearch);

            // BBL Search
            _panelBblSearch = new StackPanel { Visibility = WpfVisibility.Collapsed };

            var bblGrid = new WpfGrid();
            bblGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.3, GridUnitType.Star) });
            bblGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(6) });
            bblGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            bblGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(6) });
            bblGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var boroStack = new StackPanel();
            boroStack.Children.Add(new WpfTextBlock { Text = "Borough:", FontSize = 11, Foreground = new SolidColorBrush(COL_MUTED), Margin = new Thickness(0, 0, 0, 2) });
            _comboBorough = new WpfComboBox { Height = 28 };
            _comboBorough.Items.Add("1 - Manhattan");
            _comboBorough.Items.Add("2 - Bronx");
            _comboBorough.Items.Add("3 - Brooklyn");
            _comboBorough.Items.Add("4 - Queens");
            _comboBorough.Items.Add("5 - Staten Island");
            _comboBorough.SelectedIndex = 0;
            boroStack.Children.Add(_comboBorough);
            WpfGrid.SetColumn(boroStack, 0);
            bblGrid.Children.Add(boroStack);

            var blockStack = new StackPanel();
            blockStack.Children.Add(new WpfTextBlock { Text = "Block:", FontSize = 11, Foreground = new SolidColorBrush(COL_MUTED), Margin = new Thickness(0, 0, 0, 2) });
            _txtBlock = new WpfTextBox { Height = 28, Padding = new Thickness(4), VerticalContentAlignment = VerticalAlignment.Center, BorderBrush = new SolidColorBrush(COL_BORDER) };
            blockStack.Children.Add(_txtBlock);
            WpfGrid.SetColumn(blockStack, 2);
            bblGrid.Children.Add(blockStack);

            var lotStack = new StackPanel();
            lotStack.Children.Add(new WpfTextBlock { Text = "Lot:", FontSize = 11, Foreground = new SolidColorBrush(COL_MUTED), Margin = new Thickness(0, 0, 0, 2) });
            _txtLot = new WpfTextBox { Height = 28, Padding = new Thickness(4), VerticalContentAlignment = VerticalAlignment.Center, BorderBrush = new SolidColorBrush(COL_BORDER) };
            lotStack.Children.Add(_txtLot);
            WpfGrid.SetColumn(lotStack, 4);
            bblGrid.Children.Add(lotStack);

            _panelBblSearch.Children.Add(bblGrid);

            var btnLookupBbl = CreateStyledButton("Lookup BBL", COL_ACCENT, WpfBrushes.White);
            btnLookupBbl.Height = 28;
            btnLookupBbl.Margin = new Thickness(0, 6, 0, 0);
            btnLookupBbl.Click += async (s, e) => await PerformBblSearchAsync();
            _panelBblSearch.Children.Add(btnLookupBbl);

            searchContent.Children.Add(_panelBblSearch);

            _progressBar = new WpfProgressBar
            {
                Height = 3,
                IsIndeterminate = true,
                Margin = new Thickness(0, 6, 0, 0),
                Visibility = WpfVisibility.Collapsed
            };
            searchContent.Children.Add(_progressBar);

            searchCard.Child = searchContent;
            stack.Children.Add(searchCard);

            // ── Card 2: Level, Base Placement & Grouping (Proposal C) ──
            var baseCard = CreateCard("2. Placement, Grouping & Zoning Table");
            var baseContent = new StackPanel();

            var lvlRow = new WpfGrid { Margin = new Thickness(0, 0, 0, 8) };
            lvlRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.3, GridUnitType.Star) });
            lvlRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) });
            lvlRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.5, GridUnitType.Star) });

            var lvlStack = new StackPanel();
            lvlStack.Children.Add(new WpfTextBlock { Text = "Target Level (Level 1):", FontSize = 11, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(COL_DARK), Margin = new Thickness(0, 0, 0, 2) });
            _comboLevels = new WpfComboBox { Height = 28 };
            int defaultLevelIdx = 0;
            var lvl1 = _drawerService.GetLevel1();
            for (int i = 0; i < _levels.Count; i++)
            {
                _comboLevels.Items.Add(_levels[i].Name);
                if (lvl1 != null && _levels[i].Id == lvl1.Id) defaultLevelIdx = i;
            }
            if (_comboLevels.Items.Count > 0) _comboLevels.SelectedIndex = defaultLevelIdx;
            lvlStack.Children.Add(_comboLevels);
            WpfGrid.SetColumn(lvlStack, 0);
            lvlRow.Children.Add(lvlStack);

            var elemStack = new StackPanel();
            elemStack.Children.Add(new WpfTextBlock { Text = "Element Type:", FontSize = 11, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(COL_DARK), Margin = new Thickness(0, 0, 0, 2) });
            _comboElementType = new WpfComboBox { Height = 28 };
            _comboElementType.Items.Add("Model Curves (3D on Level)");
            _comboElementType.Items.Add("Detail Curves (2D Active View)");
            _comboElementType.Items.Add("Area Boundary Lines (Area Plan)");
            _comboElementType.SelectedIndex = 0;
            elemStack.Children.Add(_comboElementType);
            WpfGrid.SetColumn(elemStack, 2);
            lvlRow.Children.Add(elemStack);

            baseContent.Children.Add(lvlRow);

            _chkCreatePropLineLvl1 = new WpfCheckBox
            {
                Content = "🔒 Ensure Lot Boundaries are placed at Level 1",
                IsChecked = true,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#15803D")),
                Margin = new Thickness(0, 0, 0, 4)
            };
            baseContent.Children.Add(_chkCreatePropLineLvl1);

            _chkAlignPbp = new WpfCheckBox
            {
                Content = "Align Lot Anchor with Project Base Point (PBP)",
                IsChecked = true,
                FontWeight = FontWeights.Normal,
                Margin = new Thickness(0, 0, 0, 4)
            };
            baseContent.Children.Add(_chkAlignPbp);

            _comboAnchorCorner = new WpfComboBox { Height = 26, Margin = new Thickness(0, 0, 0, 8) };
            _comboAnchorCorner.Items.Add("Southwest Corner (Min X, Min Y) — Default");
            _comboAnchorCorner.Items.Add("Northwest Corner (Min X, Max Y)");
            _comboAnchorCorner.Items.Add("Southeast Corner (Max X, Min Y)");
            _comboAnchorCorner.Items.Add("Northeast Corner (Max X, Max Y)");
            _comboAnchorCorner.Items.Add("Geometric Center (Center of Bounding Box)");
            _comboAnchorCorner.SelectedIndex = 0;
            baseContent.Children.Add(_comboAnchorCorner);

            // Grouping options (Proposal C)
            var groupBorder = new Border
            {
                Background = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#F0FDF4")),
                BorderBrush = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#BBF7D0")),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(10, 8, 10, 8),
                Margin = new Thickness(0, 2, 0, 6)
            };
            var groupStack = new StackPanel();

            groupStack.Children.Add(new WpfTextBlock
            {
                Text = "📦 Model Grouping Mode:",
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#166534")),
                Margin = new Thickness(0, 0, 0, 4)
            });

            _rbGroupSingle = new WpfRadioButton
            {
                Content = "Single Group: [Address] (All elements grouped together)",
                IsChecked = true,
                FontWeight = FontWeights.Medium,
                Margin = new Thickness(0, 0, 0, 2)
            };
            groupStack.Children.Add(_rbGroupSingle);

            _rbGroupSplit = new WpfRadioButton
            {
                Content = "Split in 2 Groups: [NYC Lot - Address] & [NYC Context - Block]",
                FontWeight = FontWeights.Medium,
                Margin = new Thickness(0, 0, 0, 2)
            };
            groupStack.Children.Add(_rbGroupSplit);

            _rbGroupNone = new WpfRadioButton
            {
                Content = "Do not group elements",
                FontWeight = FontWeights.Normal,
                Margin = new Thickness(0, 0, 0, 4)
            };
            groupStack.Children.Add(_rbGroupNone);

            _chkPinGroup = new WpfCheckBox
            {
                Content = "📌 Pin / Lock Groups in Revit (prevent accidental movement)",
                IsChecked = false,
                FontWeight = FontWeights.Normal,
                Foreground = new SolidColorBrush(COL_DARK),
                Margin = new Thickness(18, 0, 0, 0)
            };
            groupStack.Children.Add(_chkPinGroup);

            groupBorder.Child = groupStack;
            baseContent.Children.Add(groupBorder);

            // Proposal B: Zoning Schedule Drafting Table
            _chkGenerateZoningTable = new WpfCheckBox
            {
                Content = "📊 Generate NYC Zoning Summary Drafting View / Table",
                IsChecked = true,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#0369A1")),
                Margin = new Thickness(0, 2, 0, 2)
            };
            baseContent.Children.Add(_chkGenerateZoningTable);

            baseCard.Child = baseContent;
            stack.Children.Add(baseCard);

            // ── Card 3: Granular Line Style Selectors ──
            var stylesCard = CreateCard("3. Line Style Selectors (Per Lot Type)");
            var stylesContent = new StackPanel();

            stylesContent.Children.Add(CreateLineStyleRow(
                out _chkDrawSubjectLot, "🔴 Development Lot (Lote en cuestión):", true,
                out _comboSubjectLineStyle, RevitLotDrawerService.STYLE_SUBJECT_RED));

            stylesContent.Children.Add(CreateLineStyleRow(
                out _chkDrawAdjacentLots, "🟠 Adjacent Lots (Lotes Circundantes / Vecinos):", true,
                out _comboAdjacentLineStyle, RevitLotDrawerService.STYLE_ADJACENT_ORANGE));

            stylesContent.Children.Add(CreateLineStyleRow(
                out _chkDrawBlockContext, "🏙️ Block Context (Resto de la Manzana):", true,
                out _comboBlockContextLineStyle, RevitLotDrawerService.STYLE_CONTEXT_GRAY));

            stylesContent.Children.Add(CreateLineStyleRow(
                out _chkDrawSidewalk, "🚶 Sidewalk Curbs (Aceras / Bordillos 12ft):", true,
                out _comboSidewalkLineStyle, RevitLotDrawerService.STYLE_SIDEWALK_BLUE));

            _chkPlaceStreetNotes = new WpfCheckBox
            {
                Content = "🔤 Place Surrounding Street Titles as Text Notes",
                IsChecked = true,
                FontWeight = FontWeights.Normal,
                Margin = new Thickness(0, 4, 0, 2)
            };
            stylesContent.Children.Add(_chkPlaceStreetNotes);

            stylesCard.Child = stylesContent;
            stack.Children.Add(stylesCard);

            // ── Card 4: 3D Context Building Masses ──
            var massCard = CreateCard("4. 🏢 3D Building Masses (Real NYC Heights)");
            var massContent = new StackPanel();

            _chkCreate3DBuildingMasses = new WpfCheckBox
            {
                Content = "🏢 Create 3D Context Masses with Real Heights (HEIGHT_ROO)",
                IsChecked = true,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#1E40AF")),
                Margin = new Thickness(0, 0, 0, 4)
            };
            massContent.Children.Add(_chkCreate3DBuildingMasses);

            _chkExtrudeSubjectLotBuilding = new WpfCheckBox
            {
                Content = "Extrude existing building on Development Lot (Lote propio)",
                IsChecked = false,
                FontWeight = FontWeights.Normal,
                Margin = new Thickness(20, 0, 0, 6)
            };
            massContent.Children.Add(_chkExtrudeSubjectLotBuilding);

            var noteSubcat = new WpfTextBlock
            {
                Text = "• Subcategory: Generic Models > NYC Context Building\n• Material: NYC - Urban Context (Auto-created, no duplicates)\n• Courtyards & Interior Holes are automatically extruded.",
                FontSize = 10.5,
                Foreground = new SolidColorBrush(COL_MUTED),
                Margin = new Thickness(20, 0, 0, 2)
            };
            massContent.Children.Add(noteSubcat);

            _chkCreate3DBuildingMasses.Checked += (s, e) => _chkExtrudeSubjectLotBuilding.IsEnabled = true;
            _chkCreate3DBuildingMasses.Unchecked += (s, e) => _chkExtrudeSubjectLotBuilding.IsEnabled = false;

            massCard.Child = massContent;
            stack.Children.Add(massCard);

            scroll.Content = stack;
            return scroll;
        }

        private UIElement CreateLineStyleRow(out WpfCheckBox chk, string label, bool defaultChecked, out WpfComboBox combo, string defaultStyleName)
        {
            var panel = new StackPanel { Margin = new Thickness(0, 0, 0, 8) };

            chk = new WpfCheckBox
            {
                Content = label,
                IsChecked = defaultChecked,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 2)
            };
            panel.Children.Add(chk);

            combo = new WpfComboBox { Height = 26, Margin = new Thickness(20, 0, 0, 0) };
            int selectedIdx = 0;
            for (int i = 0; i < _availableLineStyles.Count; i++)
            {
                combo.Items.Add(_availableLineStyles[i]);
                if (string.Equals(_availableLineStyles[i], defaultStyleName, StringComparison.OrdinalIgnoreCase))
                {
                    selectedIdx = i;
                }
            }
            combo.SelectedIndex = selectedIdx;

            var targetCombo = combo;
            chk.Checked += (s, e) => targetCombo.IsEnabled = true;
            chk.Unchecked += (s, e) => targetCombo.IsEnabled = false;

            panel.Children.Add(combo);
            return panel;
        }

        private UIElement CreateRightPanel()
        {
            _infoCardContainer = new Border
            {
                Background = new SolidColorBrush(COL_CARD),
                BorderBrush = new SolidColorBrush(COL_BORDER),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(20),
                Effect = new DropShadowEffect
                {
                    Color = WpfColors.Black,
                    Direction = 270,
                    ShadowDepth = 1,
                    Opacity = 0.05,
                    BlurRadius = 8
                }
            };

            var rootStack = new StackPanel();

            _txtPlaceholderInfo = new WpfTextBlock
            {
                Text = "🔍 Search for an address or BBL on the left to preview the tax lot geometry, zoning districts, FAR limits, 3D building heights, and surrounding streets.",
                FontSize = 13,
                Foreground = new SolidColorBrush(COL_MUTED),
                TextWrapping = TextWrapping.Wrap,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(20, 100, 20, 100)
            };
            rootStack.Children.Add(_txtPlaceholderInfo);

            _panelLotDetails = new StackPanel { Visibility = WpfVisibility.Collapsed };

            // 1. Lot Header Banner
            var banner = new Border
            {
                Background = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#EFF6FF")),
                BorderBrush = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#BFDBFE")),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(14, 10, 14, 10),
                Margin = new Thickness(0, 0, 0, 12)
            };
            var bannerStack = new StackPanel();
            _lblLotAddress = new WpfTextBlock
            {
                Text = "350 5TH AVENUE",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(COL_DARK)
            };
            _lblLotBbl = new WpfTextBlock
            {
                Text = "BBL: 1008350041 | Manhattan | Block: 835 | Lot: 41",
                FontSize = 12,
                Foreground = new SolidColorBrush(COL_ACCENT),
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 2, 0, 0)
            };
            _lblBlockContextSummary = new WpfTextBlock
            {
                Text = "🏙️ Block Context: 11 Lots | 3D Buildings Loaded | Streets: W 33RD ST, W 34TH ST, 5TH AVE",
                FontSize = 11,
                Foreground = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#0369A1")),
                FontWeight = FontWeights.Medium,
                Margin = new Thickness(0, 3, 0, 0)
            };
            bannerStack.Children.Add(_lblLotAddress);
            bannerStack.Children.Add(_lblLotBbl);
            bannerStack.Children.Add(_lblBlockContextSummary);
            banner.Child = bannerStack;
            _panelLotDetails.Children.Add(banner);

            // 2. Zoning Section
            var zoningBox = new Border
            {
                Background = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#F8FAFC")),
                BorderBrush = new SolidColorBrush(COL_BORDER),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(12),
                Margin = new Thickness(0, 0, 0, 12)
            };
            var zStack = new StackPanel();
            zStack.Children.Add(new WpfTextBlock
            {
                Text = "ZONING & URBAN PLANNING",
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(COL_MUTED),
                Margin = new Thickness(0, 0, 0, 4)
            });
            _lblZoningSummary = new WpfTextBlock
            {
                Text = "C5-3 / Special: MID (Midtown)",
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(COL_DARK)
            };
            _lblExtraDetails = new WpfTextBlock
            {
                Text = "Owner: EMPIRE STATE BLDG | Land Use: Commercial | Year Built: 1931",
                FontSize = 11,
                Foreground = new SolidColorBrush(COL_MUTED),
                Margin = new Thickness(0, 4, 0, 0)
            };
            zStack.Children.Add(_lblZoningSummary);
            zStack.Children.Add(_lblExtraDetails);
            zoningBox.Child = zStack;
            _panelLotDetails.Children.Add(zoningBox);

            // 3. FAR & Areas Grid
            var metricsGrid = new WpfGrid { Margin = new Thickness(0, 0, 0, 12) };
            metricsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            metricsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
            metricsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            metricsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
            metricsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            metricsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
            metricsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            _lblResFar = new WpfTextBlock { Text = "0.00", FontSize = 14, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(COL_DARK) };
            _lblCommFar = new WpfTextBlock { Text = "15.00", FontSize = 14, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(COL_DARK) };
            _lblFacilFar = new WpfTextBlock { Text = "15.00", FontSize = 14, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(COL_DARK) };
            _lblBuiltFar = new WpfTextBlock { Text = "28.12", FontSize = 14, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(COL_ACCENT2) };

            var c1 = CreateMetricMiniCard("Resid. FAR", _lblResFar);
            var c2 = CreateMetricMiniCard("Comm. FAR", _lblCommFar);
            var c3 = CreateMetricMiniCard("Facil. FAR", _lblFacilFar);
            var c4 = CreateMetricMiniCard("Built FAR", _lblBuiltFar);

            WpfGrid.SetColumn(c1, 0); metricsGrid.Children.Add(c1);
            WpfGrid.SetColumn(c2, 2); metricsGrid.Children.Add(c2);
            WpfGrid.SetColumn(c3, 4); metricsGrid.Children.Add(c3);
            WpfGrid.SetColumn(c4, 6); metricsGrid.Children.Add(c4);

            _panelLotDetails.Children.Add(metricsGrid);

            // 4. Lot Area & Dimensions Box
            var dimGrid = new WpfGrid { Margin = new Thickness(0, 0, 0, 10) };
            dimGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            dimGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
            dimGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            dimGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
            dimGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.3, GridUnitType.Star) });

            _lblLotArea = new WpfTextBlock { Text = "91,351 SF", FontSize = 13, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(COL_DARK) };
            _lblBldgArea = new WpfTextBlock { Text = "2,568,970 SF", FontSize = 13, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(COL_DARK) };
            _lblDimensions = new WpfTextBlock { Text = "197.5 ft × 425.0 ft", FontSize = 13, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(COL_DARK) };

            var d1 = CreateMetricMiniCard("Lot Area (PLUTO)", _lblLotArea);
            var d2 = CreateMetricMiniCard("Bldg Gross Area", _lblBldgArea);
            var d3 = CreateMetricMiniCard("Subject Lot W × D", _lblDimensions);

            WpfGrid.SetColumn(d1, 0); dimGrid.Children.Add(d1);
            WpfGrid.SetColumn(d2, 2); dimGrid.Children.Add(d2);
            WpfGrid.SetColumn(d3, 4); dimGrid.Children.Add(d3);

            _panelLotDetails.Children.Add(dimGrid);

            rootStack.Children.Add(_panelLotDetails);
            _infoCardContainer.Child = rootStack;

            return _infoCardContainer;
        }

        private UIElement CreateFooter()
        {
            var footerBorder = new Border
            {
                Background = WpfBrushes.White,
                BorderBrush = new SolidColorBrush(COL_BORDER),
                BorderThickness = new Thickness(0, 1, 0, 0),
                Padding = new Thickness(24, 14, 24, 14)
            };

            var footerGrid = new WpfGrid();
            footerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            footerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            _txtStatusMsg = new WpfTextBlock
            {
                Text = "Ready to search.",
                FontSize = 12,
                Foreground = new SolidColorBrush(COL_MUTED),
                VerticalAlignment = VerticalAlignment.Center
            };
            WpfGrid.SetColumn(_txtStatusMsg, 0);
            footerGrid.Children.Add(_txtStatusMsg);

            var btnStack = new StackPanel { Orientation = WpfOrientation.Horizontal };

            var btnCancel = CreateStyledButton("Close", (WpfColor)ColorConverter.ConvertFromString("#E2E8F0"), new SolidColorBrush(COL_DARK));
            btnCancel.Width = 90;
            btnCancel.Height = 34;
            btnCancel.Margin = new Thickness(0, 0, 10, 0);
            btnCancel.Click += (s, e) => Close();
            btnStack.Children.Add(btnCancel);

            _btnDrawInRevit = CreateStyledButton("Draw in Revit", COL_ACCENT, WpfBrushes.White);
            _btnDrawInRevit.Width = 140;
            _btnDrawInRevit.Height = 34;
            _btnDrawInRevit.FontWeight = FontWeights.Bold;
            _btnDrawInRevit.IsEnabled = false;
            _btnDrawInRevit.Click += (s, e) => ExecuteDrawLot();
            btnStack.Children.Add(_btnDrawInRevit);

            WpfGrid.SetColumn(btnStack, 1);
            footerGrid.Children.Add(btnStack);

            footerBorder.Child = footerGrid;
            return footerBorder;
        }

        // ── Helper UI Builders ──
        private Border CreateCard(string title)
        {
            var card = new Border
            {
                Background = new SolidColorBrush(COL_CARD),
                BorderBrush = new SolidColorBrush(COL_BORDER),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(14),
                Margin = new Thickness(0, 0, 0, 10),
                Effect = new DropShadowEffect
                {
                    Color = WpfColors.Black,
                    Direction = 270,
                    ShadowDepth = 1,
                    Opacity = 0.04,
                    BlurRadius = 4
                }
            };

            var stack = new StackPanel();
            stack.Children.Add(new WpfTextBlock
            {
                Text = title,
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(COL_DARK),
                Margin = new Thickness(0, 0, 0, 8)
            });

            return card;
        }

        private Border CreateMetricMiniCard(string label, WpfTextBlock valueBlock)
        {
            var border = new Border
            {
                Background = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#F8FAFC")),
                BorderBrush = new SolidColorBrush(COL_BORDER),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(8)
            };
            var stack = new StackPanel();
            stack.Children.Add(new WpfTextBlock
            {
                Text = label,
                FontSize = 10,
                Foreground = new SolidColorBrush(COL_MUTED),
                Margin = new Thickness(0, 0, 0, 2)
            });
            stack.Children.Add(valueBlock);
            border.Child = stack;
            return border;
        }

        private WpfButton CreateStyledButton(string text, WpfColor bgColor, System.Windows.Media.Brush fgBrush)
        {
            var btn = new WpfButton
            {
                Content = text,
                Background = new SolidColorBrush(bgColor),
                Foreground = fgBrush,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand,
                FontSize = 12
            };
            return btn;
        }

        // ── Search & Logic Handlers ──
        private void ToggleSearchMode(bool addressMode)
        {
            _panelAddressSearch.Visibility = addressMode ? WpfVisibility.Visible : WpfVisibility.Collapsed;
            _panelBblSearch.Visibility = addressMode ? WpfVisibility.Collapsed : WpfVisibility.Visible;
        }

        private async Task PerformAddressSearchAsync()
        {
            string query = _txtAddressQuery.Text.Trim();
            if (string.IsNullOrWhiteSpace(query))
            {
                SetStatus("Please enter a NYC address to search.", true);
                return;
            }

            SetLoading(true, $"Searching '{query}' in NYC Planning GeoSearch...");
            _searchResults.Clear();
            _listSearchResults.Visibility = WpfVisibility.Collapsed;

            try
            {
                var results = await _plutoService.SearchAddressAsync(query);
                if (results.Count == 0)
                {
                    SetStatus("No NYC addresses found matching your search.", true);
                }
                else
                {
                    foreach (var res in results)
                    {
                        _searchResults.Add(res);
                    }
                    _listSearchResults.Visibility = WpfVisibility.Visible;
                    _listSearchResults.SelectedIndex = 0;
                    SetStatus($"Found {results.Count} address matches. Select one to load geometry.");
                }
            }
            catch (Exception ex)
            {
                SetStatus($"Search error: {ex.Message}", true);
            }
            finally
            {
                SetLoading(false);
            }
        }

        private async Task PerformBblSearchAsync()
        {
            int boroughCode = _comboBorough.SelectedIndex + 1;
            if (!int.TryParse(_txtBlock.Text.Trim(), out int block) || block <= 0)
            {
                SetStatus("Please enter a valid Block number.", true);
                return;
            }
            if (!int.TryParse(_txtLot.Text.Trim(), out int lot) || lot <= 0)
            {
                SetStatus("Please enter a valid Lot number.", true);
                return;
            }

            string bbl = $"{boroughCode}{block:D5}{lot:D4}";
            await LoadLotByBblAsync(bbl);
        }

        private async Task LoadLotByBblAsync(string bbl)
        {
            SetLoading(true, $"Querying MapPLUTO & 3D Building Footprints for BBL {bbl}...");

            try
            {
                var lotInfo = await _plutoService.GetLotByBblAsync(bbl);
                if (lotInfo == null)
                {
                    SetStatus($"Could not find MapPLUTO data for BBL {bbl}.", true);
                    _currentLot = null;
                    _currentBlockContext = null;
                    _btnDrawInRevit.IsEnabled = false;
                    _panelLotDetails.Visibility = WpfVisibility.Collapsed;
                    _txtPlaceholderInfo.Visibility = WpfVisibility.Visible;
                }
                else
                {
                    _currentLot = lotInfo;

                    // Fetch full block context and 3D building footprints
                    _currentBlockContext = await _plutoService.GetBlockContextAsync(lotInfo);

                    DisplayLotInfo(lotInfo, _currentBlockContext);
                    _btnDrawInRevit.IsEnabled = true;
                    int bldgCount = _currentBlockContext.Buildings.Count;
                    int totalCount = _currentBlockContext.AllLots.Count;
                    SetStatus($"Loaded NYC Lot {lotInfo.Bbl} ({totalCount} lots in block, {bldgCount} 3D buildings). Ready to draw on Level 1 as Group.");
                }
            }
            catch (Exception ex)
            {
                SetStatus($"Error loading BBL: {ex.Message}", true);
                _btnDrawInRevit.IsEnabled = false;
            }
            finally
            {
                SetLoading(false);
            }
        }

        private void DisplayLotInfo(NycLotInfo lot, NycBlockContext blockContext)
        {
            _txtPlaceholderInfo.Visibility = WpfVisibility.Collapsed;
            _panelLotDetails.Visibility = WpfVisibility.Visible;

            _lblLotAddress.Text = string.IsNullOrWhiteSpace(lot.Address) ? $"LOT {lot.Lot}, BLOCK {lot.Block}" : lot.Address.ToUpperInvariant();
            _lblLotBbl.Text = $"BBL: {lot.Bbl} | Borough: {lot.Borough} | Block: {lot.Block} | Lot: {lot.Lot} | ZIP: {lot.ZipCode}";

            var streets = blockContext.GetSurroundingStreetNames();
            string streetSummary = streets.Count > 0 ? string.Join(", ", streets.Values) : "N/A";
            int bldgCount = blockContext.Buildings.Count;
            _lblBlockContextSummary.Text = $"🏙️ Block {lot.Block}: {blockContext.AllLots.Count} Lots | {bldgCount} 3D Buildings | Streets: {streetSummary}";

            _lblZoningSummary.Text = lot.GetZoningSummary();
            _lblExtraDetails.Text = $"Owner: {(string.IsNullOrEmpty(lot.OwnerName) ? "N/A" : lot.OwnerName)} | Class: {lot.BuildingClass} | Built: {(lot.YearBuilt > 0 ? lot.YearBuilt.ToString() : "N/A")} | Floors: {lot.NumFloors}";

            _lblResFar.Text = lot.ResidFar.ToString("F2");
            _lblCommFar.Text = lot.CommFar.ToString("F2");
            _lblFacilFar.Text = lot.FacilFar.ToString("F2");
            _lblBuiltFar.Text = lot.BuiltFar.ToString("F2");

            _lblLotArea.Text = $"{lot.LotAreaSqFt:N0} SF";
            _lblBldgArea.Text = $"{lot.BldgAreaSqFt:N0} SF";
            _lblDimensions.Text = $"{lot.WidthFt:F1} ft × {lot.DepthFt:F1} ft";
        }

        private void ExecuteDrawLot()
        {
            if (_currentLot == null || _currentBlockContext == null)
            {
                SetStatus("No lot selected to draw.", true);
                return;
            }

            LotGroupingMode grpMode = LotGroupingMode.SingleGroup;
            if (_rbGroupSplit.IsChecked == true) grpMode = LotGroupingMode.SplitGroups;
            else if (_rbGroupNone.IsChecked == true) grpMode = LotGroupingMode.NoGroup;

            var options = new LotDrawOptions
            {
                ElementType = (LotElementType)_comboElementType.SelectedIndex,
                AnchorCorner = (LotAnchorCorner)_comboAnchorCorner.SelectedIndex,
                AlignWithPbp = _chkAlignPbp.IsChecked == true,
                EnsureLevel1Placement = _chkCreatePropLineLvl1.IsChecked == true,
                GroupingMode = grpMode,
                PinCreatedGroup = _chkPinGroup.IsChecked == true,
                GenerateZoningDraftingTable = _chkGenerateZoningTable.IsChecked == true,
                DrawSubjectLot = _chkDrawSubjectLot.IsChecked == true,
                SubjectLineStyle = _comboSubjectLineStyle.SelectedItem?.ToString() ?? RevitLotDrawerService.STYLE_SUBJECT_RED,
                DrawAdjacentLots = _chkDrawAdjacentLots.IsChecked == true,
                AdjacentLineStyle = _comboAdjacentLineStyle.SelectedItem?.ToString() ?? RevitLotDrawerService.STYLE_ADJACENT_ORANGE,
                DrawRemainingBlockLots = _chkDrawBlockContext.IsChecked == true,
                BlockContextLineStyle = _comboBlockContextLineStyle.SelectedItem?.ToString() ?? RevitLotDrawerService.STYLE_CONTEXT_GRAY,
                DrawSidewalks = _chkDrawSidewalk.IsChecked == true,
                SidewalkLineStyle = _comboSidewalkLineStyle.SelectedItem?.ToString() ?? RevitLotDrawerService.STYLE_SIDEWALK_BLUE,
                PlaceStreetTextNotes = _chkPlaceStreetNotes.IsChecked == true,
                Create3DBuildingMasses = _chkCreate3DBuildingMasses.IsChecked == true,
                ExtrudeSubjectLotBuilding = _chkExtrudeSubjectLotBuilding.IsChecked == true
            };

            if (_comboLevels.SelectedIndex >= 0 && _comboLevels.SelectedIndex < _levels.Count)
            {
                options.TargetLevel = _levels[_comboLevels.SelectedIndex];
            }

            var result = _drawerService.DrawLotWithContext(_currentBlockContext, options);

            if (result.Success)
            {
                string targetLevelName = options.TargetLevel?.Name ?? "Level 1";
                MessageBox.Show(
                    $"{result.Message}\n\nLevel: {targetLevelName}\nLot: {_currentLot.Address}\nBBL: {_currentLot.Bbl}\nZoning: {_currentLot.GetZoningSummary()}\nArea: {_currentLot.LotAreaSqFt:N0} SF",
                    "BauTools — NYC Lot & Urban Context Created",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                Close();
            }
            else
            {
                MessageBox.Show(
                    result.Message,
                    "BauTools — Draw Lot Warning",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                SetStatus(result.Message, true);
            }
        }

        private void SetLoading(bool isLoading, string statusText = "")
        {
            _progressBar.Visibility = isLoading ? WpfVisibility.Visible : WpfVisibility.Collapsed;
            _btnSearch.IsEnabled = !isLoading;
            if (!string.IsNullOrEmpty(statusText))
            {
                SetStatus(statusText);
            }
        }

        private void SetStatus(string msg, bool isError = false)
        {
            _txtStatusMsg.Text = msg;
            _txtStatusMsg.Foreground = isError
                ? new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#DC2626"))
                : new SolidColorBrush(COL_MUTED);
        }
    }
}
