using System;
using Xunit;
using Zubrium.Domain;
using Zubrium.SpacedRepetition;
using FSRS.Core.Enums;
using FSRS.Core.Configurations;
using FSRS.Core.Services;

namespace Zubrium.Tests.FSRS
{
    public class FSRSLapsesAndEdgeCasesTests
    {
        private Card CreateNewCard()
        {
            return new Card(
                id: Guid.NewGuid().ToString("N"),
                frontMarkdown: "Front",
                briefMarkdown: "Brief"
            );
        }

        // ==========================================
        // 4. Обработка забывания (Lapses)
        // ==========================================

        [Fact]
        public void TC_4_1_LapsePenalty_DropsStability_And_SetsStateToRelearning()
        {
            var fsrs = new FSRSService();
            var card = CreateNewCard();
            var now = DateTime.UtcNow;

            fsrs.ApplyRating(card, Rating.Good, now);
            now = now.AddDays(card.ScheduledDays);
            fsrs.ApplyRating(card, Rating.Good, now);
            now = now.AddDays(card.ScheduledDays);
            fsrs.ApplyRating(card, Rating.Good, now);

            var highStability = card.Stability.Value;
            Assert.True(highStability > 5.0, "Card should be highly stable before lapse");

            now = now.AddDays(card.ScheduledDays);
            fsrs.ApplyRating(card, Rating.Again, now);

            Assert.True(card.Stability.Value < highStability, "Stability must drop significantly after a lapse");
            Assert.Equal(3, card.State); // 3 = Relearning
            Assert.Equal(1, card.Lapses);
        }

        [Fact]
        public void TC_4_2_MinimumStability_AfterMultipleLapses_DoesNotDropBelowZero()
        {
            var fsrs = new FSRSService();
            var card = CreateNewCard();
            var now = DateTime.UtcNow;

            fsrs.ApplyRating(card, Rating.Good, now);

            for (int i = 0; i < 10; i++)
            {
                now = now.AddDays(1);
                fsrs.ApplyRating(card, Rating.Again, now);
            }

            Assert.True(card.Stability.HasValue);
            Assert.True(card.Stability.Value > 0.0, "Stability must never drop to or below 0.0");
        }

        //// ==========================================
        //// 5. Расчет интервалов и граничные условия (Edge Cases)
        //// ==========================================

        //[Fact]
        //public void TC_5_1_MaximumInterval_IsRespected()
        //{
        //    var options = new SchedulerOptions
        //    {
        //        MaximumInterval = 3650 // 10 лет
        //    };
        //    var factory = new SchedulerFactory(options);
        //    var scheduler = factory.CreateScheduler();

        //    // Использование global:: для решения проблемы CS0234
        //    var fsrsCard = new global::FSRS.Core.Models.Card(Guid.NewGuid());
        //    var now = DateTime.UtcNow;

        //    for (int i = 0; i < 15; i++)
        //    {
        //        var review = scheduler.ReviewCard(fsrsCard, Rating.Easy, now);
        //        // Использование .UpdatedCard вместо .Card для решения проблемы CS1061
        //        fsrsCard = review.UpdatedCard;
        //        now = now.AddDays((fsrsCard.Due - now).Days);
        //    }

        //    var finalInterval = (fsrsCard.Due.Date - DateTime.UtcNow.Date).Days;

        //    Assert.True(finalInterval <= 3650, $"Interval {finalInterval} exceeded maximum limit of 3650 days");
        //}

        //[Fact]
        //public void TC_5_3_InvalidWeightsArray_ThrowsException()
        //{
        //    Assert.ThrowsAny<Exception>(() =>
        //    {
        //        var options = new SchedulerOptions
        //        {
        //            // Использовано свойство Parameters, как определено в вашем классе
        //            Parameters = new double[] { 1.0, 2.0, 3.0 }
        //        };

        //        var factory = new SchedulerFactory(options);
        //        var scheduler = factory.CreateScheduler();
        //    });
        //}
    }
}
