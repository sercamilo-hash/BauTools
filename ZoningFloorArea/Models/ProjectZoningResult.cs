using System.Collections.Generic;

namespace ZoningFloorArea.Models
{
    public class ProjectZoningResult
    {
        public string ProjectName { get; set; }
        public double LotArea { get; set; }
        public double UlebPercent { get; set; }

        public List<ZoningTableResult> BuildingTables { get; set; }
        public ZoningTableResult OverallSummary { get; set; }

        public double TotalProjectZoningFloorArea
        {
            get
            {
                double sum = 0;
                if (BuildingTables != null)
                {
                    foreach (ZoningTableResult table in BuildingTables)
                    {
                        sum += table.TotalZoningFloorArea;
                    }
                }
                return sum;
            }
        }

        public double TotalProjectFar
        {
            get { return LotArea > 0 ? TotalProjectZoningFloorArea / LotArea : 0; }
        }

        public ProjectZoningResult()
        {
            ProjectName = "PROJECT ZONING SUMMARY";
            LotArea = 34500.0;
            UlebPercent = 0.05;

            BuildingTables = new List<ZoningTableResult>();
            OverallSummary = new ZoningTableResult();
            OverallSummary.BuildingName = "ALL BUILDINGS TOTAL";
        }
    }
}
