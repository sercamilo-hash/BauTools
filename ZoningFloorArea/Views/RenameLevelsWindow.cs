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
    public class RenameLevelsWindow : Window
    {
        private readonly Document _doc;
        private readonly List<LevelRenameItem> _allItems;
        private readonly ObservableCollection<LevelRenameItem> _displayItems;

        // UI Controls
        private System.Windows.Controls.ComboBox _baseLevelCombo;
        private System.Windows.Controls.TextBox _floorCountTxt;
        private System.Windows.Controls.CheckBox _chkIncludeRoof;
        private System.Windows.Controls.CheckBox _chkIncludeBulkhead;
        private System.Windows.Controls.CheckBox _chkTwoDigits;
        private System.Windows.Controls.DataGrid _dataGrid;
        private System.Windows.Controls.TextBlock _statusSummary;

        // Color Palette matching BauTools
        private static readonly System.Windows.Media.Color COL_BG        = (System.Windows.Media.Color)ColorConverter.ConvertFromString("#F1F5F9");
        private static readonly System.Windows.Media.Color COL_CARD      = System.Windows.Media.Colors.White;
        private static readonly System.Windows.Media.Color COL_DARK      = (System.Windows.Media.Color)ColorConverter.ConvertFromString("#0F172A");
        private static readonly System.Windows.Media.Color COL_ACCENT    = (System.Windows.Media.Color)ColorConverter.ConvertFromString("#2563EB");
        private static readonly System.Windows.Media.Color COL_ACCENT2   = (System.Windows.Media.Color)ColorConverter.ConvertFromString("#0284C7");
        private static readonly System.Windows.Media.Color COL_MUTED     = (System.Windows.Media.Color)ColorConverter.ConvertFromString("#64748B");
        private static readonly System.Windows.Media.Color COL_BORDER    = (System.Windows.Media.Color)ColorConverter.ConvertFromString("#CBD5E1");
        private static readonly System.Windows.Media.Color COL_HEADER_BG = (System.Windows.Media.Color)ColorConverter.ConvertFromString("#1E293B");
        private static readonly System.Windows.Media.Color COL_SUCCESS   = (System.Windows.Media.Color)ColorConverter.ConvertFromString("#16A34A");

        public RenameLevelsWindow(Document doc)
        {
            _doc = doc;
            _allItems = new List<LevelRenameItem>();
            _displayItems = new ObservableCollection<LevelRenameItem>();

            Title = "BauTools — Rename Levels (Ordinal & Cellar/Roof)";
            Height = 720;
            Width = 980;
            MinHeight = 550;
            MinWidth = 750;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Background = new SolidColorBrush(COL_BG);
            FontFamily = new System.Windows.Media.FontFamily("Segoe UI");
            FontSize = 13;

            LoadLevelsFromDocument();
            BuildUI();
            RecalculateNames();
        }

        private void LoadLevelsFromDocument()
        {
            var levels = new FilteredElementCollector(_doc)
                .OfClass(typeof(Level))
                .Cast<Level>()
                .OrderBy(l => l.Elevation)
                .ToList();

            foreach (var lvl in levels)
            {
                // Format elevation nicely (imperial ft-in or metric depending on unit)
                string elevStr = FormatElevation(lvl.Elevation);
                var item = new LevelRenameItem(lvl, elevStr);
                _allItems.Add(item);
                _displayItems.Add(item);
            }
        }

        private string FormatElevation(double rawFeet)
        {
            try
            {
                #if REVIT2021_OR_GREATER || true
                return UnitFormatUtils.Format(_doc.GetUnits(), SpecTypeId.Length, rawFeet, false);
                #else
                return string.Format("{0:F2} ft", rawFeet);
                #endif
            }
            catch
            {
                return string.Format("{0:F2}'", rawFeet);
            }
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
            SolidColorBrush successBrush  = new SolidColorBrush(COL_SUCCESS);

            System.Windows.Controls.Grid root = new System.Windows.Controls.Grid();
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Header
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Config Options Card
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // DataGrid Card
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Footer Actions

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
            badge.Child = new TextBlock { Text = "LEVELS", FontWeight = FontWeights.ExtraBold, FontSize = 12, Foreground = System.Windows.Media.Brushes.White };
            logoLine.Children.Add(badge);

            logoLine.Children.Add(new TextBlock
            {
                Text = "BauTools — Automatic Level Renamer",
                FontSize = 17,
                FontWeight = FontWeights.Bold,
                Foreground = System.Windows.Media.Brushes.White,
                VerticalAlignment = VerticalAlignment.Center
            });
            titlePanel.Children.Add(logoLine);

            titlePanel.Children.Add(new TextBlock
            {
                Text = "Renombra niveles con nomenclatura ordinal (01 1ST FL., 02 2ND FL.), Cellar bajo 0, Roof y Bulkhead.",
                FontSize = 11,
                Foreground = new SolidColorBrush((System.Windows.Media.Color)ColorConverter.ConvertFromString("#94A3B8")),
                Margin = new Thickness(0, 4, 0, 0)
            });
            hGrid.Children.Add(titlePanel);

            headerBar.Child = hGrid;
            System.Windows.Controls.Grid.SetRow(headerBar, 0);
            root.Children.Add(headerBar);

            // ══════════════════════════════════════════════════════════
            // 1. CONFIGURATION CARD
            // ══════════════════════════════════════════════════════════
            Border configCard = new Border
            {
                Background = cardBrush,
                BorderBrush = borderBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Margin = new Thickness(16, 14, 16, 8),
                Padding = new Thickness(18, 14, 18, 14)
            };

            System.Windows.Controls.Grid configGrid = new System.Windows.Controls.Grid();
            configGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
            configGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.2, GridUnitType.Star) });
            configGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.8, GridUnitType.Star) });

            // Column 0: Base Level (Ground / 1st floor)
            StackPanel col0 = new StackPanel { Margin = new Thickness(0, 0, 16, 0) };
            col0.Children.Add(new TextBlock { Text = "NIVEL BASE (PLANTA BAJA / 01 1ST FL.):", FontWeight = FontWeights.SemiBold, FontSize = 11, Foreground = darkBrush, Margin = new Thickness(0, 0, 0, 6) });

            _baseLevelCombo = new System.Windows.Controls.ComboBox
            {
                ItemsSource = _allItems,
                DisplayMemberPath = "CurrentName",
                Height = 32,
                VerticalContentAlignment = VerticalAlignment.Center
            };
            // Select default: first level >= 0 or index 0
            int defaultBaseIdx = _allItems.FindIndex(x => x.RawElevation >= -0.001);
            _baseLevelCombo.SelectedIndex = defaultBaseIdx >= 0 ? defaultBaseIdx : 0;
            _baseLevelCombo.SelectionChanged += (s, e) => RecalculateNames();
            col0.Children.Add(_baseLevelCombo);
            System.Windows.Controls.Grid.SetColumn(col0, 0);
            configGrid.Children.Add(col0);

            // Column 1: Number of floors
            StackPanel col1 = new StackPanel { Margin = new Thickness(0, 0, 16, 0) };
            col1.Children.Add(new TextBlock { Text = "CANTIDAD DE PISOS:", FontWeight = FontWeights.SemiBold, FontSize = 11, Foreground = darkBrush, Margin = new Thickness(0, 0, 0, 6) });

            StackPanel floorCountPanel = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal };
            _floorCountTxt = new System.Windows.Controls.TextBox
            {
                Width = 60,
                Height = 32,
                TextAlignment = TextAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center,
                FontWeight = FontWeights.Bold,
                FontSize = 14
            };
            
            // Calculate initial sensible default for floor count
            int initialFloors = Math.Max(1, _allItems.Count - Math.Max(0, defaultBaseIdx) - 2);
            _floorCountTxt.Text = initialFloors.ToString();
            _floorCountTxt.TextChanged += (s, e) => RecalculateNames();

            System.Windows.Controls.Button btnMinus = new System.Windows.Controls.Button { Content = "−", Width = 30, Height = 32, FontWeight = FontWeights.Bold, Margin = new Thickness(6, 0, 2, 0) };
            btnMinus.Click += (s, e) => {
                int val;
                if (int.TryParse(_floorCountTxt.Text, out val) && val > 1) {
                    _floorCountTxt.Text = (val - 1).ToString();
                }
            };

            System.Windows.Controls.Button btnPlus = new System.Windows.Controls.Button { Content = "+", Width = 30, Height = 32, FontWeight = FontWeights.Bold, Margin = new Thickness(2, 0, 0, 0) };
            btnPlus.Click += (s, e) => {
                int val;
                if (int.TryParse(_floorCountTxt.Text, out val)) {
                    _floorCountTxt.Text = (val + 1).ToString();
                }
            };

            floorCountPanel.Children.Add(_floorCountTxt);
            floorCountPanel.Children.Add(btnMinus);
            floorCountPanel.Children.Add(btnPlus);
            col1.Children.Add(floorCountPanel);
            System.Windows.Controls.Grid.SetColumn(col1, 1);
            configGrid.Children.Add(col1);

            // Column 2: Checkboxes
            StackPanel col2 = new StackPanel();
            col2.Children.Add(new TextBlock { Text = "OPCIONES DE REMATE Y FORMATO:", FontWeight = FontWeights.SemiBold, FontSize = 11, Foreground = darkBrush, Margin = new Thickness(0, 0, 0, 6) });

            _chkIncludeRoof = new System.Windows.Controls.CheckBox { Content = "Incluir ROOF (sobre el último piso)", IsChecked = true, Margin = new Thickness(0, 2, 0, 4) };
            _chkIncludeRoof.Checked += (s, e) => RecalculateNames();
            _chkIncludeRoof.Unchecked += (s, e) => RecalculateNames();

            _chkIncludeBulkhead = new System.Windows.Controls.CheckBox { Content = "Incluir BULKHEAD (sobre Roof)", IsChecked = true, Margin = new Thickness(0, 2, 0, 4) };
            _chkIncludeBulkhead.Checked += (s, e) => RecalculateNames();
            _chkIncludeBulkhead.Unchecked += (s, e) => RecalculateNames();

            _chkTwoDigits = new System.Windows.Controls.CheckBox { Content = "Prefijo 2 dígitos (01 1ST FL., 00 CELLAR)", IsChecked = true, Margin = new Thickness(0, 2, 0, 0) };
            _chkTwoDigits.Checked += (s, e) => RecalculateNames();
            _chkTwoDigits.Unchecked += (s, e) => RecalculateNames();

            col2.Children.Add(_chkIncludeRoof);
            col2.Children.Add(_chkIncludeBulkhead);
            col2.Children.Add(_chkTwoDigits);
            System.Windows.Controls.Grid.SetColumn(col2, 2);
            configGrid.Children.Add(col2);

            configCard.Child = configGrid;
            System.Windows.Controls.Grid.SetRow(configCard, 1);
            root.Children.Add(configCard);

            // ══════════════════════════════════════════════════════════
            // 2. LIVE PREVIEW DATAGRID CARD
            // ══════════════════════════════════════════════════════════
            Border gridCard = new Border
            {
                Background = cardBrush,
                BorderBrush = borderBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Margin = new Thickness(16, 4, 16, 10),
                Padding = new Thickness(12)
            };

            System.Windows.Controls.Grid tableContainer = new System.Windows.Controls.Grid();
            tableContainer.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            tableContainer.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            TextBlock tableTitle = new TextBlock
            {
                Text = "VISTA PREVIA EN VIVO (Puedes hacer doble clic en 'Nombre Propuesto' para editarlo manualmente):",
                FontWeight = FontWeights.Bold,
                FontSize = 11,
                Foreground = mutedBrush,
                Margin = new Thickness(4, 0, 0, 8)
            };
            System.Windows.Controls.Grid.SetRow(tableTitle, 0);
            tableContainer.Children.Add(tableTitle);

            _dataGrid = new System.Windows.Controls.DataGrid
            {
                ItemsSource = _displayItems,
                AutoGenerateColumns = false,
                CanUserAddRows = false,
                CanUserDeleteRows = false,
                CanUserSortColumns = false,
                GridLinesVisibility = DataGridGridLinesVisibility.Horizontal,
                HorizontalGridLinesBrush = new SolidColorBrush((System.Windows.Media.Color)ColorConverter.ConvertFromString("#E2E8F0")),
                RowHeight = 32,
                FontSize = 12.5,
                BorderThickness = new Thickness(1),
                BorderBrush = borderBrush,
                Background = System.Windows.Media.Brushes.White,
                AlternatingRowBackground = new SolidColorBrush((System.Windows.Media.Color)ColorConverter.ConvertFromString("#F8FAFC"))
            };

            // Checkbox Column
            var chkCol = new DataGridCheckBoxColumn
            {
                Header = "Aplicar",
                Binding = new System.Windows.Data.Binding("IsSelected") { UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged },
                Width = 65
            };
            _dataGrid.Columns.Add(chkCol);

            // Elevation Column
            var elevCol = new DataGridTextColumn
            {
                Header = "Elevación",
                Binding = new System.Windows.Data.Binding("ElevationDisplay"),
                IsReadOnly = true,
                Width = 110
            };
            _dataGrid.Columns.Add(elevCol);

            // Current Name Column
            var currCol = new DataGridTextColumn
            {
                Header = "Nombre Actual",
                Binding = new System.Windows.Data.Binding("CurrentName"),
                IsReadOnly = true,
                Width = new DataGridLength(1, DataGridLengthUnitType.Star)
            };
            _dataGrid.Columns.Add(currCol);

            // Proposed Name Column (Editable)
            var propCol = new DataGridTextColumn
            {
                Header = "Nombre Propuesto ✏️ (Editable)",
                Binding = new System.Windows.Data.Binding("ProposedName") { UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged },
                IsReadOnly = false,
                Width = new DataGridLength(1.3, DataGridLengthUnitType.Star)
            };
            _dataGrid.Columns.Add(propCol);

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
                Text = string.Format("{0} niveles detectados.", _allItems.Count),
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
                Content = "Restablecer",
                Padding = new Thickness(14, 8, 14, 8),
                Margin = new Thickness(0, 0, 10, 0),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            btnReset.Click += (s, e) => RecalculateNames();
            btnPanel.Children.Add(btnReset);

            System.Windows.Controls.Button btnCancel = new System.Windows.Controls.Button
            {
                Content = "Cancelar",
                Padding = new Thickness(14, 8, 14, 8),
                Margin = new Thickness(0, 0, 10, 0),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            btnCancel.Click += (s, e) => Close();
            btnPanel.Children.Add(btnCancel);

            System.Windows.Controls.Button btnApply = new System.Windows.Controls.Button
            {
                Content = "✔ Aplicar Renombrado",
                Padding = new Thickness(18, 8, 18, 8),
                Background = accentBrush,
                Foreground = System.Windows.Media.Brushes.White,
                FontWeight = FontWeights.Bold,
                Cursor = System.Windows.Input.Cursors.Hand,
                BorderThickness = new Thickness(0)
            };
            btnApply.Click += (s, e) => ApplyRenaming();
            btnPanel.Children.Add(btnApply);

            System.Windows.Controls.Grid.SetColumn(btnPanel, 1);
            footGrid.Children.Add(btnPanel);

            footer.Child = footGrid;
            System.Windows.Controls.Grid.SetRow(footer, 3);
            root.Children.Add(footer);

            Content = root;
        }

        private void RecalculateNames()
        {
            if (_baseLevelCombo == null || _allItems.Count == 0) return;

            var baseItem = _baseLevelCombo.SelectedItem as LevelRenameItem ?? _allItems[0];
            int f;
            int floors = (_floorCountTxt != null && int.TryParse(_floorCountTxt.Text, out f)) ? Math.Max(1, f) : 1;
            bool roof = (_chkIncludeRoof != null ? _chkIncludeRoof.IsChecked : true) ?? true;
            bool bulkhead = (_chkIncludeBulkhead != null ? _chkIncludeBulkhead.IsChecked : true) ?? true;
            bool twoDigits = (_chkTwoDigits != null ? _chkTwoDigits.IsChecked : true) ?? true;

            LevelRenamerService.CalculateProposedNames(
                _allItems,
                baseItem,
                floors,
                roof,
                bulkhead,
                twoDigits);

            // Update UI status
            int changeCount = _allItems.Count(x => x.IsSelected && x.IsChanged);
            if (_statusSummary != null)
            {
                _statusSummary.Text = string.Format("⚡ {0} de {1} nivel(es) cambiarán de nombre.", changeCount, _allItems.Count);
            }

            if (_dataGrid != null)
            {
                _dataGrid.Items.Refresh();
            }
        }

        private void ApplyRenaming()
        {
            int toChange = _allItems.Count(x => x.IsSelected && x.IsChanged);
            if (toChange == 0)
            {
                MessageBox.Show("No hay cambios pendientes de renombrado para aplicar.",
                    "BauTools - Rename Levels", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var confirm = MessageBox.Show(
                string.Format("¿Estás seguro de que deseas renombrar {0} nivel(es)?\n\n" +
                "Revit actualizará los nombres de los niveles seleccionados.", toChange),
                "Confirmar Renombrado",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes) return;

            var result = LevelRenamerService.ApplyRenaming(_doc, _allItems);
            int renamedCount = result.Item1;
            List<string> errors = result.Item2;

            if (errors.Count > 0)
            {
                string msg = string.Format("Se renombraron {0} nivel(es) con algunas advertencias:\n\n{1}",
                             renamedCount,
                             string.Join("\n", errors.Take(5)));
                if (errors.Count > 5)
                {
                    msg += string.Format("\n...y {0} más.", errors.Count - 5);
                }

                MessageBox.Show(msg, "BauTools - Resultado", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else
            {
                MessageBox.Show(string.Format("✅ ¡Éxito! Se renombraron correctamente {0} nivel(es).", renamedCount),
                    "BauTools - Rename Levels", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            DialogResult = true;
            Close();
        }
    }
}
