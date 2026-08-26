using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using ZoningFloorArea.Models;

namespace ZoningFloorArea.Services
{
    public class RevitMassingBakerService
    {
        private readonly Document _doc;

        public RevitMassingBakerService(Document doc)
        {
            _doc = doc;
        }

        public int BakeScenariosIntoDesignOptions(
            List<GenerativeScenario> scenariosToBake,
            bool assignDesignOptions,
            bool createLevels,
            string optionSetName)
        {
            if (scenariosToBake == null || scenariosToBake.Count == 0) return 0;
            int totalShapesCreated = 0;

            using (Transaction tx = new Transaction(_doc, "Bake Generative Massing Options"))
            {
                tx.Start();

                try
                {
                    // 1. Create Levels if requested
                    if (createLevels)
                    {
                        CreateProjectLevelsFromScenarios(scenariosToBake);
                    }

                    // 2. Fetch existing Design Options in project if any
                    List<DesignOption> existingOptions = new List<DesignOption>();
                    if (assignDesignOptions)
                    {
                        FilteredElementCollector optColl = new FilteredElementCollector(_doc).OfClass(typeof(DesignOption));
                        existingOptions = optColl.Cast<DesignOption>().ToList();
                    }

                    // 3. For each selected scenario, create DirectShape mass volumes
                    for (int sIdx = 0; sIdx < scenariosToBake.Count; sIdx++)
                    {
                        GenerativeScenario s = scenariosToBake[sIdx];
                        
                        // Try matching existing design option by name or index
                        ElementId designOptionId = ElementId.InvalidElementId;
                        if (existingOptions.Count > 0)
                        {
                            DesignOption matched = existingOptions.FirstOrDefault(o => o.Name.IndexOf(s.Title, StringComparison.OrdinalIgnoreCase) >= 0);
                            if (matched != null)
                            {
                                designOptionId = matched.Id;
                            }
                            else if (sIdx < existingOptions.Count)
                            {
                                designOptionId = existingOptions[sIdx].Id;
                            }
                        }

                        // Create 3D Solid DirectShapes for each floor block
                        foreach (MassingFloorBlock f in s.Floors)
                        {
                            DirectShape ds = CreateFloorMassDirectShape(f, s.Title);
                            if (ds != null)
                            {
                                totalShapesCreated++;

                                if (designOptionId != ElementId.InvalidElementId)
                                {
                                    Parameter pOpt = ds.get_Parameter(BuiltInParameter.DESIGN_OPTION_PARAM);
                                    if (pOpt != null && !pOpt.IsReadOnly)
                                    {
                                        pOpt.Set(designOptionId);
                                    }
                                }
                            }
                        }
                    }

                    tx.Commit();
                }
                catch
                {
                    if (tx.HasStarted()) tx.RollBack();
                    throw;
                }
            }

            return totalShapesCreated;
        }

        private DirectShape CreateFloorMassDirectShape(MassingFloorBlock f, string scenarioTitle)
        {
            try
            {
                double halfW = f.WidthFt / 2.0;
                double halfD = f.DepthFt / 2.0;

                XYZ p0 = new XYZ(-halfW, -halfD, f.ElevationFt);
                XYZ p1 = new XYZ(halfW, -halfD, f.ElevationFt);
                XYZ p2 = new XYZ(halfW, halfD, f.ElevationFt);
                XYZ p3 = new XYZ(-halfW, halfD, f.ElevationFt);

                Line l0 = Line.CreateBound(p0, p1);
                Line l1 = Line.CreateBound(p1, p2);
                Line l2 = Line.CreateBound(p2, p3);
                Line l3 = Line.CreateBound(p3, p0);

                CurveLoop loop = new CurveLoop();
                loop.Append(l0);
                loop.Append(l1);
                loop.Append(l2);
                loop.Append(l3);

                List<CurveLoop> loops = new List<CurveLoop> { loop };
                Solid solid = GeometryCreationUtilities.CreateExtrusionGeometry(loops, XYZ.BasisZ, f.HeightFt);

                if (solid == null) return null;

                DirectShape ds = DirectShape.CreateElement(_doc, new ElementId(BuiltInCategory.OST_GenericModel));
                ds.SetShape(new GeometryObject[] { solid });
                ds.Name = string.Format("{0} - {1}", scenarioTitle, f.LevelName);

                Parameter pComments = ds.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS);
                if (pComments != null && !pComments.IsReadOnly)
                {
                    pComments.Set(string.Format("BauTools Generative | Option: {0} | Usage: {1} | Area: {2:N0} SF", scenarioTitle, f.UsageType, f.AreaSqFt));
                }

                return ds;
            }
            catch
            {
                return null;
            }
        }

        private void CreateProjectLevelsFromScenarios(List<GenerativeScenario> scenarios)
        {
            FilteredElementCollector lvlColl = new FilteredElementCollector(_doc).OfClass(typeof(Level));
            List<Level> existingLevels = lvlColl.Cast<Level>().ToList();

            GenerativeScenario maxScenario = scenarios.OrderByDescending(s => s.Floors.Count).FirstOrDefault();
            if (maxScenario == null) return;

            foreach (MassingFloorBlock f in maxScenario.Floors)
            {
                bool exists = existingLevels.Any(l => Math.Abs(l.Elevation - f.ElevationFt) < 0.1);
                if (!exists)
                {
                    try
                    {
                        Level newLvl = Level.Create(_doc, f.ElevationFt);
                        string safeName = string.Format("FL. {0:D2}", f.LevelIndex);
                        int counter = 1;
                        while (existingLevels.Any(l => string.Equals(l.Name, safeName, StringComparison.OrdinalIgnoreCase)))
                        {
                            safeName = string.Format("FL. {0:D2} ({1})", f.LevelIndex, counter++);
                        }
                        newLvl.Name = safeName;
                        existingLevels.Add(newLvl);
                    }
                    catch
                    {
                        // Ignore level naming conflicts
                    }
                }
            }
        }
    }
}