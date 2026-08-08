using CommunityToolkit.Mvvm.ComponentModel;
using Zubrium.Domain;

namespace Zubrium.Maui.Features.Study
{
    public partial class StudyCardItem : ObservableObject
    {
        public Card DomainCard { get; }
        public StudyCardPhase Phase { get; set; }

        [ObservableProperty]
        private bool _isFlipped;

        public StudyCardItem(Card card, StudyCardPhase phase)
        {
            DomainCard = card;
            Phase = phase;
            IsFlipped = false;
        }
    }
}
