using Microsoft.Maui.Graphics;

namespace Zubrium.Maui.ViewModels
{
    public class CategoryDisplayModel
    {
        public string CategoryId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int ItemCount { get; set; }
        public string ItemCountText { get; set; } = string.Empty;
        public Color IconColor { get; set; } = Colors.Gray;
    }
}
