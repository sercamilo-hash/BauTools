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

            // 2. Base Deduction Categories
            List<string> baseCategories = new List<string>
            {
                "CHASE WALLS",
                "STAIRS",
                "MECHANICAL",
                "BYCYCLE PARKING",
                "AMENITIES",
                "CORRIDOR",
                "REFUSE"
            };

            List<string> finalCategories = new List<string>(baseCategories);

            foreach (AreaDataModel d in deductionAreas)
            {
                if (!string.IsNullOrEmpty(d.DeductionType))
                {
                    string trimmedType = d.DeductionType.Trim().ToUpperInvariant();
                    bool exists = false;
                    foreach (string cat in finalCategories)
                    {
                        if (string.Equals(cat, trimmedType, StringComparison.OrdinalIgnoreCase))
                        {
                            exists = true;
                            break;
                        }
                    }
                    if (!exists)
                    {
                        finalCategories.Add(trimmedType);
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

            // 4. Build Level Rows for Residential and Commercial
            List<LevelZoningRow> resRows = new List<LevelZoningRow>();
            List<LevelZoningRow> comRows = new List<LevelZoningRow>();

            foreach (string lvlName in levelNames)
            {
                double lvlElev = levelElevations[lvlName];
                TypicalFloorGroup matchingGroup = FindMatchingGroup(lvlName, lvlElev, levelElevations, groups);

                // Residential Row
                double resGrossSqFt = 0;
                foreach (AreaDataModel a in grossAreas)
                {
                    if (string.Equals(a.LevelName, lvlName, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(a.UsageCategory, "Residential", StringComparison.OrdinalIgnoreCase))
                    {
                        resGrossSqFt += a.AreaValue;
                    }
                }

                LevelZoningRow resRow = new LevelZoningRow();
                resRow.LevelName = lvlName;
                resRow.LevelElevation = lvlElev;
                resRow.UsageCategory = "Residential";
                resRow.GrossFloorArea = resGrossSqFt * unitFactor;
                resRow.UlebPercent = config.UlebPercent;
                resRow.LotArea = lotAreaConverted;

                if (matchingGroup != null)
                {
                    resRow.GroupName = matchingGroup.Name;
                    resRow.GroupColorHex = matchingGroup.ColorHex;
                }

                foreach (string cat in result.DeductionCategories)
                {
                    double dedSqFt = 0;
                    foreach (AreaDataModel d in deductionAreas)
                    {
                        if (string.Equals(d.LevelName, lvlName, StringComparison.OrdinalIgnoreCase) &&
                            string.Equals(d.UsageCategory, "Residential", StringComparison.OrdinalIgnoreCase) &&
                            string.Equals(d.DeductionType, cat, StringComparison.OrdinalIgnoreCase))
                        {
                            dedSqFt += d.AreaValue;
                        }
                    }
                    resRow.SetDeduction(cat, dedSqFt * unitFactor);
                }

                resRows.Add(resRow);

                // Commercial Row
                double comGrossSqFt = 0;
                foreach (AreaDataModel a in grossAreas)
                {
                    if (string.Equals(a.LevelName, lvlName, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(a.UsageCategory, "Commercial", StringComparison.OrdinalIgnoreCase))
                    {
                        comGrossSqFt += a.AreaValue;
                    }
                }

                LevelZoningRow comRow = new LevelZoningRow();
                comRow.LevelName = lvlName;
                comRow.LevelElevation = lvlElev;
                comRow.UsageCategory = "Commercial";
                comRow.GrossFloorArea = comGrossSqFt * unitFactor;
                comRow.UlebPercent = config.UlebPercent;
                comRow.LotArea = lotAreaConverted;

                if (matchingGroup != null)
                {
                    comRow.GroupName = matchingGroup.Name;
                    comRow.GroupColorHex = matchingGroup.ColorHex;
                }

                foreach (string cat in result.DeductionCategories)
                {
                    double dedSqFt = 0;
                    foreach (AreaDataModel d in deductionAreas)
                    {
                        if (string.Equals(d.LevelName, lvlName, StringComparison.OrdinalIgnoreCase) &&
                            string.Equals(d.UsageCategory, "Commercial", StringComparison.OrdinalIgnoreCase) &&
                            string.Equals(d.DeductionType, cat, StringComparison.OrdinalIgnoreCase))
                        {
                            dedSqFt += d.AreaValue;
                        }
                    }
                    comRow.SetDeduction(cat, dedSqFt * unitFactor);
                }

                comRows.Add(comRow);
            }

            result.ResidentialRows = resRows;
            result.CommercialRows = comRows;

            // 5. Calculate Subtotals and Grand Total
            result.ResidentialSubtotal = CalculateSubtotal("SUBTOTAL", "Residential", resRows, result.DeductionCategories, config.UlebPercent, lotAreaConverted);
            result.CommercialSubtotal = CalculateSubtotal("SUBTOTAL", "Commercial", comRows, result.DeductionCategories, config.UlebPercent, lotAreaConverted);
            result.GrandTotal = CalculateGrandTotal("TOTAL", result.ResidentialSubtotal, result.CommercialSubtotal, result.DeductionCategories, config.UlebPercent, lotAreaConverted);

            return result;
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

            foreach (ZoningTableResult t in bldgTables)
            {
                resGross += t.ResidentialSubtotal.GrossFloorArea;
                comGross += t.CommercialSubtotal.GrossFloorArea;

                foreach (string cat in summary.DeductionCategories)
                {
                    if (!resDeds.ContainsKey(cat)) resDeds[cat] = 0;
                    if (!comDeds.ContainsKey(cat)) comDeds[cat] = 0;

                    resDeds[cat] += t.ResidentialSubtotal.GetDeduction(cat);
                    comDeds[cat] += t.CommercialSubtotal.GetDeduction(cat);
                }
            }

            summary.ResidentialSubtotal.GrossFloorArea = resGross;
            summary.CommercialSubtotal.GrossFloorArea = comGross;

            foreach (string cat in summary.DeductionCategories)
            {
                summary.ResidentialSubtotal.SetDeduction(cat, resDeds[cat]);
                summary.CommercialSubtotal.SetDeduction(cat, comDeds[cat]);
            }

            summary.GrandTotal.GrossFloorArea = resGross + comGross;
            foreach (string cat in summary.DeductionCategories)
            {
                summary.GrandTotal.SetDeduction(cat, resDeds[cat] + comDeds[cat]);
            }

            return summary;
        }

        private LevelZoningRow CalculateSubtotal(string label, string usageCat, List<LevelZoningRow> rows, List<string> categories, double ulebPercent, double lotArea)
        {
            LevelZoningRow subtotal = new LevelZoningRow();
            subtotal.LevelName = label;
            subtotal.UsageCategory = usageCat;
            subtotal.UlebPercent = ulebPercent;
            subtotal.LotArea = lotArea;

            double gross = 0;
            Dictionary<string, double> dedSums = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            foreach (string cat in categories)
            {
                dedSums[cat] = 0;
            }

            foreach (LevelZoningRow r in rows)
            {
                gross += r.GrossFloorArea;
                foreach (string cat in categories)
                {
                    dedSums[cat] += r.GetDeduction(cat);
                }
            }

            subtotal.GrossFloorArea = gross;
            foreach (string cat in categories)
            {
                subtotal.SetDeduction(cat, dedSums[cat]);
            }

            return subtotal;
        }

        private LevelZoningRow CalculateGrandTotal(string label, LevelZoningRow resSub, LevelZoningRow comSub, List<string> categories, double ulebPercent, double lotArea)
        {
            LevelZoningRow grandTotal = new LevelZoningRow();
            grandTotal.LevelName = label;
            grandTotal.UsageCategory = "Project Total";
            grandTotal.UlebPercent = ulebPercent;
            grandTotal.LotArea = lotArea;

            grandTotal.GrossFloorArea = resSub.GrossFloorArea + comSub.GrossFloorArea;

            foreach (string cat in categories)
            {
                double totalDed = resSub.GetDeduction(cat) + comSub.GetDeduction(cat);
                grandTotal.SetDeduction(cat, totalDed);
            }

            return grandTotal;
        }
    }
}
