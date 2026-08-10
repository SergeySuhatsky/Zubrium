using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using Microsoft.Maui.Storage;
using Zubrium.Content.Repository;
using Zubrium.Maui.ViewModels;
using Zubrium.Maui.Features.Study;
using Zubrium.Domain;
using Zubrium.Persistence.Entities;

namespace Zubrium.Maui.Features.Generals
{
    public partial class StudyViewModel : BaseViewModel
    {
        private readonly IStudySettings _settings;
        private List<DailyActivityEntity> _allActivities = new();

        [ObservableProperty] public partial List<string> SelectedCategoryIds { get; set; } = new();
        [ObservableProperty] public partial string SelectedCategoryName { get; set; } = "Все категории";

        // Прогресс по целям
        [ObservableProperty, NotifyPropertyChangedFor(nameof(NewCardsProgressText))] public partial int NewCardsTarget { get; set; }
        [ObservableProperty, NotifyPropertyChangedFor(nameof(NewCardsProgressText))] public partial int NewCardsStudiedToday { get; set; }
        public string NewCardsProgressText => $"Изучено сегодня: {NewCardsStudiedToday} из {NewCardsTarget}";

        [ObservableProperty, NotifyPropertyChangedFor(nameof(ReviewCardsProgressText))] public partial int ReviewCardsTarget { get; set; }
        [ObservableProperty, NotifyPropertyChangedFor(nameof(ReviewCardsProgressText))] public partial int ReviewCardsStudiedToday { get; set; }
        public string ReviewCardsProgressText => $"Повторено сегодня: {ReviewCardsStudiedToday} из {ReviewCardsTarget}";

        // График
        public List<string> ChartModes { get; } = new() { "1 день", "7 дней", "14 дней", "Месяц" };

        [ObservableProperty] 
        public partial string SelectedChartMode { get; set; } = "1 день";

        [ObservableProperty]
        public partial ObservableCollection<ChartBarModel> ChartBars { get; set; } = new();

        public StudyViewModel(IContentRepository repository, IStudySettings settings) : base(repository)
        {
            _settings = settings;

            var savedName = Preferences.Default.Get("Study_SelectedCategoryName", "Все категории");
            var savedIdsJson = Preferences.Default.Get("Study_SelectedCategoryIds", "[]");
            SelectedCategoryName = savedName;
            try { SelectedCategoryIds = JsonSerializer.Deserialize<List<string>>(savedIdsJson) ?? new List<string>(); }
            catch { SelectedCategoryIds = new List<string>(); }
        }

        partial void OnSelectedChartModeChanged(string value) => UpdateChart();

        [RelayCommand]
        public async Task RefreshData()
        {
            NewCardsTarget = _settings.DailyNewCardsTarget;
            ReviewCardsTarget = _settings.DailyReviewCardsTarget;

            _allActivities = await _repository.GetDailyActivitiesAsync();

            var today = DateTime.UtcNow.Date;
            var todayActivity = _allActivities.FirstOrDefault(a => a.Date == today);

            NewCardsStudiedToday = todayActivity?.NewCardsStudied ?? 0;
            ReviewCardsStudiedToday = todayActivity?.ReviewCardsStudied ?? 0;

            UpdateChart();
        }

        private void UpdateChart()
        {
            ChartBars.Clear();
            int stepDays = SelectedChartMode switch {
                "7 дней" => 7,
                "14 дней" => 14,
                "Месяц" => 30,
                _ => 1
            };

            var now = DateTime.UtcNow.Date;
            var bins = new int[7];
            var labels = new string[7];

            for (int i = 0; i < 7; i++)
            {
                int offsetEnd = (6 - i) * stepDays;
                var binEnd = now.AddDays(-offsetEnd);
                var binStart = binEnd.AddDays(-stepDays + 1);

                labels[i] = stepDays == 1 ? binStart.ToString("dd.MM") : $"{binStart:dd.MM}-{binEnd:dd.MM}";
                bins[i] = _allActivities.Where(a => a.Date >= binStart && a.Date <= binEnd).Sum(a => a.NewCardsStudied + a.ReviewCardsStudied);
            }

            int maxVal = bins.Max();
            if (maxVal == 0) maxVal = 1;

            for (int i = 0; i < 7; i++)
            {
                ChartBars.Add(new ChartBarModel 
                { 
                    Label = labels[i],
                    DisplayHeight = (bins[i] / (double)maxVal) * 120.0
                });
            }
        }

        public override void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query.TryGetValue("SelectedCategories", out var catsObj) && catsObj is List<CategoryDraft> categories)
            {
                if (categories.Count == 0)
                {
                    SelectedCategoryIds = new List<string>();
                    SelectedCategoryName = "Все категории";
                }
                else
                {
                    SelectedCategoryIds = categories.Select(c => c.Id!).ToList();
                    SelectedCategoryName = string.Join(", ", categories.Select(c => c.Name));
                }
                Preferences.Default.Set("Study_SelectedCategoryName", SelectedCategoryName);
                Preferences.Default.Set("Study_SelectedCategoryIds", JsonSerializer.Serialize(SelectedCategoryIds));
            }
        }

        [RelayCommand] public async Task SelectCategory() => await Shell.Current.GoToAsync(nameof(CategorySelectionPage), new Dictionary<string, object> { { "IsMultiSelect", true }, { "PreSelectedIds", SelectedCategoryIds } });
        [RelayCommand] public async Task TakeQuiz() => await Shell.Current.GoToAsync(nameof(Quizs.PickQuizPage));
        [RelayCommand] public async Task ReadArticle() => await Shell.Current.GoToAsync(nameof(Articles.PickArticlePage));
        [RelayCommand] public async Task LearnNewWords() => await StartSession(StudyMode.NewCards);
        [RelayCommand] public async Task RepeatWords() => await StartSession(StudyMode.Review);
        [RelayCommand] public async Task MixedMode() => await StartSession(StudyMode.Mixed);
        [RelayCommand] public async Task OpenProgress() => await Shell.Current.GoToAsync(nameof(Study.ProgressPage));

        private async Task StartSession(StudyMode mode)
        {
            await Shell.Current.GoToAsync(nameof(Study.StudySessionPage), new Dictionary<string, object>
            {
                { "CategoryIds", SelectedCategoryIds }, 
                { "StudyMode", mode }
            });
        }
    }

    public class ChartBarModel
    {
        public string Label { get; set; } = string.Empty;
        public double DisplayHeight { get; set; }
    }
}
