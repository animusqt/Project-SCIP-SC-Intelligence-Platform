using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using ScipDashboard.WPF.Services;

namespace ScipDashboard.WPF.ViewModels
{
    public sealed class StarQuesterViewModel : INotifyPropertyChanged
    {
        private const string StoreFile = "quests.json";
        private string _captureText = string.Empty;
        private string _questTitle = string.Empty;
        private string _faction = string.Empty;
        private string _description = string.Empty;
        private string _reward = string.Empty;
        private int _reputationPoints;
        private string _statusMessage = "Quest journal saved locally on this PC.";

        public StarQuesterViewModel()
        {
            Quests = LocalFeatureStore.Read<ObservableCollection<QuestJournalEntry>>(StoreFile);
            FactionProgress = new ObservableCollection<FactionQuestProgress>();
            RefreshFactionProgress();
            AddQuestCommand = new RelayCommand(AddQuest);
            ParseCaptureCommand = new RelayCommand(ParseCapture);
            CompleteQuestCommand = new RelayCommand<QuestJournalEntry?>(CompleteQuest);
            FailQuestCommand = new RelayCommand<QuestJournalEntry?>(FailQuest);
            RemoveQuestCommand = new RelayCommand<QuestJournalEntry?>(RemoveQuest);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        public ObservableCollection<QuestJournalEntry> Quests { get; }
        public ObservableCollection<FactionQuestProgress> FactionProgress { get; }
        public RelayCommand AddQuestCommand { get; }
        public RelayCommand ParseCaptureCommand { get; }
        public RelayCommand<QuestJournalEntry?> CompleteQuestCommand { get; }
        public RelayCommand<QuestJournalEntry?> FailQuestCommand { get; }
        public RelayCommand<QuestJournalEntry?> RemoveQuestCommand { get; }
        public int ActiveQuestCount => Quests.Count(quest => !quest.IsCompleted && !quest.IsFailed);
        public int CompletedQuestCount => Quests.Count(quest => quest.IsCompleted);
        public int FailedQuestCount => Quests.Count(quest => quest.IsFailed);
        public int TotalReputationPoints => Quests.Where(quest => quest.IsCompleted).Sum(quest => quest.ReputationPoints);
        public decimal TotalRewards => Quests.Where(quest => quest.IsCompleted).Sum(quest => ParseReward(quest.Reward));

        public string CaptureText { get => _captureText; set { _captureText = value; OnPropertyChanged(); } }
        public string QuestTitle { get => _questTitle; set { _questTitle = value; OnPropertyChanged(); } }
        public string Faction { get => _faction; set { _faction = value; OnPropertyChanged(); } }
        public string Description { get => _description; set { _description = value; OnPropertyChanged(); } }
        public string Reward { get => _reward; set { _reward = value; OnPropertyChanged(); } }
        public int ReputationPoints { get => _reputationPoints; set { _reputationPoints = Math.Max(0, value); OnPropertyChanged(); } }
        public string StatusMessage { get => _statusMessage; private set { _statusMessage = value; OnPropertyChanged(); } }

        private void ParseCapture()
        {
            var lines = CaptureText.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length == 0)
            {
                StatusMessage = "Paste a contract title or description to extract quest fields.";
                return;
            }

            var text = string.Join(Environment.NewLine, lines);
            var rewardMatch = Regex.Match(text, @"(?<reward>\d[\d,]*(?:\.\d+)?)\s*(?:aUEC|UEC)", RegexOptions.IgnoreCase);
            var factionMatch = Regex.Match(text, @"(?:faction|contractor)\s*[:\-]\s*(?<faction>[^\r\n]+)", RegexOptions.IgnoreCase);
            var title = lines.FirstOrDefault(line =>
                !Regex.IsMatch(line, @"^\s*(?:faction|contractor|reward|payout)\s*[:\-]", RegexOptions.IgnoreCase))?.Trim();

            QuestTitle = title ?? string.Empty;
            Faction = factionMatch.Success ? factionMatch.Groups["faction"].Value.Trim() : string.Empty;
            Reward = rewardMatch.Success ? rewardMatch.Groups["reward"].Value.Replace(",", string.Empty) : string.Empty;
            Description = string.Join(Environment.NewLine, lines.Skip(1)).Trim();
            StatusMessage = "Extracted fields are editable. Review them, then add the quest.";
        }

        private void AddQuest()
        {
            if (string.IsNullOrWhiteSpace(QuestTitle))
            {
                StatusMessage = "Enter a quest title before adding it.";
                return;
            }

            var quest = new QuestJournalEntry
            {
                Title = QuestTitle.Trim(),
                Faction = string.IsNullOrWhiteSpace(Faction) ? "Unassigned" : Faction.Trim(),
                Description = Description.Trim(),
                Reward = Reward.Trim(),
                ReputationPoints = ReputationPoints,
                AddedAt = DateTime.UtcNow
            };
            Quests.Insert(0, quest);
            QuestTitle = string.Empty;
            Faction = string.Empty;
            Description = string.Empty;
            Reward = string.Empty;
            ReputationPoints = 0;
            CaptureText = string.Empty;
            if (SaveQuests())
                StatusMessage = $"{quest.Title} added to the journal.";
        }

        private void CompleteQuest(QuestJournalEntry? quest)
        {
            if (quest == null || quest.IsCompleted || quest.IsFailed) return;
            quest.IsCompleted = true;
            quest.IsFailed = false;
            quest.CompletedAt = DateTime.UtcNow;
            if (SaveQuests())
                StatusMessage = $"{quest.Title} marked complete.";
        }

        private void FailQuest(QuestJournalEntry? quest)
        {
            if (quest == null || quest.IsCompleted || quest.IsFailed) return;
            quest.IsFailed = true;
            quest.CompletedAt = DateTime.UtcNow;
            if (SaveQuests())
                StatusMessage = $"{quest.Title} marked failed.";
        }

        private void RemoveQuest(QuestJournalEntry? quest)
        {
            if (quest == null || !Quests.Remove(quest)) return;
            if (SaveQuests())
                StatusMessage = $"{quest.Title} removed.";
        }

        private bool SaveQuests()
        {
            RefreshFactionProgress();
            OnPropertyChanged(nameof(ActiveQuestCount));
            OnPropertyChanged(nameof(CompletedQuestCount));
            OnPropertyChanged(nameof(FailedQuestCount));
            OnPropertyChanged(nameof(TotalReputationPoints));
            OnPropertyChanged(nameof(TotalRewards));
            try
            {
                LocalFeatureStore.Write(StoreFile, Quests);
                return true;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                StatusMessage = $"Quest journal changed for this session but could not be saved locally: {ex.Message}";
                return false;
            }
        }

        private void RefreshFactionProgress()
        {
            var groups = Quests
                .GroupBy(quest => string.IsNullOrWhiteSpace(quest.Faction) ? "Unassigned" : quest.Faction)
                .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
                .Select(group => new FactionQuestProgress
                {
                    Faction = group.Key,
                    ActiveCount = group.Count(quest => !quest.IsCompleted && !quest.IsFailed),
                    CompletedCount = group.Count(quest => quest.IsCompleted),
                    FailedCount = group.Count(quest => quest.IsFailed),
                    CompletedRewards = group.Where(quest => quest.IsCompleted).Sum(quest => ParseReward(quest.Reward)),
                    CompletedReputationPoints = group.Where(quest => quest.IsCompleted).Sum(quest => quest.ReputationPoints)
                })
                .ToList();

            FactionProgress.Clear();
            foreach (var group in groups)
                FactionProgress.Add(group);
        }

        private static decimal ParseReward(string reward) =>
            decimal.TryParse(reward.Replace(",", string.Empty), System.Globalization.NumberStyles.Number,
                System.Globalization.CultureInfo.InvariantCulture, out var amount) ? amount : 0m;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public sealed class QuestJournalEntry : INotifyPropertyChanged
    {
        private bool _isCompleted;
        private bool _isFailed;
        private DateTime? _completedAt;

        public string Title { get; set; } = string.Empty;
        public string Faction { get; set; } = "Unassigned";
        public string Description { get; set; } = string.Empty;
        public string Reward { get; set; } = string.Empty;
        public int ReputationPoints { get; set; }
        public string Status => IsCompleted ? "Complete" : IsFailed ? "Failed" : "Active";
        public bool IsCompleted
        {
            get => _isCompleted;
            set
            {
                if (_isCompleted == value) return;
                _isCompleted = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsCompleted)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Status)));
            }
        }
        public bool IsFailed
        {
            get => _isFailed;
            set
            {
                if (_isFailed == value) return;
                _isFailed = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsFailed)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Status)));
            }
        }
        public DateTime AddedAt { get; set; }
        public DateTime? CompletedAt
        {
            get => _completedAt;
            set
            {
                if (_completedAt == value) return;
                _completedAt = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CompletedAt)));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }

    public sealed class FactionQuestProgress
    {
        public string Faction { get; set; } = string.Empty;
        public int ActiveCount { get; set; }
        public int CompletedCount { get; set; }
        public int FailedCount { get; set; }
        public decimal CompletedRewards { get; set; }
        public int CompletedReputationPoints { get; set; }
    }
}
