using Microsoft.Maui.Storage;
using Zubrium.Domain;

namespace Zubrium.Maui.Services.Settings
{
    public class StudySettingsService : IStudySettings
    {
        public int TargetMasteryDays
        {
            get => Preferences.Default.Get(nameof(TargetMasteryDays), 30);
            set => Preferences.Default.Set(nameof(TargetMasteryDays), value);
        }

        public int MinRepetitions
        {
            get => Preferences.Default.Get(nameof(MinRepetitions), 4);
            set => Preferences.Default.Set(nameof(MinRepetitions), value);
        }

        public int DailyNewCardsTarget
        {
            get => Preferences.Default.Get(nameof(DailyNewCardsTarget), 5);
            set => Preferences.Default.Set(nameof(DailyNewCardsTarget), value);
        }

        public int DailyReviewCardsTarget
        {
            get => Preferences.Default.Get(nameof(DailyReviewCardsTarget), 15);
            set => Preferences.Default.Set(nameof(DailyReviewCardsTarget), value);
        }

        public double DesiredRetention
        {
            get => Preferences.Default.Get(nameof(DesiredRetention), 0.90);
            set => Preferences.Default.Set(nameof(DesiredRetention), value);
        }
    }
}
