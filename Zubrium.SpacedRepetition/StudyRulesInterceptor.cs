using System;
using FSRS.Core.Enums;
using Zubrium.Domain;

namespace Zubrium.SpacedRepetition
{
    public class StudyRulesInterceptor
    {
        private readonly ISpacedRepetitionService _fsrsService;
        private readonly IStudySettings _settings;

        public StudyRulesInterceptor(ISpacedRepetitionService fsrsService, IStudySettings settings)
        {
            _fsrsService = fsrsService;
            _settings = settings;
        }

        /// <summary>
        /// Применяет оценку FSRS и накладывает поверх бизнес-правила (обрезка интервала и проверка на IsMastered).
        /// </summary>
        public void ApplyRatingAndRules(Card card, Rating rating, DateTime now)
        {
            // 1. Вызываем базовый математический расчет FSRS
            _fsrsService.ApplyRating(card, rating, now);

            // Получаем глобальные лимиты пользователя
            int targetDays = _settings.TargetMasteryDays;
            int minReps = _settings.MinRepetitions;

            // 2. ПРАВИЛО 1: Ограничение интервала (Interval Cap)
            // Если карточка повторена недостаточное количество раз, мы не даем ей улететь далеко
            int maxAllowedInterval = Math.Max(1, targetDays / 2);

            if (card.Reps < minReps && card.ScheduledDays > maxAllowedInterval)
            {
                card.ScheduledDays = maxAllowedInterval;
                card.Due = now.AddDays(maxAllowedInterval); 
            }

            // 3. ПРАВИЛО 2: Выучено навсегда (Mastery Condition)
            // Если стабильность превысила желаемую цель И пройдена база по количеству касаний
            if (card.Stability.HasValue && card.Stability.Value >= targetDays && card.Reps >= minReps)
            {
                card.IsMastered = true;
            }
        }
    }
}
