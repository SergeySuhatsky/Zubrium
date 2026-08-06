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
    }
}
