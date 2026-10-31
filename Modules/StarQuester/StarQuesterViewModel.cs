using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Linq;
using ScipDashboard.Core.Models;
using ScipDashboard.Core.Common;

namespace ScipDashboard.Modules.StarQuester
{
    public class Quest
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> Objectives { get; set; } = new List<string>();
        public string FactionId { get; set; } = string.Empty;
        public DateTime CapturedDate { get; set; }
        public bool IsCompleted { get; set; }
    }

    public class StarQuesterViewModel : INotifyPropertyChanged
    {
        private Quest? _selectedQuest;
        public Quest? SelectedQuest
        {
            get => _selectedQuest;
            set
            {
                _selectedQuest = value;
                OnPropertyChanged();
                UpdateQuestDetails();
            }
        }

        public ObservableCollection<Quest> Quests { get; set; } = new ObservableCollection<Quest>();

        public double FactionProgress => _selectedQuest?.IsCompleted == true ? 100 : 0;
        public string FactionRewards => _selectedQuest?.IsCompleted == true ? "Faction Rep +500" : "Pending";

        public System.Windows.Input.ICommand AddQuestCommand { get; }

        public StarQuesterViewModel()
        {
            // Mock data
            Quests.Add(new Quest 
            { 
                Id = "Q1", 
                Title = "The Great Haul", 
                Description = "Deliver 500 units of Titanium to Port Aurora.", 
                Objectives = new List<string> { "Secure Cargo", "Navigate through Nebula", "Deliver to Port Aurora" },
                FactionId = "TradeGuild",
                CapturedDate = DateTime.Now,
                IsCompleted = false 
            });

            AddQuestCommand = new RelayCommand(AddQuest);
        }

        private void UpdateQuestDetails()
        {
            // Logic to refresh the UI elements in the detail pane
            OnPropertyChanged(nameof(FactionProgress));
            OnPropertyChanged(nameof(FactionRewards));
        }

        private void AddQuest()
        {
            var newQuest = new Quest 
            { 
                Id = Guid.NewGuid().ToString(), 
                Title = "New Quest", 
                Description = "Description here", 
                Objectives = new List<string> { "Objective 1" },
                FactionId = "Neutral",
                CapturedDate = DateTime.Now,
                IsCompleted = false 
            };
            Quests.Add(newQuest);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
