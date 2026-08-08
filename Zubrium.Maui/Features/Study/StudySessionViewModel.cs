using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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

        // Единая очередь!
        private List<StudyCardItem> _queue = new();

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasCards))]
        [NotifyPropertyChangedFor(nameof(IsSessionFinished))]
        public partial StudyCardItem? CurrentCard { get; set; }

        [ObservableProperty]
        public partial bool IsHiddenMenuVisible { get; set; }

        [ObservableProperty]
        public partial int CardsLeft { get; set; }

        public bool HasCards => CurrentCard != null;
        public bool IsSessionFinished => CurrentCard == null;

        public StudySessionViewModel(
            IContentRepository repository,
            ISpacedRepetitionService spacedRepetitionService) : base(repository)
        {
            _spacedRepetitionService = spacedRepetitionService;
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
            var filteredCards = new List<Card>();

            foreach (var card in domainCards)
            {
                if (card.IsKnown || card.IsMastered) continue;

                // Этап знакомства, только если карточку вообще НИКОГДА не открывали.
                bool isNew = card.Reps == 0 && card.LastReview == null;
                // Иначе она уже на этапе изучения/повторения
                bool isReview = (card.Reps > 0 || card.LastReview != null) && card.Due <= now;

                if (mode == StudyMode.NewCards && isNew) filteredCards.Add(card);
                else if (mode == StudyMode.Review && isReview) filteredCards.Add(card);
                else if (mode == StudyMode.Mixed && (isNew || isReview)) filteredCards.Add(card);
            }

            // Перемешиваем ОДНУ общую очередь (и новые, и повторяемые будут вперемешку)
            var rnd = new Random();
            foreach (var card in filteredCards.OrderBy(x => rnd.Next()))
            {
                var phase = (card.Reps == 0 && card.LastReview == null) ? StudyCardPhase.Discovery : StudyCardPhase.Review;
                _queue.Add(new StudyCardItem(card, phase));
            }

            NextCard();
        }

        private void NextCard()
        {
            if (_queue.Count > 0)
            {
                CurrentCard = _queue[0];
                _queue.RemoveAt(0);
            }
            else
            {
                CurrentCard = null;
            }

            CardsLeft = _queue.Count + (CurrentCard != null ? 1 : 0);
            IsHiddenMenuVisible = false;
        }

        private void ReinsertCurrentCard()
        {
            if (CurrentCard == null) return;
            // Вставляем карточку на 2-4 позицию вперед, чтобы она появилась снова вперемешку с остальными
            int insertIndex = Random.Shared.Next(1, Math.Min(4, _queue.Count + 1));
            if (_queue.Count == 0) insertIndex = 0;
            _queue.Insert(insertIndex, CurrentCard);
        }

        [RelayCommand]
        public void FlipCard()
        {
            if (CurrentCard != null) CurrentCard.IsFlipped = !CurrentCard.IsFlipped;
        }

        // =====================================
        // ЭТАП 0: DISCOVERY (Знакомство)
        // =====================================

        [RelayCommand]
        public async Task MarkAsKnown() // Свайп ВЛЕВО
        {
            if (CurrentCard == null) return;
            CurrentCard.DomainCard.IsKnown = true;
            await _repository.SaveCardAsync(CurrentCard.DomainCard.ToEntity());
            NextCard();
        }

        [RelayCommand]
        public async Task StartLearning() // Свайп ВПРАВО
        {
            if (CurrentCard == null) return;

            CurrentCard.Phase = StudyCardPhase.Review;
            CurrentCard.IsFlipped = false;

            CurrentCard.DomainCard.LastReview = DateTime.UtcNow; // Фиксируем, что мы ее видели

            await _repository.SaveCardAsync(CurrentCard.DomainCard.ToEntity());
            await _repository.LogDailyActivityAsync(DateTime.UtcNow.Date, 1, 0); // +1 изученная

            ReinsertCurrentCard();
            NextCard();
        }

        // =====================================
        // ЭТАП 1: REVIEW (Изучение)
        // =====================================

        [RelayCommand]
        public void ShowAgainInSession() // Свайп ВПРАВО
        {
            if (CurrentCard == null) return;
            CurrentCard.IsFlipped = false;
            ReinsertCurrentCard();
            NextCard();
        }

        [RelayCommand]
        public async Task RateGood() // Свайп ВПРАВО (Вспомнил)
        {
            if (CurrentCard == null) return;

            // Вызываем наш новый простой алгоритм
            _spacedRepetitionService.ApplySuccess(CurrentCard.DomainCard, DateTime.UtcNow);

            // Сохраняем в БД
            await _repository.SaveCardAsync(CurrentCard.DomainCard.ToEntity());
            await _repository.LogDailyActivityAsync(DateTime.UtcNow.Date, 0, 1); // +1 повторенная

            NextCard();
        }

        // =====================================
        // СКРЫТОЕ МЕНЮ (Троеточие)
        // =====================================

        [RelayCommand]
        public void ToggleHiddenMenu() => IsHiddenMenuVisible = !IsHiddenMenuVisible;

        [RelayCommand]
        public async Task ResetProgress() // Обнулить прогресс
        {
            if (CurrentCard == null) return;
            _spacedRepetitionService.ResetProgress(CurrentCard.DomainCard);
            await _repository.SaveCardAsync(CurrentCard.DomainCard.ToEntity());

            CurrentCard.Phase = StudyCardPhase.Discovery;
            CurrentCard.IsFlipped = false;
            ReinsertCurrentCard();
            NextCard();
        }

        [RelayCommand]
        public async Task FinishSession() => await GoBack();
    }
}
