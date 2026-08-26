using System;
using System.ComponentModel;

namespace ZoningFloorArea.Models
{
    public class TypicalFloorGroup : INotifyPropertyChanged
    {
        private string _id;
        private string _name;
        private string _colorHex;
        private string _sourceLevelName;
        private string _sourceLevelNameLower;
        private string _sourceLevelNameUpper;
        private string _fromLevelName;
        private string _toLevelName;
        private bool _isSingleFloorOnly;
        private bool _isDuplexModule;
        private int _order;

        public event PropertyChangedEventHandler PropertyChanged;

        public TypicalFloorGroup()
        {
            _id = Guid.NewGuid().ToString();
            _name = "Typical Floor";
            _colorHex = "#3B82F6";
            _sourceLevelName = string.Empty;
            _sourceLevelNameLower = string.Empty;
            _sourceLevelNameUpper = string.Empty;
            _fromLevelName = string.Empty;
            _toLevelName = string.Empty;
            _isSingleFloorOnly = false;
            _isDuplexModule = false;
            _order = 1;
        }

        public string Id
        {
            get { return _id; }
            set
            {
                if (_id != value)
                {
                    _id = value;
                    OnPropertyChanged("Id");
                }
            }
        }

        public string Name
        {
            get { return _name; }
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged("Name");
                }
            }
        }

        public string ColorHex
        {
            get { return _colorHex; }
            set
            {
                if (_colorHex != value)
                {
                    _colorHex = value;
                    OnPropertyChanged("ColorHex");
                }
            }
        }

        public string SourceLevelName
        {
            get { return _sourceLevelName; }
            set
            {
                if (_sourceLevelName != value)
                {
                    _sourceLevelName = value;
                    OnPropertyChanged("SourceLevelName");
                    if (_isSingleFloorOnly)
                    {
                        FromLevelName = value;
                        ToLevelName = value;
                    }
                }
            }
        }

        public string SourceLevelNameLower
        {
            get { return _sourceLevelNameLower; }
            set
            {
                if (_sourceLevelNameLower != value)
                {
                    _sourceLevelNameLower = value;
                    OnPropertyChanged("SourceLevelNameLower");
                }
            }
        }

        public string SourceLevelNameUpper
        {
            get { return _sourceLevelNameUpper; }
            set
            {
                if (_sourceLevelNameUpper != value)
                {
                    _sourceLevelNameUpper = value;
                    OnPropertyChanged("SourceLevelNameUpper");
                }
            }
        }

        public bool IsDuplexModule
        {
            get { return _isDuplexModule; }
            set
            {
                if (_isDuplexModule != value)
                {
                    _isDuplexModule = value;
                    OnPropertyChanged("IsDuplexModule");
                }
            }
        }

        public string FromLevelName
        {
            get { return _fromLevelName; }
            set
            {
                if (_fromLevelName != value)
                {
                    _fromLevelName = value;
                    OnPropertyChanged("FromLevelName");
                    OnPropertyChanged("IsSingleLevel");
                }
            }
        }

        public string ToLevelName
        {
            get { return _toLevelName; }
            set
            {
                if (_toLevelName != value)
                {
                    _toLevelName = value;
                    OnPropertyChanged("ToLevelName");
                    OnPropertyChanged("IsSingleLevel");
                }
            }
        }

        public bool IsSingleFloorOnly
        {
            get { return _isSingleFloorOnly; }
            set
            {
                if (_isSingleFloorOnly != value)
                {
                    _isSingleFloorOnly = value;
                    if (value && !string.IsNullOrEmpty(_sourceLevelName))
                    {
                        _fromLevelName = _sourceLevelName;
                        _toLevelName = _sourceLevelName;
                        OnPropertyChanged("FromLevelName");
                        OnPropertyChanged("ToLevelName");
                    }
                    OnPropertyChanged("IsSingleFloorOnly");
                    OnPropertyChanged("IsSingleLevel");
                }
            }
        }

        public int Order
        {
            get { return _order; }
            set
            {
                if (_order != value)
                {
                    _order = value;
                    OnPropertyChanged("Order");
                }
            }
        }

        public bool IsSingleLevel
        {
            get
            {
                if (_isSingleFloorOnly) return true;
                return !string.IsNullOrEmpty(_fromLevelName) && 
                       string.Equals(_fromLevelName, _toLevelName, StringComparison.OrdinalIgnoreCase);
            }
        }

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
