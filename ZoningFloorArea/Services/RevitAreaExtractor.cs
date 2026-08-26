using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using ZoningFloorArea.Models;

namespace ZoningFloorArea.Services
{
    public class RevitAreaExtractor
    {
        private readonly Document _doc;

        public RevitAreaExtractor(Document doc)
        {
            if (doc == null) throw new ArgumentNullException("doc");
            _doc = doc;
        }

        public List<string> GetAreaSchemeNames()
        {
            FilteredElementCollector collector = new FilteredElementCollector(_doc).OfClass(typeof(AreaScheme));
            List<string> list = new List<string>();
            foreach (AreaScheme s in collector)
            {
                list.Add(s.Name);
            }
            list.Sort();
            return list;
        }

        public List<string> GetAvailableAreaParameters()
        {
            FilteredElementCollector collector = new FilteredElementCollector(_doc)
                .OfCategory(BuiltInCategory.OST_Areas)
                .WhereElementIsNotElementType();

            Area sampleArea = null;
            foreach (Area a in collector)
            {
                sampleArea = a;
                break;
            }

            if (sampleArea == null)
            {
                return new List<string> { "Building", "Deduction", "Name", "Comments", "Area Type", "Number" };
            }

            List<string> paramNames = new List<string>();
            paramNames.Add("Building");
            paramNames.Add("Deduction");

            foreach (Parameter p in sampleArea.Parameters)
            {
                if (p.Definition != null && !paramNames.Contains(p.Definition.Name))
                {
                    paramNames.Add(p.Definition.Name);
                }
            }
            return paramNames;
        }

        public List<AreaDataModel> ExtractAreas(MappingConfig config)
        {
            List<AreaDataModel> results = new List<AreaDataModel>();

            FilteredElementCollector collector = new FilteredElementCollector(_doc)
                .OfCategory(BuiltInCategory.OST_Areas)
                .WhereElementIsNotElementType();

            foreach (Area area in collector)
            {
                if (area.Area <= 0) continue;

                string schemeName = area.AreaScheme != null ? area.AreaScheme.Name : string.Empty;
                string levelName = area.Level != null ? area.Level.Name : "Unassigned";
                double levelElevation = area.Level != null ? area.Level.Elevation : 0.0;

                // Extract Building Name
                string bldgName = GetParameterStringValue(area, config.BuildingParameterName);
                if (string.IsNullOrEmpty(bldgName))
                {
                    bldgName = GetParameterStringValue(area, "Building");
                }
                if (string.IsNullOrEmpty(bldgName))
                {
                    bldgName = string.IsNullOrEmpty(config.BuildingName) ? "BUILDING C" : config.BuildingName;
                }

                // Extract Deduction Type
                string deductionType = GetParameterStringValue(area, config.DeductionTypeParameterName);
                if (string.IsNullOrEmpty(deductionType))
                {
                    deductionType = GetParameterStringValue(area, "Deduction");
                }
                if (string.IsNullOrEmpty(deductionType))
                {
                    deductionType = area.Name;
                }

                string usageCategory = GetParameterStringValue(area, config.UsageCategoryParameterName);
                if (string.IsNullOrEmpty(usageCategory))
                {
                    usageCategory = "Residential";
                }
                else if (usageCategory.IndexOf("Commercial", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    usageCategory = "Commercial";
                }
                else
                {
                    usageCategory = "Residential";
                }

                AreaDataModel model = new AreaDataModel();
                model.ElementId = area.Id.ToString();
                model.Name = area.Name;
                model.AreaValue = area.Area;
                model.LevelName = levelName;
                model.LevelElevation = levelElevation;
                model.AreaSchemeName = schemeName;
                model.DeductionType = deductionType;
                model.UsageCategory = usageCategory;
                model.BuildingName = bldgName.Trim().ToUpper();

                results.Add(model);
            }

            return results;
        }

        private string GetParameterStringValue(Area area, string paramName)
        {
            if (string.IsNullOrEmpty(paramName)) return string.Empty;

            Parameter p = area.LookupParameter(paramName);
            if (p == null && string.Equals(paramName, "Name", StringComparison.OrdinalIgnoreCase))
            {
                p = area.get_Parameter(BuiltInParameter.ROOM_NAME);
            }

            if (p == null) return string.Empty;

            switch (p.StorageType)
            {
                case StorageType.String:
                    return p.AsString() ?? string.Empty;
                case StorageType.ElementId:
                    Element el = _doc.GetElement(p.AsElementId());
                    return el != null ? el.Name : string.Empty;
                default:
                    return p.AsValueString() ?? string.Empty;
            }
        }
    }
}
