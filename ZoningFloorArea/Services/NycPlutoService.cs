using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using ZoningFloorArea.Models;

namespace ZoningFloorArea.Services
{
    public class NycPlutoService
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        static NycPlutoService()
        {
            _httpClient.Timeout = TimeSpan.FromSeconds(25);
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "BauTools-Revit-Addin/1.0");
        }

        public async Task<List<NycSearchResult>> SearchAddressAsync(string query)
        {
            List<NycSearchResult> results = new List<NycSearchResult>();
            if (string.IsNullOrWhiteSpace(query))
                return results;

            try
            {
                string url = string.Format("https://geosearch.planninglabs.nyc/v1/autocomplete?text={0}", Uri.EscapeDataString(query));
                using (HttpResponseMessage response = await _httpClient.GetAsync(url))
                {
                    if (!response.IsSuccessStatusCode)
                        return results;

                    string json = await response.Content.ReadAsStringAsync();
                    using (JsonDocument doc = JsonDocument.Parse(json))
                    {
                        JsonElement features;
                        if (doc.RootElement.TryGetProperty("features", out features) && features.ValueKind == JsonValueKind.Array)
                        {
                            foreach (JsonElement feature in features.EnumerateArray())
                            {
                                JsonElement props;
                                if (!feature.TryGetProperty("properties", out props))
                                    continue;

                                string label = GetString(props, "label");
                                string houseNumber = GetString(props, "housenumber");
                                string street = GetString(props, "street");
                                string borough = GetString(props, "borough");
                                string postalCode = GetString(props, "postalcode");

                                string bbl = GetString(props, "pad_bbl");
                                if (string.IsNullOrEmpty(bbl))
                                    bbl = GetString(props, "bbl");

                                JsonElement addendum;
                                if (string.IsNullOrEmpty(bbl) && props.TryGetProperty("addendum", out addendum))
                                {
                                    JsonElement pad;
                                    if (addendum.TryGetProperty("pad", out pad))
                                    {
                                        bbl = GetString(pad, "bbl");
                                    }
                                }

                                NycSearchResult item = new NycSearchResult();
                                item.Label = label;
                                item.Address = string.Format("{0} {1}", houseNumber, street).Trim();
                                item.HouseNumber = houseNumber;
                                item.Street = street;
                                item.Borough = borough;
                                item.PostalCode = postalCode;
                                item.Bbl = bbl;
                                results.Add(item);
                            }
                        }
                    }
                }
            }
            catch
            {
            }

            return results;
        }

        public async Task<NycLotInfo> GetLotByBblAsync(string bbl)
        {
            string cleanBbl = NormalizeBbl(bbl);
            if (string.IsNullOrEmpty(cleanBbl))
                return null;

            try
            {
                string queryUrl = string.Format("https://services5.arcgis.com/GfwWNkhOj9bNBqoJ/arcgis/rest/services/MAPPLUTO/FeatureServer/0/query?where=BBL%3D{0}&outFields=*&outSR=2263&f=geojson", cleanBbl);

                using (HttpResponseMessage response = await _httpClient.GetAsync(queryUrl))
                {
                    if (!response.IsSuccessStatusCode)
                        return null;

                    string json = await response.Content.ReadAsStringAsync();
                    using (JsonDocument doc = JsonDocument.Parse(json))
                    {
                        JsonElement features;
                        if (!doc.RootElement.TryGetProperty("features", out features) || features.ValueKind != JsonValueKind.Array)
                            return null;

                        JsonElement.ArrayEnumerator featureEnumerator = features.EnumerateArray();
                        if (!featureEnumerator.MoveNext())
                            return null;

                        return ParseFeature(featureEnumerator.Current);
                    }
                }
            }
            catch
            {
                return null;
            }
        }

        public async Task<NycBlockContext> GetBlockContextAsync(NycLotInfo subjectLot)
        {
            NycBlockContext context = new NycBlockContext();
            context.Borough = subjectLot.Borough;
            context.BlockNumber = subjectLot.Block;
            context.SubjectLot = subjectLot;

            if (string.IsNullOrWhiteSpace(subjectLot.Block))
                return context;

            try
            {
                string boroCode = GetBoroughCode(subjectLot.Borough);
                string queryUrl = string.Format("https://services5.arcgis.com/GfwWNkhOj9bNBqoJ/arcgis/rest/services/MAPPLUTO/FeatureServer/0/query?where=Block%3D{0}+AND+Borough%3D%27{1}%27&outFields=*&outSR=2263&f=geojson", subjectLot.Block, boroCode);

                using (HttpResponseMessage response = await _httpClient.GetAsync(queryUrl))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        string json = await response.Content.ReadAsStringAsync();
                        using (JsonDocument doc = JsonDocument.Parse(json))
                        {
                            JsonElement features;
                            if (doc.RootElement.TryGetProperty("features", out features) && features.ValueKind == JsonValueKind.Array)
                            {
                                foreach (JsonElement feature in features.EnumerateArray())
                                {
                                    NycLotInfo lot = ParseFeature(feature);
                                    if (lot != null && lot.Bbl != subjectLot.Bbl)
                                    {
                                        context.OtherLots.Add(lot);
                                    }
                                }
                            }
                        }
                    }
                }

                context.CalculateAdjacency();
            }
            catch
            {
            }

            return context;
        }

        private static NycLotInfo ParseFeature(JsonElement feature)
        {
            JsonElement props;
            if (!feature.TryGetProperty("properties", out props))
                return null;

            string bbl = GetString(props, "BBL");
            NycLotInfo lotInfo = new NycLotInfo();
            lotInfo.Bbl = bbl;
            lotInfo.Address = GetString(props, "Address");
            lotInfo.Borough = GetBoroughName(GetString(props, "Borough"));
            lotInfo.Block = GetString(props, "Block");
            lotInfo.Lot = GetString(props, "Lot");
            lotInfo.ZipCode = GetString(props, "ZipCode");
            lotInfo.ZoningDistrict1 = GetString(props, "ZoneDist1");
            lotInfo.ZoningDistrict2 = GetString(props, "ZoneDist2");
            lotInfo.CommercialOverlay1 = GetString(props, "Overlay1");
            lotInfo.CommercialOverlay2 = GetString(props, "Overlay2");
            lotInfo.SpecialDistrict1 = GetString(props, "SPDist1");
            lotInfo.SpecialDistrict2 = GetString(props, "SPDist2");
            lotInfo.ResidFar = GetDouble(props, "ResidFAR");
            lotInfo.CommFar = GetDouble(props, "CommFAR");
            lotInfo.FacilFar = GetDouble(props, "FacilFAR");
            lotInfo.BuiltFar = GetDouble(props, "BuiltFAR");
            lotInfo.LotAreaSqFt = GetDouble(props, "LotArea");
            lotInfo.BldgAreaSqFt = GetDouble(props, "BldgArea");
            lotInfo.LotFrontageFt = GetDouble(props, "LotFront");
            lotInfo.LotDepthFt = GetDouble(props, "LotDepth");
            lotInfo.NumFloors = (int)GetDouble(props, "NumFloors");
            lotInfo.YearBuilt = (int)GetDouble(props, "YearBuilt");
            lotInfo.LandUse = GetString(props, "LandUse");
            lotInfo.OwnerName = GetString(props, "OwnerName");
            lotInfo.BuildingClass = GetString(props, "BldgClass");

            JsonElement geom;
            if (feature.TryGetProperty("geometry", out geom))
            {
                string geomType = GetString(geom, "type");
                JsonElement coords;
                if (geom.TryGetProperty("coordinates", out coords) && coords.ValueKind == JsonValueKind.Array)
                {
                    if (string.Equals(geomType, "Polygon", StringComparison.OrdinalIgnoreCase))
                    {
                        foreach (JsonElement ring in coords.EnumerateArray())
                        {
                            List<XYZ> ringPoints = ParseRing(ring);
                            if (ringPoints.Count >= 3)
                            {
                                lotInfo.PolygonRings.Add(ringPoints);
                            }
                        }
                    }
                    else if (string.Equals(geomType, "MultiPolygon", StringComparison.OrdinalIgnoreCase))
                    {
                        foreach (JsonElement poly in coords.EnumerateArray())
                        {
                            foreach (JsonElement ring in poly.EnumerateArray())
                            {
                                List<XYZ> ringPoints = ParseRing(ring);
                                if (ringPoints.Count >= 3)
                                {
                                    lotInfo.PolygonRings.Add(ringPoints);
                                }
                            }
                        }
                    }
                }
            }

            return lotInfo;
        }

        private static List<XYZ> ParseRing(JsonElement ring)
        {
            List<XYZ> points = new List<XYZ>();
            foreach (JsonElement pt in ring.EnumerateArray())
            {
                if (pt.ValueKind == JsonValueKind.Array)
                {
                    JsonElement.ArrayEnumerator ptEnum = pt.EnumerateArray();
                    if (ptEnum.MoveNext())
                    {
                        double x = ptEnum.Current.GetDouble();
                        if (ptEnum.MoveNext())
                        {
                            double y = ptEnum.Current.GetDouble();
                            points.Add(new XYZ(x, y, 0));
                        }
                    }
                }
            }
            return points;
        }

        private static string NormalizeBbl(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
            string digits = string.Empty;
            foreach (char c in raw)
            {
                if (char.IsDigit(c)) digits += c;
            }
            return digits.Length == 10 ? digits : string.Empty;
        }

        private static string GetBoroughName(string codeOrAbbr)
        {
            if (string.IsNullOrWhiteSpace(codeOrAbbr)) return "Unknown";
            string upper = codeOrAbbr.Trim().ToUpperInvariant();
            if (upper == "1" || upper == "MN" || upper == "MANHATTAN") return "Manhattan";
            if (upper == "2" || upper == "BX" || upper == "BRONX") return "Bronx";
            if (upper == "3" || upper == "BK" || upper == "BROOKLYN") return "Brooklyn";
            if (upper == "4" || upper == "QN" || upper == "QUEENS") return "Queens";
            if (upper == "5" || upper == "SI" || upper == "STATEN ISLAND") return "Staten Island";
            return codeOrAbbr;
        }

        public static string GetBoroughCode(string nameOrAbbr)
        {
            if (string.IsNullOrWhiteSpace(nameOrAbbr)) return "MN";
            string upper = nameOrAbbr.Trim().ToUpperInvariant();
            if (upper.Contains("MANHATTAN") || upper == "1" || upper == "MN") return "MN";
            if (upper.Contains("BRONX") || upper == "2" || upper == "BX") return "BX";
            if (upper.Contains("BROOKLYN") || upper == "3" || upper == "BK") return "BK";
            if (upper.Contains("QUEENS") || upper == "4" || upper == "QN") return "QN";
            if (upper.Contains("STATEN") || upper == "5" || upper == "SI") return "SI";
            return "MN";
        }

        private static string GetString(JsonElement elem, string propName)
        {
            JsonElement val;
            if (elem.TryGetProperty(propName, out val))
            {
                if (val.ValueKind == JsonValueKind.String)
                {
                    string s = val.GetString();
                    return s != null ? s.Trim() : string.Empty;
                }
                if (val.ValueKind == JsonValueKind.Number)
                    return val.ToString();
            }
            return string.Empty;
        }

        private static double GetDouble(JsonElement elem, string propName)
        {
            JsonElement val;
            if (elem.TryGetProperty(propName, out val))
            {
                double d;
                if (val.ValueKind == JsonValueKind.Number && val.TryGetDouble(out d))
                    return d;
                double parsed;
                if (val.ValueKind == JsonValueKind.String && double.TryParse(val.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out parsed))
                    return parsed;
            }
            return 0.0;
        }
    }
}
