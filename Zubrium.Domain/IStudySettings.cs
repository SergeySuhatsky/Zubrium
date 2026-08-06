namespace Zubrium.Domain
{
    public interface IStudySettings
    {
        int TargetMasteryDays { get; set; }
        int MinRepetitions { get; set; }
    }
}
