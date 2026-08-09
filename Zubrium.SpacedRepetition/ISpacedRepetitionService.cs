using System;
using Zubrium.Domain;

namespace Zubrium.SpacedRepetition
{
    public interface ISpacedRepetitionService
    {
        /// <summary>
        /// Применяет успешное повторение к карточке, вычисляя следующую дату (Due).
        /// </summary>
        void ApplySuccess(Card card, DateTime now);

        /// <summary>
        /// Сбрасывает прогресс карточки к исходному состоянию.
        /// </summary>
        void ResetProgress(Card card);
    }
}
