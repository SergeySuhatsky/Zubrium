namespace Zubrium.Maui.Features.Generals
{
    public class CategoryDraft
    {
        public string? Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsNew => string.IsNullOrEmpty(Id);
    }
}
