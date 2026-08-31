using System.Collections.Generic;

namespace ZoningFloorArea.Models
{
    public class ZoningTableResult
    {
        public string BuildingName { get; set; }
        public double LotArea { get; set; }
        public double UlebPercent { get; set; }

        public List<string> DeductionCategories { get; set; }
        public List<LevelZoningRow> Rows { get; set; }
        public LevelZoningRow TotalsRow { get; set; }

        // Backward compatibility
        public List<LevelZoningRow> ResidentialRows
        {
            get { return Rows; }
            set { Rows = value; }
        }

        public List<LevelZoningRow> CommercialRows
        {
            get { return Rows; }
            set { Rows = value; }
        }

        public LevelZoningRow ResidentialSubtotal
        {
            get { return TotalsRow; }
            set { TotalsRow = value; }
        }

        public LevelZoningRow CommercialSubtotal
        {
            get { return TotalsRow; }
            set { TotalsRow = value; }
        }

        public LevelZoningRow GrandTotal
        {
            get { return TotalsRow; }
            set { TotalsRow = value; }
        }

        public double TotalZoningFloorArea
        {
            get { return TotalsRow != null ? TotalsRow.TotalZfa : 0.0; }
        }

        public double TotalFar
        {
            get { return TotalsRow != null ? TotalsRow.TotalFar : 0.0; }
        }

        public ZoningTableResult()
        {
            BuildingName = "BUILDING C";
            LotArea = 34500.0;
            UlebPercent = 0.0;

            DeductionCategories = new List<string>
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

            Rows = new List<LevelZoningRow>();
            TotalsRow = new LevelZoningRow { LevelName = "TOTALS" };
        }
    }
}
