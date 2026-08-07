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
using FSRS.Core.Enums;

namespace Zubrium.Maui.Features.Study
{
    public partial class StudySessionViewModel : BaseViewModel
    {
        private readonly StudyRulesInterceptor _interceptor;
        private readonly ISpacedRepetitionService _fsrsService;

        private Queue<StudyCardItem> _mainQueue = new();
        private Queue<StudyCardItem> _sessionQueue = new(); // Для свайпа вправо (Показать еще)

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
            StudyRulesInterceptor interceptor,
            ISpacedRepetitionService fsrsService) : base(repository)
        {
            _interceptor = interceptor;
            _fsrsService = fsrsService;
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
            {
                allCardsEntities = await _repository.GetCardsAsync();
            }
            else
            {
                foreach (var catId in categoryIds)
                {
                    allCardsEntities.AddRange(await _repository.GetCardsByCategoryAsync(catId));
                }
            }

            var now = DateTime.UtcNow;
            var domainCards = allCardsEntities.Select(c => c.ToDomain()).ToList();
            var filteredCards = new List<Card>();

            // Фильтрация в зависимости от режима
            foreach (var card in domainCards)
            {
                if (card.IsKnown || card.IsMastered) continue;

                bool isNew = card.Reps == 0;
                bool isReview = card.Reps > 0 && card.Due <= now;

                if (mode == StudyMode.NewCards && isNew) filteredCards.Add(card);
                else if (mode == StudyMode.Review && isReview) filteredCards.Add(card);
                else if (mode == StudyMode.Mixed && (isNew || isReview)) filteredCards.Add(card);
            }

            // Перемешиваем и собираем очередь
            var rnd = new Random();
            foreach (var card in filteredCards.OrderBy(x => rnd.Next()))
            {
                // Если новая карточка - Этап 0 (Знакомство). Иначе - Этап 1 (Повторение)
                var phase = card.Reps == 0 ? StudyCardPhase.Discovery : StudyCardPhase.Review;
                _mainQueue.Enqueue(new StudyCardItem(card, phase));
            }

            NextCard();
        }

        private void NextCard()
        {
            if (_mainQueue.Count > 0)
            {
                CurrentCard = _mainQueue.Dequeue();
            }
            else if (_sessionQueue.Count > 0)
            {
                CurrentCard = _sessionQueue.Dequeue();
            }
            else
            {
                CurrentCard = null;
            }

            CardsLeft = _mainQueue.Count + _sessionQueue.Count + (CurrentCard != null ? 1 : 0);
            IsHiddenMenuVisible = false;
        }

        [RelayCommand]
        public void FlipCard()
        {
            if (CurrentCard != null)
                CurrentCard.IsFlipped = !CurrentCard.IsFlipped;
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
            // Никакого FSRS, просто переводим карточку в статус изучения и кидаем в сессионную очередь
            CurrentCard.Phase = StudyCardPhase.Review;
            CurrentCard.IsFlipped = false;
            CurrentCard.DomainCard.State = 1; // State.Learning
            await _repository.SaveCardAsync(CurrentCard.DomainCard.ToEntity());

            _sessionQueue.Enqueue(CurrentCard);
            NextCard();
        }

        // =====================================
        // ЭТАП 1: REVIEW (Изучение)
        // =====================================

        [RelayCommand]
        public async Task RateGood() // Свайп ВЛЕВО
        {
            await ApplyRatingAndProceed(Rating.Good);
        }

        [RelayCommand]
        public void ShowAgainInSession() // Свайп ВПРАВО
        {
            if (CurrentCard == null) return;
            // Не трогаем алгоритм! Просто кидаем в конец очереди текущей сессии
            CurrentCard.IsFlipped = false;
            _sessionQueue.Enqueue(CurrentCard);
            NextCard();
        }

        [RelayCommand]
        public async Task RateHard() // Свайп ВВЕРХ
        {
            await ApplyRatingAndProceed(Rating.Hard);
        }

        [RelayCommand]
        public async Task RateEasy() // Свайп ВНИЗ
        {
            await ApplyRatingAndProceed(Rating.Easy);
        }

        private async Task ApplyRatingAndProceed(Rating rating)
        {
            if (CurrentCard == null) return;
            _interceptor.ApplyRatingAndRules(CurrentCard.DomainCard, rating, DateTime.UtcNow);
            await _repository.SaveCardAsync(CurrentCard.DomainCard.ToEntity());
            NextCard();
        }

        // =====================================
        // СКРЫТОЕ МЕНЮ (Троеточие)
        // =====================================

        [RelayCommand]
        public void ToggleHiddenMenu()
        {
            IsHiddenMenuVisible = !IsHiddenMenuVisible;
        }

        [RelayCommand]
        public async Task RateAgain() // Показывать чаще (Again в FSRS)
        {
            await ApplyRatingAndProceed(Rating.Again);
        }

        [RelayCommand]
        public async Task ResetProgress() // Обнулить прогресс совсем
        {
            if (CurrentCard == null) return;
            _fsrsService.ResetProgress(CurrentCard.DomainCard);
            await _repository.SaveCardAsync(CurrentCard.DomainCard.ToEntity());

            // Возвращаем на этап знакомства
            CurrentCard.Phase = StudyCardPhase.Discovery;
            CurrentCard.IsFlipped = false;
            _sessionQueue.Enqueue(CurrentCard);
            NextCard();
        }

        [RelayCommand]
        public async Task FinishSession()
        {
            await GoBack();
        }
    }
}
