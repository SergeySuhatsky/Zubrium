using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Plugin.Maui.SwipeCardView.Core;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Zubrium.Content.Repository;
using Zubrium.Domain;
using Zubrium.Maui.ViewModels;
using Zubrium.Persistence.Mappers;
using Zubrium.SpacedRepetition;

namespace Zubrium.Maui.Features.Study
{
    public partial class StudySessionViewModel : BaseViewModel
    {
        private readonly ISpacedRepetitionService _spacedRepetitionService;
        private readonly IStudySettings _settings;

        // Плагин SwipeCardView отлично работает с ObservableCollection
        public ObservableCollection<StudyCardItem> Queue { get; } = new();

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasCards))]
        [NotifyPropertyChangedFor(nameof(IsSessionFinished))]
        private StudyCardItem? currentCard; // Сюда плагин автоматически кладет верхнюю карточку[cite: 2]

        [ObservableProperty] private bool isHiddenMenuVisible;
        [ObservableProperty] private int cardsLeft;
        [ObservableProperty] private bool isBriefVisible;
        [ObservableProperty] private bool isDetailedVisible;
        [ObservableProperty] private bool isLoadingDetailed;

        public bool HasCards => CurrentCard != null;
        public bool IsSessionFinished => CurrentCard == null && Queue.Count == 0;

        public StudySessionViewModel(
            IContentRepository repository,
            ISpacedRepetitionService spacedRepetitionService,
            IStudySettings settings) : base(repository)
        {
            _spacedRepetitionService = spacedRepetitionService;
            _settings = settings;
        }

        public override async void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            List<string> categoryIds = new();
            StudyMode mode = StudyMode.Mixed;

            if (query.TryGetValue("CategoryIds", out var catsObj) && catsObj is List<string> cats)
                categoryIds = cats;

            if (query.TryGetValue("StudyMode", out var modeObj) && modeObj is StudyMode m)
                mode = m;

            await LoadCardsAsync(categoryIds, mode);
        }

        private async Task LoadCardsAsync(List<string> categoryIds, StudyMode mode)
        {
            var allCardsEntities = new List<Persistence.Entities.CardEntity>();

            if (categoryIds.Count == 0)
                allCardsEntities = await _repository.GetCardsAsync();
            else
                foreach (var catId in categoryIds)
                    allCardsEntities.AddRange(await _repository.GetCardsByCategoryAsync(catId));

            var now = DateTime.UtcNow;
            var domainCards = allCardsEntities.Select(c => c.ToDomain()).ToList();

            var newCards = new List<Card>();
            var reviewCards = new List<Card>();

            foreach (var card in domainCards)
            {
                if (card.IsKnown || card.IsMastered) continue;

                bool isNew = card.Reps == 0 && card.LastReview == null;
                bool isReview = (card.Reps > 0 || card.LastReview != null) && card.Due <= now;

                if (isNew) newCards.Add(card);
                if (isReview) reviewCards.Add(card);
            }

            var activities = await _repository.GetDailyActivitiesAsync();
            var todayActivity = activities.FirstOrDefault(a => a.Date == DateTime.UtcNow.Date);

            int studiedNewToday = todayActivity?.NewCardsStudied ?? 0;
            int studiedReviewToday = todayActivity?.ReviewCardsStudied ?? 0;

            int remainingNew = Math.Max(0, _settings.DailyNewCardsTarget - studiedNewToday);
            int remainingReview = Math.Max(0, _settings.DailyReviewCardsTarget - studiedReviewToday);

            var filteredCards = new List<Card>();

            if (mode == StudyMode.NewCards || mode == StudyMode.Mixed)
                filteredCards.AddRange(newCards.Take(remainingNew));
            if (mode == StudyMode.Review || mode == StudyMode.Mixed)
                filteredCards.AddRange(reviewCards.Take(remainingReview));

            var rnd = new Random();
            Queue.Clear();
            foreach (var card in filteredCards.OrderBy(x => rnd.Next()))
            {
                var phase = (card.Reps == 0 && card.LastReview == null) ? StudyCardPhase.Discovery : StudyCardPhase.Review;
                Queue.Add(new StudyCardItem(card, phase));
            }

            CardsLeft = Queue.Count;
        }

        // Этот метод вызывается ПЛАГИНОМ, когда карточка улетела за экран[cite: 2]
        [RelayCommand]
        public async Task CardSwiped(SwipedCardEventArgs e)
        {
            if (e.Item is not StudyCardItem swipedCard) return;

            // Удаляем карточку, чтобы двигаться дальше по очереди[cite: 2]
            Queue.Remove(swipedCard);

            if (e.Direction == SwipeCardDirection.Left)
            {
                if (swipedCard.Phase == StudyCardPhase.Discovery)
                {
                    // "Уже знаю"
                    swipedCard.DomainCard.IsKnown = true;
                    await _repository.SaveCardAsync(swipedCard.DomainCard.ToEntity());
                }
                else
                {
                    // "Отложить" (Сброс прогресса в сессии)
                    swipedCard.DomainCard.AlgorithmData["Step"] = "0";
                    swipedCard.DomainCard.Due = DateTime.UtcNow;
                    await _repository.SaveCardAsync(swipedCard.DomainCard.ToEntity());

                    swipedCard.IsFlipped = false;
                    ReinsertCard(swipedCard);
                }
            }
            else if (e.Direction == SwipeCardDirection.Right)
            {
                if (swipedCard.Phase == StudyCardPhase.Discovery)
                {
                    // "Начать учить"
                    swipedCard.Phase = StudyCardPhase.Review;
                    swipedCard.IsFlipped = false;
                    swipedCard.DomainCard.LastReview = DateTime.UtcNow;
                    await _repository.SaveCardAsync(swipedCard.DomainCard.ToEntity());
                    await _repository.LogDailyActivityAsync(DateTime.UtcNow.Date, 1, 0);

                    ReinsertCard(swipedCard);
                }
                else
                {
                    // "Вспомнил"
                    _spacedRepetitionService.ApplySuccess(swipedCard.DomainCard, DateTime.UtcNow);
                    await _repository.SaveCardAsync(swipedCard.DomainCard.ToEntity());
                    await _repository.LogDailyActivityAsync(DateTime.UtcNow.Date, 0, 1);
                }
            }

            // Сбрасываем UI для следующей карточки
            CardsLeft = Queue.Count;
            IsBriefVisible = false;
            IsDetailedVisible = false;
            IsLoadingDetailed = false;
            IsHiddenMenuVisible = false;
        }

        private void ReinsertCard(StudyCardItem card)
        {
            // Подмешиваем карточку на 1-4 позицию вперед
            int insertIndex = Random.Shared.Next(1, Math.Min(4, Queue.Count + 1));
            if (Queue.Count == 0) insertIndex = 0;
            Queue.Insert(insertIndex, card);
        }

        [RelayCommand] public void RevealBrief() => IsBriefVisible = true;

        [RelayCommand]
        public async Task RevealDetailed()
        {
            if (IsDetailedVisible) return;
            IsLoadingDetailed = true;
            await Task.Delay(600); // Имитация/Загрузка
            IsLoadingDetailed = false;
            IsBriefVisible = true;
            IsDetailedVisible = true;
        }

        [RelayCommand] public void ToggleHiddenMenu() => IsHiddenMenuVisible = !IsHiddenMenuVisible;
        [RelayCommand] public async Task FinishSession() => await GoBack();

        [RelayCommand]
        public async Task RollbackProgress()
        {
            if (CurrentCard == null) return;
            CurrentCard.DomainCard.Due = DateTime.UtcNow;
            await _repository.SaveCardAsync(CurrentCard.DomainCard.ToEntity());
            IsHiddenMenuVisible = false;
        }

        [RelayCommand]
        public async Task ResetProgress()
        {
            if (CurrentCard == null) return;
            _spacedRepetitionService.ResetProgress(CurrentCard.DomainCard);
            await _repository.SaveCardAsync(CurrentCard.DomainCard.ToEntity());
            IsHiddenMenuVisible = false;
        }

        [RelayCommand]
        public async Task DeleteCard()
        {
            if (CurrentCard == null) return;
            bool confirmed = await App.Current.MainPage.DisplayAlert("Удаление", "Удалить карточку навсегда?", "Удалить", "Отмена");
            if (confirmed)
            {
                await _repository.DeleteCardAsync(CurrentCard.DomainCard.Id);
                Queue.Remove(CurrentCard);
                CardsLeft = Queue.Count;
                IsHiddenMenuVisible = false;
            }
        }
    }
}