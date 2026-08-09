using Microsoft.Maui.Storage;
using Zubrium.Domain;

namespace Zubrium.Maui.Services.Settings
{
    public class StudySettingsService : IStudySettings
    {
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
    }
}
