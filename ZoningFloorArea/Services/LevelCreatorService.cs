using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Autodesk.Revit.DB;
using ZoningFloorArea.Models;

namespace ZoningFloorArea.Services
{
    public static class LevelCreatorService
    {
        public static string GetOrdinal(int number)
        {
            if (number <= 0) return number.ToString();

            int rem100 = number % 100;
            if (rem100 >= 11 && rem100 <= 13)
            {
                return string.Format("{0}TH", number);
            }

            switch (number % 10)
            {
                case 1: return string.Format("{0}ST", number);
                case 2: return string.Format("{0}ND", number);
                case 3: return string.Format("{0}RD", number);
                default: return string.Format("{0}TH", number);
            }
        }

        public static string FormatLength(Document doc, double lengthFeet)
        {
            try
            {
                return UnitFormatUtils.Format(doc.GetUnits(), SpecTypeId.Length, lengthFeet, false);
            }
            catch
            {
                int feet = (int)Math.Truncate(lengthFeet);
                double remainingInches = Math.Abs((lengthFeet - feet) * 12.0);
                if (Math.Abs(lengthFeet) < 0.0001) return "0'-0\"";
                return string.Format("{0}'-{1:F0}\"", feet, remainingInches);
            }
        }

        public static bool TryParseLength(Document doc, string input, out double resultFeet)
        {
            resultFeet = 0;
            if (string.IsNullOrWhiteSpace(input)) return false;

            string clean = input.Trim();

            try
            {
                double val;
                if (UnitFormatUtils.TryParse(doc.GetUnits(), SpecTypeId.Length, clean, out val))
                {
                    resultFeet = val;
                    return true;
                }
            }
            catch
            {
            }

            Match metricMatch = Regex.Match(clean, @"^([+-]?\d+(?:\.\d+)?)\s*(m|mm|cm|meters|metros)?$", RegexOptions.IgnoreCase);
            if (metricMatch.Success)
            {
                double val;
                if (double.TryParse(metricMatch.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out val))
                {
                    string unit = metricMatch.Groups[2].Value.ToLower();
                    if (unit == "mm")
                        resultFeet = (val / 1000.0) * 3.280839895013123;
                    else if (unit == "cm")
                        resultFeet = (val / 100.0) * 3.280839895013123;
                    else if (unit == "m" || unit == "meters" || unit == "metros")
                        resultFeet = val * 3.280839895013123;
                    else
                    {
                        bool isMetric = doc.GetUnits().GetFormatOptions(SpecTypeId.Length).GetUnitTypeId() != UnitTypeId.Feet;
                        resultFeet = isMetric ? val * 3.280839895013123 : val;
                    }
                    return true;
                }
            }

            Match feetInchesMatch = Regex.Match(clean, @"^([+-]?\d+)'(?:\s*([0-9.]+)\"")?$", RegexOptions.IgnoreCase);
            if (feetInchesMatch.Success)
            {
                double f;
                if (double.TryParse(feetInchesMatch.Groups[1].Value, out f))
                {
                    double inches = 0;
                    double inc;
                    if (feetInchesMatch.Groups[2].Success && double.TryParse(feetInchesMatch.Groups[2].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out inc))
                    {
                        inches = inc;
                    }
                    resultFeet = f + (inches / 12.0);
                    return true;
                }
            }

            double simpleVal;
            if (double.TryParse(clean, NumberStyles.Float, CultureInfo.InvariantCulture, out simpleVal) ||
                double.TryParse(clean, NumberStyles.Float, CultureInfo.CurrentCulture, out simpleVal))
            {
                resultFeet = simpleVal;
                return true;
            }

            return false;
        }

        public static List<LevelCreationItem> BuildPlannedLevels(
            Document doc,
            double baseElevationFeet,
            int startFloorNumber,
            int floorCount,
            double typicalHeightFeet,
            int cellarCount,
            double cellarHeightFeet,
            bool includeRoof,
            double roofHeightFeet,
            bool includeBulkhead,
            double bulkheadHeightFeet,
            bool createViewsDefault,
            bool createCeilingViewsDefault,
            bool useTwoDigits)
        {
            List<LevelCreationItem> list = new List<LevelCreationItem>();

            for (int i = cellarCount; i >= 1; i--)
            {
                double elev = baseElevationFeet - (i * cellarHeightFeet);
                string name;
                if (i == 1)
                {
                    name = useTwoDigits ? "00 CELLAR" : "CELLAR";
                }
                else
                {
                    int cNum = i - 1;
                    name = string.Format("CELLAR {0}", cNum);
                }

                LevelCreationItem item = new LevelCreationItem();
                item.LevelName = name;
                item.ElevationFeet = elev;
                item.ElevationDisplay = FormatLength(doc, elev);
                item.LevelType = "Cellar";
                item.CreateFloorPlan = createViewsDefault;
                item.CreateCeilingPlan = createCeilingViewsDefault;
                list.Add(item);
            }

            int currentFloorNum = startFloorNumber;
            double currentElev = baseElevationFeet;

            for (int f = 0; f < floorCount; f++)
            {
                string prefix = useTwoDigits ? string.Format("{0:D2} ", currentFloorNum) : string.Format("{0} ", currentFloorNum);
                string ordinal = GetOrdinal(currentFloorNum);
                string name = string.Format("{0}{1} FL.", prefix, ordinal);

                LevelCreationItem item = new LevelCreationItem();
                item.LevelName = name;
                item.ElevationFeet = currentElev;
                item.ElevationDisplay = FormatLength(doc, currentElev);
                item.LevelType = "Typical";
                item.CreateFloorPlan = createViewsDefault;
                item.CreateCeilingPlan = createCeilingViewsDefault;
                list.Add(item);

                currentElev += typicalHeightFeet;
                currentFloorNum++;
            }

            if (includeRoof)
            {
                double roofElev = (floorCount > 0) ? (currentElev - typicalHeightFeet + roofHeightFeet) : (baseElevationFeet + roofHeightFeet);
                currentElev = roofElev;

                string prefix = useTwoDigits ? string.Format("{0:D2} ", currentFloorNum) : "";
                string name = string.Format("{0}ROOF", prefix);

                LevelCreationItem item = new LevelCreationItem();
                item.LevelName = name;
                item.ElevationFeet = roofElev;
                item.ElevationDisplay = FormatLength(doc, roofElev);
                item.LevelType = "Roof";
                item.CreateFloorPlan = createViewsDefault;
                item.CreateCeilingPlan = createCeilingViewsDefault;
                list.Add(item);

                currentFloorNum++;
            }

            if (includeBulkhead)
            {
                double bulkheadElev = currentElev + bulkheadHeightFeet;

                string prefix = useTwoDigits ? string.Format("{0:D2} ", currentFloorNum) : "";
                string name = string.Format("{0}BULKHEAD", prefix);

                LevelCreationItem item = new LevelCreationItem();
                item.LevelName = name;
                item.ElevationFeet = bulkheadElev;
                item.ElevationDisplay = FormatLength(doc, bulkheadElev);
                item.LevelType = "Bulkhead";
                item.CreateFloorPlan = createViewsDefault;
                item.CreateCeilingPlan = createCeilingViewsDefault;
                list.Add(item);
            }

            for (int i = 0; i < list.Count; i++)
            {
                list[i].Index = i + 1;
            }

            return list;
        }

        public static Tuple<int, int, List<string>> CreateLevelsInRevit(
            Document doc,
            List<LevelCreationItem> items,
            bool createCeilingPlans)
        {
            int levelsCreated = 0;
            int viewsCreated = 0;
            List<string> errors = new List<string>();

            if (items == null || items.Count == 0)
                return Tuple.Create(0, 0, errors);

            HashSet<string> existingNames = new HashSet<string>(
                new FilteredElementCollector(doc)
                    .OfClass(typeof(Level))
                    .Cast<Level>()
                    .Select(l => l.Name),
                StringComparer.OrdinalIgnoreCase);

            ViewFamilyType floorPlanVft = null;
            FilteredElementCollector vftCollector = new FilteredElementCollector(doc).OfClass(typeof(ViewFamilyType));
            foreach (ViewFamilyType vft in vftCollector)
            {
                if (vft.ViewFamily == ViewFamily.FloorPlan)
                {
                    floorPlanVft = vft;
                    break;
                }
            }

            ViewFamilyType ceilingPlanVft = null;
            if (createCeilingPlans)
            {
                foreach (ViewFamilyType vft in vftCollector)
                {
                    if (vft.ViewFamily == ViewFamily.CeilingPlan)
                    {
                        ceilingPlanVft = vft;
                        break;
                    }
                }

                if (ceilingPlanVft == null)
                {
                    errors.Add("Warning: No CeilingPlan (RCP) ViewFamilyType found in this Revit project.");
                }
            }

            using (Transaction tx = new Transaction(doc, "BauTools: Batch Create Levels"))
            {
                tx.Start();

                foreach (LevelCreationItem item in items)
                {
                    if (!item.IsIncluded) continue;

                    try
                    {
                        Level newLevel = Level.Create(doc, item.ElevationFeet);
                        levelsCreated++;

                        string targetName = item.LevelName;
                        int duplicateSuffix = 1;
                        while (existingNames.Contains(targetName))
                        {
                            targetName = string.Format("{0} ({1})", item.LevelName, duplicateSuffix++);
                        }

                        try
                        {
                            newLevel.Name = targetName;
                            existingNames.Add(targetName);
                        }
                        catch (Exception nameEx)
                        {
                            errors.Add(string.Format("Level at {0}: could not assign name '{1}': {2}", item.ElevationDisplay, targetName, nameEx.Message));
                        }

                        if (item.CreateFloorPlan && floorPlanVft != null)
                        {
                            try
                            {
                                ViewPlan floorPlan = ViewPlan.Create(doc, floorPlanVft.Id, newLevel.Id);
                                viewsCreated++;
                            }
                            catch (Exception viewEx)
                            {
                                errors.Add(string.Format("Could not create Floor Plan for '{0}': {1}", newLevel.Name, viewEx.Message));
                            }
                        }

                        if ((item.CreateCeilingPlan || createCeilingPlans) && ceilingPlanVft != null)
                        {
                            try
                            {
                                ViewPlan ceilingPlan = ViewPlan.Create(doc, ceilingPlanVft.Id, newLevel.Id);
                                viewsCreated++;
                            }
                            catch (Exception rcpEx)
                            {
                                errors.Add(string.Format("Could not create RCP (Ceiling Plan) for '{0}': {1}", newLevel.Name, rcpEx.Message));
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        errors.Add(string.Format("Error creating level '{0}' at {1}: {2}", item.LevelName, item.ElevationDisplay, ex.Message));
                    }
                }

                tx.Commit();
            }

            return Tuple.Create(levelsCreated, viewsCreated, errors);
        }
    }
}
