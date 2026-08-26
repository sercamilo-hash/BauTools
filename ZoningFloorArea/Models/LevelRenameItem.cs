using System.ComponentModel;
using Autodesk.Revit.DB;

namespace ZoningFloorArea.Models
{
    public class LevelRenameItem : INotifyPropertyChanged
    {
        public Level LevelElement { get; private set; }
        public ElementId LevelId
        {
            get { return LevelElement.Id; }
        }

        public double RawElevation
        {
            get { return LevelElement.Elevation; }
        }
        public string ElevationDisplay { get; set; }

        public string CurrentName
        {
            get { return LevelElement.Name; }
        }

        private string _proposedName = string.Empty;
        public string ProposedName
        {
            get { return _proposedName; }
            set
            {
                if (_proposedName != value)
                {
                    _proposedName = value;
                    OnPropertyChanged("ProposedName");
                    OnPropertyChanged("IsChanged");
                }
            }
        }

        public bool IsChanged
        {
            get { return !string.Equals(CurrentName, ProposedName); }
        }

        private bool _isSelected = true;
        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged("IsSelected");
                }
            }
        }

        public LevelRenameItem(Level level, string elevationFormatted)
        {
            LevelElement = level;
            ElevationDisplay = elevationFormatted;
            _proposedName = level.Name;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
