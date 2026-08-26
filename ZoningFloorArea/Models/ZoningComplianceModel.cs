using System;
using System.ComponentModel;

namespace ZoningFloorArea.Models
{
    public class ZoningLotData : INotifyPropertyChanged
    {
        private string _projectName;
        private string _address;
        private string _blockLot;
        private string _zoningDistrict;
        private string _lotType;
        private double _lotAreaSqFt;
        private double _lotWidthFt;
        private double _lotDepthFt;
        private double _baseResidentialFar;
        private double _baseCommercialFar;
        private double _baseCommunityFacilityFar;
        private double _inclusionaryBonusFar;
        private double _otherBonusFar;
        private double _maxBuildingHeightFt;

        public string ProjectName
        {
            get { return _projectName ?? "My Building Project"; }
            set { _projectName = value; OnPropertyChanged("ProjectName"); }
        }

        public string Address
        {
            get { return _address ?? ""; }
            set { _address = value; OnPropertyChanged("Address"); }
        }

        public string BlockLot
        {
            get { return _blockLot ?? ""; }
            set { _blockLot = value; OnPropertyChanged("BlockLot"); }
        }

        public string ZoningDistrict
        {
            get { return _zoningDistrict ?? "R10"; }
            set { _zoningDistrict = value; OnPropertyChanged("ZoningDistrict"); }
        }

        public string LotType
        {
            get { return _lotType ?? "Corner Lot"; }
            set { _lotType = value; OnPropertyChanged("LotType"); }
        }

        public double LotAreaSqFt
        {
            get { return _lotAreaSqFt; }
            set
            {
                _lotAreaSqFt = value;
                OnPropertyChanged("LotAreaSqFt");
                OnPropertyChanged("TotalAllowableFar");
                OnPropertyChanged("TotalAllowableZfa");
            }
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

        public double BaseResidentialFar
        {
            get { return _baseResidentialFar; }
            set
            {
                _baseResidentialFar = value;
                OnPropertyChanged("BaseResidentialFar");
                OnPropertyChanged("TotalAllowableFar");
                OnPropertyChanged("TotalAllowableZfa");
            }
        }

        public double BaseCommercialFar
        {
            get { return _baseCommercialFar; }
            set
            {
                _baseCommercialFar = value;
                OnPropertyChanged("BaseCommercialFar");
                OnPropertyChanged("TotalAllowableFar");
                OnPropertyChanged("TotalAllowableZfa");
            }
        }

        public double BaseCommunityFacilityFar
        {
            get { return _baseCommunityFacilityFar; }
            set
            {
                _baseCommunityFacilityFar = value;
                OnPropertyChanged("BaseCommunityFacilityFar");
                OnPropertyChanged("TotalAllowableFar");
                OnPropertyChanged("TotalAllowableZfa");
            }
        }

        public double InclusionaryBonusFar
        {
            get { return _inclusionaryBonusFar; }
            set
            {
                _inclusionaryBonusFar = value;
                OnPropertyChanged("InclusionaryBonusFar");
                OnPropertyChanged("TotalAllowableFar");
                OnPropertyChanged("TotalAllowableZfa");
            }
        }

        public double OtherBonusFar
        {
            get { return _otherBonusFar; }
            set
            {
                _otherBonusFar = value;
                OnPropertyChanged("OtherBonusFar");
                OnPropertyChanged("TotalAllowableFar");
                OnPropertyChanged("TotalAllowableZfa");
            }
        }

        public double MaxBuildingHeightFt
        {
            get { return _maxBuildingHeightFt; }
            set { _maxBuildingHeightFt = value; OnPropertyChanged("MaxBuildingHeightFt"); }
        }

        public double TotalAllowableFar
        {
            get
            {
                return BaseResidentialFar + BaseCommercialFar + InclusionaryBonusFar + OtherBonusFar;
            }
        }

        public double TotalAllowableZfa
        {
            get
            {
                return LotAreaSqFt * TotalAllowableFar;
            }
        }

        public ZoningLotData()
        {
            _projectName = "My Building Project";
            _zoningDistrict = "R10";
            _lotType = "Corner Lot";
            _lotAreaSqFt = 15000.0;
            _lotWidthFt = 150.0;
            _lotDepthFt = 100.0;
            _baseResidentialFar = 10.0;
            _baseCommercialFar = 0.0;
            _baseCommunityFacilityFar = 10.0;
            _inclusionaryBonusFar = 2.0;
            _otherBonusFar = 0.0;
            _maxBuildingHeightFt = 250.0;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class ZoningComplianceReport : INotifyPropertyChanged
    {
        private double _allowableZfa;
        private double _proposedZfa;
        private double _remainingZfa;
        private double _utilizationPercent;
        private bool _isOverbuilt;
        private string _statusSummary;
        private string _colorHex;

        public double AllowableZfa
        {
            get { return _allowableZfa; }
            set { _allowableZfa = value; OnPropertyChanged("AllowableZfa"); }
        }

        public double ProposedZfa
        {
            get { return _proposedZfa; }
            set { _proposedZfa = value; OnPropertyChanged("ProposedZfa"); }
        }

        public double RemainingZfa
        {
            get { return _remainingZfa; }
            set { _remainingZfa = value; OnPropertyChanged("RemainingZfa"); }
        }

        public double UtilizationPercent
        {
            get { return _utilizationPercent; }
            set { _utilizationPercent = value; OnPropertyChanged("UtilizationPercent"); }
        }

        public bool IsOverbuilt
        {
            get { return _isOverbuilt; }
            set { _isOverbuilt = value; OnPropertyChanged("IsOverbuilt"); }
        }

        public string StatusSummary
        {
            get { return _statusSummary ?? "Ready to Evaluate"; }
            set { _statusSummary = value; OnPropertyChanged("StatusSummary"); }
        }

        public string ColorHex
        {
            get { return _colorHex ?? "#10B981"; }
            set { _colorHex = value; OnPropertyChanged("ColorHex"); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}