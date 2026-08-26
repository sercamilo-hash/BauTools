using System.Collections.Generic;

namespace ZoningFloorArea.Models
{
    public class AreaDataModel
    {
        public string ElementId { get; set; }
        public string Name { get; set; }
        public double AreaValue { get; set; }
        public string LevelName { get; set; }
        public double LevelElevation { get; set; }
        public string AreaSchemeName { get; set; }
        public string DeductionType { get; set; }
        public string UsageCategory { get; set; }
        public string BuildingName { get; set; }

        public AreaDataModel()
        {
            ElementId = string.Empty;
            Name = string.Empty;
            LevelName = string.Empty;
            AreaSchemeName = string.Empty;
            DeductionType = string.Empty;
            UsageCategory = "Residential";
            BuildingName = "BUILDING C";
        }
    }
}
