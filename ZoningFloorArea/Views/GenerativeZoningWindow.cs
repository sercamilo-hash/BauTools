using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Media.Media3D;
using Autodesk.Revit.DB;
using ZoningFloorArea.Models;
using ZoningFloorArea.Services;
using WpfGrid = System.Windows.Controls.Grid;
using WpfColor = System.Windows.Media.Color;
using WpfButton = System.Windows.Controls.Button;
using WpfTextBox = System.Windows.Controls.TextBox;
using WpfTextBlock = System.Windows.Controls.TextBlock;
using WpfCheckBox = System.Windows.Controls.CheckBox;
using WpfSlider = System.Windows.Controls.Slider;
using WpfVisibility = System.Windows.Visibility;
using WpfColors = System.Windows.Media.Colors;
using WpfBrushes = System.Windows.Media.Brushes;
using WpfOrientation = System.Windows.Controls.Orientation;
using WpfLine = System.Windows.Shapes.Line;

namespace ZoningFloorArea.Views
{
    public class GenerativeZoningWindow : Window
    {
        private readonly Document _doc;
        private readonly NeuralGenerativeSolver _solver;
        private readonly RevitMassingBakerService _bakerService;
        private readonly GenerativeInputParameters _inputs;

        private List<GenerativeScenario> _scenarios;
        private GenerativeScenario _activeScenario;

        // UI Controls
        private StackPanel _scenarioCardsContainer;
        private Viewport3D _viewport3D;
        private ModelVisual3D _modelVisual;
        private PerspectiveCamera _camera;
        private double _cameraDistance = 320.0;
        private double _cameraTheta = 45.0; // Azimuth angle
        private double _cameraPhi = 35.0;   // Elevation angle
        private System.Windows.Point _lastMousePos;
        private bool _isOrbiting = false;

        private WpfTextBlock _txtPreviewTitle;
        private WpfTextBlock _txtPreviewMetrics;
        private Border _badgeZfaStatus;
        private WpfTextBlock _txtZfaStatus;
        private Border _badgeHeightStatus;
        private WpfTextBlock _txtHeightStatus;
        private Border _badgeRevenueStatus;
        private WpfTextBlock _txtRevenueStatus;

        private WpfCheckBox _chkDesignOptions;
        private WpfCheckBox _chkCreateLevels;

        private static readonly WpfColor COL_BG = (WpfColor)ColorConverter.ConvertFromString("#F8FAFC");
        private static readonly WpfColor COL_SURFACE = (WpfColor)ColorConverter.ConvertFromString("#FFFFFF");
        private static readonly WpfColor COL_BORDER = (WpfColor)ColorConverter.ConvertFromString("#E2E8F0");
        private static readonly WpfColor COL_PRIMARY = (WpfColor)ColorConverter.ConvertFromString("#0071E3");
        private static readonly WpfColor COL_TEXT_MAIN = (WpfColor)ColorConverter.ConvertFromString("#0F172A");
        private static readonly WpfColor COL_TEXT_MUTED = (WpfColor)ColorConverter.ConvertFromString("#64748B");

        public GenerativeZoningWindow(Document doc)
        {
            _doc = doc;
            _solver = new NeuralGenerativeSolver();
            _bakerService = new RevitMassingBakerService(doc);
            _inputs = new GenerativeInputParameters();

            Title = "BauTools — Neural Generative Zoning & Real-Time Massing Morphing Engine";
            Width = 1320;
            Height = 880;
            MinWidth = 1100;
            MinHeight = 740;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Background = new SolidColorBrush(COL_BG);
            FontFamily = new FontFamily("Segoe UI");
            FontSize = 12.5;

            BuildUI();
            RecalculateScenarios();
        }

        private void BuildUI()
        {
            WpfGrid root = new WpfGrid();
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Header
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Main Content
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Footer

            // ── Row 0: Header ──
            Border header = new Border
            {
                Background = new SolidColorBrush(COL_SURFACE),
                BorderBrush = new SolidColorBrush(COL_BORDER),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(24, 12, 24, 12)
            };
            StackPanel hStack = new StackPanel();
            hStack.Children.Add(new WpfTextBlock
            {
                Text = "⚡ Neural Generative Zoning & Real-Time Massing Morphing Engine",
                FontSize = 17,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(COL_TEXT_MAIN)
            });
            hStack.Children.Add(new WpfTextBlock
            {
                Text = "Live parametric synaptic sliders • Real-time volumetric morphing • Instant scenario clustering & Design Options baking.",
                FontSize = 11.5,
                Foreground = new SolidColorBrush(COL_TEXT_MUTED),
                Margin = new Thickness(0, 2, 0, 0)
            });
            header.Child = hStack;
            WpfGrid.SetRow(header, 0);
            root.Children.Add(header);

            // ── Row 1: 3-Column Workspace ──
            WpfGrid mainGrid = new WpfGrid { Margin = new Thickness(20, 14, 20, 14) };
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(320) }); // Left: Sliders
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(14) });
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.3, GridUnitType.Star) }); // Center: Neural Synapses & Scenarios
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(14) });
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.15, GridUnitType.Star) }); // Right: 3D Preview

            mainGrid.Children.Add(CreateLiveSlidersPanel());
            mainGrid.Children.Add(CreateCenterScenariosPanel());
            mainGrid.Children.Add(CreateInteractive3DPanel());

            WpfGrid.SetRow(mainGrid, 1);
            root.Children.Add(mainGrid);

            // ── Row 2: Footer ──
            root.Children.Add(CreateFooterBar());

            Content = root;
        }

        private UIElement CreateLiveSlidersPanel()
        {
            Border card = CreateCard();
            WpfGrid.SetColumn(card, 0);

            ScrollViewer scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            StackPanel sp = new StackPanel();

            sp.Children.Add(new WpfTextBlock
            {
                Text = "🎛️ LIVE SYNAPTIC ZONING SLIDERS",
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(COL_TEXT_MUTED),
                Margin = new Thickness(0, 0, 0, 10)
            });

            // 1. Regulatory Parameters
            AddSliderField(sp, "Lot Area (SF):", 3000, 60000, _inputs.LotAreaSqFt, true, "N0", v => { _inputs.LotAreaSqFt = v; OnLiveParamChanged(); });
            AddSliderField(sp, "Base Allowable FAR:", 1.0, 18.0, _inputs.BaseFar, false, "N2", v => { _inputs.BaseFar = v; OnLiveParamChanged(); });
            AddSliderField(sp, "Max Building Height (FT):", 60, 500, _inputs.MaxHeightFt, true, "N0", v => { _inputs.MaxHeightFt = v; OnLiveParamChanged(); });
            AddSliderField(sp, "Street Front Setback (FT):", 0, 35, _inputs.SetbackFrontFt, true, "N0", v => { _inputs.SetbackFrontFt = v; OnLiveParamChanged(); });
            AddSliderField(sp, "Rear Yard Setback (FT):", 10, 45, _inputs.SetbackRearFt, true, "N0", v => { _inputs.SetbackRearFt = v; OnLiveParamChanged(); });
            AddSliderField(sp, "Side Yard Setbacks (FT):", 0, 25, _inputs.SetbackSidesFt, true, "N0", v => { _inputs.SetbackSidesFt = v; OnLiveParamChanged(); });

            Border sep1 = new Border { Height = 1, Background = new SolidColorBrush(COL_BORDER), Margin = new Thickness(0, 6, 0, 10) };
            sp.Children.Add(sep1);

            sp.Children.Add(new WpfTextBlock
            {
                Text = "🏛️ BASE, DORMERS & TOWER DRIVERS",
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(COL_TEXT_MUTED),
                Margin = new Thickness(0, 0, 0, 10)
            });

            AddSliderField(sp, "Base / Podium Floors:", 1, 6, _inputs.PodiumFloors, true, "N0", v => { _inputs.PodiumFloors = (int)v; OnLiveParamChanged(); });
            AddSliderField(sp, "Base Lot Coverage (%):", 40, 100, _inputs.PodiumCoveragePercent, true, "N0", v => { _inputs.PodiumCoveragePercent = v; OnLiveParamChanged(); });
            AddSliderField(sp, "Dormer Transition Floors:", 0, 4, _inputs.DormerFloors, true, "N0", v => { _inputs.DormerFloors = (int)v; OnLiveParamChanged(); });
            AddSliderField(sp, "Dormer Setback Step (FT):", 4, 25, _inputs.DormerSetbackDepthFt, true, "N0", v => { _inputs.DormerSetbackDepthFt = v; OnLiveParamChanged(); });
            AddSliderField(sp, "Tower Lot Coverage (%):", 20, 75, _inputs.TowerCoveragePercent, true, "N0", v => { _inputs.TowerCoveragePercent = v; OnLiveParamChanged(); });
            AddSliderField(sp, "Floor-to-Floor Height (FT):", 9.5, 16.0, _inputs.FloorHeightTower, false, "N1", v => { _inputs.FloorHeightTower = v; OnLiveParamChanged(); });
            AddSliderField(sp, "Luxury Penthouse Floors:", 0, 4, _inputs.PenthouseFloors, true, "N0", v => { _inputs.PenthouseFloors = (int)v; OnLiveParamChanged(); });
            AddSliderField(sp, "Mandatory Housing (MIH %):", 0, 50, _inputs.MihPercent, true, "N0", v => { _inputs.MihPercent = v; OnLiveParamChanged(); });

            scroll.Content = sp;
            card.Child = scroll;
            return card;
        }

        private UIElement CreateCenterScenariosPanel()
        {
            Border card = CreateCard();
            WpfGrid.SetColumn(card, 2);

            WpfGrid cGrid = new WpfGrid();
            cGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Header
            cGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Cards List

            StackPanel cHdr = new StackPanel { Margin = new Thickness(0, 0, 0, 10) };
            cHdr.Children.Add(new WpfTextBlock
            {
                Text = "🧠 ACTIVE SCENARIOS & NEURAL CLUSTERS",
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(COL_TEXT_MUTED)
            });
            cHdr.Children.Add(new WpfTextBlock
            {
                Text = "Click any card to load its shape into the 3D visualizer, or use checkboxes to select masses to bake.",
                FontSize = 10.5,
                Foreground = new SolidColorBrush(COL_TEXT_MUTED)
            });
            WpfGrid.SetRow(cHdr, 0);
            cGrid.Children.Add(cHdr);

            ScrollViewer scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            _scenarioCardsContainer = new StackPanel();
            scroll.Content = _scenarioCardsContainer;
            WpfGrid.SetRow(scroll, 1);
            cGrid.Children.Add(scroll);

            card.Child = cGrid;
            return card;
        }

        private UIElement CreateInteractive3DPanel()
        {
            Border card = CreateCard();
            WpfGrid.SetColumn(card, 4);

            WpfGrid pGrid = new WpfGrid();
            pGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Title & View Cube Toolbar
            pGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // HUD Badges
            pGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // 3D Viewport

            // Header & View Orientations
            WpfGrid topBar = new WpfGrid { Margin = new Thickness(0, 0, 0, 6) };
            topBar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            topBar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            StackPanel prevHdr = new StackPanel();
            _txtPreviewTitle = new WpfTextBlock
            {
                Text = "🏢 Interactive 3D Massing Viewport",
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(COL_TEXT_MAIN)
            };
            _txtPreviewMetrics = new WpfTextBlock
            {
                Text = "Drag left-mouse to orbit 360° • Scroll wheel to zoom.",
                FontSize = 10,
                Foreground = new SolidColorBrush(COL_TEXT_MUTED)
            };
            prevHdr.Children.Add(_txtPreviewTitle);
            prevHdr.Children.Add(_txtPreviewMetrics);
            WpfGrid.SetColumn(prevHdr, 0);
            topBar.Children.Add(prevHdr);

            // Orientation Preset Buttons
            StackPanel cubeBar = new StackPanel { Orientation = WpfOrientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            cubeBar.Children.Add(CreateOrientationButton("🏛️ Front", () => SetCameraOrientation(0.0, 15.0)));
            cubeBar.Children.Add(CreateOrientationButton("🏢 Rear", () => SetCameraOrientation(180.0, 15.0)));
            cubeBar.Children.Add(CreateOrientationButton("📐 3D Orbit", () => SetCameraOrientation(45.0, 35.0)));
            cubeBar.Children.Add(CreateOrientationButton("⬆️ Top", () => SetCameraOrientation(0.0, 89.0)));

            WpfGrid.SetColumn(cubeBar, 1);
            topBar.Children.Add(cubeBar);
            WpfGrid.SetRow(topBar, 0);
            pGrid.Children.Add(topBar);

            // HUD Badges Row
            WpfGrid hudGrid = new WpfGrid { Margin = new Thickness(0, 4, 0, 8) };
            hudGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            hudGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(6) });
            hudGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            hudGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(6) });
            hudGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            _badgeZfaStatus = CreateHudBadge("ZFA CAP", out _txtZfaStatus);
            WpfGrid.SetColumn(_badgeZfaStatus, 0);
            hudGrid.Children.Add(_badgeZfaStatus);

            _badgeHeightStatus = CreateHudBadge("HEIGHT", out _txtHeightStatus);
            WpfGrid.SetColumn(_badgeHeightStatus, 2);
            hudGrid.Children.Add(_badgeHeightStatus);

            _badgeRevenueStatus = CreateHudBadge("EST. PROFORMA", out _txtRevenueStatus);
            WpfGrid.SetColumn(_badgeRevenueStatus, 4);
            hudGrid.Children.Add(_badgeRevenueStatus);

            WpfGrid.SetRow(hudGrid, 1);
            pGrid.Children.Add(hudGrid);

            // 3D Viewport Host
            Border viewportHost = new Border
            {
                Background = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#0F172A")),
                BorderBrush = new SolidColorBrush(COL_BORDER),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                ClipToBounds = true
            };

            _viewport3D = new Viewport3D { ClipToBounds = true };
            _camera = new PerspectiveCamera
            {
                FieldOfView = 45,
                NearPlaneDistance = 1.0,
                FarPlaneDistance = 2000.0
            };
            _viewport3D.Camera = _camera;

            Model3DGroup lightsGroup = new Model3DGroup();
            lightsGroup.Children.Add(new AmbientLight(WpfColor.FromRgb(120, 130, 150)));
            lightsGroup.Children.Add(new DirectionalLight(WpfColor.FromRgb(255, 255, 255), new Vector3D(-1, -2, -3)));
            lightsGroup.Children.Add(new DirectionalLight(WpfColor.FromRgb(160, 180, 200), new Vector3D(2, 1, -1)));

            ModelVisual3D lightsVisual = new ModelVisual3D { Content = lightsGroup };
            _viewport3D.Children.Add(lightsVisual);

            _modelVisual = new ModelVisual3D();
            _viewport3D.Children.Add(_modelVisual);

            viewportHost.MouseLeftButtonDown += (s, e) =>
            {
                _isOrbiting = true;
                _lastMousePos = e.GetPosition(viewportHost);
                viewportHost.CaptureMouse();
            };

            viewportHost.MouseLeftButtonUp += (s, e) =>
            {
                _isOrbiting = false;
                viewportHost.ReleaseMouseCapture();
            };

            viewportHost.MouseMove += (s, e) =>
            {
                if (_isOrbiting)
                {
                    System.Windows.Point currentPos = e.GetPosition(viewportHost);
                    double dx = currentPos.X - _lastMousePos.X;
                    double dy = currentPos.Y - _lastMousePos.Y;

                    _cameraTheta -= dx * 0.6;
                    _cameraPhi = Math.Max(5.0, Math.Min(88.0, _cameraPhi + (dy * 0.5)));

                    _lastMousePos = currentPos;
                    UpdateCameraPosition();
                }
            };

            viewportHost.MouseWheel += (s, e) =>
            {
                double delta = e.Delta > 0 ? -25.0 : 25.0;
                _cameraDistance = Math.Max(80.0, Math.Min(700.0, _cameraDistance + delta));
                UpdateCameraPosition();
            };

            viewportHost.Child = _viewport3D;
            WpfGrid.SetRow(viewportHost, 2);
            pGrid.Children.Add(viewportHost);

            UpdateCameraPosition();
            card.Child = pGrid;
            return card;
        }

        private WpfButton CreateOrientationButton(string text, Action onClick)
        {
            WpfButton btn = new WpfButton
            {
                Content = text,
                Height = 24,
                FontSize = 9.5,
                FontWeight = FontWeights.SemiBold,
                Background = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#F1F5F9")),
                Foreground = new SolidColorBrush(COL_TEXT_MAIN),
                BorderBrush = new SolidColorBrush(COL_BORDER),
                BorderThickness = new Thickness(1),
                Margin = new Thickness(0, 0, 4, 0),
                Padding = new Thickness(6, 0, 6, 0),
                Cursor = Cursors.Hand
            };
            btn.Click += (s, e) => onClick();
            return btn;
        }

        private void SetCameraOrientation(double theta, double phi)
        {
            _cameraTheta = theta;
            _cameraPhi = phi;
            UpdateCameraPosition();
        }

        private void UpdateCameraPosition()
        {
            if (_camera == null) return;

            double radTheta = _cameraTheta * Math.PI / 180.0;
            double radPhi = _cameraPhi * Math.PI / 180.0;

            double x = _cameraDistance * Math.Cos(radPhi) * Math.Sin(radTheta);
            double y = -_cameraDistance * Math.Cos(radPhi) * Math.Cos(radTheta);
            double z = _cameraDistance * Math.Sin(radPhi);

            double targetZ = _activeScenario != null ? _activeScenario.TotalHeightFt * 0.45 : 70.0;

            _camera.Position = new Point3D(x, y, z + targetZ);
            _camera.LookDirection = new Vector3D(-x, -y, targetZ - (_camera.Position.Z));
            _camera.UpDirection = new Vector3D(0, 0, 1);
        }

        private Border CreateHudBadge(string label, out WpfTextBlock valueText)
        {
            Border b = new Border
            {
                Background = new SolidColorBrush(COL_SURFACE),
                BorderBrush = new SolidColorBrush(COL_BORDER),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(5),
                Padding = new Thickness(6, 4, 6, 4)
            };

            StackPanel sp = new StackPanel();
            sp.Children.Add(new WpfTextBlock { Text = label, FontSize = 8.5, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(COL_TEXT_MUTED) });
            valueText = new WpfTextBlock { Text = "-", FontSize = 11, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(COL_TEXT_MAIN) };
            sp.Children.Add(valueText);
            b.Child = sp;
            return b;
        }

        private UIElement CreateFooterBar()
        {
            Border footer = new Border
            {
                Background = new SolidColorBrush(COL_SURFACE),
                BorderBrush = new SolidColorBrush(COL_BORDER),
                BorderThickness = new Thickness(0, 1, 0, 0),
                Padding = new Thickness(24, 10, 24, 10)
            };

            WpfGrid fGrid = new WpfGrid();
            fGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Options
            fGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Action Button

            StackPanel optStack = new StackPanel { Orientation = WpfOrientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            
            _chkDesignOptions = new WpfCheckBox
            {
                Content = "Assign to Revit Design Options",
                IsChecked = true,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 16, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            optStack.Children.Add(_chkDesignOptions);

            _chkCreateLevels = new WpfCheckBox
            {
                Content = "Auto-Generate Project Levels",
                IsChecked = true,
                FontWeight = FontWeights.Medium,
                Margin = new Thickness(0, 0, 16, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            optStack.Children.Add(_chkCreateLevels);

            WpfGrid.SetColumn(optStack, 0);
            fGrid.Children.Add(optStack);

            WpfButton btnBake = CreatePrimaryButton("🚀 Bake Selected Masses into Revit Project");
            btnBake.Height = 36;
            btnBake.Padding = new Thickness(20, 0, 20, 0);
            btnBake.Click += (s, e) => ExecuteBakeIntoRevit();
            WpfGrid.SetColumn(btnBake, 1);
            fGrid.Children.Add(btnBake);

            footer.Child = fGrid;
            WpfGrid.SetRow(footer, 2);
            return footer;
        }

        private void OnLiveParamChanged()
        {
            RecalculateScenarios();
        }

        private void RecalculateScenarios()
        {
            string previousActiveId = _activeScenario != null ? _activeScenario.Id : "scenario_interactive_custom";
            _scenarios = _solver.SolveScenarios(_inputs);

            _activeScenario = _scenarios.FirstOrDefault(s => s.Id == previousActiveId) ?? _scenarios[0];

            RefreshScenarioCardsUI();
            Render3DIsometricMassing();
            UpdateHudKpis();
            UpdateCameraPosition();
        }

        private void UpdateHudKpis()
        {
            if (_activeScenario == null) return;

            if (_txtZfaStatus != null)
            {
                _txtZfaStatus.Text = string.Format("{0:N0} SF ({1:N1}%)", _activeScenario.TotalZfa, _activeScenario.FarUtilizationPercent);
                _txtZfaStatus.Foreground = _activeScenario.FarUtilizationPercent > 100.0 ? new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#DC2626")) : new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#059669"));
            }

            if (_txtHeightStatus != null)
            {
                _txtHeightStatus.Text = string.Format("{0:N0} FT ({1} FL)", _activeScenario.TotalHeightFt, _activeScenario.TotalFloors);
                _txtHeightStatus.Foreground = _activeScenario.IsHeightExceeded ? new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#DC2626")) : new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#0284C7"));
            }

            if (_txtRevenueStatus != null)
            {
                _txtRevenueStatus.Text = string.Format("${0:N1}M", _activeScenario.EstimatedRevenueMillions);
                _txtRevenueStatus.Foreground = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#059669"));
            }
        }

        private void RefreshScenarioCardsUI()
        {
            if (_scenarioCardsContainer == null) return;
            _scenarioCardsContainer.Children.Clear();

            foreach (GenerativeScenario s in _scenarios)
            {
                GenerativeScenario cur = s;
                bool isActive = (cur == _activeScenario);

                Border c = new Border
                {
                    Background = new SolidColorBrush(isActive ? (WpfColor)ColorConverter.ConvertFromString("#EFF6FF") : COL_SURFACE),
                    BorderBrush = new SolidColorBrush(isActive ? COL_PRIMARY : COL_BORDER),
                    BorderThickness = new Thickness(isActive ? 1.5 : 1),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(12, 8, 12, 8),
                    Margin = new Thickness(0, 0, 0, 6),
                    Cursor = Cursors.Hand
                };

                WpfGrid g = new WpfGrid();
                g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Checkbox
                g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Info
                g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Metrics

                WpfCheckBox chkBake = new WpfCheckBox
                {
                    IsChecked = cur.IsSelectedForBake,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 10, 0)
                };
                chkBake.Checked += (snd, ea) => cur.IsSelectedForBake = true;
                chkBake.Unchecked += (snd, ea) => cur.IsSelectedForBake = false;
                WpfGrid.SetColumn(chkBake, 0);
                g.Children.Add(chkBake);

                StackPanel tSp = new StackPanel();
                tSp.Children.Add(new WpfTextBlock
                {
                    Text = string.Format("{0} {1}", cur.Icon, cur.Title),
                    FontWeight = FontWeights.Bold,
                    FontSize = 12,
                    Foreground = new SolidColorBrush(COL_TEXT_MAIN)
                });
                tSp.Children.Add(new WpfTextBlock
                {
                    Text = cur.Subtitle,
                    FontSize = 9.5,
                    Foreground = new SolidColorBrush(COL_TEXT_MUTED)
                });
                WpfGrid.SetColumn(tSp, 1);
                g.Children.Add(tSp);

                StackPanel kSp = new StackPanel { HorizontalAlignment = HorizontalAlignment.Right };
                kSp.Children.Add(new WpfTextBlock
                {
                    Text = string.Format("{0:N0} SF", cur.TotalZfa),
                    FontWeight = FontWeights.Bold,
                    FontSize = 11.5,
                    Foreground = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString(cur.ColorHex ?? "#2563EB")),
                    HorizontalAlignment = HorizontalAlignment.Right
                });
                kSp.Children.Add(new WpfTextBlock
                {
                    Text = string.Format("{0} FL • {1:N1}% FAR", cur.TotalFloors, cur.FarUtilizationPercent),
                    FontSize = 9,
                    Foreground = new SolidColorBrush(COL_TEXT_MUTED),
                    HorizontalAlignment = HorizontalAlignment.Right
                });
                WpfGrid.SetColumn(kSp, 2);
                g.Children.Add(kSp);

                c.Child = g;

                c.MouseLeftButtonDown += (snd, ea) =>
                {
                    _activeScenario = cur;
                    RefreshScenarioCardsUI();
                    Render3DIsometricMassing();
                    UpdateHudKpis();
                    UpdateCameraPosition();
                };

                _scenarioCardsContainer.Children.Add(c);
            }
        }

        private void Render3DIsometricMassing()
        {
            if (_modelVisual == null || _activeScenario == null) return;

            Model3DGroup buildingGroup = new Model3DGroup();

            // Draw Ground Site Polygon
            double siteW = _inputs.LotWidthFt;
            double siteD = _inputs.LotDepthFt;
            buildingGroup.Children.Add(CreateBox3D(0, 0, -1.0, siteW * 1.15, siteD * 1.15, 1.0, WpfColor.FromRgb(30, 41, 59)));

            // Draw Each Floor Slab
            foreach (MassingFloorBlock f in _activeScenario.Floors)
            {
                WpfColor fCol = (WpfColor)ColorConverter.ConvertFromString(f.ColorHex ?? "#3B82F6");
                double elevation = f.ElevationFt;
                double height = f.HeightFt;
                double width = f.WidthFt;
                double depth = f.DepthFt;
                double offsetX = f.OffsetXFt;
                double offsetY = f.OffsetYFt;

                GeometryModel3D floorModel = CreateBox3D(offsetX, offsetY, elevation, width, depth, height - 0.5, fCol);
                buildingGroup.Children.Add(floorModel);
            }

            _modelVisual.Content = buildingGroup;

            _txtPreviewTitle.Text = string.Format("{0} {1}", _activeScenario.Icon, _activeScenario.Title);
            _txtPreviewMetrics.Text = string.Format("Total ZFA: {0:N0} SF | {1} Floors ({2:N0} FT Total Height)\nBase: {3} FL | Dormers: {4} FL | Tower: {5} FL | Est. MIH: {6} Units",
                _activeScenario.TotalZfa, _activeScenario.TotalFloors, _activeScenario.TotalHeightFt,
                _activeScenario.PodiumFloors, _activeScenario.DormerFloors, _activeScenario.TowerFloors, _activeScenario.MihUnitsEstimate);
        }

        private GeometryModel3D CreateBox3D(double centerX, double centerY, double baseZ, double width, double depth, double height, WpfColor color)
        {
            double halfW = width / 2.0;
            double halfD = depth / 2.0;

            Point3D p0 = new Point3D(centerX - halfW, centerY - halfD, baseZ);
            Point3D p1 = new Point3D(centerX + halfW, centerY - halfD, baseZ);
            Point3D p2 = new Point3D(centerX + halfW, centerY + halfD, baseZ);
            Point3D p3 = new Point3D(centerX - halfW, centerY + halfD, baseZ);

            Point3D p4 = new Point3D(centerX - halfW, centerY - halfD, baseZ + height);
            Point3D p5 = new Point3D(centerX + halfW, centerY - halfD, baseZ + height);
            Point3D p6 = new Point3D(centerX + halfW, centerY + halfD, baseZ + height);
            Point3D p7 = new Point3D(centerX - halfW, centerY + halfD, baseZ + height);

            MeshGeometry3D mesh = new MeshGeometry3D();

            // Bottom
            AddQuad(mesh, p0, p3, p2, p1);
            // Top
            AddQuad(mesh, p4, p5, p6, p7);
            // Front (Street)
            AddQuad(mesh, p0, p1, p5, p4);
            // Back (Yard)
            AddQuad(mesh, p2, p3, p7, p6);
            // Right (Side)
            AddQuad(mesh, p1, p2, p6, p5);
            // Left (Side)
            AddQuad(mesh, p3, p0, p4, p7);

            MaterialGroup mat = new MaterialGroup();
            mat.Children.Add(new DiffuseMaterial(new SolidColorBrush(color)));
            mat.Children.Add(new SpecularMaterial(new SolidColorBrush(WpfColor.FromArgb(80, 255, 255, 255)), 20.0));

            return new GeometryModel3D(mesh, mat);
        }

        private void AddQuad(MeshGeometry3D mesh, Point3D p0, Point3D p1, Point3D p2, Point3D p3)
        {
            int baseIdx = mesh.Positions.Count;
            mesh.Positions.Add(p0);
            mesh.Positions.Add(p1);
            mesh.Positions.Add(p2);
            mesh.Positions.Add(p3);

            mesh.TriangleIndices.Add(baseIdx);
            mesh.TriangleIndices.Add(baseIdx + 1);
            mesh.TriangleIndices.Add(baseIdx + 2);

            mesh.TriangleIndices.Add(baseIdx);
            mesh.TriangleIndices.Add(baseIdx + 2);
            mesh.TriangleIndices.Add(baseIdx + 3);
        }

        private void ExecuteBakeIntoRevit()
        {
            List<GenerativeScenario> toBake = _scenarios.Where(s => s.IsSelectedForBake).ToList();
            if (toBake.Count == 0)
            {
                MessageBox.Show("Please select at least one scenario checkbox to bake into Revit.", "BauTools Generative", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            bool createDO = _chkDesignOptions.IsChecked == true;
            bool createLevels = _chkCreateLevels.IsChecked == true;

            try
            {
                int shapes = _bakerService.BakeScenariosIntoDesignOptions(toBake, createDO, createLevels, "BauTools Generative Zoning");
                string msg = string.Format("Successfully baked {0} massing element(s) across {1} scenario(s) into Revit!", shapes, toBake.Count);
                MessageBox.Show(msg, "BauTools — Massing Bake Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error baking massing options: " + ex.Message, "Bake Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddSliderField(StackPanel parent, string label, double min, double max, double val, bool isInt, string fmt, Action<double> onVal)
        {
            StackPanel sp = new StackPanel { Margin = new Thickness(0, 0, 0, 8) };
            
            WpfGrid hg = new WpfGrid();
            hg.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            hg.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            WpfTextBlock lbl = new WpfTextBlock { Text = label, FontSize = 9.5, Foreground = new SolidColorBrush(COL_TEXT_MUTED) };
            WpfGrid.SetColumn(lbl, 0);
            hg.Children.Add(lbl);

            WpfTextBlock valBubble = new WpfTextBlock { Text = val.ToString(fmt), FontSize = 10, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(COL_PRIMARY) };
            WpfGrid.SetColumn(valBubble, 1);
            hg.Children.Add(valBubble);

            sp.Children.Add(hg);

            WpfSlider sld = new WpfSlider
            {
                Minimum = min,
                Maximum = max,
                Value = val,
                IsSnapToTickEnabled = isInt,
                TickFrequency = isInt ? 1.0 : 0.25,
                Margin = new Thickness(0, 2, 0, 0)
            };

            sld.ValueChanged += (s, e) =>
            {
                double v = isInt ? Math.Round(sld.Value) : sld.Value;
                valBubble.Text = v.ToString(fmt);
                onVal(v);
            };

            sp.Children.Add(sld);
            parent.Children.Add(sp);
        }

        private Border CreateCard()
        {
            return new Border
            {
                Background = new SolidColorBrush(COL_SURFACE),
                BorderBrush = new SolidColorBrush(COL_BORDER),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(14)
            };
        }

        private WpfButton CreatePrimaryButton(string text)
        {
            WpfButton btn = new WpfButton
            {
                Content = text,
                Background = new SolidColorBrush(COL_PRIMARY),
                Foreground = WpfBrushes.White,
                FontWeight = FontWeights.SemiBold,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand
            };
            return btn;
        }
    }
}