using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace ZoningFloorArea.Views
{
    public class BubbleHeadsWindow : Window
    {
        private readonly Document _doc;
        private readonly Autodesk.Revit.DB.View _activeView;
        private readonly List<Autodesk.Revit.DB.Grid> _grids;
        private readonly List<Level> _levels;

        // UI Controls
        private System.Windows.Controls.CheckBox _chkGrids;
        private System.Windows.Controls.CheckBox _chkLevels;

        // Radio buttons for End0
        private System.Windows.Controls.RadioButton _rbEnd0Show;
        private System.Windows.Controls.RadioButton _rbEnd0Hide;
        private System.Windows.Controls.RadioButton _rbEnd0Keep;

        // Radio buttons for End1
        private System.Windows.Controls.RadioButton _rbEnd1Show;
        private System.Windows.Controls.RadioButton _rbEnd1Hide;
        private System.Windows.Controls.RadioButton _rbEnd1Keep;

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
        private static readonly System.Windows.Media.Color COL_DANGER    = (System.Windows.Media.Color)ColorConverter.ConvertFromString("#DC2626");

        public BubbleHeadsWindow(Document doc, Autodesk.Revit.DB.View activeView)
        {
            _doc = doc;
            _activeView = activeView;

            // Collect elements in active view
            _grids = new FilteredElementCollector(_doc, _activeView.Id)
                .OfClass(typeof(Autodesk.Revit.DB.Grid))
                .Cast<Autodesk.Revit.DB.Grid>()
                .ToList();

            _levels = new FilteredElementCollector(_doc, _activeView.Id)
                .OfClass(typeof(Level))
                .Cast<Level>()
                .ToList();

            Title = "BauTools — Bubble Heads & Datum Manager (Active View)";
            Height = 620;
            Width = 720;
            MinHeight = 560;
            MinWidth = 650;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Background = new SolidColorBrush(COL_BG);
            FontFamily = new System.Windows.Media.FontFamily("Segoe UI");
            FontSize = 13;

            BuildUI();
            UpdateSummary();
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
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Header
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // View Info Bar
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Elements Target Card
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Presets Card
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Manual Options Card
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Footer Actions

            // 0. HEADER
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
            badge.Child = new TextBlock { Text = "BUBBLES & DATUMS", FontWeight = FontWeights.ExtraBold, FontSize = 12, Foreground = System.Windows.Media.Brushes.White };
            logoLine.Children.Add(badge);

            logoLine.Children.Add(new TextBlock
            {
                Text = "BauTools — Bubble Heads & Datum Visibility",
                FontSize = 17,
                FontWeight = FontWeights.Bold,
                Foreground = System.Windows.Media.Brushes.White,
                VerticalAlignment = VerticalAlignment.Center
            });
            titlePanel.Children.Add(logoLine);

            titlePanel.Children.Add(new TextBlock
            {
                Text = "Show or Hide bubble heads for Grids and Levels exclusively in the active view.",
                FontSize = 11,
                Foreground = new SolidColorBrush((System.Windows.Media.Color)ColorConverter.ConvertFromString("#94A3B8")),
                Margin = new Thickness(0, 4, 0, 0)
            });
            hGrid.Children.Add(titlePanel);

            headerBar.Child = hGrid;
            System.Windows.Controls.Grid.SetRow(headerBar, 0);
            root.Children.Add(headerBar);

            // 1. ACTIVE VIEW INFO BAR
            Border viewInfoBar = new Border
            {
                Background = new SolidColorBrush((System.Windows.Media.Color)ColorConverter.ConvertFromString("#E2E8F0")),
                Padding = new Thickness(20, 8, 20, 8)
            };

            StackPanel vPanel = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal };
            vPanel.Children.Add(new TextBlock { Text = "👁️ ACTIVE VIEW: ", FontWeight = FontWeights.Bold, FontSize = 11.5, Foreground = darkBrush });
            vPanel.Children.Add(new TextBlock { Text = string.Format("{0} ", _activeView.Name), FontWeight = FontWeights.Bold, FontSize = 11.5, Foreground = accentBrush });
            vPanel.Children.Add(new TextBlock { Text = string.Format("({0})", _activeView.ViewType), FontSize = 11.5, Foreground = mutedBrush });

            viewInfoBar.Child = vPanel;
            System.Windows.Controls.Grid.SetRow(viewInfoBar, 1);
            root.Children.Add(viewInfoBar);

            // 2. TARGET ELEMENTS CARD
            Border targetCard = new Border
            {
                Background = cardBrush,
                BorderBrush = borderBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Margin = new Thickness(16, 12, 16, 6),
                Padding = new Thickness(16, 12, 16, 12)
            };

            StackPanel targetPanel = new StackPanel();
            targetPanel.Children.Add(new TextBlock { Text = "1. ELEMENTS TO MODIFY IN ACTIVE VIEW:", FontWeight = FontWeights.Bold, FontSize = 11.5, Foreground = darkBrush, Margin = new Thickness(0, 0, 0, 8) });

            StackPanel chkRow = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal };

            _chkGrids = new System.Windows.Controls.CheckBox
            {
                Content = string.Format("Grids ({0} in view)", _grids.Count),
                IsChecked = _grids.Count > 0,
                IsEnabled = _grids.Count > 0,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 24, 0)
            };
            _chkGrids.Checked += (s, e) => UpdateSummary();
            _chkGrids.Unchecked += (s, e) => UpdateSummary();

            _chkLevels = new System.Windows.Controls.CheckBox
            {
                Content = string.Format("Levels ({0} in view)", _levels.Count),
                IsChecked = _levels.Count > 0,
                IsEnabled = _levels.Count > 0,
                FontWeight = FontWeights.SemiBold
            };
            _chkLevels.Checked += (s, e) => UpdateSummary();
            _chkLevels.Unchecked += (s, e) => UpdateSummary();

            chkRow.Children.Add(_chkGrids);
            chkRow.Children.Add(_chkLevels);
            targetPanel.Children.Add(chkRow);

            targetCard.Child = targetPanel;
            System.Windows.Controls.Grid.SetRow(targetCard, 2);
            root.Children.Add(targetCard);

            // 3. QUICK PRESETS CARD
            Border presetCard = new Border
            {
                Background = cardBrush,
                BorderBrush = borderBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Margin = new Thickness(16, 6, 16, 6),
                Padding = new Thickness(16, 12, 16, 12)
            };

            StackPanel presetPanel = new StackPanel();
            presetPanel.Children.Add(new TextBlock { Text = "2. QUICK PRESETS (1-CLICK CONFIGURATION):", FontWeight = FontWeights.Bold, FontSize = 11.5, Foreground = darkBrush, Margin = new Thickness(0, 0, 0, 8) });

            System.Windows.Controls.Grid pGrid = new System.Windows.Controls.Grid();
            pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Preset 1: Only Left
            System.Windows.Controls.Button btnPresetLeft = CreatePresetButton("⬅ Only Left / End 0\n(Show 0, Hide 1)", () => {
                _rbEnd0Show.IsChecked = true;
                _rbEnd1Hide.IsChecked = true;
            });
            System.Windows.Controls.Grid.SetColumn(btnPresetLeft, 0);
            pGrid.Children.Add(btnPresetLeft);

            // Preset 2: Only Right
            System.Windows.Controls.Button btnPresetRight = CreatePresetButton("➡ Only Right / End 1\n(Hide 0, Show 1)", () => {
                _rbEnd0Hide.IsChecked = true;
                _rbEnd1Show.IsChecked = true;
            });
            System.Windows.Controls.Grid.SetColumn(btnPresetRight, 1);
            pGrid.Children.Add(btnPresetRight);

            // Preset 3: Both Ends
            System.Windows.Controls.Button btnPresetBoth = CreatePresetButton("↔ Both Ends\n(Show 0 & 1)", () => {
                _rbEnd0Show.IsChecked = true;
                _rbEnd1Show.IsChecked = true;
            });
            System.Windows.Controls.Grid.SetColumn(btnPresetBoth, 2);
            pGrid.Children.Add(btnPresetBoth);

            // Preset 4: Turn OFF All
            System.Windows.Controls.Button btnPresetOff = CreatePresetButton("🚫 Turn OFF All\n(Hide 0 & 1)", () => {
                _rbEnd0Hide.IsChecked = true;
                _rbEnd1Hide.IsChecked = true;
            }, true);
            System.Windows.Controls.Grid.SetColumn(btnPresetOff, 3);
            pGrid.Children.Add(btnPresetOff);

            presetPanel.Children.Add(pGrid);
            presetCard.Child = presetPanel;
            System.Windows.Controls.Grid.SetRow(presetCard, 3);
            root.Children.Add(presetCard);

            // 4. MANUAL DETAILED OPTIONS CARD
            Border manualCard = new Border
            {
                Background = cardBrush,
                BorderBrush = borderBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Margin = new Thickness(16, 6, 16, 12),
                Padding = new Thickness(16, 12, 16, 12)
            };

            StackPanel manualPanel = new StackPanel();
            manualPanel.Children.Add(new TextBlock { Text = "3. DETAILED END CONFIGURATION:", FontWeight = FontWeights.Bold, FontSize = 11.5, Foreground = darkBrush, Margin = new Thickness(0, 0, 0, 10) });

            System.Windows.Controls.Grid mGrid = new System.Windows.Controls.Grid();
            mGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            mGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Column 0: End 0
            StackPanel col0 = new StackPanel { Margin = new Thickness(0, 0, 10, 0) };
            col0.Children.Add(new TextBlock { Text = "End 0 (Left / Bottom / Start):", FontWeight = FontWeights.SemiBold, FontSize = 11, Foreground = darkBrush, Margin = new Thickness(0, 0, 0, 6) });

            _rbEnd0Show = new System.Windows.Controls.RadioButton { Content = "🟢 Show Bubble", GroupName = "End0", IsChecked = true, Margin = new Thickness(0, 2, 0, 4) };
            _rbEnd0Hide = new System.Windows.Controls.RadioButton { Content = "🔴 Hide Bubble", GroupName = "End0", Margin = new Thickness(0, 2, 0, 4) };
            _rbEnd0Keep = new System.Windows.Controls.RadioButton { Content = "⚪ Keep unchanged", GroupName = "End0", Margin = new Thickness(0, 2, 0, 4) };

            _rbEnd0Show.Checked += (s, e) => UpdateSummary();
            _rbEnd0Hide.Checked += (s, e) => UpdateSummary();
            _rbEnd0Keep.Checked += (s, e) => UpdateSummary();

            col0.Children.Add(_rbEnd0Show);
            col0.Children.Add(_rbEnd0Hide);
            col0.Children.Add(_rbEnd0Keep);
            System.Windows.Controls.Grid.SetColumn(col0, 0);
            mGrid.Children.Add(col0);

            // Column 1: End 1
            StackPanel col1 = new StackPanel { Margin = new Thickness(10, 0, 0, 0) };
            col1.Children.Add(new TextBlock { Text = "End 1 (Right / Top / End):", FontWeight = FontWeights.SemiBold, FontSize = 11, Foreground = darkBrush, Margin = new Thickness(0, 0, 0, 6) });

            _rbEnd1Show = new System.Windows.Controls.RadioButton { Content = "🟢 Show Bubble", GroupName = "End1", Margin = new Thickness(0, 2, 0, 4) };
            _rbEnd1Hide = new System.Windows.Controls.RadioButton { Content = "🔴 Hide Bubble", GroupName = "End1", IsChecked = true, Margin = new Thickness(0, 2, 0, 4) };
            _rbEnd1Keep = new System.Windows.Controls.RadioButton { Content = "⚪ Keep unchanged", GroupName = "End1", Margin = new Thickness(0, 2, 0, 4) };

            _rbEnd1Show.Checked += (s, e) => UpdateSummary();
            _rbEnd1Hide.Checked += (s, e) => UpdateSummary();
            _rbEnd1Keep.Checked += (s, e) => UpdateSummary();

            col1.Children.Add(_rbEnd1Show);
            col1.Children.Add(_rbEnd1Hide);
            col1.Children.Add(_rbEnd1Keep);
            System.Windows.Controls.Grid.SetColumn(col1, 1);
            mGrid.Children.Add(col1);

            manualPanel.Children.Add(mGrid);
            manualCard.Child = manualPanel;
            System.Windows.Controls.Grid.SetRow(manualCard, 4);
            root.Children.Add(manualCard);

            // 5. FOOTER ACTIONS
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
                Text = "Ready.",
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

            System.Windows.Controls.Button btnCancel = new System.Windows.Controls.Button
            {
                Content = "Cancel",
                Padding = new Thickness(14, 8, 14, 8),
                Margin = new Thickness(0, 0, 10, 0),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            btnCancel.Click += (s, e) => Close();
            btnPanel.Children.Add(btnCancel);

            System.Windows.Controls.Button btnApply = new System.Windows.Controls.Button
            {
                Content = "✔ Apply Changes",
                Padding = new Thickness(20, 8, 20, 8),
                Background = accentBrush,
                Foreground = System.Windows.Media.Brushes.White,
                FontWeight = FontWeights.Bold,
                Cursor = System.Windows.Input.Cursors.Hand,
                BorderThickness = new Thickness(0)
            };
            btnApply.Click += (s, e) => ApplyChanges();
            btnPanel.Children.Add(btnApply);

            System.Windows.Controls.Grid.SetColumn(btnPanel, 1);
            footGrid.Children.Add(btnPanel);

            footer.Child = footGrid;
            System.Windows.Controls.Grid.SetRow(footer, 5);
            root.Children.Add(footer);

            Content = root;
        }

        private System.Windows.Controls.Button CreatePresetButton(string text, Action onClick, bool isDanger = false)
        {
            System.Windows.Controls.Button btn = new System.Windows.Controls.Button
            {
                Content = text,
                Padding = new Thickness(8, 8, 8, 8),
                Margin = new Thickness(3, 0, 3, 0),
                FontWeight = FontWeights.SemiBold,
                FontSize = 11,
                Cursor = System.Windows.Input.Cursors.Hand,
                Background = isDanger ? new SolidColorBrush((System.Windows.Media.Color)ColorConverter.ConvertFromString("#FEE2E2")) : new SolidColorBrush((System.Windows.Media.Color)ColorConverter.ConvertFromString("#EFF6FF")),
                BorderBrush = isDanger ? new SolidColorBrush((System.Windows.Media.Color)ColorConverter.ConvertFromString("#FCA5A5")) : new SolidColorBrush((System.Windows.Media.Color)ColorConverter.ConvertFromString("#BFDBFE")),
                BorderThickness = new Thickness(1)
            };
            btn.Click += (s, e) => onClick();
            return btn;
        }

        private void UpdateSummary()
        {
            if (_statusSummary == null) return;

            int targetCount = 0;
            if (_chkGrids != null && _chkGrids.IsChecked == true) targetCount += _grids.Count;
            if (_chkLevels != null && _chkLevels.IsChecked == true) targetCount += _levels.Count;

            string end0Action = (_rbEnd0Show != null && _rbEnd0Show.IsChecked == true) ? "Show End 0" : ((_rbEnd0Hide != null && _rbEnd0Hide.IsChecked == true) ? "Hide End 0" : "Keep End 0");
            string end1Action = (_rbEnd1Show != null && _rbEnd1Show.IsChecked == true) ? "Show End 1" : ((_rbEnd1Hide != null && _rbEnd1Hide.IsChecked == true) ? "Hide End 1" : "Keep End 1");

            _statusSummary.Text = string.Format("⚡ {0} element(s) selected. Action: {1} | {2}.", targetCount, end0Action, end1Action);
        }

        private void ApplyChanges()
        {
            bool modifyGrids = _chkGrids.IsChecked == true;
            bool modifyLevels = _chkLevels.IsChecked == true;

            if (!modifyGrids && !modifyLevels)
            {
                MessageBox.Show("Please select at least one element category (Grids or Levels).", "BauTools", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int end0Mode = _rbEnd0Show.IsChecked == true ? 1 : (_rbEnd0Hide.IsChecked == true ? -1 : 0); // 1 = Show, -1 = Hide, 0 = Keep
            int end1Mode = _rbEnd1Show.IsChecked == true ? 1 : (_rbEnd1Hide.IsChecked == true ? -1 : 0);

            if (end0Mode == 0 && end1Mode == 0)
            {
                MessageBox.Show("Both ends are set to 'Keep unchanged'. No modifications to apply.", "BauTools", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            int countModified = 0;

            using (Transaction tx = new Transaction(_doc, "BauTools: Toggle Bubble Heads"))
            {
                tx.Start();

                // Process Grids
                if (modifyGrids)
                {
                    foreach (var g in _grids)
                    {
                        try
                        {
                            if (end0Mode == 1) g.ShowBubbleInView(DatumEnds.End0, _activeView);
                            else if (end0Mode == -1) g.HideBubbleInView(DatumEnds.End0, _activeView);

                            if (end1Mode == 1) g.ShowBubbleInView(DatumEnds.End1, _activeView);
                            else if (end1Mode == -1) g.HideBubbleInView(DatumEnds.End1, _activeView);

                            countModified++;
                        }
                        catch
                        {
                        }
                    }
                }

                // Process Levels
                if (modifyLevels)
                {
                    foreach (Level l in _levels)
                    {
                        try
                        {
                            if (end0Mode == 1) l.ShowBubbleInView(DatumEnds.End0, _activeView);
                            else if (end0Mode == -1) l.HideBubbleInView(DatumEnds.End0, _activeView);

                            if (end1Mode == 1) l.ShowBubbleInView(DatumEnds.End1, _activeView);
                            else if (end1Mode == -1) l.HideBubbleInView(DatumEnds.End1, _activeView);

                            countModified++;
                        }
                        catch
                        {
                        }
                    }
                }

                tx.Commit();
            }

            TaskDialog.Show("BauTools - Bubble Heads",
                string.Format("✅ Updated bubbles on {0} element(s) in active view '{1}'.", countModified, _activeView.Name));

            DialogResult = true;
            Close();
        }
    }
}
