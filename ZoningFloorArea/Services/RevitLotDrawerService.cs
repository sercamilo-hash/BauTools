using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using ZoningFloorArea.Models;

namespace ZoningFloorArea.Services
{
    public class LotDrawOptions
    {
        public LotElementType ElementType { get; set; } = LotElementType.ModelCurves;
        public LotAnchorCorner AnchorCorner { get; set; } = LotAnchorCorner.Southwest;
        public bool AlignWithPbp { get; set; } = true;

        // Toggles
        public bool EnsureLevel1Placement { get; set; } = true;
        public bool DrawSubjectLot { get; set; } = true;
        public bool DrawAdjacentLots { get; set; } = true;
        public bool DrawRemainingBlockLots { get; set; } = true;
        public bool DrawSidewalks { get; set; } = true;
        public double SidewalkWidthFt { get; set; } = 12.0;
        public bool PlaceStreetTextNotes { get; set; } = true;

        // Grouping
        public LotGroupingMode GroupingMode { get; set; } = LotGroupingMode.SingleGroup;
        public bool PinCreatedGroup { get; set; } = false;

        // Zoning Drafting Table View (Proposal B)
        public bool GenerateZoningDraftingTable { get; set; } = true;

        // 3D Building Masses (Extrusions with real NYC heights)
        public bool Create3DBuildingMasses { get; set; } = true;
        public bool ExtrudeSubjectLotBuilding { get; set; } = false;

        // Custom Line Style Names
        public string SubjectLineStyle { get; set; } = RevitLotDrawerService.STYLE_SUBJECT_RED;
        public string AdjacentLineStyle { get; set; } = RevitLotDrawerService.STYLE_ADJACENT_ORANGE;
        public string BlockContextLineStyle { get; set; } = RevitLotDrawerService.STYLE_CONTEXT_GRAY;
        public string SidewalkLineStyle { get; set; } = RevitLotDrawerService.STYLE_SIDEWALK_BLUE;

        public Level? TargetLevel { get; set; }
    }

    public class LotDrawResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string GroupName { get; set; } = string.Empty;
        public string DraftingViewName { get; set; } = string.Empty;
        public int SubjectCurvesCount { get; set; }
        public int AdjacentLotsCount { get; set; }
        public int AdjacentCurvesCount { get; set; }
        public int ContextLotsCount { get; set; }
        public int ContextCurvesCount { get; set; }
        public int SidewalkCurvesCount { get; set; }
        public int BuildingMassesCount { get; set; }
        public int TextNotesCount { get; set; }
    }

    public class RevitLotDrawerService
    {
        private readonly Document _doc;

        public const string STYLE_SUBJECT_RED     = "NYC Lot - Subject (Red)";
        public const string STYLE_ADJACENT_ORANGE = "NYC Lot - Adjacent (Orange)";
        public const string STYLE_CONTEXT_GRAY    = "NYC Block - Context (Gray)";
        public const string STYLE_SIDEWALK_BLUE   = "NYC Sidewalk - Curb (Blue)";
        public const string SUBCAT_CONTEXT_BLDG   = "NYC Context Building";
        public const string MATERIAL_CONTEXT_MASS = "NYC - Urban Context";

        public RevitLotDrawerService(Document doc)
        {
            _doc = doc;
        }

        public XYZ GetProjectBasePointPosition()
        {
            try
            {
                var basePoint = new FilteredElementCollector(_doc)
                    .OfCategory(BuiltInCategory.OST_ProjectBasePoint)
                    .WhereElementIsNotElementType()
                    .Cast<BasePoint>()
                    .FirstOrDefault();

                if (basePoint != null)
                {
                    return basePoint.Position;
                }
            }
            catch
            {
                // Fallback
            }
            return XYZ.Zero;
        }

        public Level? GetLevel1()
        {
            var levels = new FilteredElementCollector(_doc)
                .OfClass(typeof(Level))
                .Cast<Level>()
                .OrderBy(l => l.Elevation)
                .ToList();

            if (levels.Count == 0) return null;

            var lvl1 = levels.FirstOrDefault(l =>
                l.Name.Contains("1", StringComparison.OrdinalIgnoreCase) ||
                l.Name.Contains("FIRST", StringComparison.OrdinalIgnoreCase) ||
                l.Name.Contains("NIVEL 1", StringComparison.OrdinalIgnoreCase) ||
                l.Name.Contains("LEVEL 1", StringComparison.OrdinalIgnoreCase) ||
                l.Name.Contains("01", StringComparison.OrdinalIgnoreCase));

            return lvl1 ?? levels[0];
        }

        public List<string> GetAvailableLineStyles()
        {
            var styles = new List<string>
            {
                STYLE_SUBJECT_RED,
                STYLE_ADJACENT_ORANGE,
                STYLE_CONTEXT_GRAY,
                STYLE_SIDEWALK_BLUE
            };

            try
            {
                Categories categories = _doc.Settings.Categories;
                Category linesCat = categories.get_Item(BuiltInCategory.OST_Lines);
                if (linesCat != null)
                {
                    foreach (Category subCat in linesCat.SubCategories)
                    {
                        if (!styles.Contains(subCat.Name, StringComparer.OrdinalIgnoreCase))
                        {
                            styles.Add(subCat.Name);
                        }
                    }
                }
            }
            catch
            {
                // Fallback
            }

            return styles;
        }

        public LotDrawResult DrawLotWithContext(NycBlockContext blockContext, LotDrawOptions options)
        {
            var subjectLot = blockContext.SubjectLot;
            if (subjectLot.PolygonRings.Count == 0)
            {
                return new LotDrawResult
                {
                    Success = false,
                    Message = "No polygon geometry found for this NYC Tax Lot."
                };
            }

            XYZ pbpPos = options.AlignWithPbp ? GetProjectBasePointPosition() : XYZ.Zero;
            XYZ anchorPoint = subjectLot.GetAnchorPoint(options.AnchorCorner);

            Level level1 = options.TargetLevel ?? GetLevel1()!;
            double zElevation = level1?.Elevation ?? pbpPos.Z;

            double offsetX = pbpPos.X - anchorPoint.X;
            double offsetY = pbpPos.Y - anchorPoint.Y;
            double tolerance = _doc.Application.ShortCurveTolerance;

            using var tx = new Transaction(_doc, $"BauTools - NYC Lot {subjectLot.Bbl} & 3D Masses Group");
            tx.Start();

            try
            {
                EnsurePresetStylesExist();

                GraphicsStyle? styleSubject = ResolveLineStyle(options.SubjectLineStyle, STYLE_SUBJECT_RED, new Color(220, 38, 38), 4);
                GraphicsStyle? styleAdjacent = ResolveLineStyle(options.AdjacentLineStyle, STYLE_ADJACENT_ORANGE, new Color(234, 88, 12), 2);
                GraphicsStyle? styleContext = ResolveLineStyle(options.BlockContextLineStyle, STYLE_CONTEXT_GRAY, new Color(148, 163, 184), 1);
                GraphicsStyle? styleSidewalk = ResolveLineStyle(options.SidewalkLineStyle, STYLE_SIDEWALK_BLUE, new Color(2, 132, 199), 2);

                Plane plane = level1 != null
                    ? Plane.CreateByNormalAndOrigin(XYZ.BasisZ, new XYZ(0, 0, level1.Elevation))
                    : Plane.CreateByNormalAndOrigin(XYZ.BasisZ, new XYZ(0, 0, zElevation));
                SketchPlane sketchPlane = SketchPlane.Create(_doc, plane);

                var subjectElementsToGroup = new List<ElementId>();
                var contextElementsToGroup = new List<ElementId>();

                int subjectCurvesCount = 0;
                int adjacentLotsCount = 0;
                int adjacentCurvesCount = 0;
                int contextLotsCount = 0;
                int contextCurvesCount = 0;
                int sidewalkCurvesCount = 0;
                int buildingMassesCount = 0;
                int textNotesCount = 0;

                // 1. Build Subject Lot Curves
                var subjectLoops = BuildCurveLoops(subjectLot.PolygonRings, offsetX, offsetY, zElevation, tolerance);

                // 2. Draw Subject Lot Lines (on Level 1)
                if (options.DrawSubjectLot && subjectLoops.Count > 0)
                {
                    var ids = DrawLoopsWithIds(subjectLoops, options.ElementType, sketchPlane, styleSubject,
                        $"NYC Development Lot {subjectLot.Bbl} ({subjectLot.Address}) - Zoning: {subjectLot.GetZoningSummary()} - Area: {subjectLot.LotAreaSqFt:N0} SF");
                    subjectCurvesCount = ids.Count;
                    subjectElementsToGroup.AddRange(ids);
                }

                // 3. Draw Adjacent Lots (Immediate Neighbors)
                if (options.DrawAdjacentLots && blockContext.AdjacentLots.Count > 0)
                {
                    foreach (var adjLot in blockContext.AdjacentLots)
                    {
                        var adjLoops = BuildCurveLoops(adjLot.PolygonRings, offsetX, offsetY, zElevation, tolerance);
                        if (adjLoops.Count > 0)
                        {
                            var ids = DrawLoopsWithIds(adjLoops, options.ElementType, sketchPlane, styleAdjacent,
                                $"NYC Adjacent Lot {adjLot.Lot} (BBL: {adjLot.Bbl}, {adjLot.Address})");
                            if (ids.Count > 0)
                            {
                                adjacentCurvesCount += ids.Count;
                                adjacentLotsCount++;
                                contextElementsToGroup.AddRange(ids);
                            }
                        }
                    }
                }

                // 4. Draw Remaining Block Lots
                if (options.DrawRemainingBlockLots && blockContext.RemainingBlockLots.Count > 0)
                {
                    foreach (var lot in blockContext.RemainingBlockLots)
                    {
                        var contextLoops = BuildCurveLoops(lot.PolygonRings, offsetX, offsetY, zElevation, tolerance);
                        if (contextLoops.Count > 0)
                        {
                            var ids = DrawLoopsWithIds(contextLoops, options.ElementType, sketchPlane, styleContext,
                                $"NYC Block {blockContext.BlockNumber} - Lot {lot.Lot} ({lot.Address})");
                            if (ids.Count > 0)
                            {
                                contextCurvesCount += ids.Count;
                                contextLotsCount++;
                                contextElementsToGroup.AddRange(ids);
                            }
                        }
                    }
                }

                // 5. Draw Sidewalk Curbs (12 ft perimeter buffer on Level 1)
                if (options.DrawSidewalks)
                {
                    double swOffset = options.SidewalkWidthFt > 0 ? options.SidewalkWidthFt : 12.0;

                    double bMinX = blockContext.AllLots.Count > 1 ? blockContext.MinX : subjectLot.MinX;
                    double bMaxX = blockContext.AllLots.Count > 1 ? blockContext.MaxX : subjectLot.MaxX;
                    double bMinY = blockContext.AllLots.Count > 1 ? blockContext.MinY : subjectLot.MinY;
                    double bMaxY = blockContext.AllLots.Count > 1 ? blockContext.MaxY : subjectLot.MaxY;

                    double swMinX = bMinX + offsetX - swOffset;
                    double swMaxX = bMaxX + offsetX + swOffset;
                    double swMinY = bMinY + offsetY - swOffset;
                    double swMaxY = bMaxY + offsetY + swOffset;

                    XYZ p1 = new XYZ(swMinX, swMinY, zElevation);
                    XYZ p2 = new XYZ(swMaxX, swMinY, zElevation);
                    XYZ p3 = new XYZ(swMaxX, swMaxY, zElevation);
                    XYZ p4 = new XYZ(swMinX, swMaxY, zElevation);

                    var sidewalkLoop = new CurveLoop();
                    sidewalkLoop.Append(Line.CreateBound(p1, p2));
                    sidewalkLoop.Append(Line.CreateBound(p2, p3));
                    sidewalkLoop.Append(Line.CreateBound(p3, p4));
                    sidewalkLoop.Append(Line.CreateBound(p4, p1));

                    var ids = DrawLoopsWithIds(new List<CurveLoop> { sidewalkLoop }, LotElementType.ModelCurves, sketchPlane, styleSidewalk,
                        $"NYC Block {blockContext.BlockNumber} - Sidewalk Curb Perimeter ({swOffset} ft width)");
                    sidewalkCurvesCount = ids.Count;
                    contextElementsToGroup.AddRange(ids);
                }

                // 6. Generate 3D Building Masses in Generic Models > NYC Context Building
                if (options.Create3DBuildingMasses)
                {
                    Category subCat = GetOrCreateContextSubcategory();

                    if (blockContext.Buildings.Count > 0)
                    {
                        foreach (var bldg in blockContext.Buildings)
                        {
                            if (bldg.IsSubjectLotBuilding && !options.ExtrudeSubjectLotBuilding)
                                continue;

                            var bldgLoops = BuildNormalizedBuildingLoops(bldg.PolygonRings, offsetX, offsetY, zElevation, tolerance);
                            if (bldgLoops.Count > 0 && bldg.EffectiveHeightFt > 5.0)
                            {
                                try
                                {
                                    Solid solid = GeometryCreationUtilities.CreateExtrusionGeometry(bldgLoops, XYZ.BasisZ, bldg.EffectiveHeightFt);
                                    if (solid != null && solid.Volume > 0.01)
                                    {
                                        ElementId catId = (subCat != null && subCat.Id != ElementId.InvalidElementId)
                                            ? subCat.Id
                                            : new ElementId(BuiltInCategory.OST_GenericModel);

                                        DirectShape ds = DirectShape.CreateElement(_doc, catId);
                                        ds.SetShape(new List<GeometryObject> { solid });
                                        ds.Name = $"NYC Building BIN {bldg.Bin} ({bldg.EffectiveHeightFt:F0} ft)";
                                        var comm = ds.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS);
                                        comm?.Set($"Address: {bldg.Address} | BIN: {bldg.Bin} | Roof Height: {bldg.HeightRoofFt:F1} ft | Floors: {bldg.NumFloors} | Year: {bldg.YearBuilt}");

                                        if (bldg.IsSubjectLotBuilding)
                                            subjectElementsToGroup.Add(ds.Id);
                                        else
                                            contextElementsToGroup.Add(ds.Id);

                                        buildingMassesCount++;
                                    }
                                }
                                catch
                                {
                                    // Skip invalid solid
                                }
                            }
                        }
                    }
                    else
                    {
                        foreach (var lot in blockContext.AllLots)
                        {
                            if (lot.Bbl == subjectLot.Bbl && !options.ExtrudeSubjectLotBuilding)
                                continue;

                            var lotLoops = BuildCurveLoops(lot.PolygonRings, offsetX, offsetY, zElevation, tolerance);
                            double height = lot.NumFloors > 0 ? lot.NumFloors * 12.0 : 36.0;
                            if (lotLoops.Count > 0 && height > 5.0)
                            {
                                try
                                {
                                    Solid solid = GeometryCreationUtilities.CreateExtrusionGeometry(lotLoops, XYZ.BasisZ, height);
                                    if (solid != null && solid.Volume > 0.01)
                                    {
                                        ElementId catId = (subCat != null && subCat.Id != ElementId.InvalidElementId)
                                            ? subCat.Id
                                            : new ElementId(BuiltInCategory.OST_GenericModel);

                                        DirectShape ds = DirectShape.CreateElement(_doc, catId);
                                        ds.SetShape(new List<GeometryObject> { solid });
                                        ds.Name = $"NYC Lot {lot.Bbl} Mass ({height:F0} ft)";
                                        var comm = ds.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS);
                                        comm?.Set($"Lot: {lot.Address} | BBL: {lot.Bbl} | Height: {height:F0} ft | Floors: {lot.NumFloors} | Zoning: {lot.GetZoningSummary()}");

                                        if (lot.Bbl == subjectLot.Bbl)
                                            subjectElementsToGroup.Add(ds.Id);
                                        else
                                            contextElementsToGroup.Add(ds.Id);

                                        buildingMassesCount++;
                                    }
                                }
                                catch { }
                            }
                        }
                    }
                }

                // 7. Place Surrounding Street Titles as Text Notes
                if (options.PlaceStreetTextNotes)
                {
                    var streets = blockContext.GetSurroundingStreetNames();
                    double swOffset = options.SidewalkWidthFt > 0 ? options.SidewalkWidthFt : 12.0;
                    double textDistance = swOffset + 18.0;

                    double bMinX = blockContext.AllLots.Count > 1 ? blockContext.MinX : subjectLot.MinX;
                    double bMaxX = blockContext.AllLots.Count > 1 ? blockContext.MaxX : subjectLot.MaxX;
                    double bMinY = blockContext.AllLots.Count > 1 ? blockContext.MinY : subjectLot.MinY;
                    double bMaxY = blockContext.AllLots.Count > 1 ? blockContext.MaxY : subjectLot.MaxY;

                    double midX = (bMinX + bMaxX) / 2.0 + offsetX;
                    double midY = (bMinY + bMaxY) / 2.0 + offsetY;

                    double northY = bMaxY + offsetY + textDistance;
                    double southY = bMinY + offsetY - textDistance;
                    double eastX = bMaxX + offsetX + textDistance;
                    double westX = bMinX + offsetX - textDistance;

                    if (streets.TryGetValue("North", out string? northSt) && !string.IsNullOrWhiteSpace(northSt))
                    {
                        if (CreateTextAnnotation(new XYZ(midX, northY, zElevation), northSt.ToUpperInvariant()))
                            textNotesCount++;
                    }
                    if (streets.TryGetValue("South", out string? southSt) && !string.IsNullOrWhiteSpace(southSt))
                    {
                        if (CreateTextAnnotation(new XYZ(midX, southY, zElevation), southSt.ToUpperInvariant()))
                            textNotesCount++;
                    }
                    if (streets.TryGetValue("East", out string? eastSt) && !string.IsNullOrWhiteSpace(eastSt))
                    {
                        if (CreateTextAnnotation(new XYZ(eastX, midY, zElevation), eastSt.ToUpperInvariant()))
                            textNotesCount++;
                    }
                    if (streets.TryGetValue("West", out string? westSt) && !string.IsNullOrWhiteSpace(westSt))
                    {
                        if (CreateTextAnnotation(new XYZ(westX, midY, zElevation), westSt.ToUpperInvariant()))
                            textNotesCount++;
                    }
                }

                // 8. Group Creation (Single vs Split Groups)
                string groupResultSummary = string.Empty;
                if (options.GroupingMode == LotGroupingMode.SingleGroup)
                {
                    var allElements = new List<ElementId>();
                    allElements.AddRange(subjectElementsToGroup);
                    allElements.AddRange(contextElementsToGroup);

                    if (allElements.Count > 0)
                    {
                        string baseName = !string.IsNullOrWhiteSpace(subjectLot.Address)
                            ? subjectLot.Address.Trim().ToUpperInvariant()
                            : $"NYC Lot - BBL {subjectLot.Bbl}";

                        var grp = CreateAndNameGroup(allElements, baseName, $"BBL: {subjectLot.Bbl} | Zoning: {subjectLot.GetZoningSummary()} | Area: {subjectLot.LotAreaSqFt:N0} SF | Block: {blockContext.BlockNumber}", options.PinCreatedGroup);
                        if (grp != null)
                        {
                            groupResultSummary = $"📦 Group: [{grp.GroupType.Name}]";
                        }
                    }
                }
                else if (options.GroupingMode == LotGroupingMode.SplitGroups)
                {
                    var groupNames = new List<string>();

                    if (subjectElementsToGroup.Count > 0)
                    {
                        string lotName = !string.IsNullOrWhiteSpace(subjectLot.Address)
                            ? $"NYC Lot - {subjectLot.Address.Trim().ToUpperInvariant()}"
                            : $"NYC Lot - BBL {subjectLot.Bbl}";

                        var grp1 = CreateAndNameGroup(subjectElementsToGroup, lotName, $"Development Lot {subjectLot.Bbl} | Zoning: {subjectLot.GetZoningSummary()} | Area: {subjectLot.LotAreaSqFt:N0} SF", options.PinCreatedGroup);
                        if (grp1 != null) groupNames.Add(grp1.GroupType.Name);
                    }

                    if (contextElementsToGroup.Count > 0)
                    {
                        string ctxName = $"NYC Context - Block {blockContext.BlockNumber}";
                        var grp2 = CreateAndNameGroup(contextElementsToGroup, ctxName, $"NYC Context Block {blockContext.BlockNumber} ({adjacentLotsCount} adjacent, {contextLotsCount} block lots, {buildingMassesCount} 3D masses)", options.PinCreatedGroup);
                        if (grp2 != null) groupNames.Add(grp2.GroupType.Name);
                    }

                    if (groupNames.Count > 0)
                    {
                        groupResultSummary = $"📦 Groups: [{string.Join("] & [", groupNames)}]";
                    }
                }

                // 9. Generate Native Revit Zoning Summary Table (Drafting View - Proposal B)
                string draftingViewName = string.Empty;
                if (options.GenerateZoningDraftingTable)
                {
                    try
                    {
                        var dv = CreateZoningSummaryDraftingView(subjectLot, blockContext);
                        if (dv != null)
                        {
                            draftingViewName = dv.Name;
                        }
                    }
                    catch
                    {
                        // Fallback
                    }
                }

                _doc.Regenerate();
                tx.Commit();

                string levelName = level1?.Name ?? "Level 1";
                string dvMsg = !string.IsNullOrEmpty(draftingViewName) ? $"\n📊 Zoning Table View: [{draftingViewName}]" : "";

                return new LotDrawResult
                {
                    Success = true,
                    GroupName = groupResultSummary,
                    DraftingViewName = draftingViewName,
                    SubjectCurvesCount = subjectCurvesCount,
                    AdjacentLotsCount = adjacentLotsCount,
                    AdjacentCurvesCount = adjacentCurvesCount,
                    ContextLotsCount = contextLotsCount,
                    ContextCurvesCount = contextCurvesCount,
                    SidewalkCurvesCount = sidewalkCurvesCount,
                    BuildingMassesCount = buildingMassesCount,
                    TextNotesCount = textNotesCount,
                    Message = $"Successfully created on [{levelName}]: Development Lot {subjectLot.Bbl} + {adjacentLotsCount} adjacent lots + {contextLotsCount} block lots + {buildingMassesCount} 3D building masses + {textNotesCount} street titles.\n\n{groupResultSummary}{dvMsg}"
                };
            }
            catch (Exception ex)
            {
                if (tx.HasStarted())
                    tx.RollBack();

                return new LotDrawResult
                {
                    Success = false,
                    Message = $"Error drawing block context in Revit: {ex.Message}"
                };
            }
        }

        private Group? CreateAndNameGroup(List<ElementId> elementIds, string baseName, string comments, bool pinGroup)
        {
            if (elementIds.Count == 0) return null;

            try
            {
                Group createdGroup = _doc.Create.NewGroup(elementIds);
                if (createdGroup != null)
                {
                    string uniqueName = baseName;
                    int suffix = 1;
                    while (GroupTypeExists(uniqueName))
                    {
                        uniqueName = $"{baseName} ({suffix++})";
                    }

                    try
                    {
                        createdGroup.GroupType.Name = uniqueName;
                    }
                    catch { }

                    try
                    {
                        var comm = createdGroup.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS);
                        comm?.Set(comments);
                    }
                    catch { }

                    if (pinGroup)
                    {
                        createdGroup.Pinned = true;
                    }

                    return createdGroup;
                }
            }
            catch
            {
                // Fallback
            }
            return null;
        }

        /// <summary>
        /// Creates a professional native Drafting View containing the full NYC Zoning & MapPLUTO calculation table.
        /// </summary>
        private ViewDrafting? CreateZoningSummaryDraftingView(NycLotInfo lot, NycBlockContext blockContext)
        {
            ViewFamilyType? draftingVft = new FilteredElementCollector(_doc)
                .OfClass(typeof(ViewFamilyType))
                .Cast<ViewFamilyType>()
                .FirstOrDefault(v => v.ViewFamily == ViewFamily.Drafting);

            if (draftingVft == null) return null;

            string baseViewName = !string.IsNullOrWhiteSpace(lot.Address)
                ? $"NYC Zoning - {lot.Address.Trim().ToUpperInvariant()}"
                : $"NYC Zoning - BBL {lot.Bbl}";

            string viewName = baseViewName;
            int counter = 1;
            while (new FilteredElementCollector(_doc).OfClass(typeof(ViewDrafting)).Cast<ViewDrafting>().Any(v => v.Name.Equals(viewName, StringComparison.OrdinalIgnoreCase)))
            {
                viewName = $"{baseViewName} ({counter++})";
            }

            ViewDrafting dv = ViewDrafting.Create(_doc, draftingVft.Id);
            dv.Name = viewName;
            dv.Scale = 1; // 1:1

            ElementId textTypeId = _doc.GetDefaultElementTypeId(ElementTypeGroup.TextNoteType);

            // Table dimensions (in feet inside drafting view)
            double tableWidth = 6.0;
            double col1W = 2.0;
            double col2W = 1.2;
            double col3W = 1.4;
            double col4W = 1.4;
            double rowH = 0.28;
            double headerH = 0.40;
            double titleH = 0.45;
            double startX = 0.0;
            double curY = 0.0;

            // 1. Title
            DrawRect(dv, startX, curY - titleH, tableWidth, titleH);
            AddCellText(dv, startX, curY - titleH, tableWidth, titleH,
                $"NYC ZONING & PLUTO URBAN ANALYSIS — {lot.Address.ToUpperInvariant()}", textTypeId, HorizontalTextAlignment.Center);
            curY -= titleH;

            // 2. Identification Subheaders
            DrawRect(dv, startX, curY - rowH, tableWidth, rowH);
            AddCellText(dv, startX, curY - rowH, tableWidth, rowH,
                $"BBL: {lot.Bbl}   |   Borough: {lot.Borough}   |   Block: {lot.Block}   |   Lot: {lot.Lot}   |   ZIP: {lot.ZipCode}", textTypeId, HorizontalTextAlignment.Left);
            curY -= rowH;

            DrawRect(dv, startX, curY - rowH, tableWidth, rowH);
            AddCellText(dv, startX, curY - rowH, tableWidth, rowH,
                $"Zoning District(s): {lot.GetZoningSummary()}   |   Owner: {(string.IsNullOrEmpty(lot.OwnerName) ? "N/A" : lot.OwnerName)}", textTypeId, HorizontalTextAlignment.Left);
            curY -= rowH;

            DrawRect(dv, startX, curY - rowH, tableWidth, rowH);
            AddCellText(dv, startX, curY - rowH, tableWidth, rowH,
                $"Land Use: {lot.LandUse}   |   Bldg Class: {lot.BuildingClass}   |   Year Built: {(lot.YearBuilt > 0 ? lot.YearBuilt.ToString() : "N/A")}   |   Floors: {lot.NumFloors}", textTypeId, HorizontalTextAlignment.Left);
            curY -= rowH;

            // 3. Matrix Table Header
            DrawRect(dv, startX, curY - headerH, col1W, headerH);
            AddCellText(dv, startX, curY - headerH, col1W, headerH, "ZONING METRIC", textTypeId, HorizontalTextAlignment.Center);

            DrawRect(dv, startX + col1W, curY - headerH, col2W, headerH);
            AddCellText(dv, startX + col1W, curY - headerH, col2W, headerH, "FAR RATIO", textTypeId, HorizontalTextAlignment.Center);

            DrawRect(dv, startX + col1W + col2W, curY - headerH, col3W, headerH);
            AddCellText(dv, startX + col1W + col2W, curY - headerH, col3W, headerH, "LOT AREA (SF)", textTypeId, HorizontalTextAlignment.Center);

            DrawRect(dv, startX + col1W + col2W + col3W, curY - headerH, col4W, headerH);
            AddCellText(dv, startX + col1W + col2W + col3W, curY - headerH, col4W, headerH, "MAX ALLOWABLE GFA", textTypeId, HorizontalTextAlignment.Center);
            curY -= headerH;

            // 4. Matrix Rows
            double maxResGfa = lot.LotAreaSqFt * lot.ResidFar;
            double maxComGfa = lot.LotAreaSqFt * lot.CommFar;
            double maxFacGfa = lot.LotAreaSqFt * lot.FacilFar;

            DrawMatrixRow(dv, startX, ref curY, col1W, col2W, col3W, col4W, rowH, "Residential FAR", lot.ResidFar.ToString("F2"), $"{lot.LotAreaSqFt:N0} SF", maxResGfa > 0 ? $"{maxResGfa:N0} SF" : "Not Permitted", textTypeId);
            DrawMatrixRow(dv, startX, ref curY, col1W, col2W, col3W, col4W, rowH, "Commercial FAR", lot.CommFar.ToString("F2"), $"{lot.LotAreaSqFt:N0} SF", maxComGfa > 0 ? $"{maxComGfa:N0} SF" : "Not Permitted", textTypeId);
            DrawMatrixRow(dv, startX, ref curY, col1W, col2W, col3W, col4W, rowH, "Community Facility FAR", lot.FacilFar.ToString("F2"), $"{lot.LotAreaSqFt:N0} SF", maxFacGfa > 0 ? $"{maxFacGfa:N0} SF" : "Not Permitted", textTypeId);
            DrawMatrixRow(dv, startX, ref curY, col1W, col2W, col3W, col4W, rowH, "Built / Existing FAR", lot.BuiltFar.ToString("F2"), $"{lot.LotAreaSqFt:N0} SF", $"{lot.BldgAreaSqFt:N0} SF (Existing)", textTypeId);
            DrawMatrixRow(dv, startX, ref curY, col1W, col2W, col3W, col4W, rowH, "Lot Dimensions (W × D)", "-", $"{lot.LotAreaSqFt:N0} SF", $"{lot.WidthFt:F1} ft × {lot.DepthFt:F1} ft", textTypeId);

            // 5. Context Summary Footer
            var streets = blockContext.GetSurroundingStreetNames();
            string streetStr = streets.Count > 0 ? string.Join(", ", streets.Values) : "N/A";
            DrawRect(dv, startX, curY - rowH, tableWidth, rowH);
            AddCellText(dv, startX, curY - rowH, tableWidth, rowH,
                $"Block {blockContext.BlockNumber} Context: {blockContext.AllLots.Count} Lots | {blockContext.Buildings.Count} 3D Buildings | Streets: {streetStr}", textTypeId, HorizontalTextAlignment.Left);
            curY -= rowH;

            return dv;
        }

        private void DrawMatrixRow(ViewDrafting dv, double startX, ref double curY, double c1, double c2, double c3, double c4, double rowH, string t1, string t2, string t3, string t4, ElementId textTypeId)
        {
            DrawRect(dv, startX, curY - rowH, c1, rowH);
            AddCellText(dv, startX, curY - rowH, c1, rowH, t1, textTypeId, HorizontalTextAlignment.Left);

            DrawRect(dv, startX + c1, curY - rowH, c2, rowH);
            AddCellText(dv, startX + c1, curY - rowH, c2, rowH, t2, textTypeId, HorizontalTextAlignment.Center);

            DrawRect(dv, startX + c1 + c2, curY - rowH, c3, rowH);
            AddCellText(dv, startX + c1 + c2, curY - rowH, c3, rowH, t3, textTypeId, HorizontalTextAlignment.Center);

            DrawRect(dv, startX + c1 + c2 + c3, curY - rowH, c4, rowH);
            AddCellText(dv, startX + c1 + c2 + c3, curY - rowH, c4, rowH, t4, textTypeId, HorizontalTextAlignment.Center);

            curY -= rowH;
        }

        private void DrawRect(ViewDrafting dv, double x, double y, double width, double height)
        {
            XYZ p1 = new XYZ(x, y, 0);
            XYZ p2 = new XYZ(x + width, y, 0);
            XYZ p3 = new XYZ(x + width, y + height, 0);
            XYZ p4 = new XYZ(x, y + height, 0);

            _doc.Create.NewDetailCurve(dv, Line.CreateBound(p1, p2));
            _doc.Create.NewDetailCurve(dv, Line.CreateBound(p2, p3));
            _doc.Create.NewDetailCurve(dv, Line.CreateBound(p3, p4));
            _doc.Create.NewDetailCurve(dv, Line.CreateBound(p4, p1));
        }

        private void AddCellText(ViewDrafting dv, double x, double y, double width, double height, string text, ElementId textTypeId, HorizontalTextAlignment align)
        {
            try
            {
                double posX = align == HorizontalTextAlignment.Center ? x + (width / 2.0) : x + 0.08;
                double posY = y + (height / 2.0);

                var opts = new TextNoteOptions
                {
                    HorizontalAlignment = align,
                    TypeId = textTypeId
                };

                TextNote.Create(_doc, dv.Id, new XYZ(posX, posY, 0), text, opts);
            }
            catch { }
        }

        private bool GroupTypeExists(string name)
        {
            return new FilteredElementCollector(_doc)
                .OfClass(typeof(GroupType))
                .Cast<GroupType>()
                .Any(gt => gt.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        public Material GetOrCreateUrbanContextMaterial()
        {
            try
            {
                var existingMat = new FilteredElementCollector(_doc)
                    .OfClass(typeof(Material))
                    .Cast<Material>()
                    .FirstOrDefault(m => m.Name.Equals(MATERIAL_CONTEXT_MASS, StringComparison.OrdinalIgnoreCase));

                if (existingMat != null)
                    return existingMat;

                ElementId matId = Material.Create(_doc, MATERIAL_CONTEXT_MASS);
                Material newMat = (Material)_doc.GetElement(matId);
                newMat.Color = new Color(225, 229, 238);
                newMat.Transparency = 10;
                return newMat;
            }
            catch
            {
                return null!;
            }
        }

        public Category GetOrCreateContextSubcategory()
        {
            try
            {
                Categories categories = _doc.Settings.Categories;
                Category genModels = categories.get_Item(BuiltInCategory.OST_GenericModel);
                if (genModels.SubCategories.Contains(SUBCAT_CONTEXT_BLDG))
                {
                    return genModels.SubCategories.get_Item(SUBCAT_CONTEXT_BLDG);
                }

                Category newSub = categories.NewSubcategory(genModels, SUBCAT_CONTEXT_BLDG);
                newSub.LineColor = new Color(100, 116, 139);
                Material mat = GetOrCreateUrbanContextMaterial();
                if (mat != null)
                {
                    newSub.Material = mat;
                }
                return newSub;
            }
            catch
            {
                return _doc.Settings.Categories.get_Item(BuiltInCategory.OST_GenericModel);
            }
        }

        private List<ElementId> DrawLoopsWithIds(List<CurveLoop> loops, LotElementType elemType, SketchPlane? sketchPlane, GraphicsStyle? lineStyle, string comments)
        {
            var ids = new List<ElementId>();
            View activeView = _doc.ActiveView;

            foreach (var loop in loops)
            {
                foreach (Curve curve in loop)
                {
                    if (elemType == LotElementType.ModelCurves && sketchPlane != null)
                    {
                        ModelCurve mc = _doc.Create.NewModelCurve(curve, sketchPlane);
                        if (mc != null)
                        {
                            if (lineStyle != null) try { mc.LineStyle = lineStyle; } catch { }
                            var comm = mc.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS);
                            comm?.Set(comments);
                            ids.Add(mc.Id);
                        }
                    }
                    else if (elemType == LotElementType.DetailCurves && IsPlanView(activeView))
                    {
                        DetailCurve dc = _doc.Create.NewDetailCurve(activeView, curve);
                        if (dc != null)
                        {
                            if (lineStyle != null) try { dc.LineStyle = lineStyle; } catch { }
                            var comm = dc.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS);
                            comm?.Set(comments);
                            ids.Add(dc.Id);
                        }
                    }
                    else if (elemType == LotElementType.AreaBoundaryLines && activeView is ViewPlan vp && activeView.ViewType == ViewType.AreaPlan)
                    {
                        if (sketchPlane != null)
                        {
                            ModelCurve ac = _doc.Create.NewAreaBoundaryLine(sketchPlane, curve, vp);
                            if (ac != null) ids.Add(ac.Id);
                        }
                    }
                }
            }
            return ids;
        }

        private bool CreateTextAnnotation(XYZ position, string text)
        {
            try
            {
                View activeView = _doc.ActiveView;

                if (IsPlanView(activeView))
                {
                    ElementId defaultTextTypeId = _doc.GetDefaultElementTypeId(ElementTypeGroup.TextNoteType);
                    if (defaultTextTypeId != ElementId.InvalidElementId)
                    {
                        TextNoteOptions opts = new TextNoteOptions
                        {
                            HorizontalAlignment = HorizontalTextAlignment.Center,
                            TypeId = defaultTextTypeId
                        };

                        XYZ viewPos = new XYZ(position.X, position.Y, 0);
                        TextNote tn = TextNote.Create(_doc, activeView.Id, viewPos, text, opts);
                        return tn != null;
                    }
                }
            }
            catch
            {
                // Fallback
            }

            return false;
        }

        private void EnsurePresetStylesExist()
        {
            GetOrCreateLineStyle(STYLE_SUBJECT_RED, new Color(220, 38, 38), 4);
            GetOrCreateLineStyle(STYLE_ADJACENT_ORANGE, new Color(234, 88, 12), 2);
            GetOrCreateLineStyle(STYLE_CONTEXT_GRAY, new Color(148, 163, 184), 1);
            GetOrCreateLineStyle(STYLE_SIDEWALK_BLUE, new Color(2, 132, 199), 2);
        }

        private GraphicsStyle? ResolveLineStyle(string requestedName, string fallbackPreset, Color fallbackColor, int fallbackWeight)
        {
            try
            {
                Categories categories = _doc.Settings.Categories;
                Category linesCat = categories.get_Item(BuiltInCategory.OST_Lines);
                if (linesCat != null)
                {
                    if (!string.IsNullOrWhiteSpace(requestedName) && linesCat.SubCategories.Contains(requestedName))
                    {
                        return linesCat.SubCategories.get_Item(requestedName).GetGraphicsStyle(GraphicsStyleType.Projection);
                    }
                }
            }
            catch { }

            return GetOrCreateLineStyle(fallbackPreset, fallbackColor, fallbackWeight);
        }

        public GraphicsStyle GetOrCreateLineStyle(string styleName, Color color, int weight)
        {
            try
            {
                Categories categories = _doc.Settings.Categories;
                Category linesCat = categories.get_Item(BuiltInCategory.OST_Lines);
                if (linesCat != null)
                {
                    if (linesCat.SubCategories.Contains(styleName))
                    {
                        Category existingSub = linesCat.SubCategories.get_Item(styleName);
                        return existingSub.GetGraphicsStyle(GraphicsStyleType.Projection);
                    }

                    Category newSubCat = categories.NewSubcategory(linesCat, styleName);
                    newSubCat.LineColor = color;
                    newSubCat.SetLineWeight(weight, GraphicsStyleType.Projection);
                    return newSubCat.GetGraphicsStyle(GraphicsStyleType.Projection);
                }
            }
            catch { }

            Category defaultLines = _doc.Settings.Categories.get_Item(BuiltInCategory.OST_Lines);
            return defaultLines.GetGraphicsStyle(GraphicsStyleType.Projection);
        }

        private static List<CurveLoop> BuildNormalizedBuildingLoops(List<List<XYZ>> rings, double offsetX, double offsetY, double zElevation, double tolerance)
        {
            var loops = new List<CurveLoop>();
            if (rings == null || rings.Count == 0) return loops;

            for (int r = 0; r < rings.Count; r++)
            {
                var ring = rings[r];
                var cleaned = CleanAndTransformRing(ring, offsetX, offsetY, zElevation, tolerance);
                if (cleaned.Count < 3) continue;

                var loop = new CurveLoop();
                bool valid = true;

                for (int i = 0; i < cleaned.Count; i++)
                {
                    XYZ p1 = cleaned[i];
                    XYZ p2 = cleaned[(i + 1) % cleaned.Count];
                    if (p1.DistanceTo(p2) < tolerance) continue;

                    try
                    {
                        loop.Append(Line.CreateBound(p1, p2));
                    }
                    catch
                    {
                        valid = false;
                        break;
                    }
                }

                if (valid && !loop.IsOpen() && loop.Count() >= 3)
                {
                    if (r == 0)
                    {
                        if (!loop.IsCounterclockwise(XYZ.BasisZ))
                        {
                            loop = CurveLoop.CreateViaCopy(loop);
                            loop.Flip();
                        }
                        loops.Add(loop);
                    }
                    else
                    {
                        if (loop.IsCounterclockwise(XYZ.BasisZ))
                        {
                            loop = CurveLoop.CreateViaCopy(loop);
                            loop.Flip();
                        }
                        loops.Add(loop);
                    }
                }
            }
            return loops;
        }

        private static List<CurveLoop> BuildCurveLoops(List<List<XYZ>> rings, double offsetX, double offsetY, double zElevation, double tolerance)
        {
            var loops = new List<CurveLoop>();
            foreach (var ring in rings)
            {
                var cleaned = CleanAndTransformRing(ring, offsetX, offsetY, zElevation, tolerance);
                if (cleaned.Count < 3) continue;

                var loop = new CurveLoop();
                bool valid = true;

                for (int i = 0; i < cleaned.Count; i++)
                {
                    XYZ p1 = cleaned[i];
                    XYZ p2 = cleaned[(i + 1) % cleaned.Count];
                    if (p1.DistanceTo(p2) < tolerance) continue;

                    try
                    {
                        loop.Append(Line.CreateBound(p1, p2));
                    }
                    catch
                    {
                        valid = false;
                        break;
                    }
                }

                if (valid && !loop.IsOpen() && loop.Count() >= 3)
                {
                    loops.Add(loop);
                }
            }
            return loops;
        }

        private static List<XYZ> CleanAndTransformRing(List<XYZ> rawPoints, double offsetX, double offsetY, double z, double tolerance)
        {
            var result = new List<XYZ>();
            if (rawPoints == null || rawPoints.Count == 0) return result;

            for (int i = 0; i < rawPoints.Count; i++)
            {
                var pt = rawPoints[i];
                var transformed = new XYZ(pt.X + offsetX, pt.Y + offsetY, z);

                if (result.Count > 0 && result[^1].DistanceTo(transformed) < tolerance)
                    continue;

                result.Add(transformed);
            }

            if (result.Count > 1 && result[0].DistanceTo(result[^1]) < tolerance)
            {
                result.RemoveAt(result.Count - 1);
            }

            return result;
        }

        private static bool IsPlanView(View view)
        {
            if (view == null) return false;
            return view.ViewType == ViewType.FloorPlan ||
                   view.ViewType == ViewType.AreaPlan ||
                   view.ViewType == ViewType.CeilingPlan ||
                   view.ViewType == ViewType.EngineeringPlan ||
                   view.ViewType == ViewType.DraftingView;
        }
    }
}
