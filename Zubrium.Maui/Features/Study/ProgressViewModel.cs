using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Zubrium.Content.Repository;
using Zubrium.Domain;
using Zubrium.Maui.ViewModels;
using Zubrium.Maui.Features.Generals;
using Zubrium.Persistence.Mappers;

namespace Zubrium.Maui.Features.Study
{
    public partial class ProgressViewModel : BaseViewModel
    {
        [ObservableProperty]
        public partial string SelectedCategoryName { get; set; } = "Выберите категорию";

        private string? _selectedCategoryId;

        [ObservableProperty] public partial int TotalCards { get; set; }
        [ObservableProperty] public partial int KnownCards { get; set; }
        [ObservableProperty] public partial int LearningCards { get; set; }
        [ObservableProperty] public partial int MasteredCards { get; set; }
        [ObservableProperty] public partial bool HasCards { get; set; }

        [ObservableProperty]
        public partial ObservableCollection<CardProgressModel> Cards { get; set; } = new();

        public ProgressViewModel(IContentRepository repository) : base(repository) { }

        public override async void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query.TryGetValue("SelectedCategory", out var catObj) && catObj is CategoryDraft category)
            {
                SelectedCategoryName = category.Name;
                _selectedCategoryId = category.Id;
                await LoadProgressAsync();
            }
        }

        [RelayCommand]
        public async Task SelectCategory()
        {
            var navParams = new Dictionary<string, object>
            {
                { "IsMultiSelect", false },
                { "CurrentCategoryName", SelectedCategoryName == "Выберите категорию" ? "" : SelectedCategoryName }
            };
            await Shell.Current.GoToAsync(nameof(CategorySelectionPage), navParams);
        }

        private async Task LoadProgressAsync()
        {
            if (string.IsNullOrEmpty(_selectedCategoryId))
            {
                TotalCards = 0; LearningCards = 0; MasteredCards = 0; KnownCards = 0;
                Cards.Clear(); HasCards = false;
                return;
            }

            var cardEntities = await _repository.GetCardsByCategoryAsync(_selectedCategoryId);
            var domainCards = cardEntities.Select(c => c.ToDomain()).ToList();

            TotalCards = domainCards.Count;
            // Изучено (свайп "Знаю")
            KnownCards = domainCards.Count(c => c.IsKnown);
            // Выучено алгоритмом (Достигнут таргет TargetMasteryDays)
            MasteredCards = domainCards.Count(c => c.IsMastered && !c.IsKnown);
            // Учится (Reps > 0 или LastReview есть)
            LearningCards = domainCards.Count(c => !c.IsMastered && !c.IsKnown && (c.Reps > 0 || c.LastReview != null));

            Cards.Clear();
            foreach (var card in domainCards) Cards.Add(new CardProgressModel(card));
            HasCards = Cards.Count > 0;
        }
    }

    public class CardProgressModel
    {
        public string Title { get; }
        public string NextReview { get; }
        public int Reps { get; }

        public CardProgressModel(Card card)
        {
            Title = string.IsNullOrWhiteSpace(card.Title) ? "Без названия" : card.Title;
            NextReview = card.NextReviewText;
            Reps = card.Reps;
        }
    }
}
