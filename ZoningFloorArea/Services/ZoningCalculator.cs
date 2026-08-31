using System;
using System.Collections.Generic;
using System.Linq;
using ZoningFloorArea.Models;

namespace ZoningFloorArea.Services
{
    public class ZoningCalculator
    {
        private const double SQFT_TO_SQM = 0.09290304;

        public ProjectZoningResult ComputeProjectZoning(List<AreaDataModel> allAreas, MappingConfig config, List<TypicalFloorGroup> groups)
        {
            ProjectZoningResult projectResult = new ProjectZoningResult();
            projectResult.LotArea = config.LotArea;
            projectResult.UlebPercent = config.UlebPercent;

            // Group areas by Building Name
            Dictionary<string, List<AreaDataModel>> bldgGroups = new Dictionary<string, List<AreaDataModel>>(StringComparer.OrdinalIgnoreCase);

            foreach (AreaDataModel a in allAreas)
            {
                string bName = string.IsNullOrEmpty(a.BuildingName) ? config.BuildingName : a.BuildingName;
                if (!bldgGroups.ContainsKey(bName))
                {
                    bldgGroups[bName] = new List<AreaDataModel>();
                }
                bldgGroups[bName].Add(a);
            }

            if (bldgGroups.Count == 0)
            {
                bldgGroups[config.BuildingName] = allAreas;
            }

            List<ZoningTableResult> bldgTables = new List<ZoningTableResult>();

            foreach (KeyValuePair<string, List<AreaDataModel>> kvp in bldgGroups)
            {
                MappingConfig bConfig = new MappingConfig();
                bConfig.GrossAreaSchemeName = config.GrossAreaSchemeName;
                bConfig.DeductionAreaSchemeName = config.DeductionAreaSchemeName;
                bConfig.DeductionTypeParameterName = config.DeductionTypeParameterName;
                bConfig.UsageCategoryParameterName = config.UsageCategoryParameterName;
                bConfig.BuildingParameterName = config.BuildingParameterName;
                bConfig.BuildingName = kvp.Key;
                bConfig.LotArea = config.LotArea;
                bConfig.UlebPercent = config.UlebPercent;
                bConfig.DisplayUnit = config.DisplayUnit;

                ZoningTableResult t = ComputeZoningTable(kvp.Value, bConfig, groups);
                bldgTables.Add(t);
            }

            projectResult.BuildingTables = bldgTables;
            projectResult.OverallSummary = ComputeProjectGrandTotal(bldgTables, config);

            return projectResult;
        }

        public ZoningTableResult ComputeZoningTable(List<AreaDataModel> allAreas, MappingConfig config, List<TypicalFloorGroup> groups)
        {
            double unitFactor = config.DisplayUnit == UnitDisplayMode.SquareMeters ? SQFT_TO_SQM : 1.0;
            double lotAreaConverted = config.LotArea * unitFactor;

            ZoningTableResult result = new ZoningTableResult();
            result.BuildingName = config.BuildingName;
            result.LotArea = lotAreaConverted;
            result.UlebPercent = config.UlebPercent;

            // 1. Separate gross building areas and deduction areas
            List<AreaDataModel> grossAreas = new List<AreaDataModel>();
            List<AreaDataModel> deductionAreas = new List<AreaDataModel>();

            foreach (AreaDataModel a in allAreas)
            {
                if (string.Equals(a.AreaSchemeName, config.GrossAreaSchemeName, StringComparison.OrdinalIgnoreCase))
                {
                    grossAreas.Add(a);
                }
                else if (string.Equals(a.AreaSchemeName, config.DeductionAreaSchemeName, StringComparison.OrdinalIgnoreCase))
                {
                    deductionAreas.Add(a);
                }
            }

            if (deductionAreas.Count == 0)
            {
                foreach (AreaDataModel a in allAreas)
                {
                    if (!string.Equals(a.AreaSchemeName, config.GrossAreaSchemeName, StringComparison.OrdinalIgnoreCase))
                    {
                        deductionAreas.Add(a);
                    }
                }
            }

            // 2. Base Deduction Categories matching standard NYC Zoning template
            List<string> baseCategories = new List<string>
            {
                "CHASE WALL",
                "STAIRS",
                "PARKING",
                "BYCYCLE PARKING",
                "AMENITIES",
                "CORRIDOR",
                "MECH ROOM",
                "REFUSE"
            };

            List<string> finalCategories = new List<string>(baseCategories);

            foreach (AreaDataModel d in deductionAreas)
            {
                if (!string.IsNullOrEmpty(d.DeductionType))
                {
                    string normalized = NormalizeCategoryName(d.DeductionType);
                    bool exists = false;
                    foreach (string cat in finalCategories)
                    {
                        if (string.Equals(cat, normalized, StringComparison.OrdinalIgnoreCase))
                        {
                            exists = true;
                            break;
                        }
                    }
                    if (!exists)
                    {
                        finalCategories.Add(normalized);
                    }
                }
            }

            result.DeductionCategories = finalCategories;

            // 3. Get all levels sorted by elevation
            List<AreaDataModel> sortedAreas = allAreas.OrderBy(a => a.LevelElevation).ToList();
            List<string> levelNames = new List<string>();
            Dictionary<string, double> levelElevations = new Dictionary<string, double>();

            foreach (AreaDataModel a in sortedAreas)
            {
                if (!levelNames.Contains(a.LevelName))
                {
                    levelNames.Add(a.LevelName);
                    levelElevations[a.LevelName] = a.LevelElevation;
                }
            }

            if (levelNames.Count == 0)
            {
                return result;
            }

            // 4. Build Unified Level Rows (1 row per level)
            List<LevelZoningRow> rows = new List<LevelZoningRow>();

            foreach (string lvlName in levelNames)
            {
                double lvlElev = levelElevations[lvlName];
                TypicalFloorGroup matchingGroup = FindMatchingGroup(lvlName, lvlElev, levelElevations, groups);

                // Residential Gross
                double resGrossSqFt = 0;
                foreach (AreaDataModel a in grossAreas)
                {
                    if (string.Equals(a.LevelName, lvlName, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(a.UsageCategory, "Residential", StringComparison.OrdinalIgnoreCase))
                    {
                        resGrossSqFt += a.AreaValue;
                    }
                }

                // Commercial Gross
                double comGrossSqFt = 0;
                foreach (AreaDataModel a in grossAreas)
                {
                    if (string.Equals(a.LevelName, lvlName, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(a.UsageCategory, "Commercial", StringComparison.OrdinalIgnoreCase))
                    {
                        comGrossSqFt += a.AreaValue;
                    }
                }

                LevelZoningRow row = new LevelZoningRow();
                row.LevelName = lvlName;
                row.LevelElevation = lvlElev;
                row.ResidentialGrossFloorArea = resGrossSqFt * unitFactor;
                row.CommercialGrossFloorArea = comGrossSqFt * unitFactor;
                row.UlebPercent = config.UlebPercent;
                row.LotArea = lotAreaConverted;

                if (matchingGroup != null)
                {
                    row.GroupName = matchingGroup.Name;
                    row.GroupColorHex = matchingGroup.ColorHex;
                }

                // Deductions per category
                foreach (string cat in result.DeductionCategories)
                {
                    double resDedSqFt = 0;
                    double comDedSqFt = 0;

                    foreach (AreaDataModel d in deductionAreas)
                    {
                        if (string.Equals(d.LevelName, lvlName, StringComparison.OrdinalIgnoreCase))
                        {
                            string norm = NormalizeCategoryName(d.DeductionType);
                            if (string.Equals(norm, cat, StringComparison.OrdinalIgnoreCase))
                            {
                                if (string.Equals(d.UsageCategory, "Commercial", StringComparison.OrdinalIgnoreCase))
                                {
                                    comDedSqFt += d.AreaValue;
                                }
                                else
                                {
                                    resDedSqFt += d.AreaValue;
                                }
                            }
                        }
                    }

                    row.SetResidentialDeduction(cat, resDedSqFt * unitFactor);
                    row.SetCommercialDeduction(cat, comDedSqFt * unitFactor);
                }

                rows.Add(row);
            }

            result.Rows = rows;

            // 5. Calculate TOTALS Row
            LevelZoningRow totals = new LevelZoningRow();
            totals.LevelName = "TOTALS";
            totals.GroupName = "TOTALS";
            totals.UlebPercent = config.UlebPercent;
            totals.LotArea = lotAreaConverted;

            double totResGross = 0;
            double totComGross = 0;
            Dictionary<string, double> totResDeds = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, double> totComDeds = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

            foreach (string cat in result.DeductionCategories)
            {
                totResDeds[cat] = 0;
                totComDeds[cat] = 0;
            }

            foreach (LevelZoningRow r in rows)
            {
                totResGross += r.ResidentialGrossFloorArea;
                totComGross += r.CommercialGrossFloorArea;

                foreach (string cat in result.DeductionCategories)
                {
                    totResDeds[cat] += r.ResidentialDeductions.ContainsKey(cat) ? r.ResidentialDeductions[cat] : 0;
                    totComDeds[cat] += r.CommercialDeductions.ContainsKey(cat) ? r.CommercialDeductions[cat] : 0;
                }
            }

            totals.ResidentialGrossFloorArea = totResGross;
            totals.CommercialGrossFloorArea = totComGross;

            foreach (string cat in result.DeductionCategories)
            {
                totals.SetResidentialDeduction(cat, totResDeds[cat]);
                totals.SetCommercialDeduction(cat, totComDeds[cat]);
            }

            result.TotalsRow = totals;

            return result;
        }

        private string NormalizeCategoryName(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
            string t = raw.Trim().ToUpperInvariant();

            if (t == "CHASE WALLS" || t == "CHASE" || t == "SHAFTS" || t == "SHAFT") return "CHASE WALL";
            if (t == "STAIR" || t == "STAIRWELL" || t == "STAIRWAY") return "STAIRS";
            if (t == "PARKING" || t == "GARAGE") return "PARKING";
            if (t == "BICYCLE" || t == "BICYCLE PARKING" || t == "BIKE" || t == "BIKE PARKING") return "BYCYCLE PARKING";
            if (t == "AMENITY" || t == "AMENITIES") return "AMENITIES";
            if (t == "CORRIDORS" || t == "HALLWAY") return "CORRIDOR";
            if (t == "MECHANICAL" || t == "MECH" || t == "HVAC" || t == "BOILER") return "MECH ROOM";
            if (t == "TRASH" || t == "GARBAGE" || t == "COMPACTOR") return "REFUSE";

            return t;
        }

        private TypicalFloorGroup FindMatchingGroup(string lvlName, double lvlElev, Dictionary<string, double> levelElevations, List<TypicalFloorGroup> groups)
        {
            if (groups == null || groups.Count == 0) return null;

            foreach (TypicalFloorGroup g in groups)
            {
                if (string.IsNullOrEmpty(g.FromLevelName) || string.IsNullOrEmpty(g.ToLevelName))
                {
                    if (string.Equals(g.SourceLevelName, lvlName, StringComparison.OrdinalIgnoreCase))
                        return g;
                    continue;
                }

                double fromElev, toElev;
                if (levelElevations.TryGetValue(g.FromLevelName, out fromElev) && levelElevations.TryGetValue(g.ToLevelName, out toElev))
                {
                    double minE = Math.Min(fromElev, toElev);
                    double maxE = Math.Max(fromElev, toElev);

                    if (lvlElev >= minE - 0.001 && lvlElev <= maxE + 0.001)
                    {
                        return g;
                    }
                }
                else
                {
                    if (string.Equals(g.FromLevelName, lvlName, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(g.ToLevelName, lvlName, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(g.SourceLevelName, lvlName, StringComparison.OrdinalIgnoreCase))
                    {
                        return g;
                    }
                }
            }
            return null;
        }

        private ZoningTableResult ComputeProjectGrandTotal(List<ZoningTableResult> bldgTables, MappingConfig config)
        {
            ZoningTableResult summary = new ZoningTableResult();
            summary.BuildingName = "ALL BUILDINGS TOTAL";
            summary.LotArea = config.LotArea;
            summary.UlebPercent = config.UlebPercent;

            double resGross = 0;
            double comGross = 0;
            Dictionary<string, double> resDeds = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, double> comDeds = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

            foreach (string cat in summary.DeductionCategories)
            {
                resDeds[cat] = 0;
                comDeds[cat] = 0;
            }

            foreach (ZoningTableResult t in bldgTables)
            {
                if (t.TotalsRow == null) continue;
                resGross += t.TotalsRow.ResidentialGrossFloorArea;
                comGross += t.TotalsRow.CommercialGrossFloorArea;

                foreach (string cat in summary.DeductionCategories)
                {
                    resDeds[cat] += t.TotalsRow.ResidentialDeductions.ContainsKey(cat) ? t.TotalsRow.ResidentialDeductions[cat] : 0;
                    comDeds[cat] += t.TotalsRow.CommercialDeductions.ContainsKey(cat) ? t.TotalsRow.CommercialDeductions[cat] : 0;
                }
            }

            summary.TotalsRow = new LevelZoningRow
            {
                LevelName = "TOTALS",
                GroupName = "TOTALS",
                ResidentialGrossFloorArea = resGross,
                CommercialGrossFloorArea = comGross,
                UlebPercent = config.UlebPercent,
                LotArea = config.LotArea
            };

            foreach (string cat in summary.DeductionCategories)
            {
                summary.TotalsRow.SetResidentialDeduction(cat, resDeds[cat]);
                summary.TotalsRow.SetCommercialDeduction(cat, comDeds[cat]);
            }

            return summary;
        }
    }
}
