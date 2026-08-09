using System;
using Zubrium.Domain;

namespace Zubrium.SpacedRepetition
{
    public class SimpleRepetitionService : ISpacedRepetitionService
    {
        // Твои жесткие интервалы в днях
        private readonly int[] _intervals = { 1, 2, 4, 9, 25 };

        public void ApplySuccess(Card card, DateTime now)
        {
            if (now.Kind != DateTimeKind.Utc) now = now.ToUniversalTime();

            // Читаем текущий шаг из словаря алгоритма (по умолчанию 0)
            int currentStep = 0;
            if (card.AlgorithmData.TryGetValue("Step", out var stepStr) && int.TryParse(stepStr, out int parsedStep))
            {
                currentStep = parsedStep;
            }

            card.LastReview = now;
            card.Reps++;

            // Если карточка уже прошла все интервалы (мы успешно вспомнили её после 25 дней)
            if (currentStep >= _intervals.Length)
            {
                card.IsMastered = true;
            }
            else
            {
                // Применяем интервал и сдвигаем дату
                int daysToAdd = _intervals[currentStep];
                card.Due = now.AddDays(daysToAdd);

                // Увеличиваем шаг и сохраняем обратно в словарь
                currentStep++;
                card.AlgorithmData["Step"] = currentStep.ToString();
            }
        }

        public void ResetProgress(Card card)
        {
            // Полная очистка состояния
            card.AlgorithmData.Clear();
            card.Due = DateTime.UtcNow;
            card.LastReview = null;
            card.Reps = 0;
            card.IsMastered = false;
        }
    }
}
