using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using ZoningFloorArea.Models;

namespace ZoningFloorArea.Services
{
    public class GeneratedViewResult
    {
        public View MasterView { get; set; }
        public List<View> DependentViews { get; set; }
        public string RangeLabel { get; set; }
        public string ViewTypeLabel { get; set; }

        public GeneratedViewResult()
        {
            DependentViews = new List<View>();
        }
    }

    public class RevitViewGeneratorService
    {
        private readonly Document _doc;

        public RevitViewGeneratorService(Document doc)
        {
            if (doc == null) throw new ArgumentNullException("doc");
            _doc = doc;
        }

        public List<string> GetAvailableScopeBoxes()
        {
            List<string> list = new List<string>();
            try
            {
                FilteredElementCollector collector = new FilteredElementCollector(_doc)
                    .OfCategory(BuiltInCategory.OST_VolumeOfInterest)
                    .WhereElementIsNotElementType();

                foreach (Element elem in collector)
                {
                    if (elem != null && !string.IsNullOrEmpty(elem.Name))
                    {
                        list.Add(elem.Name);
                    }
                }
            }
            catch
            {
            }
            list.Sort();
            return list;
        }

        public List<string> GetAvailableViewStringParameters()
        {
            List<string> paramsList = new List<string>();
            paramsList.Add("Building");
            paramsList.Add("Comments");
            paramsList.Add("Sub-Discipline");
            paramsList.Add("Edificio");
            paramsList.Add("Title on Sheet");

            try
            {
                FilteredElementCollector collector = new FilteredElementCollector(_doc)
                    .OfClass(typeof(ViewPlan))
                    .WhereElementIsNotElementType();

                ViewPlan sampleView = collector.Cast<ViewPlan>().FirstOrDefault(v => !v.IsTemplate);
                if (sampleView != null)
                {
                    foreach (Parameter p in sampleView.Parameters)
                    {
                        if (p != null && p.StorageType == StorageType.String && !p.IsReadOnly)
                        {
                            string pName = p.Definition.Name;
                            if (!paramsList.Contains(pName))
                            {
                                paramsList.Add(pName);
                            }
                        }
                    }
                }
            }
            catch
            {
            }

            return paramsList;
        }

        public Dictionary<string, ElementId> GeneratePackageViews(
            List<BuildingDefinition> targetBuildings,
            MappingConfig config,
            List<PackageSetting> packageSettings,
            int globalViewScale,
            bool onlyTypicalRanges)
        {
            Dictionary<string, ElementId> createdMap = new Dictionary<string, ElementId>(StringComparer.OrdinalIgnoreCase);
            if (targetBuildings == null || targetBuildings.Count == 0 || packageSettings == null || packageSettings.Count == 0)
                return createdMap;

            Dictionary<string, ElementId> scopeBoxMap = GetScopeBoxElementMap();
            ViewFamilyType floorPlanVft = GetViewFamilyType(ViewFamily.FloorPlan);
            ViewFamilyType ceilingPlanVft = GetViewFamilyType(ViewFamily.CeilingPlan);
            ViewFamilyType areaPlanVft = GetViewFamilyType(ViewFamily.AreaPlan);

            AreaScheme grossScheme = !string.IsNullOrEmpty(config.GrossAreaSchemeName) ? GetAreaSchemeByName(config.GrossAreaSchemeName) : null;
            AreaScheme dedScheme = !string.IsNullOrEmpty(config.DeductionAreaSchemeName) ? GetAreaSchemeByName(config.DeductionAreaSchemeName) : null;

            ElementId masterScopeBoxId = ElementId.InvalidElementId;
            if (!string.IsNullOrEmpty(config.MasterScopeBoxName) && scopeBoxMap.ContainsKey(config.MasterScopeBoxName))
            {
                masterScopeBoxId = scopeBoxMap[config.MasterScopeBoxName];
            }

            using (Transaction tx = new Transaction(_doc, "BauTools: Generate Architectural Packages"))
            {
                tx.Start();

                foreach (PackageSetting pkg in packageSettings)
                {
                    if (!pkg.IsEnabled) continue;

                    int effectiveScale = pkg.ScaleValue > 0 ? pkg.ScaleValue : (globalViewScale > 0 ? globalViewScale : 96);

                    // Case A: Master Overall Campus Package
                    if (pkg.PackageType == ViewPackageType.MasterOverall)
                    {
                        HashSet<string> processedMasterLevels = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                        foreach (BuildingDefinition bldg in targetBuildings)
                        {
                            foreach (TypicalFloorGroup group in bldg.TypicalGroups)
                            {
                                string srcLevelName = group.IsDuplexModule ? group.SourceLevelNameLower : group.SourceLevelName;
                                if (string.IsNullOrEmpty(srcLevelName) || processedMasterLevels.Contains(srcLevelName)) continue;
                                processedMasterLevels.Add(srcLevelName);

                                Level srcLevel = GetLevelByName(srcLevelName);
                                if (srcLevel == null) continue;

                                string rangeLabel = GetGroupRangeLabel(group);
                                string viewName = string.Format("FL. {0} - MASTER OVERALL FLOOR PLAN", rangeLabel);
                                string titleOnSheet = string.Format("MASTER - {0} OVERALL FLOOR PLAN", rangeLabel.ToUpperInvariant());

                                ViewPlan plan = null;
                                if (floorPlanVft != null)
                                {
                                    plan = ViewPlan.Create(_doc, floorPlanVft.Id, srcLevel.Id);
                                }

                                if (plan != null)
                                {
                                    plan.Name = GetUniqueViewName(viewName);
                                    plan.Scale = effectiveScale;

                                    if (pkg.SelectedTemplateId != ElementId.InvalidElementId)
                                    {
                                        try { plan.ViewTemplateId = pkg.SelectedTemplateId; } catch { }
                                    }

                                    if (masterScopeBoxId != ElementId.InvalidElementId)
                                    {
                                        AssignScopeBoxToView(plan, masterScopeBoxId);
                                    }

                                    SetTitleOnSheetParameter(plan, titleOnSheet);
                                    SetViewBuildingParameter(plan, config.ViewBuildingParameterName, "Master");
                                    createdMap[plan.Name] = plan.Id;
                                }
                            }
                        }
                        continue;
                    }

                    // Case B: Building-Specific Packages (Gross, Deductions, Life Safety, RCP, Architectural)
                    foreach (BuildingDefinition bldg in targetBuildings)
                    {
                        ElementId bldgScopeBoxId = ElementId.InvalidElementId;
                        if (!string.IsNullOrEmpty(bldg.ScopeBoxName) && scopeBoxMap.ContainsKey(bldg.ScopeBoxName))
                        {
                            bldgScopeBoxId = scopeBoxMap[bldg.ScopeBoxName];
                        }

                        foreach (TypicalFloorGroup group in bldg.TypicalGroups)
                        {
                            string srcLevelName = group.IsDuplexModule ? group.SourceLevelNameLower : group.SourceLevelName;
                            if (string.IsNullOrEmpty(srcLevelName)) continue;

                            Level srcLevel = GetLevelByName(srcLevelName);
                            if (srcLevel == null) continue;

                            string rangeLabel = GetGroupRangeLabel(group);
                            string bldgTag = bldg.Name.ToUpperInvariant();

                            ViewPlan plan = null;
                            string viewName = "";
                            string titleOnSheet = "";

                            switch (pkg.PackageType)
                            {
                                case ViewPackageType.Architectural:
                                    if (floorPlanVft != null)
                                    {
                                        plan = ViewPlan.Create(_doc, floorPlanVft.Id, srcLevel.Id);
                                        viewName = string.Format("FL. {0} - ARCHITECTURAL PLAN ({1})", rangeLabel, bldgTag);
                                        titleOnSheet = string.Format("{0} - {1} FLOOR PLAN", bldgTag, rangeLabel.ToUpperInvariant());
                                    }
                                    break;

                                case ViewPackageType.CeilingPlanRCP:
                                    if (ceilingPlanVft != null)
                                    {
                                        plan = ViewPlan.Create(_doc, ceilingPlanVft.Id, srcLevel.Id);
                                        viewName = string.Format("FL. {0} - CEILING PLAN RCP ({1})", rangeLabel, bldgTag);
                                        titleOnSheet = string.Format("{0} - {1} REFLECTED CEILING PLAN", bldgTag, rangeLabel.ToUpperInvariant());
                                    }
                                    break;

                                case ViewPackageType.GrossArea:
                                    if (grossScheme != null)
                                    {
                                        plan = ViewPlan.CreateAreaPlan(_doc, grossScheme.Id, srcLevel.Id);
                                        viewName = string.Format("FL. {0} - GROSS AREA PLAN ({1})", rangeLabel, bldgTag);
                                        titleOnSheet = string.Format("{0} - {1} GROSS AREA PLAN", bldgTag, rangeLabel.ToUpperInvariant());
                                    }
                                    break;

                                case ViewPackageType.Deductions:
                                    if (dedScheme != null)
                                    {
                                        plan = ViewPlan.CreateAreaPlan(_doc, dedScheme.Id, srcLevel.Id);
                                        viewName = string.Format("FL. {0} - DEDUCTIONS PLAN ({1})", rangeLabel, bldgTag);
                                        titleOnSheet = string.Format("{0} - {1} DEDUCTIONS PLAN", bldgTag, rangeLabel.ToUpperInvariant());
                                    }
                                    break;

                                case ViewPackageType.EgressLifeSafety:
                                    if (floorPlanVft != null)
                                    {
                                        plan = ViewPlan.Create(_doc, floorPlanVft.Id, srcLevel.Id);
                                        viewName = string.Format("FL. {0} - LIFE SAFETY PLAN ({1})", rangeLabel, bldgTag);
                                        titleOnSheet = string.Format("{0} - {1} LIFE SAFETY PLAN", bldgTag, rangeLabel.ToUpperInvariant());
                                    }
                                    break;
                            }

                            if (plan != null)
                            {
                                plan.Name = GetUniqueViewName(viewName);
                                plan.Scale = effectiveScale;

                                if (pkg.SelectedTemplateId != ElementId.InvalidElementId)
                                {
                                    try { plan.ViewTemplateId = pkg.SelectedTemplateId; } catch { }
                                }

                                if (bldgScopeBoxId != ElementId.InvalidElementId)
                                {
                                    AssignScopeBoxToView(plan, bldgScopeBoxId);
                                }
                                else if (masterScopeBoxId != ElementId.InvalidElementId)
                                {
                                    AssignScopeBoxToView(plan, masterScopeBoxId);
                                }

                                SetTitleOnSheetParameter(plan, titleOnSheet);
                                SetViewBuildingParameter(plan, config.ViewBuildingParameterName, bldg.Name);
                                createdMap[plan.Name] = plan.Id;
                            }
                        }
                    }
                }

                tx.Commit();
            }

            return createdMap;
        }

        private void SetTitleOnSheetParameter(View view, string titleText)
        {
            if (view == null || string.IsNullOrEmpty(titleText)) return;
            try
            {
                Parameter p = view.get_Parameter(BuiltInParameter.VIEW_DESCRIPTION);
                if (p != null && !p.IsReadOnly)
                {
                    p.Set(titleText);
                }
            }
            catch
            {
            }
        }

        public string GetGroupRangeLabel(TypicalFloorGroup g)
        {
            if (g == null) return "TYPICAL";
            if (g.IsSingleLevel) return g.SourceLevelName ?? "TYP";
            if (g.IsDuplexModule)
            {
                return string.Format("{0}-{1} (DUPLEX)", g.FromLevelName, g.ToLevelName);
            }
            return string.Format("{0} TO {1}", g.FromLevelName, g.ToLevelName);
        }

        private Level GetLevelByName(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            return new FilteredElementCollector(_doc)
                .OfClass(typeof(Level))
                .Cast<Level>()
                .FirstOrDefault(l => string.Equals(l.Name, name, StringComparison.OrdinalIgnoreCase));
        }

        private AreaScheme GetAreaSchemeByName(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            return new FilteredElementCollector(_doc)
                .OfClass(typeof(AreaScheme))
                .Cast<AreaScheme>()
                .FirstOrDefault(s => string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase));
        }

        private ViewFamilyType GetViewFamilyType(ViewFamily vf)
        {
            return new FilteredElementCollector(_doc)
                .OfClass(typeof(ViewFamilyType))
                .Cast<ViewFamilyType>()
                .FirstOrDefault(vft => vft.ViewFamily == vf);
        }

        private Dictionary<string, ElementId> GetScopeBoxElementMap()
        {
            Dictionary<string, ElementId> map = new Dictionary<string, ElementId>(StringComparer.OrdinalIgnoreCase);
            try
            {
                FilteredElementCollector collector = new FilteredElementCollector(_doc)
                    .OfCategory(BuiltInCategory.OST_VolumeOfInterest)
                    .WhereElementIsNotElementType();

                foreach (Element elem in collector)
                {
                    if (elem != null && !string.IsNullOrEmpty(elem.Name))
                    {
                        map[elem.Name] = elem.Id;
                    }
                }
            }
            catch
            {
            }
            return map;
        }

        private void AssignScopeBoxToView(View view, ElementId scopeBoxId)
        {
            if (view == null || scopeBoxId == ElementId.InvalidElementId) return;
            try
            {
                Parameter p = view.get_Parameter(BuiltInParameter.VIEWER_VOLUME_OF_INTEREST_CROP);
                if (p != null && !p.IsReadOnly)
                {
                    p.Set(scopeBoxId);
                }
            }
            catch
            {
            }
        }

        private void SetViewBuildingParameter(View view, string paramName, string buildingName)
        {
            if (view == null || string.IsNullOrEmpty(paramName) || string.IsNullOrEmpty(buildingName)) return;
            try
            {
                Parameter p = view.LookupParameter(paramName);
                if (p != null && p.StorageType == StorageType.String && !p.IsReadOnly)
                {
                    p.Set(buildingName);
                }
            }
            catch
            {
            }
        }

        private string GetUniqueViewName(string baseName)
        {
            string candidate = baseName;
            int counter = 2;
            while (IsViewNameExists(candidate))
            {
                candidate = string.Format("{0} ({1})", baseName, counter);
                counter++;
            }
            return candidate;
        }

        public List<GeneratedViewResult> GenerateMasterAndDependentViews(
            List<BuildingDefinition> buildings,
            MappingConfig config,
            bool createArchPlans,
            bool createGrossPlans,
            bool createDedPlans,
            bool typicalMasterOnly)
        {
            List<GeneratedViewResult> results = new List<GeneratedViewResult>();
            if (buildings == null || buildings.Count == 0) return results;

            Dictionary<string, ElementId> scopeBoxMap = GetScopeBoxElementMap();
            ViewFamilyType floorPlanVft = GetViewFamilyType(ViewFamily.FloorPlan);
            ViewFamilyType areaPlanVft = GetViewFamilyType(ViewFamily.AreaPlan);

            AreaScheme grossScheme = !string.IsNullOrEmpty(config.GrossAreaSchemeName) ? GetAreaSchemeByName(config.GrossAreaSchemeName) : null;
            AreaScheme dedScheme = !string.IsNullOrEmpty(config.DeductionAreaSchemeName) ? GetAreaSchemeByName(config.DeductionAreaSchemeName) : null;

            ElementId masterScopeBoxId = ElementId.InvalidElementId;
            if (!string.IsNullOrEmpty(config.MasterScopeBoxName) && scopeBoxMap.ContainsKey(config.MasterScopeBoxName))
            {
                masterScopeBoxId = scopeBoxMap[config.MasterScopeBoxName];
            }

            using (Transaction tx = new Transaction(_doc, "BauTools: Create Master & Dependent Views"))
            {
                tx.Start();

                HashSet<string> processedLevels = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (BuildingDefinition bldg in buildings)
                {
                    foreach (TypicalFloorGroup group in bldg.TypicalGroups)
                    {
                        string srcLevelName = group.IsDuplexModule ? group.SourceLevelNameLower : group.SourceLevelName;
                        if (string.IsNullOrEmpty(srcLevelName)) continue;

                        Level srcLevel = GetLevelByName(srcLevelName);
                        if (srcLevel == null) continue;

                        string rangeLabel = GetGroupRangeLabel(group);

                        if (createArchPlans && floorPlanVft != null)
                        {
                            string key = string.Format("ARCH_{0}", srcLevelName);
                            if (!processedLevels.Contains(key))
                            {
                                processedLevels.Add(key);
                                ViewPlan masterView = ViewPlan.Create(_doc, floorPlanVft.Id, srcLevel.Id);
                                masterView.Name = GetUniqueViewName(string.Format("FL. {0} - MASTER OVERALL FLOOR PLAN", rangeLabel));
                                if (masterScopeBoxId != ElementId.InvalidElementId) AssignScopeBoxToView(masterView, masterScopeBoxId);

                                GeneratedViewResult gvr = new GeneratedViewResult
                                {
                                    MasterView = masterView,
                                    RangeLabel = rangeLabel,
                                    ViewTypeLabel = "Architectural"
                                };

                                if (buildings.Count > 1)
                                {
                                    foreach (BuildingDefinition subBldg in buildings)
                                    {
                                        ElementId depId = masterView.Duplicate(ViewDuplicateOption.AsDependent);
                                        View depView = _doc.GetElement(depId) as View;
                                        if (depView != null)
                                        {
                                            depView.Name = GetUniqueViewName(string.Format("FL. {0} - {1} FLOOR PLAN", rangeLabel, subBldg.Name.ToUpperInvariant()));
                                            if (!string.IsNullOrEmpty(subBldg.ScopeBoxName) && scopeBoxMap.ContainsKey(subBldg.ScopeBoxName))
                                            {
                                                AssignScopeBoxToView(depView, scopeBoxMap[subBldg.ScopeBoxName]);
                                            }
                                            SetViewBuildingParameter(depView, config.ViewBuildingParameterName, subBldg.Name);
                                            gvr.DependentViews.Add(depView);
                                        }
                                    }
                                }
                                results.Add(gvr);
                            }
                        }
                    }
                }

                tx.Commit();
            }

            return results;
        }

        private bool IsViewNameExists(string name)
        {
            return new FilteredElementCollector(_doc)
                .OfClass(typeof(View))
                .Cast<View>()
                .Any(v => !v.IsTemplate && string.Equals(v.Name, name, StringComparison.OrdinalIgnoreCase));
        }
    }
}