using System;
using System.Collections.Generic;
using Zubrium.Domain;
using FSRS.Core.Enums;

namespace Zubrium.SpacedRepetition
{
    public interface ISpacedRepetitionService
    {
        /// <summary>
        /// Высчитывает прогноз интервалов (в днях) для каждой из 4 оценок.
        /// Возвращает словарь, где ключ — оценка, а значение — количество дней.
        /// </summary>
        Dictionary<Rating, int> GetReviewOptions(Card domainCard, DateTime now);

        /// <summary>
        /// Применяет выбранную оценку к карточке и пересчитывает её параметры.
        /// </summary>
        void ApplyRating(Card domainCard, Rating rating, DateTime now);

        /// <summary>
        /// Сбрасывает алгоритм карточки к исходному состоянию.
        /// </summary>
        void ResetProgress(Card domainCard);
    }
}
