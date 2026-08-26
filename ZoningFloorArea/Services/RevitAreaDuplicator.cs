using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using ZoningFloorArea.Models;

namespace ZoningFloorArea.Services
{
    public class RevitAreaDuplicator
    {
        private readonly Document _doc;

        public RevitAreaDuplicator(Document doc)
        {
            _doc = doc;
        }

        public List<Level> GetAllLevels()
        {
            return new FilteredElementCollector(_doc)
                .OfClass(typeof(Level))
                .Cast<Level>()
                .OrderBy(l => l.Elevation)
                .ToList();
        }

        public List<string> GetLevelsInRange(string fromLevelName, string toLevelName)
        {
            List<Level> levels = GetAllLevels();
            if (levels.Count == 0) return new List<string>();

            Level fromLvl = levels.FirstOrDefault(l => string.Equals(l.Name, fromLevelName, StringComparison.OrdinalIgnoreCase));
            Level toLvl = levels.FirstOrDefault(l => string.Equals(l.Name, toLevelName, StringComparison.OrdinalIgnoreCase));

            if (fromLvl == null || toLvl == null) return new List<string>();

            double minElev = Math.Min(fromLvl.Elevation, toLvl.Elevation) - 0.001;
            double maxElev = Math.Max(fromLvl.Elevation, toLvl.Elevation) + 0.001;

            return levels
                .Where(l => l.Elevation >= minElev && l.Elevation <= maxElev)
                .OrderBy(l => l.Elevation)
                .Select(l => l.Name)
                .ToList();
        }

        public string GetLevelAreaSummary(string levelName, string grossSchemeName, string dedSchemeName)
        {
            if (string.IsNullOrEmpty(levelName)) return "No level selected";

            List<Level> levels = GetAllLevels();
            Level lvl = levels.FirstOrDefault(l => string.Equals(l.Name, levelName, StringComparison.OrdinalIgnoreCase));
            if (lvl == null) return "Level not found";

            int totalAreas = 0;
            double totalSqFt = 0;

            FilteredElementCollector collector = new FilteredElementCollector(_doc)
                .OfCategory(BuiltInCategory.OST_Areas)
                .WhereElementIsNotElementType();

            foreach (Area a in collector.Cast<Area>())
            {
                if (a.LevelId == lvl.Id && a.Area > 0)
                {
                    totalAreas++;
                    totalSqFt += a.Area;
                }
            }

            if (totalAreas == 0)
            {
                return "⚠️ 0 Areas modeled (Empty level)";
            }

            return string.Format("🟢 {0} Area(s) modeled ({1:N0} SF)", totalAreas, totalSqFt);
        }

        public string PropagateMultipleGroups(
            List<TypicalFloorGroup> groups,
            MappingConfig config,
            bool propagateGross,
            bool propagateDeductions)
        {
            if (groups == null || groups.Count == 0)
            {
                return "No typical floor groups defined.";
            }

            int totalCreatedAreas = 0;
            int totalLevelsUpdated = 0;
            int processedGroups = 0;

            List<Level> allLevels = GetAllLevels();

            using (Transaction tx = new Transaction(_doc, "BauTools: Propagate Typical Floor Areas"))
            {
                tx.Start();

                foreach (TypicalFloorGroup group in groups)
                {
                    if (group.IsSingleLevel)
                        continue; // Single floor: skip propagation

                    if (group.IsDuplexModule)
                    {
                        Level srcLower = allLevels.FirstOrDefault(l => string.Equals(l.Name, group.SourceLevelNameLower, StringComparison.OrdinalIgnoreCase));
                        Level srcUpper = allLevels.FirstOrDefault(l => string.Equals(l.Name, group.SourceLevelNameUpper, StringComparison.OrdinalIgnoreCase));

                        List<string> targetLevels = GetLevelsInRange(group.FromLevelName, group.ToLevelName);
                        if (targetLevels.Count == 0) continue;

                        int groupCreatedCount = 0;
                        int duplexLevelsUpdated = 0;

                        for (int i = 0; i < targetLevels.Count; i++)
                        {
                            string targetName = targetLevels[i];
                            bool isLowerStep = (i % 2 == 0);
                            Level activeSrc = isLowerStep ? srcLower : srcUpper;

                            if (activeSrc == null || string.Equals(targetName, activeSrc.Name, StringComparison.OrdinalIgnoreCase))
                                continue;

                            List<string> singleTargetList = new List<string>();
                            singleTargetList.Add(targetName);

                            if (propagateGross)
                            {
                                groupCreatedCount += PropagateSchemeAreas(activeSrc, singleTargetList, config.GrossAreaSchemeName, config, group.Name);
                            }
                            if (propagateDeductions)
                            {
                                groupCreatedCount += PropagateSchemeAreas(activeSrc, singleTargetList, config.DeductionAreaSchemeName, config, group.Name);
                            }
                            duplexLevelsUpdated++;
                        }

                        totalCreatedAreas += groupCreatedCount;
                        totalLevelsUpdated += duplexLevelsUpdated;
                        processedGroups++;
                    }
                    else
                    {
                        Level sourceLevel = allLevels.FirstOrDefault(l => string.Equals(l.Name, group.SourceLevelName, StringComparison.OrdinalIgnoreCase));
                        if (sourceLevel == null) continue;

                        List<string> targetLevels = GetLevelsInRange(group.FromLevelName, group.ToLevelName);
                        List<string> actualTargets = targetLevels.Where(n => !string.Equals(n, sourceLevel.Name, StringComparison.OrdinalIgnoreCase)).ToList();

                        if (actualTargets.Count == 0) continue;

                        int groupCreatedCount = 0;

                        if (propagateGross)
                        {
                            groupCreatedCount += PropagateSchemeAreas(sourceLevel, actualTargets, config.GrossAreaSchemeName, config, group.Name);
                        }

                        if (propagateDeductions)
                        {
                            groupCreatedCount += PropagateSchemeAreas(sourceLevel, actualTargets, config.DeductionAreaSchemeName, config, group.Name);
                        }

                        totalCreatedAreas += groupCreatedCount;
                        totalLevelsUpdated += actualTargets.Count;
                        processedGroups++;
                    }
                }

                tx.Commit();
            }

            if (processedGroups == 0)
            {
                return "All defined groups are single-level or have no target floors to duplicate.";
            }

            return string.Format("Successfully propagated {0} typical group(s) across {1} target floor(s). Created {2} area elements without recreating views.", 
                processedGroups, totalLevelsUpdated, totalCreatedAreas);
        }

        public string ClearPropagatedAreas(
            List<TypicalFloorGroup> groups,
            MappingConfig config,
            bool clearGross,
            bool clearDeductions)
        {
            if (groups == null || groups.Count == 0)
            {
                return "No typical floor groups defined to clear.";
            }

            int clearedLevelsCount = 0;
            int clearedElementsCount = 0;
            List<Level> allLevels = GetAllLevels();

            using (Transaction tx = new Transaction(_doc, "BauTools: Clear Propagated Areas"))
            {
                tx.Start();

                foreach (TypicalFloorGroup group in groups)
                {
                    if (group.IsSingleLevel)
                        continue; // Single floor: untouched

                    Level sourceLevel = allLevels.FirstOrDefault(l => string.Equals(l.Name, group.SourceLevelName, StringComparison.OrdinalIgnoreCase));
                    List<string> targetLevels = GetLevelsInRange(group.FromLevelName, group.ToLevelName);
                    List<string> actualTargets = targetLevels.Where(n => sourceLevel == null || !string.Equals(n, sourceLevel.Name, StringComparison.OrdinalIgnoreCase)).ToList();

                    foreach (string targetLevelName in actualTargets)
                    {
                        Level targetLevel = allLevels.FirstOrDefault(l => string.Equals(l.Name, targetLevelName, StringComparison.OrdinalIgnoreCase));
                        if (targetLevel == null) continue;

                        int lvlDeleted = 0;

                        if (clearGross && !string.IsNullOrEmpty(config.GrossAreaSchemeName))
                        {
                            AreaScheme grossScheme = GetAreaSchemeByName(config.GrossAreaSchemeName);
                            if (grossScheme != null)
                            {
                                ViewPlan vp = GetExistingAreaViewPlan(targetLevel, grossScheme);
                                if (vp != null)
                                {
                                    lvlDeleted += ClearViewAreasAndLines(vp, targetLevel, grossScheme);
                                }
                            }
                        }

                        if (clearDeductions && !string.IsNullOrEmpty(config.DeductionAreaSchemeName))
                        {
                            AreaScheme dedScheme = GetAreaSchemeByName(config.DeductionAreaSchemeName);
                            if (dedScheme != null)
                            {
                                ViewPlan vp = GetExistingAreaViewPlan(targetLevel, dedScheme);
                                if (vp != null)
                                {
                                    lvlDeleted += ClearViewAreasAndLines(vp, targetLevel, dedScheme);
                                }
                            }
                        }

                        if (lvlDeleted > 0)
                        {
                            clearedElementsCount += lvlDeleted;
                            clearedLevelsCount++;
                        }
                    }
                }

                tx.Commit();
            }

            if (clearedLevelsCount == 0)
            {
                return "No propagated areas were found to clear. Source modeled levels remain intact.";
            }

            return string.Format("Successfully cleared {0} propagated element(s) across {1} target floor(s). Source floors and views were 100% preserved.",
                clearedElementsCount, clearedLevelsCount);
        }

        private int PropagateSchemeAreas(Level sourceLevel, List<string> targetLevelNames, string schemeName, MappingConfig config, string groupName)
        {
            if (string.IsNullOrEmpty(schemeName)) return 0;

            AreaScheme scheme = GetAreaSchemeByName(schemeName);
            if (scheme == null) return 0;

            ViewPlan sourceAreaView = GetExistingAreaViewPlan(sourceLevel, scheme);
            if (sourceAreaView == null) return 0;

            List<ElementId> sourceBoundaryLineIds = GetAreaBoundaryLinesInView(sourceAreaView);
            List<Area> sourceAreas = GetAreasInView(sourceAreaView, sourceLevel, scheme);

            if (sourceAreas.Count == 0 && sourceBoundaryLineIds.Count == 0) return 0;

            int createdAreasCount = 0;
            List<Level> allLevels = GetAllLevels();

            foreach (string targetLevelName in targetLevelNames)
            {
                if (string.Equals(targetLevelName, sourceLevel.Name, StringComparison.OrdinalIgnoreCase))
                    continue;

                Level targetLevel = allLevels.FirstOrDefault(l => string.Equals(l.Name, targetLevelName, StringComparison.OrdinalIgnoreCase));
                if (targetLevel == null) continue;

                ViewPlan targetAreaView = GetOrCreateAreaViewPlan(targetLevel, scheme);
                if (targetAreaView == null) continue;

                // 1. Clear target areas and lines without touching views
                ClearViewAreasAndLines(targetAreaView, targetLevel, scheme);

                // 2. Copy boundary lines
                if (sourceBoundaryLineIds.Count > 0)
                {
                    CopyPasteOptions options = new CopyPasteOptions();
                    ElementTransformUtils.CopyElements(sourceAreaView, sourceBoundaryLineIds, targetAreaView, Transform.Identity, options);
                }

                // 3. Create target areas
                foreach (Area srcArea in sourceAreas)
                {
                    LocationPoint loc = srcArea.Location as LocationPoint;
                    if (loc == null) continue;

                    UV pt = new UV(loc.Point.X, loc.Point.Y);
                    Area targetArea = _doc.Create.NewArea(targetAreaView, pt);

                    if (targetArea != null)
                    {
                        CopyAreaParameters(srcArea, targetArea, config);
                        
                        if (!string.IsNullOrEmpty(groupName))
                        {
                            Parameter pComm = targetArea.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS);
                            if (pComm != null && !pComm.IsReadOnly)
                            {
                                pComm.Set(groupName);
                            }
                        }
                        createdAreasCount++;
                    }
                }
            }

            return createdAreasCount;
        }

        private AreaScheme GetAreaSchemeByName(string schemeName)
        {
            FilteredElementCollector collector = new FilteredElementCollector(_doc).OfClass(typeof(AreaScheme));
            foreach (AreaScheme s in collector)
            {
                if (string.Equals(s.Name, schemeName, StringComparison.OrdinalIgnoreCase))
                    return s;
            }
            return null;
        }

        private ViewPlan GetExistingAreaViewPlan(Level level, AreaScheme scheme)
        {
            FilteredElementCollector collector = new FilteredElementCollector(_doc).OfClass(typeof(ViewPlan));
            foreach (ViewPlan vp in collector)
            {
                if (!vp.IsTemplate && vp.LevelId == level.Id && vp.AreaScheme != null && vp.AreaScheme.Id == scheme.Id)
                {
                    return vp;
                }
            }
            return null;
        }

        private ViewPlan GetOrCreateAreaViewPlan(Level level, AreaScheme scheme)
        {
            ViewPlan existing = GetExistingAreaViewPlan(level, scheme);
            if (existing != null)
            {
                return existing;
            }

            try
            {
                ViewPlan newVp = ViewPlan.CreateAreaPlan(_doc, scheme.Id, level.Id);
                return newVp;
            }
            catch
            {
                return null;
            }
        }

        private List<ElementId> GetAreaBoundaryLinesInView(ViewPlan view)
        {
            FilteredElementCollector collector = new FilteredElementCollector(_doc, view.Id)
                .OfCategory(BuiltInCategory.OST_AreaSchemeLines)
                .WhereElementIsNotElementType();

            return collector.Select(e => e.Id).ToList();
        }

        private List<Area> GetAreasInView(ViewPlan view, Level level, AreaScheme scheme)
        {
            FilteredElementCollector collector = new FilteredElementCollector(_doc)
                .OfCategory(BuiltInCategory.OST_Areas)
                .WhereElementIsNotElementType();

            List<Area> result = new List<Area>();
            foreach (Area a in collector.Cast<Area>())
            {
                if (a.Area > 0 && a.LevelId == level.Id && a.AreaScheme != null && a.AreaScheme.Id == scheme.Id)
                {
                    result.Add(a);
                }
            }
            return result;
        }

        private int ClearViewAreasAndLines(ViewPlan view, Level level, AreaScheme scheme)
        {
            List<ElementId> toDelete = new List<ElementId>();

            // 1. Boundary lines in view
            FilteredElementCollector lines = new FilteredElementCollector(_doc, view.Id)
                .OfCategory(BuiltInCategory.OST_AreaSchemeLines)
                .WhereElementIsNotElementType();
            toDelete.AddRange(lines.Select(e => e.Id));

            // 2. Areas assigned to this level and scheme
            FilteredElementCollector areas = new FilteredElementCollector(_doc)
                .OfCategory(BuiltInCategory.OST_Areas)
                .WhereElementIsNotElementType();
            foreach (Area a in areas.Cast<Area>())
            {
                if (a.LevelId == level.Id && a.AreaScheme != null && a.AreaScheme.Id == scheme.Id)
                {
                    toDelete.Add(a.Id);
                }
            }

            if (toDelete.Count > 0)
            {
                _doc.Delete(toDelete);
                return toDelete.Count;
            }

            return 0;
        }

        private void CopyAreaParameters(Area srcArea, Area targetArea, MappingConfig config)
        {
            Parameter pNameSrc = srcArea.get_Parameter(BuiltInParameter.ROOM_NAME);
            Parameter pNameTgt = targetArea.get_Parameter(BuiltInParameter.ROOM_NAME);
            if (pNameSrc != null && pNameTgt != null && !pNameTgt.IsReadOnly)
            {
                pNameTgt.Set(pNameSrc.AsString() ?? string.Empty);
            }

            CopyParamByName(srcArea, targetArea, config.DeductionTypeParameterName);
            CopyParamByName(srcArea, targetArea, config.BuildingParameterName);
            CopyParamByName(srcArea, targetArea, config.UsageCategoryParameterName);
            CopyParamByName(srcArea, targetArea, "Comments");
            CopyParamByName(srcArea, targetArea, "Deduction");
            CopyParamByName(srcArea, targetArea, "Building");
        }

        private void CopyParamByName(Area src, Area tgt, string paramName)
        {
            if (string.IsNullOrEmpty(paramName)) return;

            Parameter pSrc = src.LookupParameter(paramName);
            Parameter pTgt = tgt.LookupParameter(paramName);

            if (pSrc != null && pTgt != null && !pTgt.IsReadOnly)
            {
                if (pSrc.StorageType == StorageType.String)
                {
                    pTgt.Set(pSrc.AsString() ?? string.Empty);
                }
                else if (pSrc.StorageType == StorageType.Double)
                {
                    pTgt.Set(pSrc.AsDouble());
                }
                else if (pSrc.StorageType == StorageType.Integer)
                {
                    pTgt.Set(pSrc.AsInteger());
                }
            }
        }
    }
}
