using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using Autodesk.Revit.DB;
using ZoningFloorArea.Models;
using ZoningFloorArea.Services;
using ZoningFloorArea.ViewModels;

// Aliases to avoid ambiguity between System.Windows and Autodesk.Revit.DB
using WpfGrid = System.Windows.Controls.Grid;
using WpfButton = System.Windows.Controls.Button;
using WpfTextBox = System.Windows.Controls.TextBox;
using WpfTextBlock = System.Windows.Controls.TextBlock;
using WpfComboBox = System.Windows.Controls.ComboBox;
using WpfCheckBox = System.Windows.Controls.CheckBox;
using WpfRadioButton = System.Windows.Controls.RadioButton;
using WpfListBox = System.Windows.Controls.ListBox;
using WpfOrientation = System.Windows.Controls.Orientation;
using WpfVisibility = System.Windows.Visibility;
using WpfColor = System.Windows.Media.Color;
using WpfColors = System.Windows.Media.Colors;
using WpfBrushes = System.Windows.Media.Brushes;
using WpfBinding = System.Windows.Data.Binding;

namespace ZoningFloorArea.Views
{
    public class MainWindow : Window
    {
        private readonly MainViewModel _vm;

        // Apple-style Neutral Palette
        private static readonly WpfColor COL_BG          = WpfColors.White;
        private static readonly WpfColor COL_SURFACE     = (WpfColor)ColorConverter.ConvertFromString("#F5F5F7");
        private static readonly WpfColor COL_TEXT_MAIN   = (WpfColor)ColorConverter.ConvertFromString("#1D1D1F");
        private static readonly WpfColor COL_TEXT_MUTED  = (WpfColor)ColorConverter.ConvertFromString("#86868B");
        private static readonly WpfColor COL_BORDER      = (WpfColor)ColorConverter.ConvertFromString("#D2D2D7");
        private static readonly WpfColor COL_BORDER_LIGHT= (WpfColor)ColorConverter.ConvertFromString("#E5E5EA");
        private static readonly WpfColor COL_PRIMARY     = (WpfColor)ColorConverter.ConvertFromString("#0071E3"); // Apple Blue
        private static readonly WpfColor COL_BTN_NEUTRAL = (WpfColor)ColorConverter.ConvertFromString("#E8E8ED");
        private static readonly WpfColor COL_BTN_HOVER   = (WpfColor)ColorConverter.ConvertFromString("#D1D1D6");
        private static readonly WpfColor COL_SUCCESS     = (WpfColor)ColorConverter.ConvertFromString("#34C759");
        private static readonly WpfColor COL_DANGER      = (WpfColor)ColorConverter.ConvertFromString("#FF3B30");

        // Color Palette for Popover
        private static readonly string[] COLOR_PALETTE = new string[]
        {
            "#0071E3", "#34C759", "#FF9500", "#AF52DE", "#FF2D55", "#5856D6", "#64748B", "#00C7BE"
        };

        // Step Panels
        private WpfButton[] _stepButtons;
        private Border[] _stepIndicatorBorders;
        private WpfGrid[] _stepPanels;
        private int _activeStepIndex = 0; // 0: Typical Floors, 1: Propagate, 2: Calculate, 3: Export

        // UI Controls for Dynamic Refresh
        private StackPanel _buildingTabBar;
        private StackPanel _typicalGroupsContainer;
        private StackPanel _towerContainer;
        private StackPanel _propagateSummaryContainer;
        private TabControl _tabControlBuildings;
        private StackPanel _step4PreviewContainer;
        private StackPanel _packagesContainer;
        private WpfTextBlock _step4SummaryBadge;
        private WpfTextBlock _txtStatus;

        // In-App Toast Container
        private Border _toastBorder;
        private WpfTextBlock _toastText;
        private DispatcherTimer _toastTimer;

        public MainWindow(Document doc) : this(new MainViewModel(doc))
        {
        }

        public MainWindow(MainViewModel vm)
        {
            _vm = vm;
            DataContext = _vm;

            Title = "BauTools — Zoning Floor Area Calculator";
            Width = 1260;
            Height = 860;
            MinWidth = 1050;
            MinHeight = 700;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Background = new SolidColorBrush(COL_BG);
            FontFamily = new System.Windows.Media.FontFamily("Segoe UI");
            FontSize = 13;

            _stepButtons = new WpfButton[4];
            _stepIndicatorBorders = new Border[4];
            _stepPanels = new WpfGrid[4];

            _vm.OnToastNotification = (msg, isError) => ShowToast(msg, isError);

            InitMinimalistStyles();
            BuildUI();
        }

        private void BuildUI()
        {
            WpfGrid rootGrid = new WpfGrid();
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 0: Header
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 1: Step Navigation Bar
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // 2: Content Area
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 3: Status & Navigation Footer

            // 0: Header
            UIElement header = CreateHeader();
            WpfGrid.SetRow(header, 0);
            rootGrid.Children.Add(header);

            // 1: Step Segmented Navigation
            UIElement stepper = CreateStepSegmentedBar();
            WpfGrid.SetRow(stepper, 1);
            rootGrid.Children.Add(stepper);

            // 2: Content Area (Houses all 4 step panels + Toast Host)
            WpfGrid contentHostGrid = new WpfGrid();

            Border contentHost = new Border
            {
                Background = new SolidColorBrush(COL_SURFACE),
                BorderBrush = new SolidColorBrush(COL_BORDER_LIGHT),
                BorderThickness = new Thickness(0, 1, 0, 1),
                Padding = new Thickness(24, 16, 24, 16)
            };

            WpfGrid contentGrid = new WpfGrid();
            _stepPanels[0] = CreateStep1Panel(); // Typical Floors, Duplex & Buildings
            _stepPanels[1] = CreateStep2Panel(); // Propagate
            _stepPanels[2] = CreateStep3Panel(); // Calculate ZFA
            _stepPanels[3] = CreateStep4Panel(); // Master & Dependent Views, Sheets & Export

            for (int i = 0; i < 4; i++)
            {
                contentGrid.Children.Add(_stepPanels[i]);
            }

            contentHost.Child = contentGrid;
            contentHostGrid.Children.Add(contentHost);

            // Toast Floating Banner (Bottom-Right overlay)
            _toastBorder = new Border
            {
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(16, 10, 16, 10),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(0, 0, 32, 24),
                Visibility = WpfVisibility.Collapsed,
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = WpfColors.Black,
                    BlurRadius = 12,
                    Opacity = 0.15,
                    ShadowDepth = 3
                }
            };
            _toastText = new WpfTextBlock { FontWeight = FontWeights.SemiBold, FontSize = 12 };
            _toastBorder.Child = _toastText;
            contentHostGrid.Children.Add(_toastBorder);

            WpfGrid.SetRow(contentHostGrid, 2);
            rootGrid.Children.Add(contentHostGrid);

            // 3: Footer
            UIElement footer = CreateFooter();
            WpfGrid.SetRow(footer, 3);
            rootGrid.Children.Add(footer);

            Content = rootGrid;

            // Activate initial step
            SwitchToStep(0);
        }

        private void ShowToast(string message, bool isError)
        {
            if (_toastBorder == null || _toastText == null) return;

            Dispatcher.Invoke(() =>
            {
                _toastText.Text = (isError ? "⚠️ " : "✓ ") + message;
                _toastText.Foreground = isError ? new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#991B1B")) : new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#065F46"));
                _toastBorder.Background = isError ? new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#FEE2E2")) : new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#D1FAE5"));
                _toastBorder.BorderBrush = isError ? new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#FCA5A5")) : new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#6EE7B7"));
                _toastBorder.BorderThickness = new Thickness(1);
                _toastBorder.Visibility = WpfVisibility.Visible;

                if (_toastTimer != null) _toastTimer.Stop();
                _toastTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3.5) };
                _toastTimer.Tick += (s, e) =>
                {
                    _toastBorder.Visibility = WpfVisibility.Collapsed;
                    _toastTimer.Stop();
                };
                _toastTimer.Start();
            });
        }

        private UIElement CreateHeader()
        {
            Border header = new Border
            {
                Background = new SolidColorBrush(COL_BG),
                Padding = new Thickness(24, 14, 24, 12)
            };

            WpfGrid grid = new WpfGrid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Left: Title & Subtitle
            StackPanel titleStack = new StackPanel();
            
            StackPanel brandRow = new StackPanel { Orientation = WpfOrientation.Horizontal };
            brandRow.Children.Add(new WpfTextBlock
            {
                Text = "BauTools",
                FontSize = 18,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(COL_TEXT_MAIN),
                VerticalAlignment = VerticalAlignment.Center
            });

            Border dot = new Border
            {
                Width = 4,
                Height = 4,
                CornerRadius = new CornerRadius(2),
                Background = new SolidColorBrush(COL_TEXT_MUTED),
                Margin = new Thickness(10, 0, 10, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            brandRow.Children.Add(dot);

            brandRow.Children.Add(new WpfTextBlock
            {
                Text = "Zoning Floor Area (ZFA) & Typical Floors Suite",
                FontSize = 14,
                FontWeight = FontWeights.Normal,
                Foreground = new SolidColorBrush(COL_TEXT_MUTED),
                VerticalAlignment = VerticalAlignment.Center
            });
            titleStack.Children.Add(brandRow);

            WpfGrid.SetColumn(titleStack, 0);
            grid.Children.Add(titleStack);

            // Right: Developer Label
            WpfTextBlock devInfo = new WpfTextBlock
            {
                Text = "Arch Sergio Castro",
                FontSize = 12,
                Foreground = new SolidColorBrush(COL_TEXT_MUTED),
                VerticalAlignment = VerticalAlignment.Center
            };
            WpfGrid.SetColumn(devInfo, 1);
            grid.Children.Add(devInfo);

            header.Child = grid;
            return header;
        }

        private UIElement CreateStepSegmentedBar()
        {
            Border barContainer = new Border
            {
                Background = new SolidColorBrush(COL_BG),
                Padding = new Thickness(24, 0, 24, 12)
            };

            Border pill = new Border
            {
                Background = new SolidColorBrush(COL_SURFACE),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(3)
            };

            WpfGrid grid = new WpfGrid();
            for (int i = 0; i < 4; i++)
            {
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            }

            string[] stepTitles = new string[]
            {
                "1. Typical Floors & Buildings",
                "2. Propagate Areas",
                "3. Calculate ZFA Matrix",
                "4. Master Views, Sheets & Export"
            };

            for (int i = 0; i < 4; i++)
            {
                int stepIdx = i;
                Border stepBorder = new Border
                {
                    CornerRadius = new CornerRadius(6),
                    Background = WpfBrushes.Transparent,
                    Padding = new Thickness(0, 8, 0, 8)
                };

                WpfButton btn = new WpfButton
                {
                    Content = stepTitles[i],
                    Background = WpfBrushes.Transparent,
                    BorderThickness = new Thickness(0),
                    FontSize = 13,
                    FontWeight = FontWeights.Medium,
                    Foreground = new SolidColorBrush(COL_TEXT_MUTED),
                    Cursor = System.Windows.Input.Cursors.Hand
                };

                btn.Click += (s, e) => SwitchToStep(stepIdx);

                stepBorder.Child = btn;
                _stepIndicatorBorders[i] = stepBorder;
                _stepButtons[i] = btn;

                WpfGrid.SetColumn(stepBorder, i);
                grid.Children.Add(stepBorder);
            }

            pill.Child = grid;
            barContainer.Child = pill;
            return barContainer;
        }

        private void SwitchToStep(int stepIndex)
        {
            _activeStepIndex = stepIndex;
            _vm.CurrentStep = stepIndex + 1;

            for (int i = 0; i < 4; i++)
            {
                bool isActive = (i == stepIndex);
                _stepIndicatorBorders[i].Background = isActive ? WpfBrushes.White : WpfBrushes.Transparent;
                _stepButtons[i].Foreground = isActive ? new SolidColorBrush(COL_TEXT_MAIN) : new SolidColorBrush(COL_TEXT_MUTED);
                _stepButtons[i].FontWeight = isActive ? FontWeights.SemiBold : FontWeights.Medium;
                _stepPanels[i].Visibility = isActive ? WpfVisibility.Visible : WpfVisibility.Collapsed;
            }

            // Trigger specific step refresh
            if (stepIndex == 0)
            {
                RefreshBuildingTabsUI();
                RefreshTypicalGroupsUI();
                RefreshTowerUI();
            }
            if (stepIndex == 1) RefreshPropagateReviewUI();
            if (stepIndex == 2) RefreshCalculateUI();
            if (stepIndex == 3) RefreshStep4PreviewUI();
        }

        // ══════════════════════════════════════════════════════════════
        // ── 3. STEP 1: TYPICAL FLOORS, DUPLEX & MULTI-BUILDINGS ──
        // ══════════════════════════════════════════════════════════════
        private WpfGrid CreateStep1Panel()
        {
            WpfGrid root = new WpfGrid();
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Row 0: Building Selector Bar
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Row 1: Main 2-Column Area

            // ── Row 0: Building Selector Bar ──
            Border bldgBarHost = new Border
            {
                Background = WpfBrushes.White,
                BorderBrush = new SolidColorBrush(COL_BORDER_LIGHT),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12, 8, 12, 8),
                Margin = new Thickness(0, 0, 0, 14)
            };

            WpfGrid bldgBarGrid = new WpfGrid();
            bldgBarGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Label
            bldgBarGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Tabs
            bldgBarGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Actions

            WpfTextBlock lblBldgs = new WpfTextBlock
            {
                Text = "PROJECT BUILDINGS:",
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(COL_TEXT_MUTED),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 12, 0)
            };
            WpfGrid.SetColumn(lblBldgs, 0);
            bldgBarGrid.Children.Add(lblBldgs);

            _buildingTabBar = new StackPanel { Orientation = WpfOrientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            WpfGrid.SetColumn(_buildingTabBar, 1);
            bldgBarGrid.Children.Add(_buildingTabBar);

            // Add Building Button
            WpfButton btnAddBldg = CreateNeutralButton("＋ Add Building");
            btnAddBldg.Height = 28;
            btnAddBldg.Padding = new Thickness(12, 0, 12, 0);
            btnAddBldg.Click += (s, e) => ShowAddBuildingDialog();
            WpfGrid.SetColumn(btnAddBldg, 2);
            bldgBarGrid.Children.Add(btnAddBldg);

            bldgBarHost.Child = bldgBarGrid;
            WpfGrid.SetRow(bldgBarHost, 0);
            root.Children.Add(bldgBarHost);

            // ── Row 1: 2-Column Split (Left: Cards, Right: Visual Tower & Settings) ──
            WpfGrid cols = new WpfGrid();
            cols.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.35, GridUnitType.Star) }); // Left: Groups list
            cols.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(20) });                    // Gap
            cols.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });   // Right: Tower Strip & Settings

            // ── Left Card: Typical Floor Groups ──
            Border leftCard = CreateCard();
            WpfGrid leftLayout = new WpfGrid();
            leftLayout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Header
            leftLayout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Scrollable Groups
            leftLayout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Actions

            // Group List Header
            WpfGrid headerRow = new WpfGrid { Margin = new Thickness(0, 0, 0, 14) };
            headerRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            StackPanel titleStack = new StackPanel();
            titleStack.Children.Add(new WpfTextBlock
            {
                Text = "Typical Floor Definitions",
                FontSize = 16,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(COL_TEXT_MAIN)
            });
            titleStack.Children.Add(new WpfTextBlock
            {
                Text = "Configure source floors (Single, Typical, or Duplex 2-Story modules). Overlaps are strictly prevented.",
                FontSize = 11.5,
                Foreground = new SolidColorBrush(COL_TEXT_MUTED),
                Margin = new Thickness(0, 2, 0, 0)
            });
            WpfGrid.SetColumn(titleStack, 0);
            headerRow.Children.Add(titleStack);

            StackPanel hdrBtnStack = new StackPanel { Orientation = WpfOrientation.Horizontal };

            WpfButton btnCopySetup = CreateNeutralButton("📑 Copy from...");
            btnCopySetup.Height = 32;
            btnCopySetup.Padding = new Thickness(12, 0, 12, 0);
            btnCopySetup.Margin = new Thickness(0, 0, 8, 0);
            btnCopySetup.ToolTip = "Copy typical floor groups from another building into this building";
            btnCopySetup.Click += (s, e) =>
            {
                if (_vm.Buildings.Count <= 1 || _vm.SelectedBuilding == null)
                {
                    _vm.TriggerToast("Add another building first to copy from.", true);
                    return;
                }
                ContextMenu cm = new ContextMenu();
                foreach (BuildingDefinition other in _vm.Buildings)
                {
                    if (other == _vm.SelectedBuilding) continue;
                    BuildingDefinition src = other;
                    MenuItem mi = new MenuItem { Header = string.Format("Copy from '{0}' ({1} groups)", src.Name, src.TypicalGroups.Count) };
                    mi.Click += (ms, me) =>
                    {
                        MessageBoxResult confirm = MessageBox.Show(
                            string.Format("Replace all typical floor groups in '{0}' with those from '{1}'?", _vm.SelectedBuilding.Name, src.Name),
                            "Confirm Copy Setup",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question);
                        if (confirm == MessageBoxResult.Yes)
                        {
                            _vm.CopyGroupsFromBuilding(_vm.SelectedBuilding, src);
                            RefreshTypicalGroupsUI();
                            RefreshTowerUI();
                        }
                    };
                    cm.Items.Add(mi);
                }
                cm.PlacementTarget = btnCopySetup;
                cm.IsOpen = true;
            };
            hdrBtnStack.Children.Add(btnCopySetup);

            WpfButton btnAdd = CreateNeutralButton("+ Add Typical Floor");
            btnAdd.Height = 32;
            btnAdd.Padding = new Thickness(14, 0, 14, 0);
            btnAdd.Click += (s, e) =>
            {
                _vm.AddTypicalGroup();
                RefreshTypicalGroupsUI();
                RefreshTowerUI();
            };
            hdrBtnStack.Children.Add(btnAdd);

            WpfGrid.SetColumn(hdrBtnStack, 1);
            headerRow.Children.Add(hdrBtnStack);

            WpfGrid.SetRow(headerRow, 0);
            leftLayout.Children.Add(headerRow);

            // Scrollable List of Group Cards
            ScrollViewer scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            _typicalGroupsContainer = new StackPanel();
            scroll.Content = _typicalGroupsContainer;
            WpfGrid.SetRow(scroll, 1);
            leftLayout.Children.Add(scroll);

            // Action Row
            WpfGrid actionRow = new WpfGrid { Margin = new Thickness(0, 14, 0, 0) };
            actionRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            actionRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            WpfButton btnSave = CreatePrimaryButton("Save to Revit Model");
            btnSave.Height = 36;
            btnSave.Padding = new Thickness(20, 0, 20, 0);
            btnSave.Click += (s, e) => _vm.SaveTypicalGroups();
            WpfGrid.SetColumn(btnSave, 1);
            actionRow.Children.Add(btnSave);

            WpfGrid.SetRow(actionRow, 2);
            leftLayout.Children.Add(actionRow);

            leftCard.Child = leftLayout;
            WpfGrid.SetColumn(leftCard, 0);
            cols.Children.Add(leftCard);

            // ── Right Card: Visual Tower Strip & Building Settings ──
            Border rightCard = CreateCard();
            WpfGrid rightLayout = new WpfGrid();
            rightLayout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Tower Header
            rightLayout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Scrollable Tower Strip
            rightLayout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Building Scope Box & Scheme Mapping

            // Tower Header
            StackPanel towerHeader = new StackPanel { Margin = new Thickness(0, 0, 0, 10) };
            towerHeader.Children.Add(new WpfTextBlock
            {
                Text = "Visual Building Tower",
                FontSize = 15,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(COL_TEXT_MAIN)
            });
            towerHeader.Children.Add(new WpfTextBlock
            {
                Text = "Live elevation diagram showing level assignments and duplex cycles top to bottom.",
                FontSize = 11.5,
                Foreground = new SolidColorBrush(COL_TEXT_MUTED)
            });
            WpfGrid.SetRow(towerHeader, 0);
            rightLayout.Children.Add(towerHeader);

            // Scrollable Tower Strip
            ScrollViewer towerScroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Margin = new Thickness(0, 0, 0, 12) };
            _towerContainer = new StackPanel();
            towerScroll.Content = _towerContainer;
            WpfGrid.SetRow(towerScroll, 1);
            rightLayout.Children.Add(towerScroll);

            // Building Scope Box & Scheme Settings Bottom Box
            Border schemeBox = new Border
            {
                Background = new SolidColorBrush(COL_SURFACE),
                BorderBrush = new SolidColorBrush(COL_BORDER_LIGHT),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(12, 10, 12, 10)
            };
            StackPanel schemeStack = new StackPanel();

            // Building Scope Box Row
            WpfGrid bldgScopeRow = new WpfGrid { Margin = new Thickness(0, 0, 0, 8) };
            bldgScopeRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            bldgScopeRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            StackPanel sbStack = new StackPanel { Margin = new Thickness(0, 0, 6, 0) };
            sbStack.Children.Add(new WpfTextBlock { Text = "Building Scope Box (Crop):", FontSize = 10, Foreground = new SolidColorBrush(COL_TEXT_MUTED) });
            WpfComboBox cScope = new WpfComboBox { Height = 26, ItemsSource = _vm.AvailableScopeBoxes };
            if (_vm.SelectedBuilding != null) cScope.SelectedItem = _vm.SelectedBuilding.ScopeBoxName;
            cScope.SelectionChanged += (s, e) =>
            {
                if (cScope.SelectedItem != null && _vm.SelectedBuilding != null)
                {
                    _vm.SelectedBuilding.ScopeBoxName = cScope.SelectedItem.ToString();
                }
            };
            sbStack.Children.Add(cScope);
            WpfGrid.SetColumn(sbStack, 0);
            bldgScopeRow.Children.Add(sbStack);

            // Lot Area
            StackPanel sLot = new StackPanel();
            sLot.Children.Add(new WpfTextBlock { Text = "Lot Area (SF):", FontSize = 10, Foreground = new SolidColorBrush(COL_TEXT_MUTED) });
            WpfTextBox tLot = new WpfTextBox { Height = 26, VerticalContentAlignment = VerticalAlignment.Center };
            tLot.SetBinding(WpfTextBox.TextProperty, new WpfBinding("Config.LotArea") { Source = _vm, Mode = BindingMode.TwoWay });
            sLot.Children.Add(tLot);
            WpfGrid.SetColumn(sLot, 1);
            bldgScopeRow.Children.Add(sLot);

            schemeStack.Children.Add(bldgScopeRow);

            // Schemes Row
            WpfGrid schemeRow = new WpfGrid();
            schemeRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            schemeRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            StackPanel sGross = new StackPanel { Margin = new Thickness(0, 0, 6, 0) };
            sGross.Children.Add(new WpfTextBlock { Text = "Gross Scheme:", FontSize = 10, Foreground = new SolidColorBrush(COL_TEXT_MUTED) });
            WpfComboBox cGross = new WpfComboBox { Height = 26, ItemsSource = _vm.AreaSchemes };
            cGross.SetBinding(WpfComboBox.SelectedItemProperty, new WpfBinding("Config.GrossAreaSchemeName") { Source = _vm, Mode = BindingMode.TwoWay });
            sGross.Children.Add(cGross);
            WpfGrid.SetColumn(sGross, 0);
            schemeRow.Children.Add(sGross);

            StackPanel sDed = new StackPanel();
            sDed.Children.Add(new WpfTextBlock { Text = "Deduction Scheme:", FontSize = 10, Foreground = new SolidColorBrush(COL_TEXT_MUTED) });
            WpfComboBox cDed = new WpfComboBox { Height = 26, ItemsSource = _vm.AreaSchemes };
            cDed.SetBinding(WpfComboBox.SelectedItemProperty, new WpfBinding("Config.DeductionAreaSchemeName") { Source = _vm, Mode = BindingMode.TwoWay });
            sDed.Children.Add(cDed);
            WpfGrid.SetColumn(sDed, 1);
            schemeRow.Children.Add(sDed);

            schemeStack.Children.Add(schemeRow);
            schemeBox.Child = schemeStack;
            WpfGrid.SetRow(schemeBox, 2);
            rightLayout.Children.Add(schemeBox);

            rightCard.Child = rightLayout;
            WpfGrid.SetColumn(rightCard, 2);
            cols.Children.Add(rightCard);

            WpfGrid.SetRow(cols, 1);
            root.Children.Add(cols);

            return root;
        }

        private void RefreshBuildingTabsUI()
        {
            if (_buildingTabBar == null) return;
            _buildingTabBar.Children.Clear();

            foreach (BuildingDefinition bldg in _vm.Buildings)
            {
                BuildingDefinition currentBldg = bldg;
                bool isSelected = (_vm.SelectedBuilding == currentBldg);

                Border tabPill = new Border
                {
                    CornerRadius = new CornerRadius(14),
                    Background = isSelected ? new SolidColorBrush(COL_PRIMARY) : new SolidColorBrush(COL_SURFACE),
                    BorderBrush = isSelected ? new SolidColorBrush(COL_PRIMARY) : new SolidColorBrush(COL_BORDER),
                    BorderThickness = new Thickness(1),
                    Padding = new Thickness(12, 4, 12, 4),
                    Margin = new Thickness(0, 0, 8, 0),
                    Cursor = System.Windows.Input.Cursors.Hand
                };

                StackPanel tabContent = new StackPanel { Orientation = WpfOrientation.Horizontal };

                // Building Name TextBlock (Double click to edit inline)
                WpfTextBlock txtBldgName = new WpfTextBlock
                {
                    Text = "🏢 " + currentBldg.Name,
                    FontWeight = isSelected ? FontWeights.Bold : FontWeights.Medium,
                    FontSize = 12,
                    Foreground = isSelected ? WpfBrushes.White : new SolidColorBrush(COL_TEXT_MAIN),
                    VerticalAlignment = VerticalAlignment.Center,
                    ToolTip = "Double-click to rename building"
                };

                // Inline rename textbox
                WpfTextBox txtEditName = new WpfTextBox
                {
                    Text = currentBldg.Name,
                    FontSize = 11.5,
                    Height = 22,
                    Padding = new Thickness(4, 0, 4, 0),
                    VerticalContentAlignment = VerticalAlignment.Center,
                    Visibility = WpfVisibility.Collapsed
                };

                Action finishRename = () =>
                {
                    if (!string.IsNullOrWhiteSpace(txtEditName.Text))
                    {
                        currentBldg.Name = txtEditName.Text.Trim();
                        txtBldgName.Text = "🏢 " + currentBldg.Name;
                    }
                    txtEditName.Visibility = WpfVisibility.Collapsed;
                    txtBldgName.Visibility = WpfVisibility.Visible;
                };

                txtEditName.LostFocus += (s, e) => finishRename();
                txtEditName.KeyDown += (s, e) =>
                {
                    if (e.Key == System.Windows.Input.Key.Enter) finishRename();
                    else if (e.Key == System.Windows.Input.Key.Escape)
                    {
                        txtEditName.Visibility = WpfVisibility.Collapsed;
                        txtBldgName.Visibility = WpfVisibility.Visible;
                    }
                };

                txtBldgName.MouseDown += (s, e) =>
                {
                    if (e.ClickCount == 2)
                    {
                        txtBldgName.Visibility = WpfVisibility.Collapsed;
                        txtEditName.Visibility = WpfVisibility.Visible;
                        txtEditName.Focus();
                        txtEditName.SelectAll();
                        e.Handled = true;
                    }
                };

                tabContent.Children.Add(txtBldgName);
                tabContent.Children.Add(txtEditName);

                // Small delete button if more than 1 building
                if (_vm.Buildings.Count > 1)
                {
                    WpfButton btnDelBldg = new WpfButton
                    {
                        Content = "✕",
                        Width = 16,
                        Height = 16,
                        Margin = new Thickness(8, 0, 0, 0),
                        FontSize = 9,
                        FontWeight = FontWeights.Bold,
                        Background = WpfBrushes.Transparent,
                        BorderThickness = new Thickness(0),
                        Foreground = isSelected ? WpfBrushes.White : new SolidColorBrush(COL_TEXT_MUTED),
                        ToolTip = "Delete building"
                    };
                    btnDelBldg.Click += (s, e) =>
                    {
                        e.Handled = true;
                        _vm.RemoveBuilding(currentBldg);
                        RefreshBuildingTabsUI();
                        RefreshTypicalGroupsUI();
                        RefreshTowerUI();
                    };
                    tabContent.Children.Add(btnDelBldg);
                }

                tabPill.MouseLeftButtonDown += (s, e) =>
                {
                    if (txtEditName.Visibility == WpfVisibility.Visible) return;
                    _vm.SelectedBuilding = currentBldg;
                    RefreshBuildingTabsUI();
                    RefreshTypicalGroupsUI();
                    RefreshTowerUI();
                };

                tabPill.Child = tabContent;
                _buildingTabBar.Children.Add(tabPill);
            }
        }

        private void RefreshTypicalGroupsUI()
        {
            if (_typicalGroupsContainer == null) return;
            _typicalGroupsContainer.Children.Clear();

            if (_vm.SelectedBuilding == null || _vm.SelectedBuilding.TypicalGroups.Count == 0)
            {
                _typicalGroupsContainer.Children.Add(new WpfTextBlock
                {
                    Text = "No Typical Floor groups defined for this building yet.\nClick '+ Add Typical Floor' above to start.",
                    Foreground = new SolidColorBrush(COL_TEXT_MUTED),
                    Margin = new Thickness(0, 20, 0, 0),
                    FontSize = 12
                });
                return;
            }

            foreach (TypicalFloorGroup group in _vm.SelectedBuilding.TypicalGroups)
            {
                TypicalFloorGroup currentGroup = group;
                Border card = new Border
                {
                    Background = new SolidColorBrush(COL_SURFACE),
                    BorderBrush = new SolidColorBrush(COL_BORDER_LIGHT),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(14, 10, 14, 10),
                    Margin = new Thickness(0, 0, 0, 10)
                };

                StackPanel cardLayout = new StackPanel();

                // ── Row 1: Top Bar (Color Chip + Name + Single / Duplex Toggles + Delete) ──
                WpfGrid topBar = new WpfGrid { Margin = new Thickness(0, 0, 0, 8) };
                topBar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(28) }); // Color Popover Chip
                topBar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.3, GridUnitType.Star) }); // Name
                topBar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Single Toggle
                topBar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Duplex Toggle
                topBar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Badge
                topBar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(28) }); // Delete

                // 1. Color Popover Chip
                Border colorBadge = new Border
                {
                    Width = 18,
                    Height = 18,
                    CornerRadius = new CornerRadius(9),
                    Background = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString(currentGroup.ColorHex ?? "#0071E3")),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Center,
                    Cursor = System.Windows.Input.Cursors.Hand,
                    ToolTip = "Click to pick color"
                };

                // Popover Popup for Color Picker
                Popup colorPopup = new Popup
                {
                    PlacementTarget = colorBadge,
                    Placement = PlacementMode.Bottom,
                    StaysOpen = false,
                    AllowsTransparency = true
                };

                Border popupBorder = new Border
                {
                    Background = WpfBrushes.White,
                    BorderBrush = new SolidColorBrush(COL_BORDER_LIGHT),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(6),
                    Effect = new System.Windows.Media.Effects.DropShadowEffect { BlurRadius = 8, Opacity = 0.15, ShadowDepth = 2 }
                };

                UniformGrid colorGrid = new UniformGrid { Columns = 4, Rows = 2 };
                foreach (string hex in COLOR_PALETTE)
                {
                    string currentHex = hex;
                    Border chip = new Border
                    {
                        Width = 20,
                        Height = 20,
                        CornerRadius = new CornerRadius(10),
                        Background = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString(currentHex)),
                        Margin = new Thickness(3),
                        Cursor = System.Windows.Input.Cursors.Hand
                    };
                    chip.MouseLeftButtonDown += (s, e) =>
                    {
                        currentGroup.ColorHex = currentHex;
                        colorBadge.Background = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString(currentHex));
                        colorPopup.IsOpen = false;
                        RefreshTowerUI();
                    };
                    colorGrid.Children.Add(chip);
                }
                popupBorder.Child = colorGrid;
                colorPopup.Child = popupBorder;

                colorBadge.MouseLeftButtonDown += (s, e) => colorPopup.IsOpen = true;

                WpfGrid.SetColumn(colorBadge, 0);
                topBar.Children.Add(colorBadge);

                // 2. Group Name
                WpfTextBox txtName = new WpfTextBox
                {
                    Text = currentGroup.Name,
                    Height = 26,
                    BorderBrush = new SolidColorBrush(COL_BORDER),
                    Padding = new Thickness(6, 2, 6, 2),
                    VerticalContentAlignment = VerticalAlignment.Center,
                    FontWeight = FontWeights.SemiBold,
                    FontSize = 12,
                    Margin = new Thickness(0, 0, 8, 0)
                };
                txtName.TextChanged += (s, e) =>
                {
                    currentGroup.Name = txtName.Text;
                    RefreshTowerUI();
                };
                WpfGrid.SetColumn(txtName, 1);
                topBar.Children.Add(txtName);

                // 3. Single Floor CheckBox
                WpfCheckBox chkSingle = new WpfCheckBox
                {
                    Content = "Single",
                    FontSize = 10.5,
                    FontWeight = FontWeights.Medium,
                    Foreground = new SolidColorBrush(COL_TEXT_MAIN),
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 6, 0),
                    IsChecked = currentGroup.IsSingleFloorOnly
                };

                // 4. Duplex Module CheckBox
                WpfCheckBox chkDuplex = new WpfCheckBox
                {
                    Content = "Duplex (2-Story)",
                    FontSize = 10.5,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#4F46E5")),
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 6, 0),
                    IsChecked = currentGroup.IsDuplexModule
                };

                // 5. Status Badge Pill
                Border badgePill = new Border
                {
                    CornerRadius = new CornerRadius(10),
                    Padding = new Thickness(7, 2, 7, 2),
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 6, 0)
                };
                WpfTextBlock badgeText = new WpfTextBlock { FontSize = 9.5, FontWeight = FontWeights.Bold };
                badgePill.Child = badgeText;

                Action updateBadge = () =>
                {
                    if (currentGroup.IsSingleLevel)
                    {
                        badgePill.Background = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#E6F4EA"));
                        badgeText.Foreground = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#137333"));
                        badgeText.Text = "⭐ SINGLE";
                    }
                    else if (currentGroup.IsDuplexModule)
                    {
                        badgePill.Background = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#EEF2FF"));
                        badgeText.Foreground = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#4F46E5"));
                        badgeText.Text = "🏢 DUPLEX (2-STORY)";
                    }
                    else
                    {
                        badgePill.Background = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#E8F0FE"));
                        badgeText.Foreground = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#1A73E8"));
                        badgeText.Text = "🔄 TYPICAL";
                    }
                    RefreshTowerUI();
                };
                updateBadge();

                WpfGrid.SetColumn(chkSingle, 2);
                topBar.Children.Add(chkSingle);

                WpfGrid.SetColumn(chkDuplex, 3);
                topBar.Children.Add(chkDuplex);

                WpfGrid.SetColumn(badgePill, 4);
                topBar.Children.Add(badgePill);

                // 6. Delete Button
                WpfButton btnDel = CreateDangerButton("✕");
                btnDel.Width = 22;
                btnDel.Height = 22;
                btnDel.Padding = new Thickness(0);
                btnDel.VerticalAlignment = VerticalAlignment.Center;
                btnDel.ToolTip = "Delete group";
                btnDel.Click += (s, e) =>
                {
                    _vm.RemoveTypicalGroup(currentGroup);
                    RefreshTypicalGroupsUI();
                    RefreshTowerUI();
                };
                WpfGrid.SetColumn(btnDel, 5);
                topBar.Children.Add(btnDel);

                cardLayout.Children.Add(topBar);

                // ── Row 2: Levels Selectors (Standard vs Duplex) ──
                WpfGrid levelsRow = new WpfGrid();
                levelsRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.2, GridUnitType.Star) }); // Source(s)
                levelsRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // From
                levelsRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // To

                // Source Column (Standard single source OR Duplex Lower & Upper sources)
                StackPanel srcStack = new StackPanel { Margin = new Thickness(0, 0, 6, 0) };

                // Standard Source Box
                StackPanel stdSrcBox = new StackPanel { Visibility = currentGroup.IsDuplexModule ? WpfVisibility.Collapsed : WpfVisibility.Visible };
                stdSrcBox.Children.Add(new WpfTextBlock { Text = "Source Level (Modeled):", FontSize = 9.5, Foreground = new SolidColorBrush(COL_TEXT_MUTED), Margin = new Thickness(0, 0, 0, 2) });
                WpfComboBox comboSrc = new WpfComboBox { Height = 26 };
                WpfTextBlock srcStatus = new WpfTextBlock { FontSize = 9, Foreground = new SolidColorBrush(COL_TEXT_MUTED), Margin = new Thickness(2, 2, 0, 0), Text = _vm.GetSourceLevelSummary(currentGroup.SourceLevelName) };
                ConfigureLevelComboBox(comboSrc, currentGroup, currentGroup.SourceLevelName, (lvlName) =>
                {
                    currentGroup.SourceLevelName = lvlName;
                    srcStatus.Text = _vm.GetSourceLevelSummary(currentGroup.SourceLevelName);
                    updateBadge();
                    RefreshTypicalGroupsUI();
                });
                stdSrcBox.Children.Add(comboSrc);
                stdSrcBox.Children.Add(srcStatus);
                srcStack.Children.Add(stdSrcBox);

                // Duplex Lower & Upper Sources Box
                StackPanel duplexSrcBox = new StackPanel { Visibility = currentGroup.IsDuplexModule ? WpfVisibility.Visible : WpfVisibility.Collapsed };
                
                // Lower Level
                duplexSrcBox.Children.Add(new WpfTextBlock { Text = "Duplex Lower (Social/Access):", FontSize = 9.5, Foreground = new SolidColorBrush(COL_TEXT_MUTED), Margin = new Thickness(0, 0, 0, 2) });
                WpfComboBox comboLower = new WpfComboBox { Height = 26 };
                WpfTextBlock lowerStatus = new WpfTextBlock { FontSize = 9, Foreground = new SolidColorBrush(COL_TEXT_MUTED), Margin = new Thickness(2, 1, 0, 4), Text = _vm.GetSourceLevelSummary(currentGroup.SourceLevelNameLower) };
                WpfTextBlock upperStatus = new WpfTextBlock { FontSize = 9, Foreground = new SolidColorBrush(COL_TEXT_MUTED), Margin = new Thickness(2, 1, 0, 0), Text = _vm.GetSourceLevelSummary(currentGroup.SourceLevelNameUpper) };

                ConfigureLevelComboBox(comboLower, currentGroup, currentGroup.SourceLevelNameLower, (lvlName) =>
                {
                    currentGroup.SourceLevelNameLower = lvlName;
                    string autoUpper = _vm.GetNextLevelAbove(lvlName);
                    if (!string.IsNullOrEmpty(autoUpper))
                    {
                        currentGroup.SourceLevelNameUpper = autoUpper;
                    }
                    lowerStatus.Text = _vm.GetSourceLevelSummary(currentGroup.SourceLevelNameLower);
                    upperStatus.Text = _vm.GetSourceLevelSummary(currentGroup.SourceLevelNameUpper);
                    updateBadge();
                    RefreshTypicalGroupsUI();
                });
                duplexSrcBox.Children.Add(comboLower);
                duplexSrcBox.Children.Add(lowerStatus);

                // Upper Level (Auto-paired)
                duplexSrcBox.Children.Add(new WpfTextBlock { Text = "Duplex Upper (Bedrooms/Void — Auto Paired):", FontSize = 9.5, Foreground = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#4F46E5")), Margin = new Thickness(0, 0, 0, 2) });
                WpfComboBox comboUpper = new WpfComboBox { Height = 26 };
                ConfigureLevelComboBox(comboUpper, currentGroup, currentGroup.SourceLevelNameUpper, (lvlName) =>
                {
                    currentGroup.SourceLevelNameUpper = lvlName;
                    upperStatus.Text = _vm.GetSourceLevelSummary(currentGroup.SourceLevelNameUpper);
                    updateBadge();
                    RefreshTypicalGroupsUI();
                });
                duplexSrcBox.Children.Add(comboUpper);
                duplexSrcBox.Children.Add(upperStatus);

                srcStack.Children.Add(duplexSrcBox);

                WpfGrid.SetColumn(srcStack, 0);
                levelsRow.Children.Add(srcStack);

                // From & To Dropdowns
                WpfComboBox comboFrom = new WpfComboBox { Height = 26, IsEnabled = !currentGroup.IsSingleFloorOnly };
                WpfComboBox comboTo = new WpfComboBox { Height = 26, IsEnabled = !currentGroup.IsSingleFloorOnly };

                // From Level Stack
                StackPanel fromStack = new StackPanel { Margin = new Thickness(0, 0, 6, 0) };
                fromStack.Children.Add(new WpfTextBlock { Text = "Range: From", FontSize = 9.5, Foreground = new SolidColorBrush(COL_TEXT_MUTED), Margin = new Thickness(0, 0, 0, 2) });
                ConfigureLevelComboBox(comboFrom, currentGroup, currentGroup.FromLevelName, (fromVal) =>
                {
                    LevelPickerItem toItem = comboTo.SelectedItem as LevelPickerItem;
                    string toVal = toItem != null ? toItem.LevelName : currentGroup.ToLevelName;
                    bool valid = _vm.ValidateAndApplyRange(currentGroup, fromVal, toVal);
                    if (valid)
                    {
                        updateBadge();
                        RefreshTypicalGroupsUI();
                    }
                });
                fromStack.Children.Add(comboFrom);
                WpfGrid.SetColumn(fromStack, 1);
                levelsRow.Children.Add(fromStack);

                // To Level Stack
                StackPanel toStack = new StackPanel();
                toStack.Children.Add(new WpfTextBlock { Text = "Range: To", FontSize = 9.5, Foreground = new SolidColorBrush(COL_TEXT_MUTED), Margin = new Thickness(0, 0, 0, 2) });
                ConfigureLevelComboBox(comboTo, currentGroup, currentGroup.ToLevelName, (toVal) =>
                {
                    LevelPickerItem fromItem = comboFrom.SelectedItem as LevelPickerItem;
                    string fromVal = fromItem != null ? fromItem.LevelName : currentGroup.FromLevelName;
                    bool valid = _vm.ValidateAndApplyRange(currentGroup, fromVal, toVal);
                    if (valid)
                    {
                        updateBadge();
                        RefreshTypicalGroupsUI();
                    }
                });
                toStack.Children.Add(comboTo);
                WpfGrid.SetColumn(toStack, 2);
                levelsRow.Children.Add(toStack);

                chkSingle.Checked += (s, e) =>
                {
                    currentGroup.IsSingleFloorOnly = true;
                    chkDuplex.IsChecked = false;
                    currentGroup.FromLevelName = currentGroup.SourceLevelName;
                    currentGroup.ToLevelName = currentGroup.SourceLevelName;
                    comboFrom.IsEnabled = false;
                    comboTo.IsEnabled = false;
                    updateBadge();
                    RefreshTypicalGroupsUI();
                };
                chkSingle.Unchecked += (s, e) =>
                {
                    currentGroup.IsSingleFloorOnly = false;
                    comboFrom.IsEnabled = true;
                    comboTo.IsEnabled = true;
                    updateBadge();
                    RefreshTypicalGroupsUI();
                };

                chkDuplex.Checked += (s, e) =>
                {
                    currentGroup.IsDuplexModule = true;
                    chkSingle.IsChecked = false;
                    string autoUpper = _vm.GetNextLevelAbove(currentGroup.SourceLevelNameLower);
                    if (!string.IsNullOrEmpty(autoUpper))
                    {
                        currentGroup.SourceLevelNameUpper = autoUpper;
                    }
                    stdSrcBox.Visibility = WpfVisibility.Collapsed;
                    duplexSrcBox.Visibility = WpfVisibility.Visible;
                    updateBadge();
                    RefreshTypicalGroupsUI();
                };
                chkDuplex.Unchecked += (s, e) =>
                {
                    currentGroup.IsDuplexModule = false;
                    stdSrcBox.Visibility = WpfVisibility.Visible;
                    duplexSrcBox.Visibility = WpfVisibility.Collapsed;
                    updateBadge();
                    RefreshTypicalGroupsUI();
                };

                cardLayout.Children.Add(levelsRow);

                // ── Row 3: Stacking Quick Action Toolbar (Shift Up/Down & Expand/Contract) ──
                if (!currentGroup.IsSingleFloorOnly)
                {
                    Border quickBar = new Border
                    {
                        Background = new SolidColorBrush(COL_BG),
                        CornerRadius = new CornerRadius(5),
                        Padding = new Thickness(8, 4, 8, 4),
                        Margin = new Thickness(0, 8, 0, 0)
                    };

                    WpfGrid qGrid = new WpfGrid();
                    qGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Label
                    qGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Spacer
                    qGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Shift buttons
                    qGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Expand buttons

                    WpfTextBlock lblStack = new WpfTextBlock
                    {
                        Text = "⚡ STACKING CONTROLS:",
                        FontSize = 9.5,
                        FontWeight = FontWeights.Bold,
                        Foreground = new SolidColorBrush(COL_TEXT_MUTED),
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    WpfGrid.SetColumn(lblStack, 0);
                    qGrid.Children.Add(lblStack);

                    // Shift Group Up/Down
                    StackPanel shiftStack = new StackPanel { Orientation = WpfOrientation.Horizontal, Margin = new Thickness(0, 0, 10, 0) };
                    
                    WpfButton btnShiftDown = CreateMicroButton("▼ Shift Down");
                    btnShiftDown.Height = 22;
                    btnShiftDown.FontSize = 10;
                    btnShiftDown.Margin = new Thickness(0, 0, 4, 0);
                    btnShiftDown.ToolTip = "Shift entire group down by 1 level (maintains floor count and height)";
                    btnShiftDown.Click += (s, e) =>
                    {
                        bool ok = _vm.ShiftGroupRange(currentGroup, -1);
                        if (ok) RefreshTypicalGroupsUI();
                    };
                    shiftStack.Children.Add(btnShiftDown);

                    WpfButton btnShiftUp = CreateMicroButton("▲ Shift Up");
                    btnShiftUp.Height = 22;
                    btnShiftUp.FontSize = 10;
                    btnShiftUp.ToolTip = "Shift entire group up by 1 level (maintains floor count and height)";
                    btnShiftUp.Click += (s, e) =>
                    {
                        bool ok = _vm.ShiftGroupRange(currentGroup, +1);
                        if (ok) RefreshTypicalGroupsUI();
                    };
                    shiftStack.Children.Add(btnShiftUp);

                    WpfGrid.SetColumn(shiftStack, 2);
                    qGrid.Children.Add(shiftStack);

                    // Expand / Contract Top Level
                    StackPanel expStack = new StackPanel { Orientation = WpfOrientation.Horizontal };

                    WpfButton btnContract = CreateMicroButton("− 1 Floor");
                    btnContract.Height = 22;
                    btnContract.FontSize = 10;
                    btnContract.Margin = new Thickness(0, 0, 4, 0);
                    btnContract.ToolTip = "Contract group by removing the top floor (frees the level above)";
                    btnContract.Click += (s, e) =>
                    {
                        bool ok = _vm.ExpandOrContractGroup(currentGroup, -1);
                        if (ok) RefreshTypicalGroupsUI();
                    };
                    expStack.Children.Add(btnContract);

                    WpfButton btnExpand = CreateMicroButton("+ 1 Floor");
                    btnExpand.Height = 22;
                    btnExpand.FontSize = 10;
                    btnExpand.ToolTip = "Expand group by adding the next free floor at the top";
                    btnExpand.Click += (s, e) =>
                    {
                        bool ok = _vm.ExpandOrContractGroup(currentGroup, +1);
                        if (ok) RefreshTypicalGroupsUI();
                    };
                    expStack.Children.Add(btnExpand);

                    WpfGrid.SetColumn(expStack, 3);
                    qGrid.Children.Add(expStack);

                    quickBar.Child = qGrid;
                    cardLayout.Children.Add(quickBar);
                }

                card.Child = cardLayout;
                _typicalGroupsContainer.Children.Add(card);
            }
        }

        private void RefreshTowerUI()
        {
            if (_towerContainer == null) return;
            _towerContainer.Children.Clear();

            _vm.RefreshTowerLevels();

            // Live Gaps / Allocation Indicator
            List<string> gaps = _vm.GetUnassignedGaps();
            Border gapBanner = new Border
            {
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(10, 6, 10, 6),
                Margin = new Thickness(0, 0, 0, 8)
            };
            if (gaps.Count == 0)
            {
                gapBanner.Background = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#ECFDF5"));
                gapBanner.BorderBrush = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#A7F3D0"));
                gapBanner.BorderThickness = new Thickness(1);
                gapBanner.Child = new WpfTextBlock
                {
                    Text = "🟢 100% Floor Area Allocated (No Gaps)",
                    FontSize = 10.5,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#065F46"))
                };
            }
            else
            {
                gapBanner.Background = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#FFFBEB"));
                gapBanner.BorderBrush = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#FDE68A"));
                gapBanner.BorderThickness = new Thickness(1);
                gapBanner.Child = new WpfTextBlock
                {
                    Text = string.Format("⚠️ {0} Unassigned Level(s): {1}", gaps.Count, string.Join(", ", gaps.Take(3).ToArray()) + (gaps.Count > 3 ? "..." : "")),
                    FontSize = 10.5,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#92400E")),
                    TextTrimming = TextTrimming.CharacterEllipsis
                };
            }
            _towerContainer.Children.Add(gapBanner);

            foreach (LevelTowerItem lvl in _vm.TowerLevels)
            {
                Border levelRow = new Border
                {
                    Background = WpfBrushes.White,
                    BorderBrush = new SolidColorBrush(COL_BORDER_LIGHT),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(5),
                    Padding = new Thickness(10, 6, 10, 6),
                    Margin = new Thickness(0, 0, 0, 4)
                };

                WpfGrid rowGrid = new WpfGrid();
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(65) }); // Elevation
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Level Name
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.4, GridUnitType.Star) }); // Assignment Badge

                // Elevation
                WpfTextBlock txtElev = new WpfTextBlock
                {
                    Text = lvl.ElevationDisplay,
                    FontSize = 10,
                    Foreground = new SolidColorBrush(COL_TEXT_MUTED),
                    VerticalAlignment = VerticalAlignment.Center
                };
                WpfGrid.SetColumn(txtElev, 0);
                rowGrid.Children.Add(txtElev);

                // Level Name
                WpfTextBlock txtName = new WpfTextBlock
                {
                    Text = lvl.LevelName,
                    FontSize = 11.5,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(COL_TEXT_MAIN),
                    VerticalAlignment = VerticalAlignment.Center
                };
                WpfGrid.SetColumn(txtName, 1);
                rowGrid.Children.Add(txtName);

                // Assignment Pill
                Border assignPill = new Border
                {
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(8, 2, 8, 2),
                    VerticalAlignment = VerticalAlignment.Center
                };

                if (lvl.IsAssigned)
                {
                    assignPill.Background = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString(lvl.ColorHex));
                    string badgeLabel = lvl.IsSingleFloor ? string.Format("⭐ {0}", lvl.AssignedGroupName) : string.Format("🔄 {0}", lvl.AssignedGroupName);
                    assignPill.Child = new WpfTextBlock
                    {
                        Text = badgeLabel,
                        FontSize = 10,
                        FontWeight = FontWeights.Bold,
                        Foreground = WpfBrushes.White,
                        TextTrimming = TextTrimming.CharacterEllipsis
                    };
                }
                else
                {
                    assignPill.Background = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#F1F5F9"));
                    assignPill.Child = new WpfTextBlock
                    {
                        Text = "⚪ Unassigned",
                        FontSize = 10,
                        Foreground = new SolidColorBrush(COL_TEXT_MUTED)
                    };
                }

                WpfGrid.SetColumn(assignPill, 2);
                rowGrid.Children.Add(assignPill);

                levelRow.Child = rowGrid;
                _towerContainer.Children.Add(levelRow);
            }
        }

        // ══════════════════════════════════════════════════════════════
        // ── 4. STEP 2: PROPAGATE (Review & Execute) ──
        // ══════════════════════════════════════════════════════════════
        private WpfGrid CreateStep2Panel()
        {
            WpfGrid grid = new WpfGrid();
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            Border card = CreateCard();
            WpfGrid cardLayout = new WpfGrid();
            cardLayout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Title
            cardLayout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // View Preservation Alert
            cardLayout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Options
            cardLayout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Summary List

            // Title
            StackPanel titleStack = new StackPanel { Margin = new Thickness(0, 0, 0, 12) };
            titleStack.Children.Add(new WpfTextBlock
            {
                Text = "Area Propagation Preview (All Buildings)",
                FontSize = 16,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(COL_TEXT_MAIN)
            });
            titleStack.Children.Add(new WpfTextBlock
            {
                Text = "Duplicates Area Boundary Lines and Area calculation elements from source floors to target levels without altering view setups.",
                FontSize = 12,
                Foreground = new SolidColorBrush(COL_TEXT_MUTED),
                Margin = new Thickness(0, 2, 0, 0)
            });
            WpfGrid.SetRow(titleStack, 0);
            cardLayout.Children.Add(titleStack);

            // View Preservation Alert
            Border alertBox = new Border
            {
                Background = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#EFF6FF")),
                BorderBrush = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#BFDBFE")),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(12, 8, 12, 8),
                Margin = new Thickness(0, 0, 0, 14)
            };
            WpfTextBlock alertText = new WpfTextBlock
            {
                Text = "🛡️ Non-Destructive Propagation: BauTools only copies Area Boundary Lines and Area calculations into existing Revit floor views. It never deletes or duplicates ViewPlans.",
                FontSize = 11.5,
                Foreground = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#1E40AF")),
                TextWrapping = TextWrapping.Wrap
            };
            alertBox.Child = alertText;
            WpfGrid.SetRow(alertBox, 1);
            cardLayout.Children.Add(alertBox);

            // Scheme Checkboxes
            StackPanel optionsPanel = new StackPanel { Orientation = WpfOrientation.Horizontal, Margin = new Thickness(0, 0, 0, 16) };
            
            WpfCheckBox chkGross = new WpfCheckBox
            {
                Content = "Propagate Gross Building Areas",
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 24, 0),
                IsChecked = _vm.PropagateGrossArea
            };
            chkGross.Checked += (s, e) => _vm.PropagateGrossArea = true;
            chkGross.Unchecked += (s, e) => _vm.PropagateGrossArea = false;
            optionsPanel.Children.Add(chkGross);

            WpfCheckBox chkDed = new WpfCheckBox
            {
                Content = "Propagate Rentable Deductions Areas",
                FontWeight = FontWeights.SemiBold,
                IsChecked = _vm.PropagateDeductionsArea
            };
            chkDed.Checked += (s, e) => _vm.PropagateDeductionsArea = true;
            chkDed.Unchecked += (s, e) => _vm.PropagateDeductionsArea = false;
            optionsPanel.Children.Add(chkDed);

            WpfGrid.SetRow(optionsPanel, 2);
            cardLayout.Children.Add(optionsPanel);

            // Scrollable Propagation Summary
            ScrollViewer scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            _propagateSummaryContainer = new StackPanel();
            scroll.Content = _propagateSummaryContainer;
            WpfGrid.SetRow(scroll, 3);
            cardLayout.Children.Add(scroll);

            card.Child = cardLayout;
            WpfGrid.SetRow(card, 0);
            grid.Children.Add(card);

            // Action Bar with Revert + Propagate
            Border actionBar = new Border { Margin = new Thickness(0, 16, 0, 0) };
            WpfGrid actGrid = new WpfGrid();
            actGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Revert
            actGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Spacer
            actGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Propagate

            WpfButton btnRevert = CreateDangerButton("↺ Revert / Clear Propagated Areas");
            btnRevert.Height = 38;
            btnRevert.Padding = new Thickness(16, 0, 16, 0);
            btnRevert.ToolTip = "Safely removes copied areas from target levels without modifying source floors or deleting views.";
            btnRevert.Click += (s, e) =>
            {
                _vm.RevertPropagatedAreas();
                RefreshPropagateReviewUI();
            };
            WpfGrid.SetColumn(btnRevert, 0);
            actGrid.Children.Add(btnRevert);

            WpfButton btnPropagate = CreatePrimaryButton("⚡ Propagate Areas in Revit Model");
            btnPropagate.Height = 38;
            btnPropagate.Padding = new Thickness(24, 0, 24, 0);
            btnPropagate.Click += (s, e) =>
            {
                _vm.PropagateAreasFromTypicalGroups();
                SwitchToStep(2); // Advance to Calculate step
            };
            WpfGrid.SetColumn(btnPropagate, 2);
            actGrid.Children.Add(btnPropagate);

            actionBar.Child = actGrid;
            WpfGrid.SetRow(actionBar, 1);
            grid.Children.Add(actionBar);

            return grid;
        }

        private void RefreshPropagateReviewUI()
        {
            if (_propagateSummaryContainer == null) return;
            _propagateSummaryContainer.Children.Clear();

            int totalGroupsCount = 0;
            foreach (BuildingDefinition bldg in _vm.Buildings)
            {
                totalGroupsCount += bldg.TypicalGroups.Count;
            }

            if (totalGroupsCount == 0)
            {
                _propagateSummaryContainer.Children.Add(new WpfTextBlock
                {
                    Text = "No Typical Floor groups defined yet. Please go to Step 1 to add typical floor groups.",
                    Foreground = new SolidColorBrush(COL_TEXT_MUTED),
                    Margin = new Thickness(0, 20, 0, 0)
                });
                return;
            }

            foreach (BuildingDefinition bldg in _vm.Buildings)
            {
                if (bldg.TypicalGroups.Count == 0) continue;

                WpfTextBlock bldgHeader = new WpfTextBlock
                {
                    Text = "🏢 " + bldg.Name.ToUpperInvariant(),
                    FontSize = 13,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(COL_TEXT_MAIN),
                    Margin = new Thickness(0, 8, 0, 6)
                };
                _propagateSummaryContainer.Children.Add(bldgHeader);

                foreach (TypicalFloorGroup g in bldg.TypicalGroups)
                {
                    Border b = new Border
                    {
                        Background = new SolidColorBrush(COL_SURFACE),
                        BorderBrush = new SolidColorBrush(COL_BORDER_LIGHT),
                        BorderThickness = new Thickness(1),
                        CornerRadius = new CornerRadius(6),
                        Padding = new Thickness(14, 10, 14, 10),
                        Margin = new Thickness(0, 0, 0, 8)
                    };

                    WpfGrid gGrid = new WpfGrid();
                    gGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(28) });
                    gGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.4, GridUnitType.Star) });
                    gGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    gGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.3, GridUnitType.Star) });
                    gGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.3, GridUnitType.Star) });

                    // Color
                    Border dot = new Border
                    {
                        Width = 14,
                        Height = 14,
                        CornerRadius = new CornerRadius(7),
                        Background = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString(g.ColorHex ?? "#0071E3")),
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    WpfGrid.SetColumn(dot, 0);
                    gGrid.Children.Add(dot);

                    // Group Name
                    WpfTextBlock txtName = new WpfTextBlock { Text = g.Name, FontWeight = FontWeights.SemiBold, VerticalAlignment = VerticalAlignment.Center };
                    WpfGrid.SetColumn(txtName, 1);
                    gGrid.Children.Add(txtName);

                    // Source
                    string srcLabel = g.IsDuplexModule ? string.Format("Lower: {0} | Upper: {1}", g.SourceLevelNameLower, g.SourceLevelNameUpper) : "Source: " + g.SourceLevelName;
                    WpfTextBlock txtSrc = new WpfTextBlock { Text = srcLabel, Foreground = new SolidColorBrush(COL_TEXT_MUTED), VerticalAlignment = VerticalAlignment.Center };
                    WpfGrid.SetColumn(txtSrc, 2);
                    gGrid.Children.Add(txtSrc);

                    // Range
                    string rangeStr = g.IsSingleLevel ? "Single Floor (" + g.SourceLevelName + ")" : "Range: " + g.FromLevelName + " → " + g.ToLevelName;
                    WpfTextBlock txtRange = new WpfTextBlock { Text = rangeStr, Foreground = new SolidColorBrush(COL_TEXT_MUTED), VerticalAlignment = VerticalAlignment.Center };
                    WpfGrid.SetColumn(txtRange, 3);
                    gGrid.Children.Add(txtRange);

                    // Status Badge
                    Border statusPill = new Border
                    {
                        CornerRadius = new CornerRadius(10),
                        Padding = new Thickness(8, 3, 8, 3),
                        HorizontalAlignment = HorizontalAlignment.Left,
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    WpfTextBlock statusText = new WpfTextBlock { FontSize = 10.5, FontWeight = FontWeights.Bold };
                    statusPill.Child = statusText;

                    if (g.IsSingleLevel)
                    {
                        statusPill.Background = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#E6F4EA"));
                        statusText.Foreground = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#137333"));
                        statusText.Text = "⭐ Single Floor (Excluded)";
                    }
                    else if (g.IsDuplexModule)
                    {
                        statusPill.Background = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#EEF2FF"));
                        statusText.Foreground = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#4F46E5"));
                        statusText.Text = "🏢 Alternating Duplex Cycles";
                    }
                    else
                    {
                        statusPill.Background = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#E8F0FE"));
                        statusText.Foreground = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#1A73E8"));
                        statusText.Text = "🔄 Will Propagate Areas";
                    }

                    WpfGrid.SetColumn(statusPill, 4);
                    gGrid.Children.Add(statusPill);

                    b.Child = gGrid;
                    _propagateSummaryContainer.Children.Add(b);
                }
            }
        }

        // ══════════════════════════════════════════════════════════════
        // ══════════════════════════════════════════════════════════════
        // ── 5. STEP 3: CALCULATE ZFA & ZONING COMPLIANCE HUD ──
        // ══════════════════════════════════════════════════════════════
        private WpfGrid CreateStep3Panel()
        {
            WpfGrid grid = new WpfGrid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Row 0: Zoning Compliance HUD Banner
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Row 1: Building Selector Pills + Recalculate
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Row 2: DataGrid Matrix

            // ── Row 0: Zoning Envelope & Compliance HUD Banner ──
            _complianceBanner = new Border
            {
                Background = WpfBrushes.White,
                BorderBrush = new SolidColorBrush(COL_BORDER_LIGHT),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(16, 12, 16, 12),
                Margin = new Thickness(0, 0, 0, 12)
            };

            _complianceHudContainer = new StackPanel();
            _complianceBanner.Child = _complianceHudContainer;
            WpfGrid.SetRow(_complianceBanner, 0);
            grid.Children.Add(_complianceBanner);

            // ── Row 1: Building Selection & Recalculate ──
            WpfGrid topRow = new WpfGrid { Margin = new Thickness(0, 0, 0, 10) };
            topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Building Checkbox Pills
            ItemsControl bldgItems = new ItemsControl
            {
                ItemsSource = _vm.BuildingItems,
                ItemsPanel = new ItemsPanelTemplate(new FrameworkElementFactory(typeof(WrapPanel)))
            };

            FrameworkElementFactory factory = new FrameworkElementFactory(typeof(WpfCheckBox));
            factory.SetBinding(WpfCheckBox.ContentProperty, new WpfBinding("Name"));
            factory.SetBinding(WpfCheckBox.IsCheckedProperty, new WpfBinding("IsSelected") { Mode = BindingMode.TwoWay });
            factory.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 0, 12, 0));
            factory.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            bldgItems.ItemTemplate = new DataTemplate { VisualTree = factory };

            WpfGrid.SetColumn(bldgItems, 0);
            topRow.Children.Add(bldgItems);

            WpfButton btnRecalc = CreateNeutralButton("↻ Recalculate ZFA");
            btnRecalc.Height = 30;
            btnRecalc.Padding = new Thickness(14, 0, 14, 0);
            btnRecalc.Click += (s, e) =>
            {
                _vm.CalculateTable();
                RefreshCalculateUI();
            };
            WpfGrid.SetColumn(btnRecalc, 1);
            topRow.Children.Add(btnRecalc);

            WpfGrid.SetRow(topRow, 1);
            grid.Children.Add(topRow);

            // ── Row 2: TabControl for Buildings ──
            _tabControlBuildings = new TabControl
            {
                Background = WpfBrushes.White,
                BorderBrush = new SolidColorBrush(COL_BORDER_LIGHT),
                BorderThickness = new Thickness(1)
            };

            WpfGrid.SetRow(_tabControlBuildings, 2);
            grid.Children.Add(_tabControlBuildings);

            return grid;
        }

        private Border _complianceBanner;
        private StackPanel _complianceHudContainer;

        private void RefreshCalculateUI()
        {
            RefreshComplianceHudUI();

            if (_tabControlBuildings == null) return;
            _tabControlBuildings.Items.Clear();

            foreach (ZoningTableResult tbl in _vm.DisplayedTables)
            {
                TabItem tab = new TabItem
                {
                    Header = tbl.BuildingName,
                    FontSize = 13,
                    FontWeight = FontWeights.SemiBold
                };

                tab.Content = CreateBuildingDataGrid(tbl);
                _tabControlBuildings.Items.Add(tab);
            }
        }

        private void RefreshComplianceHudUI()
        {
            if (_complianceHudContainer == null) return;
            _complianceHudContainer.Children.Clear();

            _vm.EvaluateCompliance();
            ZoningLotData lot = _vm.LotData;
            ZoningComplianceReport rep = _vm.ComplianceReport;

            WpfGrid hudGrid = new WpfGrid();
            hudGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.15, GridUnitType.Star) }); // Left: Lot inputs & Excel buttons
            hudGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(16) });                    // Gap
            hudGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.35, GridUnitType.Star) }); // Right: Compliance HUD & Gauge

            // ── Left Card: Lot & FAR Allowances ──
            StackPanel leftStack = new StackPanel();

            WpfGrid lHdrGrid = new WpfGrid { Margin = new Thickness(0, 0, 0, 6) };
            lHdrGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            lHdrGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            WpfTextBlock lTitle = new WpfTextBlock
            {
                Text = "ZONING ENVELOPE & LOT PARAMETERS:",
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(COL_TEXT_MUTED),
                VerticalAlignment = VerticalAlignment.Center
            };
            WpfGrid.SetColumn(lTitle, 0);
            lHdrGrid.Children.Add(lTitle);

            // Action Buttons: Import & Template
            StackPanel btnStack = new StackPanel { Orientation = WpfOrientation.Horizontal };

            WpfButton btnImp = CreateNeutralButton("📥 Import Excel");
            btnImp.Height = 24;
            btnImp.FontSize = 10.5;
            btnImp.Padding = new Thickness(8, 0, 8, 0);
            btnImp.Margin = new Thickness(0, 0, 6, 0);
            btnImp.ToolTip = "Import Lot Area, Zoning District, and Allowable FARs from a standard Excel file.";
            btnImp.Click += (s, e) =>
            {
                Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = "Excel & CSV Files (*.xls;*.xml;*.csv)|*.xls;*.xml;*.csv|All Files (*.*)|*.*",
                    Title = "Import Zoning Lot Parameters"
                };
                if (dlg.ShowDialog() == true)
                {
                    _vm.ImportZoningExcel(dlg.FileName);
                    RefreshCalculateUI();
                }
            };
            btnStack.Children.Add(btnImp);

            WpfButton btnTpl = CreateNeutralButton("📄 Excel Template");
            btnTpl.Height = 24;
            btnTpl.FontSize = 10.5;
            btnTpl.Padding = new Thickness(8, 0, 8, 0);
            btnTpl.ToolTip = "Download a clean, pre-formatted Excel template to fill in project zoning data.";
            btnTpl.Click += (s, e) =>
            {
                Microsoft.Win32.SaveFileDialog saveDlg = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "Excel Spreadsheet (*.xls)|*.xls",
                    FileName = "BauTools_Zoning_Lot_Template.xls",
                    Title = "Export BauTools Zoning Excel Template"
                };
                if (saveDlg.ShowDialog() == true)
                {
                    _vm.ExportZoningTemplateExcel(saveDlg.FileName);
                }
            };
            btnStack.Children.Add(btnTpl);

            WpfGrid.SetColumn(btnStack, 1);
            lHdrGrid.Children.Add(btnStack);
            leftStack.Children.Add(lHdrGrid);

            // Lot Form Fields Row
            WpfGrid formGrid = new WpfGrid();
            formGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            formGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
            formGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.9, GridUnitType.Star) });
            formGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
            formGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Lot Area
            StackPanel sArea = new StackPanel();
            sArea.Children.Add(new WpfTextBlock { Text = "Lot Area (SF):", FontSize = 9.5, Foreground = new SolidColorBrush(COL_TEXT_MUTED), Margin = new Thickness(0, 0, 0, 2) });
            WpfTextBox txtArea = new WpfTextBox { Text = lot.LotAreaSqFt.ToString("N0"), Height = 26, FontSize = 11, VerticalContentAlignment = VerticalAlignment.Center };
            txtArea.TextChanged += (s, e) =>
            {
                double v;
                if (double.TryParse(txtArea.Text.Replace(",", ""), out v)) { lot.LotAreaSqFt = v; _vm.EvaluateCompliance(); RefreshComplianceHudUI(); }
            };
            sArea.Children.Add(txtArea);
            WpfGrid.SetColumn(sArea, 0);
            formGrid.Children.Add(sArea);

            // District
            StackPanel sDist = new StackPanel();
            sDist.Children.Add(new WpfTextBlock { Text = "District:", FontSize = 9.5, Foreground = new SolidColorBrush(COL_TEXT_MUTED), Margin = new Thickness(0, 0, 0, 2) });
            WpfTextBox txtDist = new WpfTextBox { Text = lot.ZoningDistrict, Height = 26, FontSize = 11, VerticalContentAlignment = VerticalAlignment.Center };
            txtDist.TextChanged += (s, e) => { lot.ZoningDistrict = txtDist.Text; };
            sDist.Children.Add(txtDist);
            WpfGrid.SetColumn(sDist, 2);
            formGrid.Children.Add(sDist);

            // Allowable FAR
            StackPanel sFar = new StackPanel();
            sFar.Children.Add(new WpfTextBlock { Text = "Allowable FAR:", FontSize = 9.5, Foreground = new SolidColorBrush(COL_TEXT_MUTED), Margin = new Thickness(0, 0, 0, 2) });
            WpfTextBox txtFar = new WpfTextBox { Text = lot.BaseResidentialFar.ToString("N2"), Height = 26, FontSize = 11, VerticalContentAlignment = VerticalAlignment.Center };
            txtFar.TextChanged += (s, e) =>
            {
                double v;
                if (double.TryParse(txtFar.Text, out v)) { lot.BaseResidentialFar = v; _vm.EvaluateCompliance(); RefreshComplianceHudUI(); }
            };
            sFar.Children.Add(txtFar);
            WpfGrid.SetColumn(sFar, 4);
            formGrid.Children.Add(sFar);

            leftStack.Children.Add(formGrid);
            WpfGrid.SetColumn(leftStack, 0);
            hudGrid.Children.Add(leftStack);

            // ── Right Card: Compliance HUD & Gauge ──
            StackPanel rightStack = new StackPanel();

            WpfGrid rHdr = new WpfGrid { Margin = new Thickness(0, 0, 0, 6) };
            rHdr.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            rHdr.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            WpfTextBlock rTitle = new WpfTextBlock
            {
                Text = "ZONING COMPLIANCE & CAPACITY:",
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(COL_TEXT_MUTED)
            };
            WpfGrid.SetColumn(rTitle, 0);
            rHdr.Children.Add(rTitle);

            // Status Pill
            WpfColor statCol = (WpfColor)ColorConverter.ConvertFromString(rep.ColorHex ?? "#10B981");
            Border statusPill = new Border
            {
                Background = new SolidColorBrush(WpfColor.FromArgb(35, statCol.R, statCol.G, statCol.B)),
                BorderBrush = new SolidColorBrush(statCol),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(8, 2, 8, 2)
            };
            statusPill.Child = new WpfTextBlock
            {
                Text = rep.StatusSummary,
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(statCol)
            };
            WpfGrid.SetColumn(statusPill, 1);
            rHdr.Children.Add(statusPill);
            rightStack.Children.Add(rHdr);

            // KPI 3-Pill Row
            WpfGrid kpiGrid = new WpfGrid { Margin = new Thickness(0, 0, 0, 6) };
            kpiGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            kpiGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
            kpiGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            kpiGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
            kpiGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Allowable
            Border bAllow = CreateKpiPill("Max Cap", string.Format("{0:N0} SF", rep.AllowableZfa), "#475569");
            WpfGrid.SetColumn(bAllow, 0);
            kpiGrid.Children.Add(bAllow);

            // Proposed
            Border bProp = CreateKpiPill("Proposed", string.Format("{0:N0} SF", rep.ProposedZfa), "#1E40AF");
            WpfGrid.SetColumn(bProp, 2);
            kpiGrid.Children.Add(bProp);

            // Balance
            string balPrefix = rep.RemainingZfa >= 0 ? "+" : "";
            Border bBal = CreateKpiPill("Balance", string.Format("{0}{1:N0} SF", balPrefix, rep.RemainingZfa), rep.ColorHex);
            WpfGrid.SetColumn(bBal, 4);
            kpiGrid.Children.Add(bBal);

            rightStack.Children.Add(kpiGrid);

            // Battery / Progress Bar
            Border gaugeBg = new Border
            {
                Height = 8,
                Background = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#F1F5F9")),
                BorderBrush = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#CBD5E1")),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                ClipToBounds = true
            };

            double clampedPct = Math.Min(100.0, Math.Max(0.0, rep.UtilizationPercent));
            WpfGrid barGrid = new WpfGrid();
            barGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(clampedPct, GridUnitType.Star) });
            barGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(Math.Max(0.01, 100.0 - clampedPct), GridUnitType.Star) });

            Border fillBar = new Border { Background = new SolidColorBrush(statCol), CornerRadius = new CornerRadius(3) };
            WpfGrid.SetColumn(fillBar, 0);
            barGrid.Children.Add(fillBar);

            gaugeBg.Child = barGrid;
            rightStack.Children.Add(gaugeBg);

            WpfGrid.SetColumn(rightStack, 2);
            hudGrid.Children.Add(rightStack);

            _complianceHudContainer.Children.Add(hudGrid);
        }

        private Border CreateKpiPill(string label, string val, string hex)
        {
            WpfColor c = (WpfColor)ColorConverter.ConvertFromString(hex ?? "#475569");
            Border b = new Border
            {
                Background = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#F8FAFC")),
                BorderBrush = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#E2E8F0")),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(6, 3, 6, 3)
            };

            StackPanel sp = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
            sp.Children.Add(new WpfTextBlock { Text = label.ToUpperInvariant(), FontSize = 8.5, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(COL_TEXT_MUTED), HorizontalAlignment = HorizontalAlignment.Center });
            sp.Children.Add(new WpfTextBlock { Text = val, FontSize = 11.5, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(c), HorizontalAlignment = HorizontalAlignment.Center });

            b.Child = sp;
            return b;
        }

        private UIElement CreateBuildingDataGrid(ZoningTableResult tableResult)
        {
            Border host = new Border { Background = WpfBrushes.White, Padding = new Thickness(12) };

            ScrollViewer scroll = new ScrollViewer
            {
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };

            DataGrid grid = new DataGrid
            {
                AutoGenerateColumns = false,
                CanUserAddRows = false,
                CanUserDeleteRows = false,
                IsReadOnly = true,
                GridLinesVisibility = DataGridGridLinesVisibility.Horizontal,
                HorizontalGridLinesBrush = new SolidColorBrush(COL_BORDER_LIGHT),
                Background = WpfBrushes.White,
                RowBackground = WpfBrushes.White,
                AlternatingRowBackground = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#FAFAFA")),
                BorderThickness = new Thickness(0),
                ColumnHeaderHeight = 36,
                RowHeight = 30,
                FontSize = 12
            };

            // 1. Group Indicator Column
            DataGridTextColumn colGroup = new DataGridTextColumn
            {
                Header = "Group",
                Binding = new WpfBinding("GroupName"),
                Width = new DataGridLength(110)
            };
            grid.Columns.Add(colGroup);

            // 2. Level Column
            DataGridTextColumn colLevel = new DataGridTextColumn
            {
                Header = "Level",
                Binding = new WpfBinding("LevelName"),
                FontWeight = FontWeights.SemiBold,
                Width = new DataGridLength(120)
            };
            grid.Columns.Add(colLevel);

            // 3. Gross Floor Area
            DataGridTextColumn colGross = new DataGridTextColumn
            {
                Header = "Gross Floor Area",
                Binding = new WpfBinding("GrossFloorArea") { StringFormat = "{0:N2}" },
                Width = new DataGridLength(120)
            };
            grid.Columns.Add(colGross);

            // 4. Dynamic Deduction Columns
            foreach (string cat in tableResult.DeductionCategories)
            {
                DataGridTextColumn colDed = new DataGridTextColumn
                {
                    Header = cat,
                    Binding = new WpfBinding("Deductions[" + cat + "]") { StringFormat = "{0:N2}" },
                    Width = new DataGridLength(100)
                };
                grid.Columns.Add(colDed);
            }

            // 5. Total Deductions
            DataGridTextColumn colTotDed = new DataGridTextColumn
            {
                Header = "Total Deductions",
                Binding = new WpfBinding("TotalDeductions") { StringFormat = "{0:N2}" },
                Width = new DataGridLength(115)
            };
            grid.Columns.Add(colTotDed);

            // 6. Net Area
            DataGridTextColumn colNet = new DataGridTextColumn
            {
                Header = "Net Area",
                Binding = new WpfBinding("NetArea") { StringFormat = "{0:N2}" },
                Width = new DataGridLength(110)
            };
            grid.Columns.Add(colNet);

            // 7. 5% ULEB
            DataGridTextColumn colUleb = new DataGridTextColumn
            {
                Header = "5% ULEB",
                Binding = new WpfBinding("UlebAmount") { StringFormat = "{0:N2}" },
                Width = new DataGridLength(90)
            };
            grid.Columns.Add(colUleb);

            // 8. Zoning Floor Area
            DataGridTextColumn colZfa = new DataGridTextColumn
            {
                Header = "Zoning Floor Area",
                Binding = new WpfBinding("ZoningFloorArea") { StringFormat = "{0:N2}" },
                FontWeight = FontWeights.Bold,
                Width = new DataGridLength(130)
            };
            grid.Columns.Add(colZfa);

            // 9. FAR
            DataGridTextColumn colFar = new DataGridTextColumn
            {
                Header = "FAR",
                Binding = new WpfBinding("Far") { StringFormat = "{0:N2}" },
                Width = new DataGridLength(80)
            };
            grid.Columns.Add(colFar);

            // Build Items Source
            List<LevelZoningRow> displayList = new List<LevelZoningRow>();
            if (tableResult.ResidentialRows != null) displayList.AddRange(tableResult.ResidentialRows);
            if (tableResult.ResidentialSubtotal != null) displayList.Add(tableResult.ResidentialSubtotal);
            if (tableResult.CommercialRows != null) displayList.AddRange(tableResult.CommercialRows);
            if (tableResult.CommercialSubtotal != null) displayList.Add(tableResult.CommercialSubtotal);
            if (tableResult.GrandTotal != null) displayList.Add(tableResult.GrandTotal);

            grid.ItemsSource = displayList;
            scroll.Content = grid;
            host.Child = scroll;
            return host;
        }

        // ══════════════════════════════════════════════════════════════
        // ── 6. STEP 4: SMART SHEET DIAGRAMMER & VIEW COMPOSER ──
        // ══════════════════════════════════════════════════════════════
        private WpfGrid CreateStep4Panel()
        {
            WpfGrid grid = new WpfGrid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.25, GridUnitType.Star) }); // Left: Sheet Composer Settings & Packages
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(20) });                    // Gap
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.1, GridUnitType.Star) });   // Right: Live Visual Preview & Actions

            // ── Left Card: Sheet Composer & Package Selector ──
            Border cardConfig = CreateCard();
            WpfGrid cfgGrid = new WpfGrid();
            cfgGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Header
            cfgGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Scrollable Settings

            // Header
            StackPanel hdr = new StackPanel { Margin = new Thickness(0, 0, 0, 14) };
            hdr.Children.Add(new WpfTextBlock
            {
                Text = "Sheet Diagrammer & Package Composer",
                FontSize = 16,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(COL_TEXT_MAIN)
            });
            hdr.Children.Add(new WpfTextBlock
            {
                Text = "Compose typical floors, ZFA deductions, ceilings (RCP), and egress plans into multi-viewport sheets.",
                FontSize = 11.5,
                Foreground = new SolidColorBrush(COL_TEXT_MUTED),
                Margin = new Thickness(0, 2, 0, 0)
            });
            WpfGrid.SetRow(hdr, 0);
            cfgGrid.Children.Add(hdr);

            // Scrollable Content
            ScrollViewer scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            StackPanel formStack = new StackPanel();

            // 1. Titleblock Selection & Workspace Bar
            Border tbBox = new Border
            {
                Background = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#EFF6FF")),
                BorderBrush = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#BFDBFE")),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(10, 8, 10, 8),
                Margin = new Thickness(0, 0, 0, 10)
            };
            StackPanel tbStack = new StackPanel();
            tbStack.Children.Add(new WpfTextBlock
            {
                Text = "📐 PROJECT TITLEBLOCK & DRAWING WORKSPACE:",
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#1E40AF")),
                Margin = new Thickness(0, 0, 0, 4)
            });

            WpfComboBox comboTb = new WpfComboBox
            {
                Height = 28,
                ItemsSource = _vm.AvailableTitleblocks,
                DisplayMemberPath = "Name",
                SelectedItem = _vm.SelectedTitleblock
            };
            comboTb.SelectionChanged += (s, e) =>
            {
                _vm.SelectedTitleblock = comboTb.SelectedItem as TitleblockItem;
                RefreshStep4PreviewUI();
            };
            tbStack.Children.Add(comboTb);
            tbBox.Child = tbStack;
            formStack.Children.Add(tbBox);

            // 2. View Packages Section with 1 to 8 Matrix Grid
            WpfGrid pkgHdrGrid = new WpfGrid { Margin = new Thickness(0, 4, 0, 8) };
            pkgHdrGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            pkgHdrGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            pkgHdrGrid.Children.Add(new WpfTextBlock
            {
                Text = "CONFIGURACION INDEPENDIENTE POR PAQUETE DE PLANOS:",
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(COL_TEXT_MUTED),
                VerticalAlignment = VerticalAlignment.Center
            });

            WpfButton btnAddPkg = CreateNeutralButton("➕ Agregar Paquete");
            btnAddPkg.Height = 26;
            btnAddPkg.FontSize = 10.5;
            btnAddPkg.Padding = new Thickness(10, 0, 10, 0);
            btnAddPkg.Click += (s, e) =>
            {
                string newPkgName = string.Format("Paquete {0}", _vm.PackageSettings.Count + 1);
                string defaultScheme = _vm.AreaSchemes.Count > 0 ? _vm.AreaSchemes[0] : "";
                _vm.AddCustomPackage(newPkgName, "P-", ViewPlanKind.AreaPlan, defaultScheme);
                RefreshPackageListUI();
                RefreshStep4PreviewUI();
            };
            WpfGrid.SetColumn(btnAddPkg, 1);
            pkgHdrGrid.Children.Add(btnAddPkg);

            formStack.Children.Add(pkgHdrGrid);

            _packagesContainer = new StackPanel();
            formStack.Children.Add(_packagesContainer);
            RefreshPackageListUI();

            // Scope Box & Parameters Section
            WpfGrid scopeGrid = new WpfGrid { Margin = new Thickness(0, 6, 0, 10) };
            scopeGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            scopeGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) });
            scopeGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            StackPanel msStack = new StackPanel();
            msStack.Children.Add(new WpfTextBlock { Text = "Master Scope Box (Overall):", FontSize = 9.5, Foreground = new SolidColorBrush(COL_TEXT_MUTED), Margin = new Thickness(0, 0, 0, 2) });
            WpfComboBox cMasterScope = new WpfComboBox { Height = 28, ItemsSource = _vm.AvailableScopeBoxes, SelectedItem = _vm.Config.MasterScopeBoxName };
            cMasterScope.SelectionChanged += (s, e) => { if (cMasterScope.SelectedItem != null) _vm.Config.MasterScopeBoxName = cMasterScope.SelectedItem.ToString(); };
            msStack.Children.Add(cMasterScope);
            WpfGrid.SetColumn(msStack, 0);
            scopeGrid.Children.Add(msStack);

            StackPanel vpStack = new StackPanel();
            vpStack.Children.Add(new WpfTextBlock { Text = "Building View Parameter:", FontSize = 9.5, Foreground = new SolidColorBrush(COL_TEXT_MUTED), Margin = new Thickness(0, 0, 0, 2) });
            WpfComboBox cViewParam = new WpfComboBox { Height = 28, ItemsSource = _vm.AvailableViewParameters, SelectedItem = _vm.Config.ViewBuildingParameterName };
            cViewParam.SelectionChanged += (s, e) => { if (cViewParam.SelectedItem != null) _vm.Config.ViewBuildingParameterName = cViewParam.SelectedItem.ToString(); };
            vpStack.Children.Add(cViewParam);
            WpfGrid.SetColumn(vpStack, 2);
            scopeGrid.Children.Add(vpStack);

            formStack.Children.Add(scopeGrid);

            // Checkbox: Reposition
            WpfCheckBox chkRepo = new WpfCheckBox
            {
                Content = "Reposition & update viewports if views already exist on sheets",
                IsChecked = _vm.RepositionIfExists,
                FontWeight = FontWeights.Medium,
                Margin = new Thickness(0, 0, 0, 4)
            };
            chkRepo.Checked += (s, e) => _vm.RepositionIfExists = true;
            chkRepo.Unchecked += (s, e) => _vm.RepositionIfExists = false;
            formStack.Children.Add(chkRepo);

            scroll.Content = formStack;
            WpfGrid.SetRow(scroll, 1);
            cfgGrid.Children.Add(scroll);

            cardConfig.Child = cfgGrid;
            WpfGrid.SetColumn(cardConfig, 0);
            grid.Children.Add(cardConfig);

            // ── Right Card: Live Visual Sheet Preview & Action Bar ──
            Border cardPreview = CreateCard();
            WpfGrid prevLayout = new WpfGrid();
            prevLayout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Header
            prevLayout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Scrollable Sheet Previews
            prevLayout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Action Bar

            // Header
            StackPanel prevHdr = new StackPanel { Margin = new Thickness(0, 0, 0, 10) };
            prevHdr.Children.Add(new WpfTextBlock
            {
                Text = "Live Sheet & Matrix Canvas Visualizer",
                FontSize = 15,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(COL_TEXT_MAIN)
            });
            prevHdr.Children.Add(new WpfTextBlock
            {
                Text = "Simulated drawing workspace displaying real viewport matrix slots, building Scope Boxes, and Title on Sheet badges.",
                FontSize = 11.5,
                Foreground = new SolidColorBrush(COL_TEXT_MUTED)
            });
            WpfGrid.SetRow(prevHdr, 0);
            prevLayout.Children.Add(prevHdr);

            // Scrollable Preview Container
            ScrollViewer prevScroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            _step4PreviewContainer = new StackPanel();
            prevScroll.Content = _step4PreviewContainer;
            WpfGrid.SetRow(prevScroll, 1);
            prevLayout.Children.Add(prevScroll);

            // Action Bar
            Border actBox = new Border { Margin = new Thickness(0, 14, 0, 0) };
            WpfGrid actGrid = new WpfGrid();
            actGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Summary badge
            actGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Buttons

            _step4SummaryBadge = new WpfTextBlock
            {
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#1D4ED8")),
                Margin = new Thickness(0, 0, 0, 10)
            };
            WpfGrid.SetRow(_step4SummaryBadge, 0);
            actGrid.Children.Add(_step4SummaryBadge);

            WpfGrid btnGrid = new WpfGrid();
            btnGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Excel
            btnGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Spacer
            btnGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Compose

            WpfButton btnExcel = CreateNeutralButton("📊 Export Excel (.xls)");
            btnExcel.Height = 36;
            btnExcel.Padding = new Thickness(14, 0, 14, 0);
            btnExcel.Click += (s, e) => _vm.ExportExcelCommand.Execute(null);
            WpfGrid.SetColumn(btnExcel, 0);
            btnGrid.Children.Add(btnExcel);

            WpfButton btnCompose = CreatePrimaryButton("🚀 Generate Views & Compose Sheets in Revit");
            btnCompose.Height = 38;
            btnCompose.Padding = new Thickness(22, 0, 22, 0);
            btnCompose.Click += (s, e) =>
            {
                _vm.ExecuteComposeSheets();
                RefreshStep4PreviewUI();
            };
            WpfGrid.SetColumn(btnCompose, 2);
            btnGrid.Children.Add(btnCompose);

            WpfGrid.SetRow(btnGrid, 1);
            actGrid.Children.Add(btnGrid);

            actBox.Child = actGrid;
            WpfGrid.SetRow(actBox, 2);
            prevLayout.Children.Add(actBox);

            cardPreview.Child = prevLayout;
            WpfGrid.SetColumn(cardPreview, 2);
            grid.Children.Add(cardPreview);

            return grid;
        }

        private void RefreshPackageListUI()
        {
            if (_packagesContainer == null) return;
            _packagesContainer.Children.Clear();

            foreach (PackageSetting pkg in _vm.PackageSettings)
            {
                PackageSetting currentPkg = pkg;

                // Hide Master package if only 1 building
                if (currentPkg.PackageType == ViewPackageType.MasterOverall && _vm.Buildings.Count <= 1)
                    continue;

                Border pBox = new Border
                {
                    Background = new SolidColorBrush(COL_SURFACE),
                    BorderBrush = new SolidColorBrush(COL_BORDER_LIGHT),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(10, 8, 10, 8),
                    Margin = new Thickness(0, 0, 0, 8)
                };

                StackPanel pCardStack = new StackPanel();

                // Row 1: Checkbox & Name + Prefix + Delete Button (if custom)
                WpfGrid r1Grid = new WpfGrid { Margin = new Thickness(0, 0, 0, 6) };
                r1Grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                r1Grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(70) });
                if (currentPkg.IsCustomPackage)
                {
                    r1Grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(28) });
                }

                WpfCheckBox chk = new WpfCheckBox
                {
                    Content = string.Format("{0} {1}", currentPkg.Icon, currentPkg.DisplayName),
                    IsChecked = currentPkg.IsEnabled,
                    FontWeight = FontWeights.SemiBold,
                    VerticalAlignment = VerticalAlignment.Center
                };
                chk.Checked += (s, e) => { currentPkg.IsEnabled = true; RefreshStep4PreviewUI(); };
                chk.Unchecked += (s, e) => { currentPkg.IsEnabled = false; RefreshStep4PreviewUI(); };
                WpfGrid.SetColumn(chk, 0);
                r1Grid.Children.Add(chk);

                WpfTextBox txtPfx = new WpfTextBox
                {
                    Text = currentPkg.SheetPrefix,
                    Height = 24,
                    FontSize = 11,
                    VerticalContentAlignment = VerticalAlignment.Center,
                    ToolTip = "Sheet Number Prefix (e.g. Z-, ZD-, LS-, RCP-, M-)"
                };
                txtPfx.TextChanged += (s, e) => { currentPkg.SheetPrefix = txtPfx.Text; RefreshStep4PreviewUI(); };
                WpfGrid.SetColumn(txtPfx, 1);
                r1Grid.Children.Add(txtPfx);

                if (currentPkg.IsCustomPackage)
                {
                    WpfButton btnDelPkg = new WpfButton
                    {
                        Content = "✕",
                        Width = 20,
                        Height = 20,
                        FontSize = 10,
                        FontWeight = FontWeights.Bold,
                        Background = WpfBrushes.Transparent,
                        BorderThickness = new Thickness(0),
                        Foreground = new SolidColorBrush(COL_DANGER),
                        ToolTip = "Eliminar este paquete de planos"
                    };
                    btnDelPkg.Click += (s, e) =>
                    {
                        _vm.RemovePackage(currentPkg);
                        RefreshPackageListUI();
                        RefreshStep4PreviewUI();
                    };
                    WpfGrid.SetColumn(btnDelPkg, 2);
                    r1Grid.Children.Add(btnDelPkg);
                }

                pCardStack.Children.Add(r1Grid);

                // Row 1.5: View Plan Kind (Tipo de Vista) + Revit Area Scheme Dropdown
                WpfGrid rKindGrid = new WpfGrid { Margin = new Thickness(0, 0, 0, 6) };
                rKindGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.3, GridUnitType.Star) }); // View Kind
                rKindGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(6) });
                rKindGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.7, GridUnitType.Star) }); // Area Scheme

                // View Kind Dropdown
                StackPanel vkStack = new StackPanel();
                vkStack.Children.Add(new WpfTextBlock { Text = "Tipo de Plano:", FontSize = 8.5, Foreground = new SolidColorBrush(COL_TEXT_MUTED) });
                WpfComboBox comboVk = new WpfComboBox { Height = 24, FontSize = 10 };
                comboVk.Items.Add("🏢 Floor Plan (Arquitectura)");
                comboVk.Items.Add("📐 Area Plan (Planta de Áreas)");
                comboVk.Items.Add("💡 Reflected Ceiling (RCP)");

                switch (currentPkg.ViewKind)
                {
                    case ViewPlanKind.FloorPlan: comboVk.SelectedIndex = 0; break;
                    case ViewPlanKind.AreaPlan: comboVk.SelectedIndex = 1; break;
                    case ViewPlanKind.CeilingPlan: comboVk.SelectedIndex = 2; break;
                    default: comboVk.SelectedIndex = 0; break;
                }

                // Area Scheme Dropdown
                StackPanel asStack = new StackPanel();
                asStack.Children.Add(new WpfTextBlock { Text = "Esquema de Área (Revit Scheme):", FontSize = 8.5, Foreground = new SolidColorBrush(COL_TEXT_MUTED) });
                WpfComboBox comboAs = new WpfComboBox { Height = 24, FontSize = 10, ItemsSource = _vm.AreaSchemes };

                if (!string.IsNullOrEmpty(currentPkg.SelectedAreaSchemeName))
                {
                    comboAs.SelectedItem = currentPkg.SelectedAreaSchemeName;
                }
                else if (_vm.AreaSchemes.Count > 0)
                {
                    if (currentPkg.PackageType == ViewPackageType.GrossArea && !string.IsNullOrEmpty(_vm.Config.GrossAreaSchemeName))
                        comboAs.SelectedItem = _vm.Config.GrossAreaSchemeName;
                    else if (currentPkg.PackageType == ViewPackageType.Deductions && !string.IsNullOrEmpty(_vm.Config.DeductionAreaSchemeName))
                        comboAs.SelectedItem = _vm.Config.DeductionAreaSchemeName;
                    else
                        comboAs.SelectedIndex = 0;
                }

                comboAs.IsEnabled = (currentPkg.ViewKind == ViewPlanKind.AreaPlan);

                comboVk.SelectionChanged += (s, e) =>
                {
                    switch (comboVk.SelectedIndex)
                    {
                        case 0: currentPkg.ViewKind = ViewPlanKind.FloorPlan; break;
                        case 1: currentPkg.ViewKind = ViewPlanKind.AreaPlan; break;
                        case 2: currentPkg.ViewKind = ViewPlanKind.CeilingPlan; break;
                    }
                    comboAs.IsEnabled = (currentPkg.ViewKind == ViewPlanKind.AreaPlan);
                    if (currentPkg.ViewKind == ViewPlanKind.AreaPlan && comboAs.SelectedItem != null)
                    {
                        currentPkg.SelectedAreaSchemeName = comboAs.SelectedItem.ToString();
                    }
                    RefreshStep4PreviewUI();
                };

                comboAs.SelectionChanged += (s, e) =>
                {
                    if (comboAs.SelectedItem != null)
                    {
                        currentPkg.SelectedAreaSchemeName = comboAs.SelectedItem.ToString();
                        RefreshStep4PreviewUI();
                    }
                };

                vkStack.Children.Add(comboVk);
                WpfGrid.SetColumn(vkStack, 0);
                rKindGrid.Children.Add(vkStack);

                asStack.Children.Add(comboAs);
                WpfGrid.SetColumn(asStack, 2);
                rKindGrid.Children.Add(asStack);

                pCardStack.Children.Add(rKindGrid);

                // Row 2: Matrix Layout (1 to 8) + View Template + Scale
                WpfGrid r2Grid = new WpfGrid();
                r2Grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.1, GridUnitType.Star) }); // Matrix
                r2Grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(6) });
                r2Grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.3, GridUnitType.Star) }); // Template
                r2Grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(6) });
                r2Grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.1, GridUnitType.Star) }); // Scale

                // Matrix Combo (1 to 8 plans per sheet)
                StackPanel mxStack = new StackPanel();
                mxStack.Children.Add(new WpfTextBlock { Text = "Grid Matrix:", FontSize = 8.5, Foreground = new SolidColorBrush(COL_TEXT_MUTED) });
                WpfComboBox comboMatrix = new WpfComboBox { Height = 24, FontSize = 10 };
                comboMatrix.Items.Add("1 Plan / Sheet (1x1)");
                comboMatrix.Items.Add("2 Plans (1x2)");
                comboMatrix.Items.Add("3 Plans (1x3)");
                comboMatrix.Items.Add("4 Plans (2x2 Matrix)");
                comboMatrix.Items.Add("6 Plans (2x3 Matrix)");
                comboMatrix.Items.Add("8 Plans (2x4 Matrix)");

                switch (currentPkg.LayoutMode)
                {
                    case SheetLayoutMode.Single1View: comboMatrix.SelectedIndex = 0; break;
                    case SheetLayoutMode.Dual2Views: comboMatrix.SelectedIndex = 1; break;
                    case SheetLayoutMode.Triple3Views: comboMatrix.SelectedIndex = 2; break;
                    case SheetLayoutMode.Quad4Views: comboMatrix.SelectedIndex = 3; break;
                    case SheetLayoutMode.Hex6Views: comboMatrix.SelectedIndex = 4; break;
                    case SheetLayoutMode.Octo8Views: comboMatrix.SelectedIndex = 5; break;
                    default: comboMatrix.SelectedIndex = 3; break;
                }

                comboMatrix.SelectionChanged += (s, e) =>
                {
                    switch (comboMatrix.SelectedIndex)
                    {
                        case 0: currentPkg.LayoutMode = SheetLayoutMode.Single1View; break;
                        case 1: currentPkg.LayoutMode = SheetLayoutMode.Dual2Views; break;
                        case 2: currentPkg.LayoutMode = SheetLayoutMode.Triple3Views; break;
                        case 3: currentPkg.LayoutMode = SheetLayoutMode.Quad4Views; break;
                        case 4: currentPkg.LayoutMode = SheetLayoutMode.Hex6Views; break;
                        case 5: currentPkg.LayoutMode = SheetLayoutMode.Octo8Views; break;
                    }
                    RefreshStep4PreviewUI();
                };
                mxStack.Children.Add(comboMatrix);
                WpfGrid.SetColumn(mxStack, 0);
                r2Grid.Children.Add(mxStack);

                // View Template Dropdown
                StackPanel vtStack = new StackPanel();
                vtStack.Children.Add(new WpfTextBlock { Text = "View Template:", FontSize = 8.5, Foreground = new SolidColorBrush(COL_TEXT_MUTED) });
                WpfComboBox comboVt = new WpfComboBox
                {
                    Height = 24,
                    FontSize = 10,
                    ItemsSource = _vm.AvailableViewTemplates,
                    DisplayMemberPath = "Name",
                    SelectedIndex = 0
                };
                comboVt.SelectionChanged += (s, e) =>
                {
                    ViewTemplateItem sel = comboVt.SelectedItem as ViewTemplateItem;
                    if (sel != null) currentPkg.SelectedTemplateId = sel.TemplateId;
                };
                vtStack.Children.Add(comboVt);
                WpfGrid.SetColumn(vtStack, 2);
                r2Grid.Children.Add(vtStack);

                // Scale Dropdown
                StackPanel scStack = new StackPanel();
                scStack.Children.Add(new WpfTextBlock { Text = "Escala:", FontSize = 8.5, Foreground = new SolidColorBrush(COL_TEXT_MUTED) });
                WpfComboBox comboSc = new WpfComboBox { Height = 24, FontSize = 10 };
                comboSc.Items.Add("1/4\" (1:48)");
                comboSc.Items.Add("3/16\" (1:64)");
                comboSc.Items.Add("1/8\" (1:96)");
                comboSc.Items.Add("3/32\" (1:128)");
                comboSc.Items.Add("1/16\" (1:192)");
                comboSc.Items.Add("1:50 Metric");
                comboSc.Items.Add("1:100 Metric");
                comboSc.Items.Add("1:200 Metric");

                if (currentPkg.ScaleValue == 48) comboSc.SelectedIndex = 0;
                else if (currentPkg.ScaleValue == 64) comboSc.SelectedIndex = 1;
                else if (currentPkg.ScaleValue == 96) comboSc.SelectedIndex = 2;
                else if (currentPkg.ScaleValue == 128) comboSc.SelectedIndex = 3;
                else if (currentPkg.ScaleValue == 192) comboSc.SelectedIndex = 4;
                else if (currentPkg.ScaleValue == 50) comboSc.SelectedIndex = 5;
                else if (currentPkg.ScaleValue == 100) comboSc.SelectedIndex = 6;
                else if (currentPkg.ScaleValue == 200) comboSc.SelectedIndex = 7;
                else comboSc.SelectedIndex = 2;

                comboSc.SelectionChanged += (s, e) =>
                {
                    switch (comboSc.SelectedIndex)
                    {
                        case 0: currentPkg.ScaleValue = 48; currentPkg.ScaleDisplay = "1/4\" = 1'-0\""; break;
                        case 1: currentPkg.ScaleValue = 64; currentPkg.ScaleDisplay = "3/16\" = 1'-0\""; break;
                        case 2: currentPkg.ScaleValue = 96; currentPkg.ScaleDisplay = "1/8\" = 1'-0\""; break;
                        case 3: currentPkg.ScaleValue = 128; currentPkg.ScaleDisplay = "3/32\" = 1'-0\""; break;
                        case 4: currentPkg.ScaleValue = 192; currentPkg.ScaleDisplay = "1/16\" = 1'-0\""; break;
                        case 5: currentPkg.ScaleValue = 50; currentPkg.ScaleDisplay = "1:50 Metric"; break;
                        case 6: currentPkg.ScaleValue = 100; currentPkg.ScaleDisplay = "1:100 Metric"; break;
                        case 7: currentPkg.ScaleValue = 200; currentPkg.ScaleDisplay = "1:200 Metric"; break;
                    }
                    RefreshStep4PreviewUI();
                };
                scStack.Children.Add(comboSc);
                WpfGrid.SetColumn(scStack, 4);
                r2Grid.Children.Add(scStack);

                pCardStack.Children.Add(r2Grid);
                pBox.Child = pCardStack;
                _packagesContainer.Children.Add(pBox);
            }
        }

        private void RefreshStep4PreviewUI()
        {
            if (_step4PreviewContainer == null) return;
            _step4PreviewContainer.Children.Clear();

            _vm.ComputePlannedSheets();

            int totalViews = _vm.PlannedSheets.Sum(s => s.Viewports.Count);
            if (_step4SummaryBadge != null)
            {
                _step4SummaryBadge.Text = string.Format("⚡ Ready to generate {0} view(s) across {1} planned sheet(s) in Revit.", totalViews, _vm.PlannedSheets.Count);
            }

            if (_vm.PlannedSheets.Count == 0)
            {
                _step4PreviewContainer.Children.Add(new WpfTextBlock
                {
                    Text = "No sheets planned. Please enable at least one package on the left and configure typical floor groups in Step 1.",
                    Foreground = new SolidColorBrush(COL_TEXT_MUTED),
                    Margin = new Thickness(0, 20, 0, 0)
                });
                return;
            }

            foreach (PlannedSheet ps in _vm.PlannedSheets)
            {
                Border sheetCard = new Border
                {
                    Background = WpfBrushes.White,
                    BorderBrush = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#93C5FD")),
                    BorderThickness = new Thickness(1.5),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(12),
                    Margin = new Thickness(0, 0, 0, 10)
                };

                StackPanel sStack = new StackPanel();

                // Sheet Header Bar
                WpfGrid shHdr = new WpfGrid { Margin = new Thickness(0, 0, 0, 8) };
                shHdr.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                shHdr.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                shHdr.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                Border numPill = new Border
                {
                    Background = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#DBEAFE")),
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(8, 2, 8, 2),
                    Margin = new Thickness(0, 0, 8, 0)
                };
                numPill.Child = new WpfTextBlock
                {
                    Text = ps.SheetNumber,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#1E40AF")),
                    FontSize = 11.5
                };
                WpfGrid.SetColumn(numPill, 0);
                shHdr.Children.Add(numPill);

                WpfTextBlock txtShName = new WpfTextBlock
                {
                    Text = ps.SheetName,
                    FontWeight = FontWeights.SemiBold,
                    VerticalAlignment = VerticalAlignment.Center,
                    FontSize = 12,
                    Foreground = new SolidColorBrush(COL_TEXT_MAIN)
                };
                WpfGrid.SetColumn(txtShName, 1);
                shHdr.Children.Add(txtShName);

                // Scale badge
                Border scBadge = new Border
                {
                    Background = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#F1F5F9")),
                    BorderBrush = new SolidColorBrush(COL_BORDER_LIGHT),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(6, 2, 6, 2)
                };
                scBadge.Child = new WpfTextBlock
                {
                    Text = "📐 " + ps.ScaleDisplay,
                    FontSize = 9.5,
                    FontWeight = FontWeights.Medium,
                    Foreground = new SolidColorBrush(COL_TEXT_MUTED)
                };
                WpfGrid.SetColumn(scBadge, 2);
                shHdr.Children.Add(scBadge);

                sStack.Children.Add(shHdr);

                // Simulated Viewport Layout Canvas (Matrix 1 to 8)
                int rows = 1;
                int cols = 1;
                switch (ps.LayoutMode)
                {
                    case SheetLayoutMode.Single1View: rows = 1; cols = 1; break;
                    case SheetLayoutMode.Dual2Views: rows = 1; cols = 2; break;
                    case SheetLayoutMode.Triple3Views: rows = 1; cols = 3; break;
                    case SheetLayoutMode.Quad4Views: rows = 2; cols = 2; break;
                    case SheetLayoutMode.Hex6Views: rows = 2; cols = 3; break;
                    case SheetLayoutMode.Octo8Views: rows = 2; cols = 4; break;
                }

                Border canvas = new Border
                {
                    Background = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#F8FAFC")),
                    BorderBrush = new SolidColorBrush(COL_BORDER_LIGHT),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(6),
                    Height = rows > 1 ? 120 : 75
                };

                WpfGrid vpGrid = new WpfGrid();
                for (int c = 0; c < cols; c++)
                {
                    if (c > 0) vpGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(6) });
                    vpGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                }
                for (int r = 0; r < rows; r++)
                {
                    if (r > 0) vpGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(6) });
                    vpGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                }

                for (int vIdx = 0; vIdx < ps.Viewports.Count; vIdx++)
                {
                    PlannedViewport vp = ps.Viewports[vIdx];
                    Border vpBox = new Border
                    {
                        Background = WpfBrushes.White,
                        BorderBrush = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#CBD5E1")),
                        BorderThickness = new Thickness(1),
                        CornerRadius = new CornerRadius(4),
                        Padding = new Thickness(4)
                    };

                    StackPanel vpContent = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
                    vpContent.Children.Add(new WpfTextBlock
                    {
                        Text = !string.IsNullOrEmpty(vp.FormattedTitleOnSheet) ? vp.FormattedTitleOnSheet : ("📐 " + vp.LevelName),
                        FontWeight = FontWeights.Bold,
                        FontSize = 9.5,
                        Foreground = new SolidColorBrush(COL_TEXT_MAIN),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        TextWrapping = TextWrapping.NoWrap,
                        TextTrimming = TextTrimming.CharacterEllipsis
                    });

                    // View Kind & Area Scheme Badge
                    string kindBadgeText = "";
                    string kindBadgeCol = "#2563EB";
                    if (vp.ViewKind == ViewPlanKind.AreaPlan)
                    {
                        string sName = !string.IsNullOrEmpty(vp.AreaSchemeName) ? vp.AreaSchemeName : "Area";
                        kindBadgeText = string.Format("📐 Area: {0}", sName);
                        kindBadgeCol = "#7C3AED"; // Purple
                    }
                    else if (vp.ViewKind == ViewPlanKind.CeilingPlan)
                    {
                        kindBadgeText = "💡 RCP Ceiling";
                        kindBadgeCol = "#D97706"; // Amber
                    }
                    else
                    {
                        kindBadgeText = "🏢 Floor Plan";
                        kindBadgeCol = "#2563EB"; // Blue
                    }

                    vpContent.Children.Add(new WpfTextBlock
                    {
                        Text = kindBadgeText,
                        FontSize = 8,
                        FontWeight = FontWeights.SemiBold,
                        Foreground = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString(kindBadgeCol)),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(0, 1, 0, 1)
                    });

                    string scopeBoxLabel = !string.IsNullOrEmpty(vp.ScopeBoxName) && vp.ScopeBoxName != "(None)" ?
                        string.Format("🟢 Scope: {0}", vp.ScopeBoxName) : "⚪ No Scope Box";

                    vpContent.Children.Add(new WpfTextBlock
                    {
                        Text = scopeBoxLabel,
                        FontSize = 7.5,
                        Foreground = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#10B981")),
                        HorizontalAlignment = HorizontalAlignment.Center
                    });

                    vpBox.Child = vpContent;

                    int colIdx = (vIdx % cols) * 2;
                    int rowIdx = (vIdx / cols) * 2;

                    WpfGrid.SetColumn(vpBox, colIdx);
                    WpfGrid.SetRow(vpBox, rowIdx);
                    vpGrid.Children.Add(vpBox);
                }

                canvas.Child = vpGrid;
                sStack.Children.Add(canvas);

                sheetCard.Child = sStack;
                _step4PreviewContainer.Children.Add(sheetCard);
            }
        }

        // ══════════════════════════════════════════════════════════════
        // ── 7. FOOTER (Status & Step Navigation) ──
        // ══════════════════════════════════════════════════════════════
        private UIElement CreateFooter()
        {
            Border footer = new Border
            {
                Background = new SolidColorBrush(COL_BG),
                Padding = new Thickness(24, 14, 24, 16)
            };

            WpfGrid grid = new WpfGrid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Status
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Next / Back Buttons

            // Status Message
            _txtStatus = new WpfTextBlock
            {
                FontSize = 12,
                Foreground = new SolidColorBrush(COL_TEXT_MUTED),
                VerticalAlignment = VerticalAlignment.Center
            };
            _txtStatus.SetBinding(WpfTextBlock.TextProperty, new WpfBinding("StatusMessage"));
            WpfGrid.SetColumn(_txtStatus, 0);
            grid.Children.Add(_txtStatus);

            // Back & Next Buttons
            StackPanel navStack = new StackPanel { Orientation = WpfOrientation.Horizontal };

            WpfButton btnBack = CreateNeutralButton("Back");
            btnBack.Height = 34;
            btnBack.Padding = new Thickness(16, 0, 16, 0);
            btnBack.Margin = new Thickness(0, 0, 10, 0);
            btnBack.Click += (s, e) =>
            {
                if (_activeStepIndex > 0) SwitchToStep(_activeStepIndex - 1);
            };
            navStack.Children.Add(btnBack);

            WpfButton btnNext = CreatePrimaryButton("Next Step →");
            btnNext.Height = 34;
            btnNext.Padding = new Thickness(18, 0, 18, 0);
            btnNext.Click += (s, e) =>
            {
                if (_activeStepIndex < 3) SwitchToStep(_activeStepIndex + 1);
            };
            navStack.Children.Add(btnNext);

            WpfGrid.SetColumn(navStack, 1);
            grid.Children.Add(navStack);

            footer.Child = grid;
            return footer;
        }

        private void ConfigureLevelComboBox(WpfComboBox combo, TypicalFloorGroup currentGroup, string selectedLevelName, Action<string> onLevelSelected)
        {
            List<LevelPickerItem> items = _vm.GetLevelPickerItemsForGroup(currentGroup);
            combo.ItemsSource = items;
            combo.DisplayMemberPath = "DisplayText";

            LevelPickerItem currentSel = items.FirstOrDefault(i => string.Equals(i.LevelName, selectedLevelName, StringComparison.OrdinalIgnoreCase));
            if (currentSel != null)
            {
                combo.SelectedItem = currentSel;
            }

            Style itemStyle = new Style(typeof(ComboBoxItem));
            DataTrigger trigDisabled = new DataTrigger { Binding = new WpfBinding("IsAvailable"), Value = false };
            trigDisabled.Setters.Add(new Setter(ComboBoxItem.IsEnabledProperty, false));
            trigDisabled.Setters.Add(new Setter(ComboBoxItem.ForegroundProperty, new SolidColorBrush(COL_TEXT_MUTED)));
            itemStyle.Triggers.Add(trigDisabled);
            combo.ItemContainerStyle = itemStyle;

            combo.SelectionChanged += (s, e) =>
            {
                LevelPickerItem sel = combo.SelectedItem as LevelPickerItem;
                if (sel != null)
                {
                    if (!sel.IsAvailable)
                    {
                        _vm.TriggerToast(string.Format("'{0}' is already occupied by '{1}'.", sel.LevelName, sel.OccupiedByGroupName), true);
                        LevelPickerItem prev = items.FirstOrDefault(i => string.Equals(i.LevelName, selectedLevelName, StringComparison.OrdinalIgnoreCase));
                        combo.SelectedItem = prev;
                        return;
                    }
                    onLevelSelected(sel.LevelName);
                }
            };
        }

        private void ShowAddBuildingDialog()
        {
            if (_vm.Buildings.Count == 0)
            {
                _vm.AddBuilding("Building 1");
                RefreshBuildingTabsUI();
                RefreshTypicalGroupsUI();
                RefreshTowerUI();
                return;
            }

            Window dlg = new Window
            {
                Title = "Add New Building",
                Width = 490,
                SizeToContent = SizeToContent.Height,
                MinHeight = 360,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                ResizeMode = ResizeMode.NoResize,
                Background = new SolidColorBrush(COL_BG),
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 12.5
            };

            WpfGrid g = new WpfGrid { Margin = new Thickness(24, 20, 24, 24) };
            g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 0: Title
            g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 1: Name input
            g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 2: Options
            g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 3: Buttons

            // Title
            StackPanel tStack = new StackPanel { Margin = new Thickness(0, 0, 0, 14) };
            tStack.Children.Add(new WpfTextBlock { Text = "🏢 Add New Building", FontSize = 16, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(COL_TEXT_MAIN) });
            tStack.Children.Add(new WpfTextBlock { Text = "Specify the building name and optionally copy typical floor groups from an existing building.", FontSize = 11, Foreground = new SolidColorBrush(COL_TEXT_MUTED), TextWrapping = TextWrapping.Wrap });
            WpfGrid.SetRow(tStack, 0);
            g.Children.Add(tStack);

            // Name
            StackPanel nStack = new StackPanel { Margin = new Thickness(0, 0, 0, 14) };
            nStack.Children.Add(new WpfTextBlock { Text = "Building Name:", FontSize = 11, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 4) });
            WpfTextBox txtName = new WpfTextBox { Text = string.Format("Building {0}", _vm.Buildings.Count + 1), Height = 28, Padding = new Thickness(6, 2, 6, 2), VerticalContentAlignment = VerticalAlignment.Center };
            nStack.Children.Add(txtName);
            WpfGrid.SetRow(nStack, 1);
            g.Children.Add(nStack);

            // Options
            Border optBox = new Border { Background = new SolidColorBrush(COL_SURFACE), BorderBrush = new SolidColorBrush(COL_BORDER_LIGHT), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(6), Padding = new Thickness(12, 10, 12, 10), Margin = new Thickness(0, 0, 0, 18) };
            StackPanel optStack = new StackPanel();

            WpfRadioButton rbCopy = new WpfRadioButton { Content = "📋 Copy typical floor setup from existing building:", IsChecked = true, FontWeight = FontWeights.Medium, Margin = new Thickness(0, 0, 0, 6) };
            WpfComboBox cSourceBldg = new WpfComboBox { Height = 26, ItemsSource = _vm.Buildings, DisplayMemberPath = "Name", SelectedItem = _vm.SelectedBuilding ?? _vm.Buildings[0], Margin = new Thickness(20, 0, 0, 8) };

            WpfRadioButton rbBlank = new WpfRadioButton { Content = "⚪ Start with blank configuration (No typical floors)", FontWeight = FontWeights.Medium };

            rbCopy.Checked += (s, e) => cSourceBldg.IsEnabled = true;
            rbBlank.Checked += (s, e) => cSourceBldg.IsEnabled = false;

            optStack.Children.Add(rbCopy);
            optStack.Children.Add(cSourceBldg);
            optStack.Children.Add(rbBlank);
            optBox.Child = optStack;
            WpfGrid.SetRow(optBox, 2);
            g.Children.Add(optBox);

            // Action Execute Helper
            Action doCreate = () =>
            {
                string bName = string.IsNullOrWhiteSpace(txtName.Text) ? string.Format("Building {0}", _vm.Buildings.Count + 1) : txtName.Text.Trim();
                BuildingDefinition srcBldg = cSourceBldg.SelectedItem as BuildingDefinition;
                if (rbCopy.IsChecked == true && srcBldg != null)
                {
                    _vm.DuplicateBuilding(srcBldg, bName);
                }
                else
                {
                    _vm.AddBuilding(bName);
                }

                RefreshBuildingTabsUI();
                RefreshTypicalGroupsUI();
                RefreshTowerUI();
                dlg.Close();
            };

            // Buttons
            StackPanel btnRow = new StackPanel { Orientation = WpfOrientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            WpfButton btnCancel = CreateNeutralButton("Cancel");
            btnCancel.Height = 34;
            btnCancel.Padding = new Thickness(16, 0, 16, 0);
            btnCancel.Margin = new Thickness(0, 0, 10, 0);
            btnCancel.Click += (s, e) => dlg.Close();
            btnRow.Children.Add(btnCancel);

            WpfButton btnOk = CreatePrimaryButton("＋ Create Building");
            btnOk.Height = 34;
            btnOk.Padding = new Thickness(20, 0, 20, 0);
            btnOk.Click += (s, e) => doCreate();
            btnRow.Children.Add(btnOk);

            WpfGrid.SetRow(btnRow, 3);
            g.Children.Add(btnRow);

            // Key triggers
            dlg.KeyDown += (s, e) =>
            {
                if (e.Key == System.Windows.Input.Key.Enter) doCreate();
                else if (e.Key == System.Windows.Input.Key.Escape) dlg.Close();
            };

            dlg.Content = g;
            dlg.ShowDialog();
        }

        private Border CreateCard()
        {
            return new Border
            {
                Background = WpfBrushes.White,
                BorderBrush = new SolidColorBrush(COL_BORDER_LIGHT),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(20)
            };
        }

        private Style _primaryBtnStyle;
        private Style _neutralBtnStyle;
        private Style _dangerBtnStyle;
        private Style _microBtnStyle;

        private void InitMinimalistStyles()
        {
            _primaryBtnStyle = CreateMinimalistButtonStyle(
                COL_PRIMARY,
                (WpfColor)ColorConverter.ConvertFromString("#0077ED"),
                (WpfColor)ColorConverter.ConvertFromString("#005BB5"),
                WpfColors.White,
                (WpfColor)ColorConverter.ConvertFromString("#0064C8"),
                6,
                FontWeights.SemiBold);

            _neutralBtnStyle = CreateMinimalistButtonStyle(
                WpfColors.White,
                (WpfColor)ColorConverter.ConvertFromString("#F8FAFC"),
                (WpfColor)ColorConverter.ConvertFromString("#F1F5F9"),
                (WpfColor)ColorConverter.ConvertFromString("#1E293B"),
                (WpfColor)ColorConverter.ConvertFromString("#CBD5E1"),
                6,
                FontWeights.Medium);

            _dangerBtnStyle = CreateMinimalistButtonStyle(
                (WpfColor)ColorConverter.ConvertFromString("#FEF2F2"),
                (WpfColor)ColorConverter.ConvertFromString("#FEE2E2"),
                (WpfColor)ColorConverter.ConvertFromString("#FECACA"),
                (WpfColor)ColorConverter.ConvertFromString("#DC2626"),
                (WpfColor)ColorConverter.ConvertFromString("#FCA5A5"),
                6,
                FontWeights.SemiBold);

            _microBtnStyle = CreateMinimalistButtonStyle(
                (WpfColor)ColorConverter.ConvertFromString("#F8FAFC"),
                (WpfColor)ColorConverter.ConvertFromString("#E2E8F0"),
                (WpfColor)ColorConverter.ConvertFromString("#CBD5E1"),
                (WpfColor)ColorConverter.ConvertFromString("#334155"),
                (WpfColor)ColorConverter.ConvertFromString("#E2E8F0"),
                4,
                FontWeights.SemiBold);
        }

        private static Style CreateMinimalistButtonStyle(
            WpfColor defaultBg,
            WpfColor hoverBg,
            WpfColor pressedBg,
            WpfColor textCol,
            WpfColor borderCol,
            int cornerRadius,
            FontWeight fontWeight)
        {
            Style style = new Style(typeof(WpfButton));
            style.Setters.Add(new Setter(WpfButton.ForegroundProperty, new SolidColorBrush(textCol)));
            style.Setters.Add(new Setter(WpfButton.FontWeightProperty, fontWeight));
            style.Setters.Add(new Setter(WpfButton.CursorProperty, System.Windows.Input.Cursors.Hand));
            style.Setters.Add(new Setter(WpfButton.SnapsToDevicePixelsProperty, true));

            FrameworkElementFactory borderFactory = new FrameworkElementFactory(typeof(Border), "btnBorder");
            borderFactory.SetValue(Border.BackgroundProperty, new SolidColorBrush(defaultBg));
            borderFactory.SetValue(Border.BorderBrushProperty, new SolidColorBrush(borderCol));
            borderFactory.SetValue(Border.BorderThicknessProperty, new Thickness(1));
            borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(cornerRadius));

            FrameworkElementFactory contentPresenter = new FrameworkElementFactory(typeof(ContentPresenter));
            contentPresenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            contentPresenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            contentPresenter.SetValue(ContentPresenter.MarginProperty, new Thickness(6, 0, 6, 0));
            contentPresenter.SetValue(ContentPresenter.RecognizesAccessKeyProperty, true);

            borderFactory.AppendChild(contentPresenter);

            ControlTemplate template = new ControlTemplate(typeof(WpfButton));
            template.VisualTree = borderFactory;

            // Hover trigger
            Trigger hoverTrigger = new Trigger { Property = WpfButton.IsMouseOverProperty, Value = true };
            hoverTrigger.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(hoverBg), "btnBorder"));
            template.Triggers.Add(hoverTrigger);

            // Pressed trigger
            Trigger pressedTrigger = new Trigger { Property = WpfButton.IsPressedProperty, Value = true };
            pressedTrigger.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(pressedBg), "btnBorder"));
            template.Triggers.Add(pressedTrigger);

            // Disabled trigger
            Trigger disabledTrigger = new Trigger { Property = WpfButton.IsEnabledProperty, Value = false };
            disabledTrigger.Setters.Add(new Setter(Border.OpacityProperty, 0.45, "btnBorder"));
            template.Triggers.Add(disabledTrigger);

            style.Setters.Add(new Setter(WpfButton.TemplateProperty, template));
            return style;
        }

        private WpfButton CreatePrimaryButton(string text)
        {
            if (_primaryBtnStyle == null) InitMinimalistStyles();
            return new WpfButton
            {
                Content = text,
                Style = _primaryBtnStyle
            };
        }

        private WpfButton CreateNeutralButton(string text)
        {
            if (_neutralBtnStyle == null) InitMinimalistStyles();
            return new WpfButton
            {
                Content = text,
                Style = _neutralBtnStyle
            };
        }

        private WpfButton CreateDangerButton(string text)
        {
            if (_dangerBtnStyle == null) InitMinimalistStyles();
            return new WpfButton
            {
                Content = text,
                Style = _dangerBtnStyle
            };
        }

        private WpfButton CreateMicroButton(string text)
        {
            if (_microBtnStyle == null) InitMinimalistStyles();
            return new WpfButton
            {
                Content = text,
                Style = _microBtnStyle
            };
        }
    }
}
