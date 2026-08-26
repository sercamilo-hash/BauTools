using System.ComponentModel;

namespace ZoningFloorArea.Models
{
    public class LevelCreationItem : INotifyPropertyChanged
    {
        private int _index;
        public int Index
        {
            get { return _index; }
            set
            {
                if (_index != value)
                {
                    _index = value;
                    OnPropertyChanged("Index");
                }
            }
        }

        private string _levelName;
        public string LevelName
        {
            get { return _levelName; }
            set
            {
                if (_levelName != value)
                {
                    _levelName = value;
                    OnPropertyChanged("LevelName");
                }
            }
        }

        private double _elevationFeet;
        public double ElevationFeet
        {
            get { return _elevationFeet; }
            set
            {
                if (_elevationFeet != value)
                {
                    _elevationFeet = value;
                    OnPropertyChanged("ElevationFeet");
                }
            }
        }

        private string _elevationDisplay;
        public string ElevationDisplay
        {
            get { return _elevationDisplay; }
            set
            {
                if (_elevationDisplay != value)
                {
                    _elevationDisplay = value;
                    OnPropertyChanged("ElevationDisplay");
                }
            }
        }

        private string _levelType;
        public string LevelType
        {
            get { return _levelType; }
            set
            {
                if (_levelType != value)
                {
                    _levelType = value;
                    OnPropertyChanged("LevelType");
                }
            }
        }

        private bool _isIncluded;
        public bool IsIncluded
        {
            get { return _isIncluded; }
            set
            {
                if (_isIncluded != value)
                {
                    _isIncluded = value;
                    OnPropertyChanged("IsIncluded");
                }
            }
        }

        private bool _createFloorPlan;
        public bool CreateFloorPlan
        {
            get { return _createFloorPlan; }
            set
            {
                if (_createFloorPlan != value)
                {
                    _createFloorPlan = value;
                    OnPropertyChanged("CreateFloorPlan");
                }
            }
        }

        private bool _createCeilingPlan;
        public bool CreateCeilingPlan
        {
            get { return _createCeilingPlan; }
            set
            {
                if (_createCeilingPlan != value)
                {
                    _createCeilingPlan = value;
                    OnPropertyChanged("CreateCeilingPlan");
                }
            }
        }

        public LevelCreationItem()
        {
            _levelName = string.Empty;
            _elevationDisplay = string.Empty;
            _levelType = "Typical";
            _isIncluded = true;
            _createFloorPlan = true;
            _createCeilingPlan = false;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
