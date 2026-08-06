using System;
using Zubrium.Domain;
using FSRS.Core.Models;

namespace Zubrium.SpacedRepetition
{
    public interface ISpacedRepetitionService
    {
        void ApplyRating(Card domainCard, Rating rating, DateTime now);
        void ResetProgress(Card domainCard);
    }
}
