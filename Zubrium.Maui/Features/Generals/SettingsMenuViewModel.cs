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
        public partial int DailyNewCardsTarget { get; set; }

        [ObservableProperty]
        public partial int DailyReviewCardsTarget { get; set; }

        public SettingsMenuViewModel(IContentRepository repository, IStudySettings settings) : base(repository)
        {
            _settings = settings;

            DailyNewCardsTarget = _settings.DailyNewCardsTarget;
            DailyReviewCardsTarget = _settings.DailyReviewCardsTarget;
        }

        partial void OnDailyNewCardsTargetChanged(int value) => _settings.DailyNewCardsTarget = value;
        partial void OnDailyReviewCardsTargetChanged(int value) => _settings.DailyReviewCardsTarget = value;

        public override void ApplyQueryAttributes(IDictionary<string, object> query)
        {
        }
    }
}
