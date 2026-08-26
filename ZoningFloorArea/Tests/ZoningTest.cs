using System;
using System.Collections.Generic;
using ZoningFloorArea.Models;
using ZoningFloorArea.Services;

namespace ZoningFloorArea.Tests
{
    public class ZoningTest
    {
        public static void RunMockVerification()
        {
            List<AreaDataModel> mockAreas = new List<AreaDataModel>();
            mockAreas.Add(new AreaDataModel { AreaSchemeName = "Gross Building", LevelName = "1st", LevelElevation = 10, AreaValue = 1737.94, UsageCategory = "Residential" });
            mockAreas.Add(new AreaDataModel { AreaSchemeName = "Rentable", LevelName = "1st", LevelElevation = 10, DeductionType = "AMENITIES", AreaValue = 250.91, UsageCategory = "Residential" });
            mockAreas.Add(new AreaDataModel { AreaSchemeName = "Rentable", LevelName = "1st", LevelElevation = 10, DeductionType = "CORRIDOR", AreaValue = 376.94, UsageCategory = "Residential" });

            mockAreas.Add(new AreaDataModel { AreaSchemeName = "Gross Building", LevelName = "3rd", LevelElevation = 30, AreaValue = 4428.28, UsageCategory = "Residential" });
            mockAreas.Add(new AreaDataModel { AreaSchemeName = "Rentable", LevelName = "3rd", LevelElevation = 30, DeductionType = "CHASE WALLS", AreaValue = 109.49, UsageCategory = "Residential" });
            mockAreas.Add(new AreaDataModel { AreaSchemeName = "Rentable", LevelName = "3rd", LevelElevation = 30, DeductionType = "STAIRS", AreaValue = 31.28, UsageCategory = "Residential" });
            mockAreas.Add(new AreaDataModel { AreaSchemeName = "Rentable", LevelName = "3rd", LevelElevation = 30, DeductionType = "MECHANICAL", AreaValue = 16.67, UsageCategory = "Residential" });
            mockAreas.Add(new AreaDataModel { AreaSchemeName = "Rentable", LevelName = "3rd", LevelElevation = 30, DeductionType = "CORRIDOR", AreaValue = 416.72, UsageCategory = "Residential" });
            mockAreas.Add(new AreaDataModel { AreaSchemeName = "Rentable", LevelName = "3rd", LevelElevation = 30, DeductionType = "REFUSE", AreaValue = 24.00, UsageCategory = "Residential" });

            MappingConfig config = new MappingConfig();
            config.BuildingName = "BUILDING C";
            config.GrossAreaSchemeName = "Gross Building";
            config.DeductionAreaSchemeName = "Rentable";
            config.LotArea = 34500.0;
            config.UlebPercent = 0.05;

            List<TypicalFloorGroup> groups = new List<TypicalFloorGroup>();

            ZoningCalculator calc = new ZoningCalculator();
            ZoningTableResult result = calc.ComputeZoningTable(mockAreas, config, groups);

            Console.WriteLine("=== ZFA CALCULATOR TEST RESULTS ===");
            Console.WriteLine(string.Format("Building: {0}", result.BuildingName));
            Console.WriteLine(string.Format("Lot Area: {0:N2} sq ft", result.LotArea));
            Console.WriteLine(string.Format("Residential Rows: {0}", result.ResidentialRows.Count));

            foreach (LevelZoningRow r in result.ResidentialRows)
            {
                Console.WriteLine(string.Format("Level: {0} | Gross: {1:N2} | Deductions: {2:N2} | Net: {3:N2} | ULEB (5%): {4:N2} | ZFA: {5:N2} | FAR: {6:N2}", r.LevelName, r.GrossFloorArea, r.TotalDeductions, r.NetArea, r.UlebAmount, r.ZoningFloorArea, r.Far));
            }

            Console.WriteLine(string.Format("Subtotal ZFA: {0:N2}", result.ResidentialSubtotal.ZoningFloorArea));
            Console.WriteLine(string.Format("Total ZFA: {0:N2}", result.TotalZoningFloorArea));
            Console.WriteLine(string.Format("Total FAR: {0:N2}", result.TotalFar));
        }
    }
}
