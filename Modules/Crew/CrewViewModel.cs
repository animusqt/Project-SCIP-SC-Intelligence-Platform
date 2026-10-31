using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Linq;

namespace ScipDashboard.Modules.Crew
{
    public class CrewMember
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string[] Assignments { get; set; } = Array.Empty<string>();
    }

    public class CrewViewModel : INotifyPropertyChanged
    {
        private string _searchQuery = string.Empty;
        public string SearchQuery
        {
            get => _searchQuery;
            set
            {
                _searchQuery = value;
                OnPropertyChanged();
                UpdateFilteredCrew();
            }
        }

        private ObservableCollection<CrewMember> _allCrew = new ObservableCollection<CrewMember>();
        public ObservableCollection<CrewMember> AllCrew
        {
            get => _allCrew;
            set { _allCrew = value; OnPropertyChanged(); }
        }

        private ObservableCollection<CrewMember> _filteredCrew = new ObservableCollection<CrewMember>();
        public ObservableCollection<CrewMember> FilteredCrew
        {
            get => _filteredCrew;
            set { _filteredCrew = value; OnPropertyChanged(); }
        }

        private CrewMember? _selectedMember;
        public CrewMember? SelectedMember
        {
            get => _selectedMember;
            set
            {
                _selectedMember = value;
                OnPropertyChanged();
                UpdateDetailView();
            }
        }

        public CrewViewModel()
        {
            // Mock data
            AllCrew = new ObservableCollection<CrewMember>
            {
                new CrewMember { Id = "C1", Name = "Captain Vance", Role = "Command", Status = "Active", Assignments = new[] { "Ship_01", "Quest_04" } },
                new CrewMember { Id = "C2", Name = "Sgt. Miller", Role = "Security", Status = "Active", Assignments = new[] { "Ship_01" } },
                new CrewMember { Id = "C3", Name = "Dr. Aris", Role = "Medic", Status = "Off-Duty", Assignments = new string[] { } },
                new CrewMember { Id = "C4", Name = "Jax", Role = "Engineer", Status = "Active", Assignments = new[] { "Ship_02" } }
            };

            FilteredCrew = new ObservableCollection<CrewMember>(AllCrew);
        }

        private void UpdateFilteredCrew()
        {
            FilteredCrew.Clear();
            var query = SearchQuery?.ToLower() ?? "";
            var filtered = AllCrew.Where(c => c.Name.ToLower().Contains(query) || 
                                               c.Role.ToLower().Contains(query)).ToList();
            
            foreach (var item in filtered)
            {
                FilteredCrew.Add(item);
            }
        }

        private void UpdateDetailView()
        {
            // Logic to update UI elements via DataBinding or Manual Refresh
            OnPropertyChanged(nameof(SelectedMember));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
