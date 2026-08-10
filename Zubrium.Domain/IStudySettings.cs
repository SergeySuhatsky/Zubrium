namespace Zubrium.Domain
{
    public interface IStudySettings
    {
        int DailyNewCardsTarget { get; set; }
        int DailyReviewCardsTarget { get; set; }
    }
}
