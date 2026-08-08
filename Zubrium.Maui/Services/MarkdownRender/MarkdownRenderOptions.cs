using System;
using System.Collections.Generic;
using System.Text;

namespace Zubrium.Maui.Services.MarkdownRender
{
    public class MarkdownRenderOptions
    {
        public Color TextColor { get; set; } = Colors.Black;
        public Color LatexTextColor { get; set; } = Colors.Black;
        public double BaseFontSize { get; set; } = 14.0;
        public double LatexFontSize { get; set; } = 14.0;
        public Color CodeBackgroundColor { get; set; } = Color.FromArgb("#F6F8FA");
        public Color QuoteBarColor { get; set; } = Color.FromArgb("#D0D7DE");
        public string FontFamily { get; set; } = "Segoe UI"; // Подставьте логику выбора платформы
        public float LatexScaleFactor { get; set; } = 2.0f;
    }
}
