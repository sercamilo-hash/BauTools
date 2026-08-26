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
            int viewScale,
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

                        foreach (PackageSetting pkg in packageSettings)
                        {
                            if (!pkg.IsEnabled) continue;

                            ViewPlan plan = null;
                            string viewName = "";

                            switch (pkg.PackageType)
                            {
                                case ViewPackageType.Architectural:
                                    if (floorPlanVft != null)
                                    {
                                        plan = ViewPlan.Create(_doc, floorPlanVft.Id, srcLevel.Id);
                                        viewName = string.Format("FL. {0} - ARCHITECTURAL PLAN ({1})", rangeLabel, bldg.Name.ToUpperInvariant());
                                    }
                                    break;

                                case ViewPackageType.CeilingPlanRCP:
                                    if (ceilingPlanVft != null)
                                    {
                                        plan = ViewPlan.Create(_doc, ceilingPlanVft.Id, srcLevel.Id);
                                        viewName = string.Format("FL. {0} - CEILING PLAN RCP ({1})", rangeLabel, bldg.Name.ToUpperInvariant());
                                    }
                                    break;

                                case ViewPackageType.GrossArea:
                                    if (grossScheme != null)
                                    {
                                        plan = ViewPlan.CreateAreaPlan(_doc, grossScheme.Id, srcLevel.Id);
                                        viewName = string.Format("FL. {0} - GROSS AREA PLAN ({1})", rangeLabel, bldg.Name.ToUpperInvariant());
                                    }
                                    break;

                                case ViewPackageType.Deductions:
                                    if (dedScheme != null)
                                    {
                                        plan = ViewPlan.CreateAreaPlan(_doc, dedScheme.Id, srcLevel.Id);
                                        viewName = string.Format("FL. {0} - DEDUCTIONS PLAN ({1})", rangeLabel, bldg.Name.ToUpperInvariant());
                                    }
                                    break;

                                case ViewPackageType.EgressLifeSafety:
                                    if (floorPlanVft != null)
                                    {
                                        plan = ViewPlan.Create(_doc, floorPlanVft.Id, srcLevel.Id);
                                        viewName = string.Format("FL. {0} - EGRESS & LIFE SAFETY ({1})", rangeLabel, bldg.Name.ToUpperInvariant());
                                    }
                                    break;
                            }

                            if (plan != null)
                            {
                                plan.Name = GetUniqueViewName(viewName);
                                if (viewScale > 0) plan.Scale = viewScale;

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

                List<TypicalFloorGroup> allGroups = new List<TypicalFloorGroup>();
                foreach (BuildingDefinition bldg in buildings)
                {
                    foreach (TypicalFloorGroup g in bldg.TypicalGroups)
                    {
                        allGroups.Add(g);
                    }
                }

                foreach (TypicalFloorGroup group in allGroups)
                {
                    string srcLevelName = group.IsDuplexModule ? group.SourceLevelNameLower : group.SourceLevelName;
                    if (string.IsNullOrEmpty(srcLevelName)) continue;

                    Level srcLevel = GetLevelByName(srcLevelName);
                    if (srcLevel == null) continue;

                    string rangeStr = GetGroupRangeLabel(group);

                    if (createArchPlans && floorPlanVft != null)
                    {
                        GeneratedViewResult res = CreateMasterAndDependentsForType(
                            srcLevel, floorPlanVft, null, group, buildings, config,
                            rangeStr, "ARCHITECTURAL PLAN", masterScopeBoxId, scopeBoxMap);
                        if (res != null) results.Add(res);
                    }

                    if (createGrossPlans && areaPlanVft != null && grossScheme != null)
                    {
                        GeneratedViewResult res = CreateMasterAndDependentsForType(
                            srcLevel, areaPlanVft, grossScheme, group, buildings, config,
                            rangeStr, "GROSS AREA PLAN", masterScopeBoxId, scopeBoxMap);
                        if (res != null) results.Add(res);
                    }

                    if (createDedPlans && areaPlanVft != null && dedScheme != null)
                    {
                        GeneratedViewResult res = CreateMasterAndDependentsForType(
                            srcLevel, areaPlanVft, dedScheme, group, buildings, config,
                            rangeStr, "DEDUCTIONS PLAN", masterScopeBoxId, scopeBoxMap);
                        if (res != null) results.Add(res);
                    }
                }

                tx.Commit();
            }

            return results;
        }

        private GeneratedViewResult CreateMasterAndDependentsForType(
            Level srcLevel,
            ViewFamilyType vft,
            AreaScheme scheme,
            TypicalFloorGroup group,
            List<BuildingDefinition> buildings,
            MappingConfig config,
            string rangeStr,
            string typeLabel,
            ElementId masterScopeBoxId,
            Dictionary<string, ElementId> scopeBoxMap)
        {
            ViewPlan masterView = null;

            if (scheme != null)
            {
                masterView = ViewPlan.CreateAreaPlan(_doc, scheme.Id, srcLevel.Id);
            }
            else
            {
                masterView = ViewPlan.Create(_doc, vft.Id, srcLevel.Id);
            }

            if (masterView == null) return null;

            string masterName = string.Format("FL. {0} - {1} (OVERALL MASTER)", rangeStr, typeLabel);
            masterView.Name = GetUniqueViewName(masterName);

            if (masterScopeBoxId != ElementId.InvalidElementId)
            {
                AssignScopeBoxToView(masterView, masterScopeBoxId);
            }

            GeneratedViewResult result = new GeneratedViewResult();
            result.MasterView = masterView;
            result.RangeLabel = rangeStr;
            result.ViewTypeLabel = typeLabel;

            foreach (BuildingDefinition bldg in buildings)
            {
                ElementId bldgScopeBoxId = ElementId.InvalidElementId;
                if (!string.IsNullOrEmpty(bldg.ScopeBoxName) && scopeBoxMap.ContainsKey(bldg.ScopeBoxName))
                {
                    bldgScopeBoxId = scopeBoxMap[bldg.ScopeBoxName];
                }

                try
                {
                    ElementId depViewId = masterView.Duplicate(ViewDuplicateOption.AsDependent);
                    View depView = _doc.GetElement(depViewId) as View;
                    if (depView != null)
                    {
                        string depName = string.Format("FL. {0} - {1} ({2})", rangeStr, typeLabel, bldg.Name.ToUpperInvariant());
                        depView.Name = GetUniqueViewName(depName);

                        if (bldgScopeBoxId != ElementId.InvalidElementId)
                        {
                            AssignScopeBoxToView(depView, bldgScopeBoxId);
                        }

                        SetViewBuildingParameter(depView, config.ViewBuildingParameterName, bldg.Name);
                        result.DependentViews.Add(depView);
                    }
                }
                catch
                {
                }
            }

            return result;
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

        private void SetViewBuildingParameter(View view, string paramName, string buildingValue)
        {
            if (view == null || string.IsNullOrEmpty(paramName) || string.IsNullOrEmpty(buildingValue)) return;
            try
            {
                Parameter p = view.LookupParameter(paramName);
                if (p != null && !p.IsReadOnly && p.StorageType == StorageType.String)
                {
                    p.Set(buildingValue);
                }
                else
                {
                    Parameter pComm = view.get_Parameter(BuiltInParameter.VIEW_DESCRIPTION);
                    if (pComm != null && !pComm.IsReadOnly)
                    {
                        pComm.Set(buildingValue);
                    }
                }
            }
            catch
            {
            }
        }

        public string GetGroupRangeLabel(TypicalFloorGroup group)
        {
            if (group.IsSingleLevel)
            {
                return group.SourceLevelName.ToUpperInvariant();
            }

            string from = CleanLevelPrefix(group.FromLevelName);
            string to = CleanLevelPrefix(group.ToLevelName);

            if (group.IsDuplexModule)
            {
                return string.Format("{0}-{1} DUPLEX", from, to);
            }

            return string.Format("{0}-{1} TYPICAL", from, to);
        }

        private string CleanLevelPrefix(string levelName)
        {
            if (string.IsNullOrEmpty(levelName)) return "";
            return levelName.Replace("Level ", "L").Replace("Piso ", "P").Replace("Nivel ", "N");
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

        private ViewFamilyType GetViewFamilyType(ViewFamily viewFamily)
        {
            FilteredElementCollector collector = new FilteredElementCollector(_doc).OfClass(typeof(ViewFamilyType));
            foreach (ViewFamilyType vft in collector)
            {
                if (vft.ViewFamily == viewFamily) return vft;
            }
            return null;
        }

        private AreaScheme GetAreaSchemeByName(string schemeName)
        {
            FilteredElementCollector collector = new FilteredElementCollector(_doc).OfClass(typeof(AreaScheme));
            foreach (AreaScheme s in collector)
            {
                if (string.Equals(s.Name, schemeName, StringComparison.OrdinalIgnoreCase)) return s;
            }
            return null;
        }

        private Level GetLevelByName(string name)
        {
            FilteredElementCollector collector = new FilteredElementCollector(_doc).OfClass(typeof(Level));
            foreach (Level l in collector)
            {
                if (string.Equals(l.Name, name, StringComparison.OrdinalIgnoreCase)) return l;
            }
            return null;
        }

        private string GetUniqueViewName(string baseName)
        {
            string name = baseName;
            int counter = 1;

            while (IsViewNameTaken(name))
            {
                name = string.Format("{0} ({1})", baseName, counter);
                counter++;
            }

            return name;
        }

        private bool IsViewNameTaken(string name)
        {
            FilteredElementCollector collector = new FilteredElementCollector(_doc).OfClass(typeof(View));
            foreach (View v in collector)
            {
                if (string.Equals(v.Name, name, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }
    }
}