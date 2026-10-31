using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Linq;

namespace ScipDashboard.Modules.Scrap
{
    public class ScrapItem
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public string Condition { get; set; } = string.Empty;
    }

    public class ScrapViewModel : INotifyPropertyChanged
    {
        private ScrapItem? _selectedItem;
        public ScrapItem? SelectedItem
        {
            get => _selectedItem;
            set
            {
                _selectedItem = value;
                OnPropertyChanged();
                UpdateDetailFields();
            }
        }

        public ObservableCollection<ScrapItem> ScrapItems { get; set; } = new ObservableCollection<ScrapItem>();

        public ScrapViewModel()
        {
            // Mock data
            ScrapItems.Add(new ScrapItem { Id = "S1", Name = "Titanium Scrap", Type = "Metal", Quantity = 50, Condition = "Good" });
            ScrapItems.Add(new ScrapItem { Id = "S2", Name = "Circuit Boards", Type = "Electronics", Quantity = 20, Condition = "Damaged" });
            ScrapItems.Add(new ScrapItem { Id = "S3", Name = "Fuel Cells", Type = "Energy", Quantity = 10, Condition = "Used" });
        }

        public void UpdateDetails(ScrapItem item)
        {
            SelectedItem = item;
        }

        private void UpdateDetailFields()
        {
            // Logic to update the UI elements in the View
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
