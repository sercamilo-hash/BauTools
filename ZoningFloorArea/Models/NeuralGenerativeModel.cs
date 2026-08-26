using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace ZoningFloorArea.Models
{
    public enum MassFloorUsage
    {
        CommercialPodium,
        DormerSetbackTransition,
        TypicalResidential,
        InclusionaryHousing,
        PenthouseLuxury,
        RoofTerrace
    }

    public enum BuildingTypology
    {
        PodiumCentralTower,
        SteppedWeddingCake,
        SlenderPencilTower,
        LShapedCourtyard,
        TwinTowers
    }

    public class MassingFloorBlock
    {
        public int LevelIndex { get; set; }
        public string LevelName { get; set; }
        public double ElevationFt { get; set; }
        public double HeightFt { get; set; }
        public double WidthFt { get; set; }
        public double DepthFt { get; set; }
        public double OffsetXFt { get; set; }
        public double OffsetYFt { get; set; }
        public double AreaSqFt { get { return WidthFt * DepthFt; } }
        public MassFloorUsage UsageType { get; set; }
        public string ColorHex { get; set; }

        public MassingFloorBlock()
        {
            ColorHex = "#3B82F6";
        }
    }

    public class GenerativeScenario : INotifyPropertyChanged
    {
        private bool _isSelectedForBake;

        public string Id { get; set; }
        public string Title { get; set; }
        public string Subtitle { get; set; }
        public string Icon { get; set; }
        public string ColorHex { get; set; }
        
        public bool IsSelectedForBake
        {
            get { return _isSelectedForBake; }
            set { _isSelectedForBake = value; OnPropertyChanged("IsSelectedForBake"); }
        }

        public double TotalZfa { get; set; }
        public double FarUtilizationPercent { get; set; }
        public double HighFloorPercentage { get; set; }
        public int MihUnitsEstimate { get; set; }
        public double EstimatedFacadeArea { get; set; }
        public double EstimatedRevenueMillions { get; set; }
        public int TotalFloors { get; set; }
        public int PodiumFloors { get; set; }
        public int DormerFloors { get; set; }
        public int TowerFloors { get; set; }
        public double TotalHeightFt { get; set; }
        public bool IsHeightExceeded { get; set; }

        public List<MassingFloorBlock> Floors { get; set; }

        public GenerativeScenario()
        {
            Floors = new List<MassingFloorBlock>();
            ColorHex = "#3B82F6";
            _isSelectedForBake = true;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(name));
        }
    }

    public class GenerativeInputParameters : INotifyPropertyChanged
    {
        private double _lotAreaSqFt;
        private double _lotWidthFt;
        private double _lotDepthFt;
        private double _baseFar;
        private double _maxHeightFt;
        private double _setbackFrontFt;
        private double _setbackRearFt;
        private double _setbackSidesFt;
        private double _mihPercent;
        private double _floorHeightPodium;
        private double _floorHeightTower;

        // Dynamic 3D Morphing Sliders
        private BuildingTypology _selectedTypology;
        private int _podiumFloors;
        private double _podiumCoveragePercent;
        private double _towerCoveragePercent;
        private int _dormerFloors;
        private double _dormerSetbackDepthFt;
        private int _penthouseFloors;

        public double LotAreaSqFt
        {
            get { return _lotAreaSqFt; }
            set { _lotAreaSqFt = value; OnPropertyChanged("LotAreaSqFt"); }
        }

        public double LotWidthFt
        {
            get { return _lotWidthFt; }
            set { _lotWidthFt = value; OnPropertyChanged("LotWidthFt"); }
        }

        public double LotDepthFt
        {
            get { return _lotDepthFt; }
            set { _lotDepthFt = value; OnPropertyChanged("LotDepthFt"); }
        }

        public double BaseFar
        {
            get { return _baseFar; }
            set { _baseFar = value; OnPropertyChanged("BaseFar"); }
        }

        public double MaxHeightFt
        {
            get { return _maxHeightFt; }
            set { _maxHeightFt = value; OnPropertyChanged("MaxHeightFt"); }
        }

        public double SetbackFrontFt
        {
            get { return _setbackFrontFt; }
            set { _setbackFrontFt = value; OnPropertyChanged("SetbackFrontFt"); }
        }

        public double SetbackRearFt
        {
            get { return _setbackRearFt; }
            set { _setbackRearFt = value; OnPropertyChanged("SetbackRearFt"); }
        }

        public double SetbackSidesFt
        {
            get { return _setbackSidesFt; }
            set { _setbackSidesFt = value; OnPropertyChanged("SetbackSidesFt"); }
        }

        public double MihPercent
        {
            get { return _mihPercent; }
            set { _mihPercent = value; OnPropertyChanged("MihPercent"); }
        }

        public double FloorHeightPodium
        {
            get { return _floorHeightPodium; }
            set { _floorHeightPodium = value; OnPropertyChanged("FloorHeightPodium"); }
        }

        public double FloorHeightTower
        {
            get { return _floorHeightTower; }
            set { _floorHeightTower = value; OnPropertyChanged("FloorHeightTower"); }
        }

        public BuildingTypology SelectedTypology
        {
            get { return _selectedTypology; }
            set { _selectedTypology = value; OnPropertyChanged("SelectedTypology"); }
        }

        public int PodiumFloors
        {
            get { return _podiumFloors; }
            set { _podiumFloors = value; OnPropertyChanged("PodiumFloors"); }
        }

        public double PodiumCoveragePercent
        {
            get { return _podiumCoveragePercent; }
            set { _podiumCoveragePercent = value; OnPropertyChanged("PodiumCoveragePercent"); }
        }

        public double TowerCoveragePercent
        {
            get { return _towerCoveragePercent; }
            set { _towerCoveragePercent = value; OnPropertyChanged("TowerCoveragePercent"); }
        }

        public int DormerFloors
        {
            get { return _dormerFloors; }
            set { _dormerFloors = value; OnPropertyChanged("DormerFloors"); }
        }

        public double DormerSetbackDepthFt
        {
            get { return _dormerSetbackDepthFt; }
            set { _dormerSetbackDepthFt = value; OnPropertyChanged("DormerSetbackDepthFt"); }
        }

        public int PenthouseFloors
        {
            get { return _penthouseFloors; }
            set { _penthouseFloors = value; OnPropertyChanged("PenthouseFloors"); }
        }

        public GenerativeInputParameters()
        {
            _lotAreaSqFt = 15000.0;
            _lotWidthFt = 150.0;
            _lotDepthFt = 100.0;
            _baseFar = 10.0;
            _maxHeightFt = 250.0;
            _setbackFrontFt = 15.0;
            _setbackRearFt = 20.0;
            _setbackSidesFt = 10.0;
            _mihPercent = 25.0;
            _floorHeightPodium = 15.0;
            _floorHeightTower = 11.0;

            _selectedTypology = BuildingTypology.PodiumCentralTower;
            _podiumFloors = 3;
            _podiumCoveragePercent = 80.0;
            _towerCoveragePercent = 45.0;
            _dormerFloors = 2;
            _dormerSetbackDepthFt = 12.0;
            _penthouseFloors = 2;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(name));
        }
    }
}