using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Autodesk.Revit.DB;
using ZoningFloorArea.Models;
using ZoningFloorArea.Services;

namespace ZoningFloorArea.ViewModels
{
    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Predicate<object> _canExecute;

        public RelayCommand(Action<object> execute, Predicate<object> canExecute)
        {
            if (execute == null) throw new ArgumentNullException("execute");
            _execute = execute;
            _canExecute = canExecute;
        }

        public RelayCommand(Action<object> execute) : this(execute, null)
        {
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute(parameter);
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public void Execute(object parameter)
        {
            _execute(parameter);
        }
    }

    public class LevelPickerItem
    {
        public string LevelName { get; set; }
        public bool IsAvailable { get; set; }
        public string OccupiedByGroupName { get; set; }

        public string DisplayText
        {
            get
            {
                if (IsAvailable)
                {
                    return LevelName;
                }
                return string.Format("{0}  🔒 (In: {1})", LevelName, OccupiedByGroupName);
            }
        }

        public override string ToString()
        {
            return DisplayText;
        }
    }

    public class BuildingFilterItem : INotifyPropertyChanged
    {
        private string _name;
        public string Name
        {
            get { return _name; }
            set { _name = value; OnPropertyChanged("Name"); }
        }

        private bool _isSelected;
        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                _isSelected = value;
                OnPropertyChanged("IsSelected");
                if (SelectionChanged != null) SelectionChanged();
            }
        }

        public Action SelectionChanged;

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class LevelTowerItem : INotifyPropertyChanged
    {
        private string _levelName;
        private double _elevation;
        private string _elevationDisplay;
        private string _assignedGroupName;
        private string _colorHex;
        private bool _isSingleFloor;

        public string LevelName
        {
            get { return _levelName; }
            set { _levelName = value; OnPropertyChanged("LevelName"); }
        }

        public double Elevation
        {
            get { return _elevation; }
            set { _elevation = value; OnPropertyChanged("Elevation"); }
        }

        public string ElevationDisplay
        {
            get { return _elevationDisplay; }
            set { _elevationDisplay = value; OnPropertyChanged("ElevationDisplay"); }
        }

        public string AssignedGroupName
        {
            get { return _assignedGroupName; }
            set { _assignedGroupName = value; OnPropertyChanged("AssignedGroupName"); }
        }

        public string ColorHex
        {
            get { return _colorHex; }
            set { _colorHex = value; OnPropertyChanged("ColorHex"); }
        }

        public bool IsSingleFloor
        {
            get { return _isSingleFloor; }
            set { _isSingleFloor = value; OnPropertyChanged("IsSingleFloor"); }
        }

        public bool IsAssigned
        {
            get { return !string.IsNullOrEmpty(_assignedGroupName); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propName)
        {
            if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(propName));
        }
    }

    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly Document _doc;
        private readonly RevitAreaExtractor _extractor;
        private readonly ZoningCalculator _calculator;
        private readonly RevitSheetTableDrawer _sheetDrawer;
        private readonly RevitAreaDuplicator _duplicator;
        private readonly TypicalFloorStorageService _storageService;
        private readonly RevitViewGeneratorService _viewGenService;
        private readonly RevitSheetPlacementService _sheetPlaceService;
        private readonly ExcelZoningBridgeService _excelBridgeService;
        private readonly SmartScaleAdvisorService _scaleAdvisor;

        public MappingConfig Config { get; set; }
        public ObservableCollection<string> AreaSchemes { get; set; }
        public ObservableCollection<string> AvailableParameters { get; set; }
        public ObservableCollection<string> AvailableLevels { get; set; }
        public ObservableCollection<string> AvailableScopeBoxes { get; set; }
        public ObservableCollection<string> AvailableViewParameters { get; set; }
        public ObservableCollection<SheetItem> AvailableSheets { get; set; }
        public ObservableCollection<BuildingFilterItem> BuildingItems { get; set; }
        public ObservableCollection<ZoningTableResult> DisplayedTables { get; set; }

        public ObservableCollection<BuildingDefinition> Buildings { get; set; }
        public ObservableCollection<LevelTowerItem> TowerLevels { get; set; }
        public List<GeneratedViewResult> LastGeneratedViews { get; set; }

        private SheetItem _selectedSheet;
        public SheetItem SelectedSheet
        {
            get { return _selectedSheet; }
            set { _selectedSheet = value; OnPropertyChanged("SelectedSheet"); }
        }

        public Action<string, bool> OnToastNotification;

        private BuildingDefinition _selectedBuilding;
        public BuildingDefinition SelectedBuilding
        {
            get { return _selectedBuilding; }
            set
            {
                if (_selectedBuilding != value)
                {
                    _selectedBuilding = value;
                    OnPropertyChanged("SelectedBuilding");
                    OnPropertyChanged("TypicalGroups");
                    RefreshTowerLevels();
                }
            }
        }

        public ObservableCollection<TypicalFloorGroup> TypicalGroups
        {
            get
            {
                return _selectedBuilding != null ? _selectedBuilding.TypicalGroups : new ObservableCollection<TypicalFloorGroup>();
            }
        }

        private int _currentStep;
        public int CurrentStep
        {
            get { return _currentStep; }
            set
            {
                if (_currentStep != value)
                {
                    _currentStep = value;
                    OnPropertyChanged("CurrentStep");
                }
            }
        }

        private bool _propagateGrossArea;
        public bool PropagateGrossArea
        {
            get { return _propagateGrossArea; }
            set { _propagateGrossArea = value; OnPropertyChanged("PropagateGrossArea"); }
        }

        private bool _propagateDeductionsArea;
        public bool PropagateDeductionsArea
        {
            get { return _propagateDeductionsArea; }
            set { _propagateDeductionsArea = value; OnPropertyChanged("PropagateDeductionsArea"); }
        }

        public ObservableCollection<TitleblockItem> AvailableTitleblocks { get; set; }
        private TitleblockItem _selectedTitleblock;
        public TitleblockItem SelectedTitleblock
        {
            get { return _selectedTitleblock; }
            set { _selectedTitleblock = value; OnPropertyChanged("SelectedTitleblock"); }
        }

        public ObservableCollection<ViewTemplateItem> AvailableViewTemplates { get; set; }
        public ObservableCollection<PackageSetting> PackageSettings { get; set; }
        public ObservableCollection<PlannedSheet> PlannedSheets { get; set; }

        private SheetLayoutMode _selectedLayoutMode;
        public SheetLayoutMode SelectedLayoutMode
        {
            get { return _selectedLayoutMode; }
            set
            {
                _selectedLayoutMode = value;
                OnPropertyChanged("SelectedLayoutMode");
                ComputePlannedSheets();
            }
        }

        private int _selectedViewScale;
        public int SelectedViewScale
        {
            get { return _selectedViewScale; }
            set { _selectedViewScale = value; OnPropertyChanged("SelectedViewScale"); }
        }

        private bool _onlyTypicalRanges;
        public bool OnlyTypicalRanges
        {
            get { return _onlyTypicalRanges; }
            set
            {
                _onlyTypicalRanges = value;
                OnPropertyChanged("OnlyTypicalRanges");
                ComputePlannedSheets();
            }
        }

        private bool _repositionIfExists;
        public bool RepositionIfExists
        {
            get { return _repositionIfExists; }
            set { _repositionIfExists = value; OnPropertyChanged("RepositionIfExists"); }
        }

        private ZoningLotData _lotData;
        public ZoningLotData LotData
        {
            get { return _lotData; }
            set
            {
                _lotData = value;
                OnPropertyChanged("LotData");
                EvaluateCompliance();
            }
        }

        private ZoningComplianceReport _complianceReport;
        public ZoningComplianceReport ComplianceReport
        {
            get { return _complianceReport; }
            set
            {
                _complianceReport = value;
                OnPropertyChanged("ComplianceReport");
            }
        }

        private ProjectZoningResult _projectResult;
        public ProjectZoningResult ProjectResult
        {
            get { return _projectResult; }
            set
            {
                _projectResult = value;
                OnPropertyChanged("ProjectResult");
            }
        }

        private ZoningTableResult _selectedTableResult;
        public ZoningTableResult SelectedTableResult
        {
            get { return _selectedTableResult; }
            set
            {
                _selectedTableResult = value;
                OnPropertyChanged("SelectedTableResult");
            }
        }

        private string _statusMessage;
        public string StatusMessage
        {
            get { return _statusMessage; }
            set
            {
                _statusMessage = value;
                OnPropertyChanged("StatusMessage");
            }
        }

        public ICommand CalculateCommand { get; private set; }
        public ICommand ExportExcelCommand { get; private set; }
        public ICommand CreateRevitViewsCommand { get; private set; }
        public ICommand PropagateAreasCommand { get; private set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public MainViewModel(Document doc)
        {
            _doc = doc;
            _extractor = new RevitAreaExtractor(doc);
            _calculator = new ZoningCalculator();
            _sheetDrawer = new RevitSheetTableDrawer(doc);
            _duplicator = new RevitAreaDuplicator(doc);
            _storageService = new TypicalFloorStorageService();
            _viewGenService = new RevitViewGeneratorService(doc);
            _sheetPlaceService = new RevitSheetPlacementService(doc);
            _excelBridgeService = new ExcelZoningBridgeService();
            _scaleAdvisor = new SmartScaleAdvisorService();
            _lotData = new ZoningLotData();
            _complianceReport = new ZoningComplianceReport();

            Config = new MappingConfig();
            AreaSchemes = new ObservableCollection<string>();
            AvailableParameters = new ObservableCollection<string>();
            AvailableLevels = new ObservableCollection<string>();
            AvailableScopeBoxes = new ObservableCollection<string>();
            AvailableViewParameters = new ObservableCollection<string>();
            AvailableSheets = new ObservableCollection<SheetItem>();
            BuildingItems = new ObservableCollection<BuildingFilterItem>();
            DisplayedTables = new ObservableCollection<ZoningTableResult>();
            TowerLevels = new ObservableCollection<LevelTowerItem>();
            LastGeneratedViews = new List<GeneratedViewResult>();

            _propagateGrossArea = true;
            _propagateDeductionsArea = true;
            _currentStep = 0; // Step 1 default

            InitializeData();

            CalculateCommand = new RelayCommand(p => CalculateTable());
            ExportExcelCommand = new RelayCommand(p => ExportToExcel());
            CreateRevitViewsCommand = new RelayCommand(p => CreateDraftingViews());
            PropagateAreasCommand = new RelayCommand(p => PropagateAreasFromTypicalGroups());
        }

        private void InitializeData()
        {
            try
            {
                // 1. Schemes
                List<string> schemes = _extractor.GetAreaSchemeNames();
                foreach (string s in schemes) AreaSchemes.Add(s);

                string grossMatch = schemes.FirstOrDefault(s => s.IndexOf("Gross", StringComparison.OrdinalIgnoreCase) >= 0);
                Config.GrossAreaSchemeName = grossMatch ?? (schemes.Count > 0 ? schemes[0] : string.Empty);

                string dedMatch = schemes.FirstOrDefault(s => s.IndexOf("Deduction", StringComparison.OrdinalIgnoreCase) >= 0 || s.IndexOf("Rentable", StringComparison.OrdinalIgnoreCase) >= 0);
                Config.DeductionAreaSchemeName = dedMatch ?? (schemes.Count > 1 ? schemes[1] : Config.GrossAreaSchemeName);

                // 2. Parameters
                List<string> paramsList = _extractor.GetAvailableAreaParameters();
                foreach (string p in paramsList) AvailableParameters.Add(p);

                string dedParam = paramsList.FirstOrDefault(p =>
                    string.Equals(p, "Deductions", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(p, "Deduction", StringComparison.OrdinalIgnoreCase) ||
                    p.IndexOf("Deduction", StringComparison.OrdinalIgnoreCase) >= 0);
                Config.DeductionTypeParameterName = dedParam ?? (paramsList.Contains("Name") ? "Name" : (paramsList.Count > 0 ? paramsList[0] : "Deductions"));

                string bldgParam = paramsList.FirstOrDefault(p => p.IndexOf("Building", StringComparison.OrdinalIgnoreCase) >= 0);
                Config.BuildingParameterName = bldgParam ?? "Building";

                string usageParam = paramsList.FirstOrDefault(p => p.IndexOf("Usage", StringComparison.OrdinalIgnoreCase) >= 0 || p.IndexOf("Category", StringComparison.OrdinalIgnoreCase) >= 0);
                Config.UsageCategoryParameterName = usageParam ?? "UsageCategory";

                // 3. Scope Boxes & View Parameters
                List<string> sBoxes = _viewGenService.GetAvailableScopeBoxes();
                foreach (string sb in sBoxes) AvailableScopeBoxes.Add(sb);

                List<string> vParams = _viewGenService.GetAvailableViewStringParameters();
                foreach (string vp in vParams) AvailableViewParameters.Add(vp);

                if (AvailableViewParameters.Contains("Building")) Config.ViewBuildingParameterName = "Building";
                else if (AvailableViewParameters.Contains("Comments")) Config.ViewBuildingParameterName = "Comments";

                // 4. Sheets & Titleblocks
                List<SheetItem> sheets = _sheetPlaceService.GetExistingSheets();
                foreach (SheetItem sh in sheets) AvailableSheets.Add(sh);
                if (AvailableSheets.Count > 0) SelectedSheet = AvailableSheets[0];

                AvailableTitleblocks = new ObservableCollection<TitleblockItem>();
                List<TitleblockItem> tblocks = _sheetPlaceService.GetAvailableTitleblocks();
                foreach (TitleblockItem tb in tblocks) AvailableTitleblocks.Add(tb);
                if (AvailableTitleblocks.Count > 0) SelectedTitleblock = AvailableTitleblocks[0];

                AvailableViewTemplates = new ObservableCollection<ViewTemplateItem>();
                AvailableViewTemplates.Add(new ViewTemplateItem { Name = "(None)", TemplateId = ElementId.InvalidElementId });
                List<ViewTemplateItem> vTemplates = _sheetPlaceService.GetAvailableViewTemplates();
                foreach (ViewTemplateItem vt in vTemplates) AvailableViewTemplates.Add(vt);

                PackageSettings = new ObservableCollection<PackageSetting>
                {
                    new PackageSetting(ViewPackageType.MasterOverall, "Master Overall Plans", "🌐", "M-", 101, SheetLayoutMode.Single1View, 192, "1/16\" = 1'-0\" (1:192)", ViewPlanKind.FloorPlan),
                    new PackageSetting(ViewPackageType.GrossArea, "Gross Area Plans", "📐", "Z-", 101, SheetLayoutMode.Quad4Views, 96, "1/8\" = 1'-0\" (1:96)", ViewPlanKind.AreaPlan, Config.GrossAreaSchemeName),
                    new PackageSetting(ViewPackageType.Deductions, "Deductions Plans", "✂️", "ZD-", 101, SheetLayoutMode.Quad4Views, 96, "1/8\" = 1'-0\" (1:96)", ViewPlanKind.AreaPlan, Config.DeductionAreaSchemeName),
                    new PackageSetting(ViewPackageType.EgressLifeSafety, "Life Safety Plans", "🚨", "LS-", 101, SheetLayoutMode.Dual2Views, 96, "1/8\" = 1'-0\" (1:96)", ViewPlanKind.FloorPlan),
                    new PackageSetting(ViewPackageType.CeilingPlanRCP, "Reflected Ceiling (RCP)", "💡", "RCP-", 101, SheetLayoutMode.Quad4Views, 96, "1/8\" = 1'-0\" (1:96)", ViewPlanKind.CeilingPlan),
                    new PackageSetting(ViewPackageType.Architectural, "Floor Plans", "🏛️", "A-", 101, SheetLayoutMode.Dual2Views, 96, "1/8\" = 1'-0\" (1:96)", ViewPlanKind.FloorPlan)
                };

                PlannedSheets = new ObservableCollection<PlannedSheet>();
                _selectedLayoutMode = SheetLayoutMode.Quad4Views;
                _selectedViewScale = 96;
                _onlyTypicalRanges = true;
                _repositionIfExists = true;

                // 5. Levels
                List<Level> levels = _duplicator.GetAllLevels();
                foreach (Level l in levels)
                {
                    AvailableLevels.Add(l.Name);
                }

                // 6. Load Multi-Buildings from Storage
                List<BuildingDefinition> loadedBldgs = _storageService.LoadBuildings(_doc);
                Buildings = new ObservableCollection<BuildingDefinition>(loadedBldgs);
                SelectedBuilding = Buildings.Count > 0 ? Buildings[0] : new BuildingDefinition("Building 1");

                RefreshTowerLevels();
                ComputePlannedSheets();
                StatusMessage = string.Format("Ready. {0} level(s) loaded across {1} building(s).", AvailableLevels.Count, Buildings.Count);
            }
            catch (Exception ex)
            {
                StatusMessage = "Initialization Error: " + ex.Message;
            }
        }

        public void AddBuilding(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                name = string.Format("Building {0}", Buildings.Count + 1);
            }

            BuildingDefinition newBldg = new BuildingDefinition(name);
            Buildings.Add(newBldg);
            SelectedBuilding = newBldg;
            SaveTypicalGroups();
            StatusMessage = string.Format("Created '{0}'.", name);
            TriggerToast(string.Format("Building '{0}' created.", name), false);
        }

        public BuildingDefinition DuplicateBuilding(BuildingDefinition sourceBuilding, string newName)
        {
            if (string.IsNullOrWhiteSpace(newName))
            {
                newName = string.Format("Building {0}", Buildings.Count + 1);
            }

            BuildingDefinition newBldg = new BuildingDefinition(newName);
            if (sourceBuilding != null)
            {
                newBldg.ScopeBoxName = sourceBuilding.ScopeBoxName;
                foreach (TypicalFloorGroup srcGroup in sourceBuilding.TypicalGroups)
                {
                    TypicalFloorGroup g = new TypicalFloorGroup();
                    g.Name = srcGroup.Name;
                    g.ColorHex = srcGroup.ColorHex;
                    g.IsSingleFloorOnly = srcGroup.IsSingleFloorOnly;
                    g.IsDuplexModule = srcGroup.IsDuplexModule;
                    g.SourceLevelName = srcGroup.SourceLevelName;
                    g.SourceLevelNameLower = srcGroup.SourceLevelNameLower;
                    g.SourceLevelNameUpper = srcGroup.SourceLevelNameUpper;
                    g.FromLevelName = srcGroup.FromLevelName;
                    g.ToLevelName = srcGroup.ToLevelName;
                    g.Order = srcGroup.Order;
                    newBldg.TypicalGroups.Add(g);
                }
            }

            Buildings.Add(newBldg);
            SelectedBuilding = newBldg;
            SaveTypicalGroups();
            RefreshTowerLevels();
            string msg = sourceBuilding != null ? string.Format("Created '{0}' by copying layout from '{1}'.", newName, sourceBuilding.Name) : string.Format("Created '{0}'.", newName);
            StatusMessage = msg;
            TriggerToast(msg, false);
            return newBldg;
        }

        public void CopyGroupsFromBuilding(BuildingDefinition targetBuilding, BuildingDefinition sourceBuilding)
        {
            if (targetBuilding == null || sourceBuilding == null || targetBuilding == sourceBuilding) return;

            targetBuilding.TypicalGroups.Clear();
            foreach (TypicalFloorGroup srcGroup in sourceBuilding.TypicalGroups)
            {
                TypicalFloorGroup g = new TypicalFloorGroup();
                g.Name = srcGroup.Name;
                g.ColorHex = srcGroup.ColorHex;
                g.IsSingleFloorOnly = srcGroup.IsSingleFloorOnly;
                g.IsDuplexModule = srcGroup.IsDuplexModule;
                g.SourceLevelName = srcGroup.SourceLevelName;
                g.SourceLevelNameLower = srcGroup.SourceLevelNameLower;
                g.SourceLevelNameUpper = srcGroup.SourceLevelNameUpper;
                g.FromLevelName = srcGroup.FromLevelName;
                g.ToLevelName = srcGroup.ToLevelName;
                g.Order = srcGroup.Order;
                targetBuilding.TypicalGroups.Add(g);
            }

            SaveTypicalGroups();
            RefreshTowerLevels();
            string msg = string.Format("Copied {0} typical group(s) from '{1}' to '{2}'.", targetBuilding.TypicalGroups.Count, sourceBuilding.Name, targetBuilding.Name);
            StatusMessage = msg;
            TriggerToast(msg, false);
        }

        public void AddCustomPackage(string name, string prefix, ViewPlanKind kind, string schemeName)
        {
            if (string.IsNullOrWhiteSpace(name)) name = string.Format("Custom Package {0}", PackageSettings.Count + 1);
            if (string.IsNullOrWhiteSpace(prefix)) prefix = "C-";

            string icon = (kind == ViewPlanKind.AreaPlan) ? "📐" : (kind == ViewPlanKind.CeilingPlan ? "💡" : "🏢");
            PackageSetting pkg = new PackageSetting(
                ViewPackageType.Custom,
                name.Trim(),
                icon,
                prefix.Trim().ToUpperInvariant(),
                101,
                SheetLayoutMode.Quad4Views,
                96,
                "1/8\" = 1'-0\" (1:96)",
                kind,
                schemeName);
            pkg.IsCustomPackage = true;
            PackageSettings.Add(pkg);
            ComputePlannedSheets();
            string msg = string.Format("Added package '{0}' ({1}).", pkg.DisplayName, kind);
            StatusMessage = msg;
            TriggerToast(msg, false);
        }

        public void RemovePackage(PackageSetting pkg)
        {
            if (pkg != null && PackageSettings.Contains(pkg))
            {
                PackageSettings.Remove(pkg);
                ComputePlannedSheets();
                string msg = string.Format("Removed package '{0}'.", pkg.DisplayName);
                StatusMessage = msg;
                TriggerToast(msg, false);
            }
        }

        public bool CreateDeductionParameterInRevit(string paramName = "Deductions")
        {
            try
            {
                bool ok = _extractor.CreateAreaSharedParameter(paramName);
                if (ok)
                {
                    AvailableParameters.Clear();
                    foreach (string p in _extractor.GetAvailableAreaParameters()) AvailableParameters.Add(p);
                    Config.DeductionTypeParameterName = paramName;
                    CalculateTable();
                    string msg = string.Format("Successfully created and bound '{0}' parameter to Areas in Revit.", paramName);
                    StatusMessage = msg;
                    TriggerToast(msg, false);
                    return true;
                }
                else
                {
                    StatusMessage = "Could not create parameter in Revit.";
                    TriggerToast("Error creating parameter in Revit.", true);
                    return false;
                }
            }
            catch (Exception ex)
            {
                StatusMessage = "Error: " + ex.Message;
                TriggerToast("Parameter Error: " + ex.Message, true);
                return false;
            }
        }

        public string GetNextLevelAbove(string levelName)
        {
            if (string.IsNullOrEmpty(levelName)) return null;
            List<Level> allLevels = _duplicator.GetAllLevels().OrderBy(l => l.Elevation).ToList();
            for (int i = 0; i < allLevels.Count; i++)
            {
                if (string.Equals(allLevels[i].Name, levelName, StringComparison.OrdinalIgnoreCase))
                {
                    if (i + 1 < allLevels.Count)
                    {
                        return allLevels[i + 1].Name;
                    }
                    break;
                }
            }
            return null;
        }

        public void RemoveBuilding(BuildingDefinition bldg)
        {
            if (bldg != null && Buildings.Contains(bldg))
            {
                if (Buildings.Count <= 1)
                {
                    TriggerToast("Project must contain at least one building.", true);
                    return;
                }

                MessageBoxResult res = MessageBox.Show(string.Format("Are you sure you want to delete '{0}' and its typical floor groups?", bldg.Name),
                    "Confirm Delete Building", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (res == MessageBoxResult.Yes)
                {
                    Buildings.Remove(bldg);
                    SelectedBuilding = Buildings[0];
                    SaveTypicalGroups();
                    StatusMessage = string.Format("Deleted '{0}'.", bldg.Name);
                    TriggerToast(string.Format("Deleted '{0}'.", bldg.Name), false);
                }
            }
        }

        public void AddTypicalGroup()
        {
            if (AvailableLevels.Count == 0 || SelectedBuilding == null) return;

            string[] defaultColors = new string[] { "#3B82F6", "#14B8A6", "#F59E0B", "#8B5CF6", "#F43F5E", "#22C55E", "#64748B", "#F97316" };
            int colorIdx = SelectedBuilding.TypicalGroups.Count % defaultColors.Length;

            // Find first unassigned level for this building
            HashSet<string> assigned = GetAssignedLevelsInBuilding(SelectedBuilding, null);
            string firstAvailableLvl = AvailableLevels.FirstOrDefault(l => !assigned.Contains(l));
            if (string.IsNullOrEmpty(firstAvailableLvl))
            {
                firstAvailableLvl = AvailableLevels[0];
            }

            TypicalFloorGroup newGroup = new TypicalFloorGroup();
            newGroup.Name = string.Format("Typical Floor {0}", SelectedBuilding.TypicalGroups.Count + 1);
            newGroup.ColorHex = defaultColors[colorIdx];
            newGroup.SourceLevelName = firstAvailableLvl;
            newGroup.SourceLevelNameLower = firstAvailableLvl;
            newGroup.SourceLevelNameUpper = firstAvailableLvl;
            newGroup.FromLevelName = firstAvailableLvl;
            newGroup.ToLevelName = firstAvailableLvl;
            newGroup.Order = SelectedBuilding.TypicalGroups.Count + 1;

            SelectedBuilding.TypicalGroups.Add(newGroup);
            RefreshTowerLevels();
            StatusMessage = "Added new Typical Floor group.";
            TriggerToast("Added new Typical Floor group.", false);
        }

        public void RemoveTypicalGroup(TypicalFloorGroup group)
        {
            if (group != null && SelectedBuilding != null && SelectedBuilding.TypicalGroups.Contains(group))
            {
                SelectedBuilding.TypicalGroups.Remove(group);
                RefreshTowerLevels();
                StatusMessage = string.Format("Removed group '{0}'.", group.Name);
                TriggerToast(string.Format("Removed group '{0}'.", group.Name), false);
            }
        }

        public HashSet<string> GetAssignedLevelsInBuilding(BuildingDefinition bldg, TypicalFloorGroup excludeGroup)
        {
            HashSet<string> set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (bldg == null || bldg.TypicalGroups == null) return set;

            foreach (TypicalFloorGroup g in bldg.TypicalGroups)
            {
                if (excludeGroup != null && g == excludeGroup) continue;

                if (g.IsSingleLevel)
                {
                    if (!string.IsNullOrEmpty(g.SourceLevelName)) set.Add(g.SourceLevelName);
                }
                else
                {
                    List<string> inRange = _duplicator.GetLevelsInRange(g.FromLevelName, g.ToLevelName);
                    foreach (string lvl in inRange) set.Add(lvl);
                }
            }
            return set;
        }

        public List<LevelPickerItem> GetLevelPickerItemsForGroup(TypicalFloorGroup currentGroup)
        {
            List<LevelPickerItem> list = new List<LevelPickerItem>();
            if (SelectedBuilding == null) return list;

            Dictionary<string, string> occupiedMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (TypicalFloorGroup g in SelectedBuilding.TypicalGroups)
            {
                if (g == currentGroup) continue;

                if (g.IsSingleLevel)
                {
                    if (!string.IsNullOrEmpty(g.SourceLevelName))
                        occupiedMap[g.SourceLevelName] = g.Name;
                }
                else
                {
                    List<string> range = _duplicator.GetLevelsInRange(g.FromLevelName, g.ToLevelName);
                    foreach (string lvl in range)
                        occupiedMap[lvl] = g.Name;
                }
            }

            foreach (string lvl in AvailableLevels)
            {
                bool isOccupied = occupiedMap.ContainsKey(lvl);
                list.Add(new LevelPickerItem
                {
                    LevelName = lvl,
                    IsAvailable = !isOccupied,
                    OccupiedByGroupName = isOccupied ? occupiedMap[lvl] : string.Empty
                });
            }

            return list;
        }

        public bool ValidateAndApplyRange(TypicalFloorGroup group, string fromLvl, string toLvl)
        {
            if (SelectedBuilding == null || group == null) return false;

            HashSet<string> assignedOthers = GetAssignedLevelsInBuilding(SelectedBuilding, group);
            List<string> candidateRange = _duplicator.GetLevelsInRange(fromLvl, toLvl);

            // Check overlap
            List<string> colliding = candidateRange.Where(l => assignedOthers.Contains(l)).ToList();
            if (colliding.Count > 0)
            {
                TriggerToast(string.Format("Overlap conflict: Level(s) [{0}] are already assigned in {1}.", string.Join(", ", colliding.ToArray()), SelectedBuilding.Name), true);
                return false;
            }

            group.FromLevelName = fromLvl;
            group.ToLevelName = toLvl;
            RefreshTowerLevels();
            return true;
        }

        public bool ShiftGroupRange(TypicalFloorGroup group, int delta)
        {
            if (group == null || SelectedBuilding == null || delta == 0) return false;

            List<Level> sortedLevels = _duplicator.GetAllLevels().OrderBy(l => l.Elevation).ToList();
            if (sortedLevels.Count == 0) return false;

            int fromIdx = -1;
            int toIdx = -1;

            string curFrom = group.FromLevelName;
            string curTo = group.ToLevelName;
            if (group.IsSingleLevel)
            {
                curFrom = group.SourceLevelName;
                curTo = group.SourceLevelName;
            }

            for (int i = 0; i < sortedLevels.Count; i++)
            {
                if (string.Equals(sortedLevels[i].Name, curFrom, StringComparison.OrdinalIgnoreCase)) fromIdx = i;
                if (string.Equals(sortedLevels[i].Name, curTo, StringComparison.OrdinalIgnoreCase)) toIdx = i;
            }

            if (fromIdx < 0 || toIdx < 0) return false;

            int newFromIdx = fromIdx + delta;
            int newToIdx = toIdx + delta;

            if (newFromIdx < 0)
            {
                TriggerToast("Cannot shift down: Already at the lowest level.", true);
                return false;
            }

            if (newToIdx >= sortedLevels.Count)
            {
                TriggerToast("Cannot shift up: Already at the top level.", true);
                return false;
            }

            string newFrom = sortedLevels[newFromIdx].Name;
            string newTo = sortedLevels[newToIdx].Name;

            HashSet<string> assignedOthers = GetAssignedLevelsInBuilding(SelectedBuilding, group);
            List<string> candidateRange = _duplicator.GetLevelsInRange(newFrom, newTo);
            List<string> colliding = candidateRange.Where(l => assignedOthers.Contains(l)).ToList();

            if (colliding.Count > 0)
            {
                TriggerToast(string.Format("Collision: Level(s) [{0}] are occupied in {1}.", string.Join(", ", colliding.ToArray()), SelectedBuilding.Name), true);
                return false;
            }

            group.FromLevelName = newFrom;
            group.ToLevelName = newTo;

            if (group.IsSingleLevel)
            {
                group.SourceLevelName = newFrom;
            }
            else if (group.IsDuplexModule)
            {
                int srcLowerIdx = -1;
                for (int i = 0; i < sortedLevels.Count; i++)
                {
                    if (string.Equals(sortedLevels[i].Name, group.SourceLevelNameLower, StringComparison.OrdinalIgnoreCase)) srcLowerIdx = i;
                }
                int newLowerIdx = (srcLowerIdx >= 0) ? srcLowerIdx + delta : newFromIdx;
                if (newLowerIdx >= newFromIdx && newLowerIdx <= newToIdx)
                {
                    group.SourceLevelNameLower = sortedLevels[newLowerIdx].Name;
                }
                else
                {
                    group.SourceLevelNameLower = newFrom;
                }
                string autoUpper = GetNextLevelAbove(group.SourceLevelNameLower);
                if (!string.IsNullOrEmpty(autoUpper)) group.SourceLevelNameUpper = autoUpper;
            }
            else
            {
                int srcIdx = -1;
                for (int i = 0; i < sortedLevels.Count; i++)
                {
                    if (string.Equals(sortedLevels[i].Name, group.SourceLevelName, StringComparison.OrdinalIgnoreCase)) srcIdx = i;
                }
                int newSrcIdx = (srcIdx >= 0) ? srcIdx + delta : newFromIdx;
                if (newSrcIdx >= newFromIdx && newSrcIdx <= newToIdx)
                {
                    group.SourceLevelName = sortedLevels[newSrcIdx].Name;
                }
                else
                {
                    group.SourceLevelName = newFrom;
                }
            }

            SaveTypicalGroups();
            RefreshTowerLevels();
            string msg = string.Format("Shifted '{0}' to {1} → {2}.", group.Name, newFrom, newTo);
            StatusMessage = msg;
            TriggerToast(msg, false);
            return true;
        }

        public bool ExpandOrContractGroup(TypicalFloorGroup group, int delta)
        {
            if (group == null || SelectedBuilding == null || delta == 0 || group.IsSingleLevel) return false;

            List<Level> sortedLevels = _duplicator.GetAllLevels().OrderBy(l => l.Elevation).ToList();
            if (sortedLevels.Count == 0) return false;

            int fromIdx = -1;
            int toIdx = -1;

            for (int i = 0; i < sortedLevels.Count; i++)
            {
                if (string.Equals(sortedLevels[i].Name, group.FromLevelName, StringComparison.OrdinalIgnoreCase)) fromIdx = i;
                if (string.Equals(sortedLevels[i].Name, group.ToLevelName, StringComparison.OrdinalIgnoreCase)) toIdx = i;
            }

            if (fromIdx < 0 || toIdx < 0) return false;

            int newToIdx = toIdx + delta;

            if (delta < 0)
            {
                if (newToIdx <= fromIdx)
                {
                    TriggerToast("Cannot shrink further: A typical range must contain at least 2 levels.", true);
                    return false;
                }
                string newTo = sortedLevels[newToIdx].Name;
                group.ToLevelName = newTo;
                SaveTypicalGroups();
                RefreshTowerLevels();
                string msg = string.Format("Contracted '{0}' to {1} → {2}.", group.Name, group.FromLevelName, newTo);
                StatusMessage = msg;
                TriggerToast(msg, false);
                return true;
            }
            else
            {
                if (newToIdx >= sortedLevels.Count)
                {
                    TriggerToast("Cannot expand: Top level reached.", true);
                    return false;
                }

                string newTopLvl = sortedLevels[newToIdx].Name;
                HashSet<string> assignedOthers = GetAssignedLevelsInBuilding(SelectedBuilding, group);
                if (assignedOthers.Contains(newTopLvl))
                {
                    TriggerToast(string.Format("Cannot expand: '{0}' is already occupied.", newTopLvl), true);
                    return false;
                }

                group.ToLevelName = newTopLvl;
                SaveTypicalGroups();
                RefreshTowerLevels();
                string msg = string.Format("Expanded '{0}' to {1} → {2}.", group.Name, group.FromLevelName, newTopLvl);
                StatusMessage = msg;
                TriggerToast(msg, false);
                return true;
            }
        }

        public List<string> GetUnassignedGaps()
        {
            List<string> gaps = new List<string>();
            if (SelectedBuilding == null) return gaps;

            HashSet<string> assigned = GetAssignedLevelsInBuilding(SelectedBuilding, null);
            foreach (string lvl in AvailableLevels)
            {
                if (!assigned.Contains(lvl))
                {
                    gaps.Add(lvl);
                }
            }
            return gaps;
        }

        public void RefreshTowerLevels()
        {
            TowerLevels.Clear();
            List<Level> allLevels = _duplicator.GetAllLevels().OrderByDescending(l => l.Elevation).ToList();

            foreach (Level lvl in allLevels)
            {
                LevelTowerItem item = new LevelTowerItem();
                item.LevelName = lvl.Name;
                item.Elevation = lvl.Elevation;
                item.ElevationDisplay = LevelCreatorService.FormatLength(_doc, lvl.Elevation);

                TypicalFloorGroup assignedGroup = null;
                bool isDuplexUpper = false;
                bool isDuplexLower = false;

                if (SelectedBuilding != null)
                {
                    foreach (TypicalFloorGroup g in SelectedBuilding.TypicalGroups)
                    {
                        if (g.IsSingleLevel)
                        {
                            if (string.Equals(g.SourceLevelName, lvl.Name, StringComparison.OrdinalIgnoreCase))
                            {
                                assignedGroup = g;
                                break;
                            }
                        }
                        else
                        {
                            List<string> range = _duplicator.GetLevelsInRange(g.FromLevelName, g.ToLevelName);
                            int lvlIdx = range.FindIndex(r => string.Equals(r, lvl.Name, StringComparison.OrdinalIgnoreCase));
                            if (lvlIdx >= 0)
                            {
                                assignedGroup = g;
                                if (g.IsDuplexModule)
                                {
                                    if (lvlIdx % 2 == 0) isDuplexLower = true;
                                    else isDuplexUpper = true;
                                }
                                break;
                            }
                        }
                    }
                }

                if (assignedGroup != null)
                {
                    string label = assignedGroup.Name;
                    if (isDuplexLower) label += " (Lower)";
                    else if (isDuplexUpper) label += " (Upper)";

                    item.AssignedGroupName = label;
                    item.ColorHex = assignedGroup.ColorHex ?? "#3B82F6";
                    item.IsSingleFloor = assignedGroup.IsSingleLevel;
                }
                else
                {
                    item.AssignedGroupName = string.Empty;
                    item.ColorHex = "#CBD5E1"; // Subtle gray unassigned
                    item.IsSingleFloor = false;
                }

                TowerLevels.Add(item);
            }
        }

        public void TriggerToast(string message, bool isError)
        {
            if (OnToastNotification != null)
            {
                OnToastNotification(message, isError);
            }
        }

        public void ComputePlannedSheets()
        {
            if (PlannedSheets == null) PlannedSheets = new ObservableCollection<PlannedSheet>();
            PlannedSheets.Clear();
            if (Buildings == null || Buildings.Count == 0 || PackageSettings == null) return;

            List<BuildingDefinition> activeBldgs = Buildings.ToList();
            bool isMultiBuilding = activeBldgs.Count > 1;

            foreach (PackageSetting pkg in PackageSettings)
            {
                if (!pkg.IsEnabled) continue;
                if (pkg.PackageType == ViewPackageType.MasterOverall && !isMultiBuilding) continue;

                int maxPerSheet = (int)pkg.LayoutMode; // 1, 2, 3, 4, 6, 8
                int sheetNumberCounter = pkg.StartNumber;

                // Update Scale Recommendation for this package
                double refWidth = activeBldgs[0].FootprintWidthFt > 0 ? activeBldgs[0].FootprintWidthFt : 150.0;
                double refDepth = activeBldgs[0].FootprintDepthFt > 0 ? activeBldgs[0].FootprintDepthFt : 100.0;
                ScaleOption rec = _scaleAdvisor.RecommendScale(refWidth, refDepth, SelectedTitleblock, pkg.LayoutMode);
                pkg.RecommendedScaleDisplay = rec.DisplayName;

                // ── CASE A: Master Overall Campus Package ──
                if (pkg.PackageType == ViewPackageType.MasterOverall)
                {
                    List<PlannedViewport> queuedMaster = new List<PlannedViewport>();
                    HashSet<string> seenLevels = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                    foreach (BuildingDefinition b in activeBldgs)
                    {
                        foreach (TypicalFloorGroup g in b.TypicalGroups)
                        {
                            string srcLvl = g.IsDuplexModule ? g.SourceLevelNameLower : g.SourceLevelName;
                            if (string.IsNullOrEmpty(srcLvl) || seenLevels.Contains(srcLvl)) continue;
                            seenLevels.Add(srcLvl);

                            string rangeLabel = _viewGenService.GetGroupRangeLabel(g);
                            string kindSuffix = (pkg.ViewKind == ViewPlanKind.AreaPlan) ? "AREA PLAN" : "FLOOR PLAN";
                            queuedMaster.Add(new PlannedViewport
                            {
                                LevelName = srcLvl,
                                LevelRangeLabel = rangeLabel,
                                BuildingName = "Master",
                                ScopeBoxName = Config.MasterScopeBoxName,
                                ViewName = string.Format("FL. {0} - MASTER OVERALL {1}", rangeLabel, kindSuffix),
                                FormattedTitleOnSheet = string.Format("MASTER - {0} OVERALL {1}", rangeLabel.ToUpperInvariant(), kindSuffix),
                                PackageType = pkg.PackageType,
                                ViewKind = pkg.ViewKind,
                                AreaSchemeName = pkg.SelectedAreaSchemeName
                            });
                        }
                    }

                    for (int i = 0; i < queuedMaster.Count; i += maxPerSheet)
                    {
                        List<PlannedViewport> chunk = queuedMaster.Skip(i).Take(maxPerSheet).ToList();
                        for (int k = 0; k < chunk.Count; k++) chunk[k].GridIndex = k;

                        string sNum = string.Format("{0}{1}", pkg.SheetPrefix, sheetNumberCounter++);
                        string sName = chunk.Count == 1 ? string.Format("Master Overall - {0}", chunk[0].LevelName) : "Master Overall Campus Plans";

                        PlannedSheets.Add(new PlannedSheet
                        {
                            SheetNumber = sNum,
                            SheetName = sName,
                            BuildingName = "Master",
                            ScopeBoxName = Config.MasterScopeBoxName,
                            PackageType = pkg.PackageType,
                            LayoutMode = pkg.LayoutMode,
                            ScaleValue = pkg.ScaleValue,
                            ScaleDisplay = pkg.ScaleDisplay,
                            HasSummaryTable = pkg.IncludeSummaryTableOnSheet,
                            Viewports = chunk
                        });
                    }
                    continue;
                }

                // ── CASE B: Building-Specific Packages (Gross, Deductions, Life Safety, RCP, Floor Plans) ──
                foreach (BuildingDefinition bldg in activeBldgs)
                {
                    List<PlannedViewport> queuedViewports = new List<PlannedViewport>();

                    foreach (TypicalFloorGroup group in bldg.TypicalGroups)
                    {
                        string srcLevel = group.IsDuplexModule ? group.SourceLevelNameLower : group.SourceLevelName;
                        if (string.IsNullOrEmpty(srcLevel)) continue;

                        string rangeLabel = _viewGenService.GetGroupRangeLabel(group);
                        string bldgTag = bldg.Name.ToUpperInvariant();
                        string vName = "";
                        string titleOnSheet = "";

                        switch (pkg.PackageType)
                        {
                            case ViewPackageType.GrossArea:
                                vName = string.Format("FL. {0} - GROSS AREA PLAN ({1})", rangeLabel, bldgTag);
                                titleOnSheet = string.Format("{0} - {1} GROSS AREA PLAN", bldgTag, rangeLabel.ToUpperInvariant());
                                break;
                            case ViewPackageType.Deductions:
                                vName = string.Format("FL. {0} - DEDUCTIONS PLAN ({1})", rangeLabel, bldgTag);
                                titleOnSheet = string.Format("{0} - {1} DEDUCTIONS PLAN", bldgTag, rangeLabel.ToUpperInvariant());
                                break;
                            case ViewPackageType.Architectural:
                                if (pkg.ViewKind == ViewPlanKind.AreaPlan)
                                {
                                    string schName = !string.IsNullOrEmpty(pkg.SelectedAreaSchemeName) ? pkg.SelectedAreaSchemeName : "Area";
                                    vName = string.Format("FL. {0} - {1} PLAN ({2})", rangeLabel, schName.ToUpperInvariant(), bldgTag);
                                    titleOnSheet = string.Format("{0} - {1} {2} PLAN", bldgTag, rangeLabel.ToUpperInvariant(), schName.ToUpperInvariant());
                                }
                                else
                                {
                                    vName = string.Format("FL. {0} - ARCHITECTURAL PLAN ({1})", rangeLabel, bldgTag);
                                    titleOnSheet = string.Format("{0} - {1} FLOOR PLAN", bldgTag, rangeLabel.ToUpperInvariant());
                                }
                                break;
                            case ViewPackageType.CeilingPlanRCP:
                                vName = string.Format("FL. {0} - CEILING PLAN RCP ({1})", rangeLabel, bldgTag);
                                titleOnSheet = string.Format("{0} - {1} REFLECTED CEILING PLAN", bldgTag, rangeLabel.ToUpperInvariant());
                                break;
                                case ViewPackageType.EgressLifeSafety:
                                if (pkg.ViewKind == ViewPlanKind.AreaPlan)
                                {
                                    vName = string.Format("FL. {0} - LIFE SAFETY AREA PLAN ({1})", rangeLabel, bldgTag);
                                    titleOnSheet = string.Format("{0} - {1} LIFE SAFETY AREA PLAN", bldgTag, rangeLabel.ToUpperInvariant());
                                }
                                else
                                {
                                    vName = string.Format("FL. {0} - LIFE SAFETY PLAN ({1})", rangeLabel, bldgTag);
                                    titleOnSheet = string.Format("{0} - {1} LIFE SAFETY PLAN", bldgTag, rangeLabel.ToUpperInvariant());
                                }
                                break;
                            case ViewPackageType.Custom:
                            default:
                                string pkgTitle = !string.IsNullOrEmpty(pkg.DisplayName) ? pkg.DisplayName.ToUpperInvariant() : "CUSTOM";
                                if (pkg.ViewKind == ViewPlanKind.AreaPlan)
                                {
                                    string sch = !string.IsNullOrEmpty(pkg.SelectedAreaSchemeName) ? pkg.SelectedAreaSchemeName.ToUpperInvariant() : "AREA";
                                    vName = string.Format("FL. {0} - {1} [{2}] ({3})", rangeLabel, pkgTitle, sch, bldgTag);
                                    titleOnSheet = string.Format("{0} - {1} {2}", bldgTag, rangeLabel.ToUpperInvariant(), pkgTitle);
                                }
                                else if (pkg.ViewKind == ViewPlanKind.CeilingPlan)
                                {
                                    vName = string.Format("FL. {0} - {1} RCP ({2})", rangeLabel, pkgTitle, bldgTag);
                                    titleOnSheet = string.Format("{0} - {1} {2}", bldgTag, rangeLabel.ToUpperInvariant(), pkgTitle);
                                }
                                else
                                {
                                    vName = string.Format("FL. {0} - {1} ({2})", rangeLabel, pkgTitle, bldgTag);
                                    titleOnSheet = string.Format("{0} - {1} {2}", bldgTag, rangeLabel.ToUpperInvariant(), pkgTitle);
                                }
                                break;
                        }

                        queuedViewports.Add(new PlannedViewport
                        {
                            LevelName = srcLevel,
                            LevelRangeLabel = rangeLabel,
                            BuildingName = bldg.Name,
                            ScopeBoxName = bldg.ScopeBoxName,
                            ViewName = vName,
                            FormattedTitleOnSheet = titleOnSheet,
                            PackageType = pkg.PackageType,
                            ViewKind = pkg.ViewKind,
                            AreaSchemeName = pkg.SelectedAreaSchemeName
                        });
                    }

                    for (int i = 0; i < queuedViewports.Count; i += maxPerSheet)
                    {
                        List<PlannedViewport> chunk = queuedViewports.Skip(i).Take(maxPerSheet).ToList();
                        for (int k = 0; k < chunk.Count; k++) chunk[k].GridIndex = k;

                        string sNum = string.Format("{0}{1}", pkg.SheetPrefix, sheetNumberCounter++);
                        string sName = chunk.Count == 1 ?
                            string.Format("{0} - {1} ({2})", bldg.Name, pkg.DisplayName, chunk[0].LevelName) :
                            string.Format("{0} - {1} (Typical Floors)", bldg.Name, pkg.DisplayName);

                        PlannedSheets.Add(new PlannedSheet
                        {
                            SheetNumber = sNum,
                            SheetName = sName,
                            BuildingName = bldg.Name,
                            ScopeBoxName = bldg.ScopeBoxName,
                            PackageType = pkg.PackageType,
                            LayoutMode = pkg.LayoutMode,
                            ScaleValue = pkg.ScaleValue,
                            ScaleDisplay = pkg.ScaleDisplay,
                            HasSummaryTable = pkg.IncludeSummaryTableOnSheet,
                            Viewports = chunk
                        });
                    }
                }
            }
        }

        public void ExecuteComposeSheets()
        {
            try
            {
                ComputePlannedSheets();
                if (PlannedSheets.Count == 0)
                {
                    TriggerToast("No sheets planned. Please enable at least one package and configure typical floors.", true);
                    return;
                }

                ElementId tbId = SelectedTitleblock != null ? SelectedTitleblock.FamilySymbolId : ElementId.InvalidElementId;

                // 1. Generate all views with scale, templates, and scope boxes
                Dictionary<string, ElementId> createdViews = _viewGenService.GeneratePackageViews(
                    Buildings.ToList(),
                    Config,
                    PackageSettings.ToList(),
                    SelectedViewScale,
                    OnlyTypicalRanges);

                // 2. Compose sheets and place viewports with Titleblock bounds
                int placedCount = _sheetPlaceService.ComposePlannedSheets(
                    PlannedSheets.ToList(),
                    tbId,
                    RepositionIfExists,
                    createdViews,
                    SelectedTitleblock);

                // Refresh project sheets
                AvailableSheets.Clear();
                foreach (SheetItem sh in _sheetPlaceService.GetExistingSheets()) AvailableSheets.Add(sh);

                string msg = string.Format("Successfully generated {0} view(s) and placed {1} viewport(s) across {2} sheet(s) in Revit.",
                    createdViews.Count, placedCount, PlannedSheets.Count);
                StatusMessage = msg;
                TriggerToast(msg, false);
            }
            catch (Exception ex)
            {
                StatusMessage = "Composition Error: " + ex.Message;
                TriggerToast("Error: " + ex.Message, true);
            }
        }

        public void GenerateProjectViews(bool createArch, bool createGross, bool createDed, bool typicalMasterOnly)
        {
            try
            {
                if (!createArch && !createGross && !createDed)
                {
                    TriggerToast("Please select at least one view type (Architectural, Gross, or Deductions).", true);
                    return;
                }

                LastGeneratedViews = _viewGenService.GenerateMasterAndDependentViews(
                    Buildings.ToList(),
                    Config,
                    createArch,
                    createGross,
                    createDed,
                    typicalMasterOnly);

                int masterCount = LastGeneratedViews.Count;
                int depCount = LastGeneratedViews.Sum(r => r.DependentViews.Count);

                string msg = string.Format("Created {0} Master View(s) and {1} Dependent View(s) in Project Browser.", masterCount, depCount);
                StatusMessage = msg;
                TriggerToast(msg, false);
            }
            catch (Exception ex)
            {
                StatusMessage = "View Generation Error: " + ex.Message;
                TriggerToast("Error creating views: " + ex.Message, true);
            }
        }

        public void PlaceViewsOnSelectedSheet()
        {
            try
            {
                if (SelectedSheet == null)
                {
                    TriggerToast("Please select a target Sheet first.", true);
                    return;
                }

                if (LastGeneratedViews == null || LastGeneratedViews.Count == 0)
                {
                    TriggerToast("No recently generated views found. Click 'Create Master & Dependent Views' first.", true);
                    return;
                }

                List<ElementId> viewIdsToPlace = new List<ElementId>();
                foreach (GeneratedViewResult r in LastGeneratedViews)
                {
                    if (r.MasterView != null) viewIdsToPlace.Add(r.MasterView.Id);
                    foreach (View dep in r.DependentViews)
                    {
                        if (dep != null) viewIdsToPlace.Add(dep.Id);
                    }
                }

                int placed = _sheetPlaceService.PlaceViewsOnSheet(SelectedSheet.SheetId, viewIdsToPlace);
                string msg = string.Format("Successfully placed {0} view(s) onto Sheet {1}.", placed, SelectedSheet.DisplayName);
                StatusMessage = msg;
                TriggerToast(msg, false);
            }
            catch (Exception ex)
            {
                StatusMessage = "Sheet Placement Error: " + ex.Message;
                TriggerToast("Error placing views on sheet: " + ex.Message, true);
            }
        }

        public void SaveTypicalGroups()
        {
            bool ok = _storageService.SaveBuildings(_doc, Buildings.ToList());
            if (ok)
            {
                StatusMessage = "All building typical floor definitions saved to Revit model.";
                TriggerToast("Typical floor definitions saved to model.", false);
            }
            else
            {
                StatusMessage = "Error saving definitions to Revit model.";
                TriggerToast("Error saving definitions to Revit model.", true);
            }
        }

        public string GetSourceLevelSummary(string levelName)
        {
            return _duplicator.GetLevelAreaSummary(levelName, Config.GrossAreaSchemeName, Config.DeductionAreaSchemeName);
        }

        public string GetSourceLevelDetail(string levelName)
        {
            return _duplicator.GetLevelAreaDetail(levelName, Config.GrossAreaSchemeName, Config.DeductionAreaSchemeName, Config.DeductionTypeParameterName);
        }

        public void RevertPropagatedAreas()
        {
            try
            {
                List<TypicalFloorGroup> allGroups = new List<TypicalFloorGroup>();
                foreach (BuildingDefinition b in Buildings)
                {
                    allGroups.AddRange(b.TypicalGroups);
                }

                if (allGroups.Count == 0)
                {
                    StatusMessage = "No typical floor groups defined.";
                    MessageBox.Show("Please define typical floor groups in Step 1 before clearing.", "BauTools", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                MessageBoxResult confirm = MessageBox.Show(
                    "Are you sure you want to revert and clear all propagated areas and boundary lines across target levels for all buildings?\n\n• Source modeled levels will remain 100% untouched.\n• Revit view plans will NOT be deleted or modified.",
                    "Confirm Clear Propagated Areas",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (confirm != MessageBoxResult.Yes) return;

                StatusMessage = "Clearing propagated areas from target levels...";
                string msg = _duplicator.ClearPropagatedAreas(
                    allGroups,
                    Config,
                    PropagateGrossArea,
                    PropagateDeductionsArea
                );

                StatusMessage = msg;
                MessageBox.Show(msg, "BauTools — Clear Complete", MessageBoxButton.OK, MessageBoxImage.Information);

                // Auto-refresh calculation table
                CalculateTable();
            }
            catch (Exception ex)
            {
                StatusMessage = "Clear Error: " + ex.Message;
                MessageBox.Show("Error clearing areas: " + ex.Message, "Clear Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void PropagateAreasFromTypicalGroups()
        {
            try
            {
                List<TypicalFloorGroup> allGroups = new List<TypicalFloorGroup>();
                foreach (BuildingDefinition b in Buildings)
                {
                    allGroups.AddRange(b.TypicalGroups);
                }

                if (allGroups.Count == 0)
                {
                    StatusMessage = "No typical floor groups defined to propagate.";
                    MessageBox.Show("Please add at least one Typical Floor group in Step 1.", "BauTools", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Save groups first
                _storageService.SaveBuildings(_doc, Buildings.ToList());

                StatusMessage = "Propagating typical floor areas across model...";
                string msg = _duplicator.PropagateMultipleGroups(
                    allGroups,
                    Config,
                    PropagateGrossArea,
                    PropagateDeductionsArea
                );

                StatusMessage = msg;
                MessageBox.Show(msg, "BauTools — Propagation Complete", MessageBoxButton.OK, MessageBoxImage.Information);

                // Auto-refresh calculations
                CalculateTable();
            }
            catch (Exception ex)
            {
                StatusMessage = "Propagation Error: " + ex.Message;
                MessageBox.Show("Error: " + ex.Message, "Propagation Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void CalculateTable()
        {
            try
            {
                List<TypicalFloorGroup> allGroups = new List<TypicalFloorGroup>();
                foreach (BuildingDefinition b in Buildings)
                {
                    allGroups.AddRange(b.TypicalGroups);
                }

                List<AreaDataModel> rawAreas = _extractor.ExtractAreas(Config);
                ProjectResult = _calculator.ComputeProjectZoning(rawAreas, Config, allGroups);

                // Populate / sync building checkboxes
                SyncBuildingItems(ProjectResult.BuildingTables);

                // Filter displayed tables
                UpdateDisplayedTables();

                // Live Compliance Evaluation
                EvaluateCompliance();

                StatusMessage = string.Format("ZFA calculated: {0:N0} SF total across {1} building(s).", ProjectResult.TotalProjectZoningFloorArea, ProjectResult.BuildingTables.Count);
            }
            catch (Exception ex)
            {
                StatusMessage = "Calculation Error: " + ex.Message;
                MessageBox.Show("Error calculating ZFA: " + ex.Message, "Calculation Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void EvaluateCompliance()
        {
            if (ComplianceReport == null) ComplianceReport = new ZoningComplianceReport();
            if (LotData == null) LotData = new ZoningLotData();

            double allowable = LotData.TotalAllowableZfa;
            double proposed = ProjectResult != null ? ProjectResult.TotalProjectZoningFloorArea : 0.0;
            double remaining = allowable - proposed;
            double pct = allowable > 0 ? (proposed / allowable) * 100.0 : 0.0;
            bool isOver = proposed > allowable && allowable > 0;

            ComplianceReport.AllowableZfa = allowable;
            ComplianceReport.ProposedZfa = proposed;
            ComplianceReport.RemainingZfa = remaining;
            ComplianceReport.UtilizationPercent = pct;
            ComplianceReport.IsOverbuilt = isOver;

            if (allowable <= 0)
            {
                ComplianceReport.StatusSummary = "Please enter Lot Area and Allowable FAR to evaluate compliance.";
                ComplianceReport.ColorHex = "#64748B"; // Neutral Gray
            }
            else if (isOver)
            {
                ComplianceReport.StatusSummary = string.Format("⚠️ OVERBUILT: Exceeds allowable ZFA by {0:N0} SF ({1:N1}% of Cap)", Math.Abs(remaining), pct);
                ComplianceReport.ColorHex = "#EF4444"; // Red
            }
            else if (pct >= 95.0)
            {
                ComplianceReport.StatusSummary = string.Format("🟢 OPTIMAL: {0:N1}% Consumed ({1:N0} SF Unused Balance)", pct, remaining);
                ComplianceReport.ColorHex = "#10B981"; // Emerald Green
            }
            else
            {
                ComplianceReport.StatusSummary = string.Format("🔵 COMPLIANT: {0:N1}% Consumed ({1:N0} SF Unused Air Rights)", pct, remaining);
                ComplianceReport.ColorHex = "#3B82F6"; // Blue
            }
        }

        public void ImportZoningExcel(string filePath)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                {
                    TriggerToast("Invalid file path.", true);
                    return;
                }

                ZoningLotData imported = _excelBridgeService.ImportZoningFromExcel(filePath);
                if (imported != null)
                {
                    LotData = imported;
                    EvaluateCompliance();
                    string msg = string.Format("Loaded Lot: {0:N0} SF, District: {1}, Total FAR: {2:N2}.",
                        imported.LotAreaSqFt, imported.ZoningDistrict, imported.TotalAllowableFar);
                    StatusMessage = msg;
                    TriggerToast("Excel Lot Data Imported Successfully!", false);
                }
                else
                {
                    TriggerToast("Could not parse Excel file. Check format.", true);
                }
            }
            catch (Exception ex)
            {
                TriggerToast("Import error: " + ex.Message, true);
            }
        }

        public void ExportZoningTemplateExcel(string filePath)
        {
            try
            {
                bool ok = _excelBridgeService.ExportZoningTemplate(filePath, LotData);
                if (ok)
                {
                    TriggerToast("Excel template saved successfully!", false);
                }
                else
                {
                    TriggerToast("Could not save Excel template.", true);
                }
            }
            catch (Exception ex)
            {
                TriggerToast("Export error: " + ex.Message, true);
            }
        }

        private void SyncBuildingItems(List<ZoningTableResult> tables)
        {
            List<string> newBldgNames = tables.Select(t => t.BuildingName).ToList();

            for (int i = BuildingItems.Count - 1; i >= 0; i--)
            {
                if (!newBldgNames.Contains(BuildingItems[i].Name))
                    BuildingItems.RemoveAt(i);
            }

            foreach (string bName in newBldgNames)
            {
                if (!BuildingItems.Any(item => item.Name == bName))
                {
                    BuildingFilterItem newItem = new BuildingFilterItem { Name = bName, IsSelected = true };
                    newItem.SelectionChanged = () => UpdateDisplayedTables();
                    BuildingItems.Add(newItem);
                }
            }
        }

        public void UpdateDisplayedTables()
        {
            DisplayedTables.Clear();
            if (ProjectResult == null) return;

            List<string> selectedNames = BuildingItems.Where(i => i.IsSelected).Select(i => i.Name).ToList();

            foreach (ZoningTableResult tbl in ProjectResult.BuildingTables)
            {
                if (selectedNames.Contains(tbl.BuildingName))
                {
                    DisplayedTables.Add(tbl);
                }
            }

            if (DisplayedTables.Count > 0)
            {
                SelectedTableResult = DisplayedTables[0];
            }
        }

        public void ExportToExcel()
        {
            try
            {
                if (ProjectResult == null || ProjectResult.BuildingTables.Count == 0)
                {
                    MessageBox.Show("Please calculate the ZFA matrix first in Step 3 before exporting.", "BauTools", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                Microsoft.Win32.SaveFileDialog sfd = new Microsoft.Win32.SaveFileDialog
                {
                    Title = "Export Zoning Floor Area Matrix to Excel",
                    Filter = "Excel XML Spreadsheet (*.xls)|*.xls",
                    FileName = string.Format("BauTools_ZFA_Summary_{0:yyyyMMdd}.xls", DateTime.Now)
                };

                if (sfd.ShowDialog() == true)
                {
                    ExcelExporter.ExportProjectToExcelXml(ProjectResult, sfd.FileName);
                    StatusMessage = "Successfully exported to " + Path.GetFileName(sfd.FileName);
                    MessageBox.Show("Excel workbook generated successfully:\n" + sfd.FileName, "BauTools — Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = "Export Error: " + ex.Message;
                MessageBox.Show("Error generating Excel report: " + ex.Message, "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void CreateDraftingViews()
        {
            try
            {
                if (ProjectResult == null || ProjectResult.BuildingTables.Count == 0)
                {
                    MessageBox.Show("Please calculate the ZFA matrix first in Step 3 before creating views.", "BauTools", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                List<ZoningTableResult> tablesToDraw = DisplayedTables.Count > 0 ? DisplayedTables.ToList() : ProjectResult.BuildingTables;
                int count = 0;
                foreach (ZoningTableResult tbl in tablesToDraw)
                {
                    ViewDrafting vd = _sheetDrawer.CreateZoningTableDraftingView(tbl, "ZFA - " + tbl.BuildingName);
                    if (vd != null) count++;
                }

                StatusMessage = string.Format("Created {0} Revit drafting view(s) under Project Browser.", count);
                MessageBox.Show(string.Format("Successfully created {0} drafting view(s) with native vector tables in Revit.", count),
                    "BauTools — Drafting Views Created", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                StatusMessage = "Drafting View Error: " + ex.Message;
                MessageBox.Show("Error creating drafting views: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        protected void OnPropertyChanged(string propName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propName));
            }
        }
    }
}
