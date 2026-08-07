using CommunityToolkit.Mvvm.ComponentModel;

namespace Zubrium.Maui.Features.Generals
{
    public partial class CategoryDraft : ObservableObject
    {
        public string? Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsNew => string.IsNullOrEmpty(Id);

        [ObservableProperty]
        public partial bool IsSelected { get; set; }
    }
}
