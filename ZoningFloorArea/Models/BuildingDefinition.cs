using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace ZoningFloorArea.Models
{
    public class BuildingDefinition : INotifyPropertyChanged
    {
        private string _id;
        private string _name;
        private string _scopeBoxName;
        private ObservableCollection<TypicalFloorGroup> _typicalGroups;

        public event PropertyChangedEventHandler PropertyChanged;

        public string ScopeBoxName
        {
            get { return _scopeBoxName; }
            set
            {
                if (_scopeBoxName != value)
                {
                    _scopeBoxName = value;
                    OnPropertyChanged("ScopeBoxName");
                }
            }
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

        public ObservableCollection<TypicalFloorGroup> TypicalGroups
        {
            get { return _typicalGroups; }
            set
            {
                if (_typicalGroups != value)
                {
                    _typicalGroups = value;
                    OnPropertyChanged("TypicalGroups");
                }
            }
        }

        public BuildingDefinition()
        {
            _id = Guid.NewGuid().ToString();
            _name = "Building 1";
            _typicalGroups = new ObservableCollection<TypicalFloorGroup>();
        }

        public BuildingDefinition(string name)
        {
            _id = Guid.NewGuid().ToString();
            _name = name;
            _typicalGroups = new ObservableCollection<TypicalFloorGroup>();
        }

        public TypicalFloorGroup GetGroupForLevel(string levelName)
        {
            if (string.IsNullOrEmpty(levelName) || _typicalGroups == null) return null;

            foreach (TypicalFloorGroup g in _typicalGroups)
            {
                if (g.IsSingleLevel)
                {
                    if (string.Equals(g.SourceLevelName, levelName, StringComparison.OrdinalIgnoreCase))
                    {
                        return g;
                    }
                }
                else
                {
                    // Check if levelName falls in range or matches source
                    if (string.Equals(g.SourceLevelName, levelName, StringComparison.OrdinalIgnoreCase))
                    {
                        return g;
                    }
                }
            }
            return null;
        }

        protected void OnPropertyChanged(string propName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propName));
            }
        }
    }
}
