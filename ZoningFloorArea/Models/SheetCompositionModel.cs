using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace ZoningFloorArea.Models
{
    public enum SheetLayoutMode
    {
        Single1View = 1,
        Dual2Views = 2,
        Triple3Views = 3,
        Quad4Views = 4,
        Hex6Views = 6,
        Octo8Views = 8
    }

    public enum ViewPlanKind
    {
        FloorPlan = 0,    // Standard Architectural Floor Plan
        AreaPlan = 1,     // Area Plan (associated with an AreaScheme)
        CeilingPlan = 2   // Reflected Ceiling Plan (RCP)
    }

    public enum ViewPackageType
    {
        MasterOverall = 0,
        Architectural = 1,
        CeilingPlanRCP = 2,
        GrossArea = 3,
        Deductions = 4,
        EgressLifeSafety = 5
    }

    public class TitleblockItem
    {
        public string Name { get; set; }
        public ElementId FamilySymbolId { get; set; }
        public double WidthInches { get; set; }
        public double HeightInches { get; set; }
        public double UsableWidthInches { get; set; }
        public double UsableHeightInches { get; set; }

        public TitleblockItem()
        {
            WidthInches = 36.0;
            HeightInches = 24.0;
            UsableWidthInches = 31.0;
            UsableHeightInches = 22.0;
        }

        public override string ToString()
        {
            return Name;
        }
    }

    public class ViewTemplateItem
    {
        public string Name { get; set; }
        public ElementId TemplateId { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }

    public class PackageSetting
    {
        public ViewPackageType PackageType { get; set; }
        public ViewPlanKind ViewKind { get; set; }
        public string SelectedAreaSchemeName { get; set; }
        public ElementId SelectedAreaSchemeId { get; set; }
        public string DisplayName { get; set; }
        public string Icon { get; set; }
        public bool IsEnabled { get; set; }
        public string SheetPrefix { get; set; }
        public int StartNumber { get; set; }
        public string ViewTemplateName { get; set; }
        public ElementId SelectedTemplateId { get; set; }
        public SheetLayoutMode LayoutMode { get; set; }
        public int ScaleValue { get; set; }
        public string ScaleDisplay { get; set; }
        public string RecommendedScaleDisplay { get; set; }
        public bool IncludeSummaryTableOnSheet { get; set; }

        public PackageSetting(ViewPackageType type, string name, string icon, string prefix, int startNum, SheetLayoutMode defaultLayout, int defaultScale, string scaleDisp, ViewPlanKind viewKind = ViewPlanKind.FloorPlan, string defaultScheme = "")
        {
            PackageType = type;
            ViewKind = viewKind;
            SelectedAreaSchemeName = defaultScheme ?? string.Empty;
            SelectedAreaSchemeId = ElementId.InvalidElementId;
            DisplayName = name;
            Icon = icon;
            IsEnabled = true;
            SheetPrefix = prefix;
            StartNumber = startNum;
            ViewTemplateName = "(None)";
            SelectedTemplateId = ElementId.InvalidElementId;
            LayoutMode = defaultLayout;
            ScaleValue = defaultScale;
            ScaleDisplay = scaleDisp;
            RecommendedScaleDisplay = scaleDisp;
            IncludeSummaryTableOnSheet = (type == ViewPackageType.GrossArea || type == ViewPackageType.Deductions);
        }
    }

    public class PlannedViewport
    {
        public string LevelName { get; set; }
        public string LevelRangeLabel { get; set; }
        public string BuildingName { get; set; }
        public string ScopeBoxName { get; set; }
        public string ViewName { get; set; }
        public string FormattedTitleOnSheet { get; set; }
        public ViewPackageType PackageType { get; set; }
        public ViewPlanKind ViewKind { get; set; }
        public string AreaSchemeName { get; set; }
        public int GridIndex { get; set; } // 0 to 7
        public ElementId ExistingViewId { get; set; }

        public PlannedViewport()
        {
            ViewKind = ViewPlanKind.FloorPlan;
            AreaSchemeName = string.Empty;
            GridIndex = 0;
            ExistingViewId = ElementId.InvalidElementId;
        }
    }

    public class PlannedSheet
    {
        public string SheetNumber { get; set; }
        public string SheetName { get; set; }
        public string BuildingName { get; set; }
        public string ScopeBoxName { get; set; }
        public ViewPackageType PackageType { get; set; }
        public SheetLayoutMode LayoutMode { get; set; }
        public int ScaleValue { get; set; }
        public string ScaleDisplay { get; set; }
        public bool HasSummaryTable { get; set; }
        public List<PlannedViewport> Viewports { get; set; }

        public PlannedSheet()
        {
            Viewports = new List<PlannedViewport>();
            LayoutMode = SheetLayoutMode.Quad4Views;
            ScaleValue = 96;
            ScaleDisplay = "1/8\" = 1'-0\"";
            HasSummaryTable = false;
        }

        public string Summary
        {
            get
            {
                return string.Format("{0} - {1} ({2} View(s) @ {3})", SheetNumber, SheetName, Viewports.Count, ScaleDisplay);
            }
        }
    }
}