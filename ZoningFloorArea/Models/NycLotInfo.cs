using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;

namespace ZoningFloorArea.Models
{
    public enum LotElementType
    {
        ModelCurves,        // 3D Model Curves on Level
        DetailCurves,       // 2D Detail Curves in active plan view
        AreaBoundaryLines   // Area Boundary Lines (for Area/Zoning plans)
    }

    public enum LotAnchorCorner
    {
        Southwest, // Min X, Min Y (Default)
        Northwest, // Min X, Max Y
        Southeast, // Max X, Min Y
        Northeast, // Max X, Max Y
        Center     // Center of Bounding Box
    }

    public enum LotGroupingMode
    {
        SingleGroup, // All elements in 1 Group named after Lot Address
        SplitGroups, // 2 Groups: [NYC Lot - Address] and [NYC Context - Block]
        NoGroup      // Leave elements ungrouped
    }

    public class NycSearchResult
    {
        public string Label { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Borough { get; set; } = string.Empty;
        public string Bbl { get; set; } = string.Empty;
        public string HouseNumber { get; set; } = string.Empty;
        public string Street { get; set; } = string.Empty;
        public string PostalCode { get; set; } = string.Empty;

        public override string ToString() => Label;
    }

    public class NycBuildingFootprint
    {
        public int Bin { get; set; }
        public string BaseBbl { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public double HeightRoofFt { get; set; }
        public double GroundElevFt { get; set; }
        public double NumFloors { get; set; }
        public int YearBuilt { get; set; }
        public string BldgClass { get; set; } = string.Empty;
        public bool IsSubjectLotBuilding { get; set; }

        public double EffectiveHeightFt
        {
            get
            {
                if (HeightRoofFt > 5.0) return HeightRoofFt;
                if (NumFloors > 0) return NumFloors * 12.0;
                return 30.0;
            }
        }

        public List<List<XYZ>> PolygonRings { get; set; } = new List<List<XYZ>>();
    }

    public class NycLotInfo
    {
        // Identifiers
        public string Bbl { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Borough { get; set; } = string.Empty;
        public string Block { get; set; } = string.Empty;
        public string Lot { get; set; } = string.Empty;
        public string ZipCode { get; set; } = string.Empty;

        // Zoning & Urban Planning
        public string ZoningDistrict1 { get; set; } = string.Empty;
        public string ZoningDistrict2 { get; set; } = string.Empty;
        public string CommercialOverlay1 { get; set; } = string.Empty;
        public string CommercialOverlay2 { get; set; } = string.Empty;
        public string SpecialDistrict1 { get; set; } = string.Empty;
        public string SpecialDistrict2 { get; set; } = string.Empty;

        // FAR (Floor Area Ratio)
        public double ResidFar { get; set; }
        public double CommFar { get; set; }
        public double FacilFar { get; set; }
        public double BuiltFar { get; set; }

        // Areas & Dimensions (Values from NYC PLUTO)
        public double LotAreaSqFt { get; set; }
        public double BldgAreaSqFt { get; set; }
        public double LotFrontageFt { get; set; }
        public double LotDepthFt { get; set; }
        public double NumFloors { get; set; }
        public int YearBuilt { get; set; }
        public string LandUse { get; set; } = string.Empty;
        public string OwnerName { get; set; } = string.Empty;
        public string BuildingClass { get; set; } = string.Empty;

        // Adjacency to development lot
        public bool IsAdjacent { get; set; }

        // Geometry: List of polygon rings (each ring is a list of [X, Y] in EPSG:2263 US Survey Feet)
        public List<List<XYZ>> PolygonRings { get; set; } = new List<List<XYZ>>();

        // Bounding Box in State Plane Coordinates (Feet)
        public double MinX => PolygonRings.SelectMany(r => r).DefaultIfEmpty(XYZ.Zero).Min(p => p.X);
        public double MaxX => PolygonRings.SelectMany(r => r).DefaultIfEmpty(XYZ.Zero).Max(p => p.X);
        public double MinY => PolygonRings.SelectMany(r => r).DefaultIfEmpty(XYZ.Zero).Min(p => p.Y);
        public double MaxY => PolygonRings.SelectMany(r => r).DefaultIfEmpty(XYZ.Zero).Max(p => p.Y);

        public double WidthFt => Math.Max(0, MaxX - MinX);
        public double DepthFt => Math.Max(0, MaxY - MinY);

        public XYZ GetAnchorPoint(LotAnchorCorner corner)
        {
            switch (corner)
            {
                case LotAnchorCorner.Southwest:
                    return new XYZ(MinX, MinY, 0);
                case LotAnchorCorner.Northwest:
                    return new XYZ(MinX, MaxY, 0);
                case LotAnchorCorner.Southeast:
                    return new XYZ(MaxX, MinY, 0);
                case LotAnchorCorner.Northeast:
                    return new XYZ(MaxX, MaxY, 0);
                case LotAnchorCorner.Center:
                    return new XYZ((MinX + MaxX) / 2.0, (MinY + MaxY) / 2.0, 0);
                default:
                    return new XYZ(MinX, MinY, 0);
            }
        }

        public string GetZoningSummary()
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(ZoningDistrict1)) parts.Add(ZoningDistrict1);
            if (!string.IsNullOrWhiteSpace(ZoningDistrict2)) parts.Add(ZoningDistrict2);
            if (!string.IsNullOrWhiteSpace(CommercialOverlay1)) parts.Add($"Overlay: {CommercialOverlay1}");
            if (!string.IsNullOrWhiteSpace(SpecialDistrict1)) parts.Add($"Special: {SpecialDistrict1}");
            return parts.Count > 0 ? string.Join(" / ", parts) : "N/A";
        }
    }

    public class NycBlockContext
    {
        public string Borough { get; set; } = string.Empty;
        public string BlockNumber { get; set; } = string.Empty;
        public NycLotInfo SubjectLot { get; set; } = new NycLotInfo();
        public List<NycLotInfo> OtherLots { get; set; } = new List<NycLotInfo>();
        public List<NycBuildingFootprint> Buildings { get; set; } = new List<NycBuildingFootprint>();

        public List<NycLotInfo> AdjacentLots => OtherLots.Where(l => l.IsAdjacent).ToList();
        public List<NycLotInfo> RemainingBlockLots => OtherLots.Where(l => !l.IsAdjacent && l.Bbl != SubjectLot.Bbl).ToList();

        public List<NycLotInfo> AllLots
        {
            get
            {
                var list = new List<NycLotInfo> { SubjectLot };
                list.AddRange(OtherLots.Where(l => l.Bbl != SubjectLot.Bbl));
                return list;
            }
        }

        // Bounding Box of the Entire Block
        public double MinX => AllLots.Select(l => l.MinX).DefaultIfEmpty(0).Min();
        public double MaxX => AllLots.Select(l => l.MaxX).DefaultIfEmpty(0).Max();
        public double MinY => AllLots.Select(l => l.MinY).DefaultIfEmpty(0).Min();
        public double MaxY => AllLots.Select(l => l.MaxY).DefaultIfEmpty(0).Max();

        public double WidthFt => Math.Max(0, MaxX - MinX);
        public double DepthFt => Math.Max(0, MaxY - MinY);

        /// <summary>
        /// Computes adjacency between other lots in the block and the subject lot.
        /// </summary>
        public void CalculateAdjacency(double toleranceFt = 1.0)
        {
            if (SubjectLot == null || SubjectLot.PolygonRings.Count == 0) return;

            var subjectPoints = SubjectLot.PolygonRings.SelectMany(r => r).ToList();

            foreach (var other in OtherLots)
            {
                if (other.Bbl == SubjectLot.Bbl)
                {
                    other.IsAdjacent = false;
                    continue;
                }

                bool isAdjacent = false;
                var otherPoints = other.PolygonRings.SelectMany(r => r).ToList();

                foreach (var pOther in otherPoints)
                {
                    foreach (var pSub in subjectPoints)
                    {
                        if (pOther.DistanceTo(pSub) <= toleranceFt)
                        {
                            isAdjacent = true;
                            break;
                        }
                    }
                    if (isAdjacent) break;
                }

                if (!isAdjacent && other.MinX <= SubjectLot.MaxX + toleranceFt &&
                    other.MaxX >= SubjectLot.MinX - toleranceFt &&
                    other.MinY <= SubjectLot.MaxY + toleranceFt &&
                    other.MaxY >= SubjectLot.MinY - toleranceFt)
                {
                    isAdjacent = true;
                }

                other.IsAdjacent = isAdjacent;
            }
        }

        /// <summary>
        /// Identifies surrounding street names from the address registry of the block.
        /// </summary>
        public Dictionary<string, string> GetSurroundingStreetNames()
        {
            var streets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var lot in AllLots)
            {
                if (string.IsNullOrWhiteSpace(lot.Address)) continue;
                string addr = lot.Address.Trim();

                int firstSpace = addr.IndexOf(' ');
                if (firstSpace > 0 && firstSpace < addr.Length - 1 && char.IsDigit(addr[0]))
                {
                    string street = addr.Substring(firstSpace + 1).Trim();
                    if (!string.IsNullOrWhiteSpace(street))
                    {
                        streets.Add(street);
                    }
                }
                else
                {
                    streets.Add(addr);
                }
            }

            var result = new Dictionary<string, string>();
            var streetList = streets.ToList();

            if (streetList.Count == 0 && !string.IsNullOrWhiteSpace(SubjectLot.Address))
            {
                streetList.Add(SubjectLot.Address);
            }

            if (streetList.Count > 0) result["North"] = streetList[0];
            if (streetList.Count > 1) result["South"] = streetList[1];
            if (streetList.Count > 2) result["East"] = streetList[2];
            if (streetList.Count > 3) result["West"] = streetList[3];

            if (streetList.Count == 1)
            {
                result["South"] = streetList[0];
            }

            return result;
        }
    }
}
