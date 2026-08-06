using CommunityToolkit.Mvvm.ComponentModel;
using Zubrium.Content.Repository;
using Zubrium.Domain;
using Zubrium.Maui.ViewModels;

namespace Zubrium.Maui.Features.Generals
{
    public partial class SettingsMenuViewModel : BaseViewModel
    {
        private readonly IStudySettings _settings;

        [ObservableProperty]
        public partial int TargetMasteryDays { get; set; }

        [ObservableProperty]
        public partial int MinRepetitions { get; set; }

        public SettingsMenuViewModel(IContentRepository repository, IStudySettings settings) : base(repository)
        {
            _settings = settings;

            TargetMasteryDays = _settings.TargetMasteryDays;
            MinRepetitions = _settings.MinRepetitions;
        }

        partial void OnTargetMasteryDaysChanged(int value)
        {
            _settings.TargetMasteryDays = value;
        }

        partial void OnMinRepetitionsChanged(int value)
        {
            _settings.MinRepetitions = value;
        }

        public override void ApplyQueryAttributes(IDictionary<string, object> query)
        {
        }
    }
}
