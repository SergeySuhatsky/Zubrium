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

        public ObservableCollection<StudyCardItem> Queue { get; } = new();

        private List<StudyCardItem> _deck = new();

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasCards))]
        [NotifyPropertyChangedFor(nameof(IsSessionFinished))]
        private StudyCardItem? currentCard;

        [ObservableProperty] private bool isHiddenMenuVisible;
        [ObservableProperty] private int cardsLeft;

        public bool HasCards => CurrentCard != null;
        public bool IsSessionFinished => CurrentCard == null && Queue.Count == 0 && _deck.Count == 0;

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
            _deck.Clear();
            Queue.Clear();

            foreach (var card in filteredCards.OrderBy(x => rnd.Next()))
            {
                var phase = (card.Reps == 0 && card.LastReview == null) ? StudyCardPhase.Discovery : StudyCardPhase.Review;
                _deck.Add(new StudyCardItem(card, phase));
            }

            if (_deck.Count > 0) { Queue.Add(_deck[0]); _deck.RemoveAt(0); }
            if (_deck.Count > 0) { Queue.Add(_deck[0]); _deck.RemoveAt(0); }

            if (Queue.Count > 0)
            {
                Queue[0].IsTopCard = true;
                CurrentCard = Queue[0];
            }

            CardsLeft = Queue.Count + _deck.Count;
        }

        [RelayCommand]
        public async Task CardSwiped(SwipedCardEventArgs e)
        {
            if (e.Item is not StudyCardItem swipedCard) return;

            bool needsRepeat = false;

            if (e.Direction == SwipeCardDirection.Left)
            {
                if (swipedCard.Phase == StudyCardPhase.Discovery)
                {
                    swipedCard.DomainCard.IsKnown = true;
                    await _repository.SaveCardAsync(swipedCard.DomainCard.ToEntity());
                }
                else
                {
                    swipedCard.DomainCard.AlgorithmData["Step"] = "0";
                    swipedCard.DomainCard.Due = DateTime.UtcNow;
                    await _repository.SaveCardAsync(swipedCard.DomainCard.ToEntity());
                    swipedCard.IsFlipped = false;
                    needsRepeat = true;
                }
            }
            else if (e.Direction == SwipeCardDirection.Right)
            {
                if (swipedCard.Phase == StudyCardPhase.Discovery)
                {
                    swipedCard.Phase = StudyCardPhase.Review;
                    swipedCard.IsFlipped = false;
                    swipedCard.DomainCard.LastReview = DateTime.UtcNow;
                    await _repository.SaveCardAsync(swipedCard.DomainCard.ToEntity());
                    await _repository.LogDailyActivityAsync(DateTime.UtcNow.Date, 1, 0);
                    needsRepeat = true;
                }
                else
                {
                    _spacedRepetitionService.ApplySuccess(swipedCard.DomainCard, DateTime.UtcNow);
                    await _repository.SaveCardAsync(swipedCard.DomainCard.ToEntity());
                    await _repository.LogDailyActivityAsync(DateTime.UtcNow.Date, 0, 1);
                }
            }

            if (needsRepeat)
            {
                swipedCard.IsBriefVisible = false;
                swipedCard.IsDetailedVisible = false;
                swipedCard.IsTopCard = false;

                int insertIndex = Random.Shared.Next(0, Math.Min(3, _deck.Count + 1));
                _deck.Insert(insertIndex, swipedCard);
            }

            Queue.Remove(swipedCard);

            if (_deck.Count > 0 && Queue.Count < 2)
            {
                Queue.Add(_deck[0]);
                _deck.RemoveAt(0);
            }

            if (Queue.Count > 0)
            {
                Queue[0].IsTopCard = true;
                CurrentCard = Queue[0];
            }
            else
            {
                CurrentCard = null;
            }

            CardsLeft = Queue.Count + _deck.Count;
            IsHiddenMenuVisible = false;
            OnPropertyChanged(nameof(IsSessionFinished));
        }

        [RelayCommand] 
        public void RevealBrief() 
        {
            if (CurrentCard != null)
            {
                CurrentCard.IsBriefVisible = true;
            }
        }

        [RelayCommand]
        public async Task RevealDetailed()
        {
            if (CurrentCard == null || CurrentCard.IsDetailedVisible) return;

            CurrentCard.IsLoadingDetailed = true;
            await Task.Delay(600);
            CurrentCard.IsLoadingDetailed = false;

            CurrentCard.IsBriefVisible = true;
            CurrentCard.IsDetailedVisible = true;
        }

        [RelayCommand]
        public void ToggleScroll(StudyCardItem card) => card.IsScrollEnabled = !card.IsScrollEnabled;

        [RelayCommand]
        public void UnlockScroll(StudyCardItem card) => card.IsScrollEnabled = true;

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
