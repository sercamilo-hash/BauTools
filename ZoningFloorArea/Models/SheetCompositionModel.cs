using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace ZoningFloorArea.Models
{
    public enum SheetLayoutMode
    {
        SingleView = 1,
        SideBySide2Views = 2,
        Matrix4Views = 4
    }

    public enum ViewPackageType
    {
        Architectural = 0,
        CeilingPlanRCP = 1,
        GrossArea = 2,
        Deductions = 3,
        EgressLifeSafety = 4
    }

    public class TitleblockItem
    {
        public string Name { get; set; }
        public ElementId FamilySymbolId { get; set; }

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
        public string DisplayName { get; set; }
        public string Icon { get; set; }
        public bool IsEnabled { get; set; }
        public string SheetPrefix { get; set; }
        public int StartNumber { get; set; }
        public string ViewTemplateName { get; set; }
        public ElementId SelectedTemplateId { get; set; }

        public PackageSetting(ViewPackageType type, string name, string icon, string prefix, int startNum)
        {
            PackageType = type;
            DisplayName = name;
            Icon = icon;
            IsEnabled = true;
            SheetPrefix = prefix;
            StartNumber = startNum;
            ViewTemplateName = "(None)";
            SelectedTemplateId = ElementId.InvalidElementId;
        }
    }

    public class PlannedViewport
    {
        public string LevelName { get; set; }
        public string BuildingName { get; set; }
        public string ViewName { get; set; }
        public ViewPackageType PackageType { get; set; }
        public int GridIndex { get; set; } // 0, 1, 2, 3
        public ElementId ExistingViewId { get; set; }
    }

    public class PlannedSheet
    {
        public string SheetNumber { get; set; }
        public string SheetName { get; set; }
        public string BuildingName { get; set; }
        public ViewPackageType PackageType { get; set; }
        public SheetLayoutMode LayoutMode { get; set; }
        public List<PlannedViewport> Viewports { get; set; }

        public PlannedSheet()
        {
            Viewports = new List<PlannedViewport>();
        }

        public string Summary
        {
            get
            {
                return string.Format("{0} - {1} ({2} View(s))", SheetNumber, SheetName, Viewports.Count);
            }
        }
    }
}