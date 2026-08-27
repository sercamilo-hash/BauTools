using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;

namespace ZoningFloorArea.Models
{
    public enum LotElementType
    {
        ModelCurves = 0,
        DetailLines = 1,
        RoomSeparators = 2,
        AreaBoundaryLines = 3
    }

    public enum LotAnchorCorner
    {
        Southwest = 0,
        Northwest = 1,
        Southeast = 2,
        Northeast = 3,
        Center = 4
    }

    public enum LotGroupingMode
    {
        SingleGroup = 0,
        SplitSubjectAndContext = 1,
        NoGrouping = 2,
        SplitGroups = 1,
        NoGroup = 2
    }

    public class NycSearchResult
    {
        public string Label { get; set; }
        public string Borough { get; set; }
        public string Block { get; set; }
        public string Lot { get; set; }
        public string Bbl { get; set; }
        public string Address { get; set; }
        public string HouseNumber { get; set; }
        public string Street { get; set; }
        public string PostalCode { get; set; }

        public NycSearchResult()
        {
            Label = string.Empty;
            Borough = string.Empty;
            Block = string.Empty;
            Lot = string.Empty;
            Bbl = string.Empty;
            Address = string.Empty;
            HouseNumber = string.Empty;
            Street = string.Empty;
            PostalCode = string.Empty;
        }

        public override string ToString()
        {
            return Label;
        }
    }

    public class NycBuildingFootprint
    {
        public string Bin { get; set; }
        public string Address { get; set; }
        public double HeightRoofFt { get; set; }
        public double GroundElevationFt { get; set; }
        public int NumFloors { get; set; }
        public int YearBuilt { get; set; }
        public bool IsSubjectBuilding { get; set; }
        public bool IsSubjectLotBuilding { get { return IsSubjectBuilding; } set { IsSubjectBuilding = value; } }
        public List<List<XYZ>> PolygonRings { get; set; }

        public double EffectiveHeightFt
        {
            get
            {
                if (HeightRoofFt > 5.0) return HeightRoofFt;
                if (NumFloors > 0) return NumFloors * 11.5;
                return 30.0;
            }
        }

        public NycBuildingFootprint()
        {
            Bin = string.Empty;
            Address = string.Empty;
            PolygonRings = new List<List<XYZ>>();
        }
    }

    public class NycLotInfo
    {
        public string Bbl { get; set; }
        public string Borough { get; set; }
        public string Block { get; set; }
        public string Lot { get; set; }
        public string Address { get; set; }
        public string ZipCode { get; set; }

        public double LotAreaSqFt { get; set; }
        public double TotalBldgAreaSqFt { get; set; }
        public double ResAreaSqFt { get; set; }
        public double ComAreaSqFt { get; set; }
        public double OfficeAreaSqFt { get; set; }
        public double RetailAreaSqFt { get; set; }
        public double GarageAreaSqFt { get; set; }
        public double StorageAreaSqFt { get; set; }
        public double FactoryAreaSqFt { get; set; }
        public double OtherAreaSqFt { get; set; }

        public int NumFloors { get; set; }
        public int NumBuildings { get; set; }
        public int YearBuilt { get; set; }

        public double BuiltFar { get; set; }
        public double ResFar { get; set; }
        public double ResidFar { get { return ResFar; } set { ResFar = value; } }
        public double CommFar { get; set; }
        public double FacilFar { get; set; }

        public double LotFrontageFt { get { return WidthFt; } set { } }
        public double LotDepthFt { get { return DepthFt; } set { } }
        public double BldgAreaSqFt { get { return TotalBldgAreaSqFt; } set { TotalBldgAreaSqFt = value; } }

        public string ZoningDistrict1 { get; set; }
        public string ZoningDistrict2 { get; set; }
        public string CommercialOverlay1 { get; set; }
        public string CommercialOverlay2 { get; set; }
        public string SpecialDistrict1 { get; set; }
        public string SpecialDistrict2 { get; set; }

        public string LandUse { get; set; }
        public string OwnerName { get; set; }
        public string BuildingClass { get; set; }

        public bool IsAdjacent { get; set; }
        public List<List<XYZ>> PolygonRings { get; set; }

        public double MinX
        {
            get
            {
                var pts = PolygonRings.SelectMany(r => r).ToList();
                return pts.Count > 0 ? pts.Min(p => p.X) : 0.0;
            }
        }
        public double MaxX
        {
            get
            {
                var pts = PolygonRings.SelectMany(r => r).ToList();
                return pts.Count > 0 ? pts.Max(p => p.X) : 0.0;
            }
        }
        public double MinY
        {
            get
            {
                var pts = PolygonRings.SelectMany(r => r).ToList();
                return pts.Count > 0 ? pts.Min(p => p.Y) : 0.0;
            }
        }
        public double MaxY
        {
            get
            {
                var pts = PolygonRings.SelectMany(r => r).ToList();
                return pts.Count > 0 ? pts.Max(p => p.Y) : 0.0;
            }
        }

        public double WidthFt { get { return Math.Max(0, MaxX - MinX); } }
        public double DepthFt { get { return Math.Max(0, MaxY - MinY); } }

        public NycLotInfo()
        {
            Bbl = string.Empty;
            Borough = string.Empty;
            Address = string.Empty;
            ZipCode = string.Empty;
            ZoningDistrict1 = string.Empty;
            ZoningDistrict2 = string.Empty;
            CommercialOverlay1 = string.Empty;
            CommercialOverlay2 = string.Empty;
            SpecialDistrict1 = string.Empty;
            SpecialDistrict2 = string.Empty;
            LandUse = string.Empty;
            OwnerName = string.Empty;
            BuildingClass = string.Empty;
            PolygonRings = new List<List<XYZ>>();
        }

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
            if (!string.IsNullOrWhiteSpace(CommercialOverlay1)) parts.Add(string.Format("Overlay: {0}", CommercialOverlay1));
            if (!string.IsNullOrWhiteSpace(SpecialDistrict1)) parts.Add(string.Format("Special: {0}", SpecialDistrict1));
            return parts.Count > 0 ? string.Join(" / ", parts.ToArray()) : "N/A";
        }
    }

    public class NycBlockContext
    {
        public string Borough { get; set; }
        public string BlockNumber { get; set; }
        public NycLotInfo SubjectLot { get; set; }
        public List<NycLotInfo> OtherLots { get; set; }
        public List<NycBuildingFootprint> Buildings { get; set; }

        public List<NycLotInfo> AdjacentLots
        {
            get { return OtherLots.Where(l => l.IsAdjacent).ToList(); }
        }

        public List<NycLotInfo> RemainingBlockLots
        {
            get { return OtherLots.Where(l => !l.IsAdjacent && l.Bbl != SubjectLot.Bbl).ToList(); }
        }

        public List<NycLotInfo> AllLots
        {
            get
            {
                var list = new List<NycLotInfo>();
                if (SubjectLot != null) list.Add(SubjectLot);
                if (OtherLots != null) list.AddRange(OtherLots.Where(l => SubjectLot == null || l.Bbl != SubjectLot.Bbl));
                return list;
            }
        }

        public double MinX
        {
            get { var lots = AllLots; return lots.Count > 0 ? lots.Min(l => l.MinX) : 0.0; }
        }
        public double MaxX
        {
            get { var lots = AllLots; return lots.Count > 0 ? lots.Max(l => l.MaxX) : 0.0; }
        }
        public double MinY
        {
            get { var lots = AllLots; return lots.Count > 0 ? lots.Min(l => l.MinY) : 0.0; }
        }
        public double MaxY
        {
            get { var lots = AllLots; return lots.Count > 0 ? lots.Max(l => l.MaxY) : 0.0; }
        }

        public double WidthFt { get { return Math.Max(0, MaxX - MinX); } }
        public double DepthFt { get { return Math.Max(0, MaxY - MinY); } }

        public NycBlockContext()
        {
            Borough = string.Empty;
            BlockNumber = string.Empty;
            SubjectLot = new NycLotInfo();
            OtherLots = new List<NycLotInfo>();
            Buildings = new List<NycBuildingFootprint>();
        }

        public void CalculateAdjacency(double toleranceFt)
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

                bool isAdj = false;
                foreach (var ring in other.PolygonRings)
                {
                    foreach (var pt in ring)
                    {
                        foreach (var subPt in subjectPoints)
                        {
                            double dist = Math.Sqrt(Math.Pow(pt.X - subPt.X, 2) + Math.Pow(pt.Y - subPt.Y, 2));
                            if (dist <= toleranceFt)
                            {
                                isAdj = true;
                                break;
                            }
                        }
                        if (isAdj) break;
                    }
                    if (isAdj) break;
                }
                other.IsAdjacent = isAdj;
            }
        }

        public void CalculateAdjacency()
        {
            CalculateAdjacency(1.0);
        }

        public Dictionary<string, string> GetSurroundingStreetNames()
        {
            var dict = new Dictionary<string, string>();
            var addresses = AllLots.Select(l => l.Address).Where(a => !string.IsNullOrWhiteSpace(a)).Distinct().ToList();
            if (addresses.Count > 0)
            {
                dict["Streets"] = string.Join(", ", addresses.Take(4).ToArray());
            }
            return dict;
        }
    }
}