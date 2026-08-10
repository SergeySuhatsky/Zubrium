using CommunityToolkit.Mvvm.ComponentModel;
using Zubrium.Domain;

namespace Zubrium.Maui.Features.Study
{
    public partial class StudyCardItem : ObservableObject
    {
        public Card DomainCard { get; }

        [ObservableProperty] private StudyCardPhase phase;
        [ObservableProperty] private bool isFlipped;

        [ObservableProperty] private bool isTopCard;
        [ObservableProperty] private bool isBriefVisible;
        [ObservableProperty] private bool isDetailedVisible;
        [ObservableProperty] private bool isLoadingDetailed;
        [ObservableProperty] private bool isScrollEnabled;

        public StudyCardItem(Card card, StudyCardPhase phase)
        {
            DomainCard = card;
            Phase = phase;
            IsFlipped = false;
        }
    }
}
