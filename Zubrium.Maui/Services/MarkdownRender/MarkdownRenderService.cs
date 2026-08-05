/*
 * This code was taken and modified from the following repository:
 * https://github.com/aqua0801/MauiMarkdownRendererDemo
 * 
 * The original work is licensed under the MIT License.
 */

using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using CSharpMath.SkiaSharp;
using Markdig;
using Markdig.Extensions.Mathematics;
using Markdig.Extensions.Tables;
using Markdig.Extensions.TaskLists;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Microsoft.Maui.Controls.Shapes;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using Typography.OpenFont;

namespace Zubrium.Maui.Services.MarkdownRender
{
    public class MarkdownRenderService : IMarkdownRenderService
    {
        // Тяжелые объекты: инициализируются 1 раз на весь срок жизни приложения
        private readonly MarkdownPipeline _pipeline;
        private readonly CodeColorizer _colorizer;

        // Храним загруженные шрифты в памяти, чтобы не читать их с диска каждый раз
        private readonly List<Typeface> _mathTypefaces = new();

        // Внутренние структуры для передачи данных
        private readonly record struct RenderedBlock(IView View, string Text);

        // Семафор для предотвращения одновременного доступа к шрифтам
        private readonly SemaphoreSlim _mathRenderSemaphore = new SemaphoreSlim(1, 1);

        private sealed class LatexImage : Image
        {
            public string Latex { get; set; } = string.Empty;
        }

        private sealed class ColoredCodeBlock : Border { }

        private sealed class HighlightedLabel : Label
        {
            public string Code { get; set; } = string.Empty;
            public string Language { get; set; } = string.Empty;
        }

        private static readonly string MonospaceFont =
#if WINDOWS
            "Cascadia Code, Consolas, Courier New"
#elif ANDROID
            "monospace"
#elif IOS || MACCATALYST
            "Menlo"
#else
            "Courier New"
#endif
            ;

        public MarkdownRenderService()
        {
            _pipeline = new MarkdownPipelineBuilder()
                .UseAdvancedExtensions()
                .UseMathematics()
                .UseTaskLists()
                .Build();

            _colorizer = new CodeColorizer();
        }

        public async Task WarmUpAsync()
        {
            const string warmupMarkdown =
                """
                # x
                $x$
                ```csharp
                        var x = 1;
                ```
                """;

            try
            {
                await RenderToViewAsync(warmupMarkdown, new MarkdownRenderOptions());
                Debug.WriteLine("[MarkdownRenderService] Warmup completed successfully!");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MarkdownRenderService] Warmup failed: {ex.Message}");
            }
        }

        public void AddMathFont(Stream fontStream)
        {
            try
            {
                var face = new OpenFontReader().Read(fontStream);
                if (face != null) _mathTypefaces.Add(face);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MarkdownRenderService] Font stream load failed: {ex.Message}");
            }
        }

        public async Task AddMathFontAsync(string appPackageFileName)
        {
            try
            {
                using var stream = await FileSystem.OpenAppPackageFileAsync(appPackageFileName);
                var face = new OpenFontReader().Read(stream);
                if (face != null) _mathTypefaces.Add(face);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MarkdownRenderService] Font '{appPackageFileName}' failed: {ex.Message}");
            }
        }

        public async Task<View> RenderToViewAsync(string markdown, MarkdownRenderOptions options, CancellationToken token = default)
        {
            var container = new VerticalStackLayout();

            if (string.IsNullOrWhiteSpace(markdown))
                return container;

            try
            {
                var document = Markdown.Parse(markdown, _pipeline);

                foreach (var block in document)
                {
                    token.ThrowIfCancellationRequested();
                    if (block is LinkReferenceDefinitionGroup) continue;

                    var rendered = await RenderBlockAsync(block, options, indentLevel: 0, token);
                    if (rendered.HasValue && rendered.Value.View is View view)
                    {
                        AttachCopyGesture(view, rendered.Value.Text);
                        container.Children.Add(view);
                    }
                }

                //////////////////////////////////////////////
                ///Тут кнопка для копирования всего текста///
                /////////////////////////////////////////////

                //container.Children.Add(BuildCopyAllButton(markdown));
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("[MarkdownRenderService] Render canceled by user input.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MarkdownRenderService] Render error: {ex}");
            }

            return container;
        }

        private void AttachCopyGesture(View view, string textToCopy)
        {
            if (string.IsNullOrWhiteSpace(textToCopy)) return;

            var tap = new TapGestureRecognizer { NumberOfTapsRequired = 2 };
            tap.Tapped += async (s, e) =>
            {
                await Clipboard.Default.SetTextAsync(textToCopy);
                var toast = Toast.Make("Content copied !", ToastDuration.Short, 14);
                //To do Доделать, чтобы не было ошибки при вызове Show() в потоке, который не является UI-потоком

                //await toast.Show();
            };
            view.GestureRecognizers.Add(tap);
        }

        private View BuildCopyAllButton(string fullText)
        {
            var btn = new Button
            {
                Text = "⎘ Copy All",
                FontSize = 11,
                Padding = new Thickness(8, 4),
                CornerRadius = 6,
                HorizontalOptions = LayoutOptions.End,
                Margin = new Thickness(0, 4, 0, 0),
                BorderWidth = 0,
                BorderColor = Colors.Transparent,
                BackgroundColor = Colors.Transparent,
            };

            VisualStateManager.SetVisualStateGroups(btn, new VisualStateGroupList
            {
                new VisualStateGroup
                {
                    States =
                    {
                        new VisualState { Name = "Normal" },
                        new VisualState
                        {
                            Name = "PointerOver",
                            Setters =
                            {
                                new Setter
                                {
                                    Property = Button.BackgroundColorProperty,
                                    Value    = Color.FromArgb("#1A808080")
                                }
                            }
                        }
                    }
                }
            });

            btn.Clicked += async (_, _) =>
            {
                await Clipboard.Default.SetTextAsync(fullText);
                var toast = Toast.Make("All content copied !", ToastDuration.Short, 14);
                await toast.Show();
            };

            return btn;
        }

        private async Task<RenderedBlock?> RenderBlockAsync(Block block, MarkdownRenderOptions options, int indentLevel, CancellationToken token)
        {
            switch (block)
            {
                case ParagraphBlock paragraph:
                    {
                        var rb = await RenderParagraphAsync(paragraph, options, token);
                        if (rb.View is View v) v.Margin = new Thickness(0, 4, 0, 4);
                        return rb;
                    }

                case HeadingBlock heading:
                    {
                        double multiplier = GetHeadingMultiplier(heading.Level);
                        var formatted = RenderInlinesToFormattedString(heading.Inline, options, fontSize: options.BaseFontSize * multiplier);

                        foreach (var span in formatted.Spans)
                            span.FontAttributes |= FontAttributes.Bold;

                        var lbl = new Label
                        {
                            FormattedText = formatted,
                            FontAttributes = FontAttributes.Bold,
                            FontFamily = options.FontFamily,
                            TextColor = options.TextColor,
                            FontSize = options.BaseFontSize * multiplier,
                            Margin = new Thickness(0, heading.Level <= 2 ? 18 : 12, 0, 4),
                            LineBreakMode = LineBreakMode.WordWrap
                        };

                        if (heading.Level <= 2)
                        {
                            var stack = new VerticalStackLayout { Spacing = 4 };
                            stack.Children.Add(lbl);
                            stack.Children.Add(new BoxView
                            {
                                HeightRequest = heading.Level == 1 ? 2 : 1,
                                BackgroundColor = Color.FromArgb("#D0D7DE"),
                                HorizontalOptions = LayoutOptions.Fill
                            });
                            stack.Margin = lbl.Margin;
                            lbl.Margin = Thickness.Zero;
                            return new RenderedBlock(stack, formatted.ToString());
                        }

                        return new RenderedBlock(lbl, formatted.ToString());
                    }

                case MathBlock math:
                    {
                        var latex = math.Lines.ToString();
                        var view = await CreateLatexViewAsync(latex, options, token);
                        var wrapper = new ContentView
                        {
                            Content = view,
                            Margin = new Thickness(0, 10, 0, 10),
                            HorizontalOptions = LayoutOptions.Start
                        };
                        return new RenderedBlock(wrapper, latex);
                    }

                case CodeBlock code:
                    return await RenderCodeBlockAsync(code, options, token);

                case ThematicBreakBlock:
                    {
                        var hr = new BoxView
                        {
                            HeightRequest = 1,
                            BackgroundColor = Color.FromArgb("#D0D7DE"),
                            HorizontalOptions = LayoutOptions.Fill,
                            Margin = new Thickness(0, 12)
                        };
                        return new RenderedBlock(hr, "---");
                    }

                case ListBlock list:
                    {
                        var view = await RenderListAsync(list, options, indentLevel, token);
                        view.Margin = new Thickness(0, 4);
                        return new RenderedBlock(view, string.Empty);
                    }

                case QuoteBlock quote:
                    {
                        var view = await RenderQuoteAsync(quote, options, token);
                        return new RenderedBlock(view, string.Empty);
                    }

                case Table table:
                    return await RenderTableAsync(table, options, token);

                default:
                    {
                        var text = block.ToString() ?? string.Empty;
                        var lbl = new Label
                        {
                            Text = text,
                            TextColor = options.TextColor,
                            FontSize = options.BaseFontSize,
                            FontFamily = options.FontFamily,
                            LineBreakMode = LineBreakMode.WordWrap
                        };
                        return new RenderedBlock(lbl, text);
                    }
            }
        }

        private async Task<RenderedBlock> RenderParagraphAsync(ParagraphBlock paragraph, MarkdownRenderOptions options, CancellationToken token)
        {
            bool needsViews = paragraph.Inline?.Descendants()
                .Any(i => i is MathInline || (i is LinkInline l && l.IsImage)) ?? false;

            if (!needsViews)
            {
                var formatted = RenderInlinesToFormattedString(paragraph.Inline, options, fontSize: options.BaseFontSize);

                var lbl = new Label
                {
                    FormattedText = formatted,
                    FontFamily = options.FontFamily,
                    TextColor = options.TextColor,
                    FontSize = options.BaseFontSize,
                    LineHeight = 1.4,
                    LineBreakMode = LineBreakMode.WordWrap
                };
                return new RenderedBlock(lbl, formatted.ToString());
            }

            var container = new FlexLayout
            {
                Direction = Microsoft.Maui.Layouts.FlexDirection.Row,
                Wrap = Microsoft.Maui.Layouts.FlexWrap.Wrap,
                AlignItems = Microsoft.Maui.Layouts.FlexAlignItems.Center,
                HorizontalOptions = LayoutOptions.Fill
            };

            var sb = new StringBuilder();
            foreach (var cv in await RenderInlinesToViewsAsync(paragraph.Inline, options, token))
            {
                container.Children.Add(cv.View);
                sb.Append(cv.Text);
            }

            return new RenderedBlock(container, sb.ToString());
        }

        private async Task<RenderedBlock> RenderCodeBlockAsync(CodeBlock code, MarkdownRenderOptions options, CancellationToken token)
        {
            var codeText = code.Lines.ToString();
            string? lang = (code is FencedCodeBlock fenced) ? fenced.Info?.Trim().ToLowerInvariant() : null;

            if (lang == "markdown")
            {
                var inner = new VerticalStackLayout();
                var nestedDoc = Markdown.Parse(codeText, _pipeline);
                foreach (var nestedBlock in nestedDoc)
                {
                    var rb = await RenderBlockAsync(nestedBlock, options, 0,token);
                    if (rb.HasValue && rb.Value.View != null) inner.Children.Add(rb.Value.View);
                }
                return new RenderedBlock(WrapInCodeBorder(inner, options.CodeBackgroundColor), codeText);
            }

            FormattedString highlightedText;
            lock (_colorizer)
            {
                _colorizer.FontSize = options.BaseFontSize;
                _colorizer.FontFamily = MonospaceFont;
                highlightedText = _colorizer.Highlight(codeText, lang);
            }

            var highlightedLabel = new HighlightedLabel
            {
                FormattedText = highlightedText,
                FontSize = options.BaseFontSize,
                LineBreakMode = LineBreakMode.NoWrap,
                Code = codeText,
                Language = lang ?? string.Empty
            };

            var scroll = new ScrollView
            {
                Orientation = ScrollOrientation.Horizontal,
                Content = highlightedLabel
            };

            return new RenderedBlock(WrapInCodeBorder(scroll, options.CodeBackgroundColor), codeText);
        }

        private ColoredCodeBlock WrapInCodeBorder(View content, Color bgColor)
        {
            return new ColoredCodeBlock
            {
                Content = content,
                Padding = new Thickness(14, 10),
                BackgroundColor = bgColor,
                StrokeShape = new RoundRectangle { CornerRadius = 8 },
                Margin = new Thickness(0, 8, 0, 8)
            };
        }

        private async Task<RenderedBlock> RenderTableAsync(Table table, MarkdownRenderOptions options, CancellationToken token)
        {
            var outerBorder = new Border
            {
                StrokeShape = new RoundRectangle { CornerRadius = 6 },
                StrokeThickness = 1,
                Stroke = new SolidColorBrush(Color.FromArgb("#D0D7DE")),
                Margin = new Thickness(0, 8, 0, 8),
                Padding = Thickness.Zero
            };

            var grid = new Grid { ColumnSpacing = 0, RowSpacing = 0 };

            int colCount = table.ColumnDefinitions.Count;
            for (int c = 0; c < colCount; c++)
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            int r = 0;
            foreach (TableRow row in table)
            {
                bool isHeader = row.IsHeader;
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                int c = 0;
                foreach (TableCell cell in row)
                {
                    if (cell.Count < 1) { c++; continue; }

                    var cellContent = new VerticalStackLayout
                    {
                        Padding = new Thickness(10, 6),
                        Spacing = 2,
                        HorizontalOptions = LayoutOptions.Fill,
                        BackgroundColor = isHeader ? Color.FromArgb("#F6F8FA") : Colors.Transparent
                    };

                    foreach (var cellBlock in cell)
                    {
                        var rendered = await RenderBlockAsync(cellBlock, options, 0, token);
                        if (rendered?.View is View cellBlockView)
                        {
                            cellBlockView.Margin = Thickness.Zero;
                            if (isHeader) BoldAllLabels(cellBlockView);
                            cellContent.Children.Add(cellBlockView);
                        }
                    }

                    var cellWrapper = new Grid();
                    cellWrapper.Children.Add(cellContent);

                    if (r < table.Count - 1)
                        cellWrapper.Children.Add(new BoxView
                        {
                            HeightRequest = 1,
                            BackgroundColor = Color.FromArgb("#D0D7DE"),
                            VerticalOptions = LayoutOptions.End
                        });

                    if (c < colCount - 1)
                        cellWrapper.Children.Add(new BoxView
                        {
                            WidthRequest = 1,
                            BackgroundColor = Color.FromArgb("#D0D7DE"),
                            HorizontalOptions = LayoutOptions.End
                        });

                    grid.Add(cellWrapper, c, r);
                    c++;
                }
                r++;
            }

            outerBorder.Content = grid;
            return new RenderedBlock(outerBorder, string.Empty);
        }

        private async Task<View> RenderQuoteAsync(QuoteBlock quote, MarkdownRenderOptions options, CancellationToken token)
        {
            var content = new VerticalStackLayout { Spacing = 2 };
            foreach (var block in quote)
            {
                var rendered = await RenderBlockAsync(block, options, 0, token);
                if (rendered?.View != null)
                    content.Children.Add(rendered.Value.View);
            }

            var bar = new BoxView { BackgroundColor = options.QuoteBarColor };
            var body = new VerticalStackLayout
            {
                Padding = new Thickness(12, 0, 0, 0),
                Children = { content }
            };

            var grid = new Grid
            {
                Margin = new Thickness(0, 8),
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = 4 },
                    new ColumnDefinition { Width = GridLength.Star }
                }
            };
            grid.Add(bar, 0, 0);
            grid.Add(body, 1, 0);
            return grid;
        }

        private async Task<View> RenderListAsync(ListBlock list, MarkdownRenderOptions options, int indentLevel, CancellationToken token)
        {
            var container = new VerticalStackLayout { Spacing = 2 };
            int index = list.IsOrdered && int.TryParse(list.OrderedStart, out int start) ? start : 1;

            foreach (var item in list.OfType<ListItemBlock>())
            {
                var row = new Grid
                {
                    ColumnDefinitions =
                    {
                        new ColumnDefinition { Width = GridLength.Auto },
                        new ColumnDefinition { Width = GridLength.Star }
                    },
                    ColumnSpacing = 6,
                    Padding = new Thickness(indentLevel * 16, 2, 0, 2)
                };

                bool isTask = false;
                if (item.Count > 0 && item[0] is ParagraphBlock pb && pb.Inline?.FirstChild is TaskList task)
                {
                    isTask = true;
                    var cb = new CheckBox
                    {
                        IsChecked = task.Checked,
                        IsEnabled = false,
                        VerticalOptions = LayoutOptions.Start
                    };
                    Grid.SetColumn(cb, 0);
                    row.Children.Add(cb);
                }

                if (!isTask)
                {
                    string marker = list.IsOrdered ? $"{index}." : "•";
                    var bullet = new Label
                    {
                        Text = marker,
                        FontFamily = options.FontFamily,
                        TextColor = options.TextColor,
                        FontSize = options.BaseFontSize,
                        VerticalTextAlignment = TextAlignment.Start,
                        MinimumWidthRequest = list.IsOrdered ? 28 : 16
                    };
                    Grid.SetColumn(bullet, 0);
                    row.Children.Add(bullet);
                }

                var itemContent = new VerticalStackLayout { Spacing = 0 };
                foreach (var subBlock in item)
                {
                    int nextIndent = subBlock is ListBlock ? indentLevel + 1 : indentLevel;
                    var rendered = await RenderBlockAsync(subBlock, options, nextIndent, token);
                    if (rendered?.View != null)
                        itemContent.Children.Add(rendered.Value.View);
                }

                Grid.SetColumn(itemContent, 1);
                row.Children.Add(itemContent);
                container.Children.Add(row);

                if (list.IsOrdered) index++;
            }

            return container;
        }

        private FormattedString RenderInlinesToFormattedString(
            ContainerInline? container,
            MarkdownRenderOptions options,
            FontAttributes inheritedAttributes = FontAttributes.None,
            TextDecorations inheritedDecorations = TextDecorations.None,
            Color? inheritedColor = null,
            double? fontSize = null)
        {
            var formatted = new FormattedString();
            if (container == null) return formatted;

            double size = fontSize ?? options.BaseFontSize;

            foreach (var inline in container)
            {
                switch (inline)
                {
                    case LiteralInline literal:
                        formatted.Spans.Add(new Span
                        {
                            Text = literal.Content.ToString(),
                            FontSize = size,
                            FontAttributes = inheritedAttributes,
                            TextDecorations = inheritedDecorations,
                            TextColor = inheritedColor ?? options.TextColor
                        });
                        break;

                    case LineBreakInline lb:
                        formatted.Spans.Add(new Span
                        {
                            Text = lb.IsHard ? "\n" : " ",
                            FontSize = size
                        });
                        break;

                    case EmphasisInline emphasis:
                        var attrs = inheritedAttributes;
                        var decs = inheritedDecorations;

                        if (emphasis.DelimiterChar is '*' or '_')
                        {
                            if (emphasis.DelimiterCount == 1) attrs |= FontAttributes.Italic;
                            else if (emphasis.DelimiterCount >= 2) attrs |= FontAttributes.Bold;
                        }
                        else if (emphasis.DelimiterChar == '~' && emphasis.DelimiterCount == 2)
                            decs |= TextDecorations.Strikethrough;

                        var inner = RenderInlinesToFormattedString(emphasis, options, attrs, decs, inheritedColor, size);
                        foreach (var s in inner.Spans) formatted.Spans.Add(s);
                        break;

                    case CodeInline code:
                        formatted.Spans.Add(new Span
                        {
                            Text = $"\u202F{code.Content}\u202F",
                            FontSize = size,
                            FontFamily = MonospaceFont,
                            BackgroundColor = options.CodeBackgroundColor
                        });
                        break;

                    case LinkInline link when !link.IsImage:
                        var linkInner = RenderInlinesToFormattedString(link, options, inheritedAttributes, inheritedDecorations | TextDecorations.Underline, Color.FromArgb("#1A73E8"), size);
                        foreach (var span in linkInner.Spans)
                        {
                            if (!string.IsNullOrEmpty(link.Url))
                            {
                                var gesture = new TapGestureRecognizer();
                                var url = link.Url;
                                gesture.Tapped += async (_, _) => await Launcher.Default.OpenAsync(url);
                                span.GestureRecognizers.Add(gesture);
                            }
                            formatted.Spans.Add(span);
                        }
                        break;

                    case AutolinkInline autolink:
                        var alSpan = new Span
                        {
                            Text = autolink.Url,
                            FontSize = size,
                            TextColor = Color.FromArgb("#1A73E8"),
                            TextDecorations = TextDecorations.Underline
                        };
                        var alGesture = new TapGestureRecognizer();
                        var alUrl = autolink.Url;
                        alGesture.Tapped += async (_, _) => await Launcher.Default.OpenAsync(alUrl);
                        alSpan.GestureRecognizers.Add(alGesture);
                        formatted.Spans.Add(alSpan);
                        break;

                    case ContainerInline ci:
                        var ciInner = RenderInlinesToFormattedString(ci, options, inheritedAttributes, inheritedDecorations, inheritedColor, size);
                        foreach (var s in ciInner.Spans) formatted.Spans.Add(s);
                        break;
                }
            }
            return formatted;
        }

        private async Task<List<RenderedBlock>> RenderInlinesToViewsAsync(ContainerInline? container, MarkdownRenderOptions options, CancellationToken token)
        {
            var views = new List<RenderedBlock>();
            if (container == null) return views;

            foreach (var inline in container)
            {
                token.ThrowIfCancellationRequested();
                switch (inline)
                {
                    case LineBreakInline lb:
                        var lblBreak = new Label { Text = lb.IsHard ? "\n" : " ", FontFamily = options.FontFamily, TextColor = options.TextColor };
                        views.Add(new RenderedBlock(lblBreak, lb.IsHard ? "\n" : " "));
                        break;

                    case LiteralInline lit:
                        var text = lit.Content.ToString();
                        if (views.Count > 0 && views[^1].View is Label prev && prev.GestureRecognizers.Count == 0 && prev.FontFamily == options.FontFamily)
                        {
                            prev.Text += text;
                            views[^1] = new RenderedBlock(prev, views[^1].Text + text);
                        }
                        else
                        {
                            var lblLit = new Label { Text = text, FontFamily = options.FontFamily, TextColor = options.TextColor, FontSize = options.BaseFontSize, LineBreakMode = LineBreakMode.WordWrap };
                            views.Add(new RenderedBlock(lblLit, text));
                        }
                        break;

                    case EmphasisInline emp:
                        var innerViews = await RenderInlinesToViewsAsync(emp, options, token);
                        foreach (var cv in innerViews)
                        {
                            if (cv.View is Label lblEmp)
                            {
                                if (emp.DelimiterChar is '*' or '_')
                                    lblEmp.FontAttributes |= emp.DelimiterCount >= 2 ? FontAttributes.Bold : FontAttributes.Italic;
                                else if (emp.DelimiterChar == '~' && emp.DelimiterCount == 2)
                                    lblEmp.TextDecorations |= TextDecorations.Strikethrough;
                            }
                            views.Add(cv);
                        }
                        break;

                    case CodeInline code:
                        var lblCode = new Label { Text = code.Content, FontFamily = MonospaceFont, FontSize = options.BaseFontSize, LineBreakMode = LineBreakMode.WordWrap };
                        views.Add(new RenderedBlock(
                            new Border { Content = lblCode, Padding = new Thickness(5, 2), BackgroundColor = options.CodeBackgroundColor, StrokeShape = new RoundRectangle { CornerRadius = 4 } },
                            code.Content));
                        break;

                    case LinkInline img when img.IsImage:
                        var image = new Image { Source = ImageSource.FromUri(new Uri(img.Url ?? string.Empty)), Aspect = Aspect.AspectFit, WidthRequest = 300, HeightRequest = 200 };
                        views.Add(new RenderedBlock(image, img.Url ?? string.Empty));
                        break;

                    case LinkInline link:
                        var innerLinks = await RenderInlinesToViewsAsync(link, options, token);
                        foreach (var cv in innerLinks)
                        {
                            if (cv.View is Label lblLink)
                            {
                                lblLink.TextColor = Color.FromArgb("#1A73E8");
                                lblLink.TextDecorations = TextDecorations.Underline;
                                var gesture = new TapGestureRecognizer();
                                var url = link.Url;
                                gesture.Tapped += async (_, _) => { if (!string.IsNullOrEmpty(url)) await Launcher.Default.OpenAsync(url); };
                                lblLink.GestureRecognizers.Add(gesture);
                            }
                            views.Add(cv);
                        }
                        break;

                    case AutolinkInline autolink:
                        var lblAl = new Label { Text = autolink.Url, FontFamily = options.FontFamily, TextColor = Color.FromArgb("#1A73E8"), TextDecorations = TextDecorations.Underline };
                        var alGesture = new TapGestureRecognizer();
                        var alUrl = autolink.Url;
                        alGesture.Tapped += async (_, _) => await Launcher.Default.OpenAsync(alUrl);
                        lblAl.GestureRecognizers.Add(alGesture);
                        views.Add(new RenderedBlock(lblAl, autolink.Url));
                        break;

                    case MathInline math:
                        var latex = math.Content.ToString();
                        var view = await CreateLatexViewAsync(latex, options, token);
                        views.Add(new RenderedBlock(view, latex));
                        break;

                    case ContainerInline ci:
                        views.AddRange(await RenderInlinesToViewsAsync(ci, options, token));
                        break;
                }
            }
            return views;
        }

        private async Task<LatexImage> CreateLatexViewAsync(string latex, MarkdownRenderOptions options, CancellationToken token)
        {
            await _mathRenderSemaphore.WaitAsync(token);
            try {
                var (source, w, h) = await Task.Run(() => RenderLatexToSource(latex, options), token);
                return new LatexImage
                {
                    Source = source,
                    WidthRequest = w,
                    HeightRequest = h,
                    Latex = latex,
                    Aspect = Aspect.AspectFit
                };
            }

            finally 
            {
                _mathRenderSemaphore.Release();
            }
            
        }

        private (ImageSource source, float width, float height) RenderLatexToSource(string latex, MarkdownRenderOptions options)
        {
            // Локальный MathPainter (потокобезопасность)
            var painter = new MathPainter
            {
                FontSize = (float)options.LatexFontSize,
                TextColor = options.LatexTextColor.ToSKColor(),
                LaTeX = PreprocessLatex(latex)
            };

            // Добавляем кэшированные кастомные шрифты
            foreach (var typeface in _mathTypefaces)
            {
                painter.LocalTypefaces.Append(typeface);
            }

            var size = painter.Measure();
            float w = MathF.Ceiling(size.Width) + 6;
            float h = MathF.Ceiling(size.Height) + 6;

            var info = new SKImageInfo((int)w, (int)h, SKColorType.Rgba8888, SKAlphaType.Premul);
            using var surface = SKSurface.Create(info);
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.Transparent);
            painter.Draw(canvas);

            using var image = surface.Snapshot();
            using var skData = image.Encode(SKEncodedImageFormat.Png, 100);
            var bytes = skData.ToArray();

            return (ImageSource.FromStream(() => new MemoryStream(bytes)), w, h);
        }

        private static string PreprocessLatex(string latex)
        {
            if (string.IsNullOrWhiteSpace(latex)) return latex;

            latex = Regex.Replace(latex, @"\\cfrac(?=[^a-zA-Z]|$)", @"\frac");
            latex = Regex.Replace(latex, @"\\dfrac(?=[^a-zA-Z]|$)", @"\frac");
            latex = Regex.Replace(latex, @"\\tfrac(?=[^a-zA-Z]|$)", @"\frac");
            latex = Regex.Replace(latex, @"\\boldsymbol(?=[^a-zA-Z]|$)", @"\mathbf");
            latex = Regex.Replace(latex, @"\\bm(?=[^a-zA-Z]|$)", @"\mathbf");
            latex = Regex.Replace(latex, @"\\operatorname\*(?=[^a-zA-Z]|$)", @"\text");
            latex = Regex.Replace(latex, @"\\operatorname(?=[^a-zA-Z]|$)", @"\text");

            latex = RewriteArrayEnvironment(latex);
            latex = latex.Replace(@"\hline", "");

            latex = Regex.Replace(latex, @"\\begin\{align\*?\}", @"\begin{aligned}");
            latex = Regex.Replace(latex, @"\\end\{align\*?\}", @"\end{aligned}");
            latex = Regex.Replace(latex, @"\\begin\{equation\*?\}", "");
            latex = Regex.Replace(latex, @"\\end\{equation\*?\}", "");

            latex = StripCommandWithArg(latex, "DeclareMathOperator");
            latex = StripCommandWithArg(latex, "label");
            latex = StripCommandWithArg(latex, "tag");

            latex = latex.Replace(@"\nonumber", "");
            latex = latex.Replace(@"\allowbreak", "");
            latex = latex.Replace(@"\!", "");

            return latex.Trim();
        }

        private static string RewriteArrayEnvironment(string latex)
        {
            latex = Regex.Replace(latex, @"\\begin\{array\*?\}\s*\{[^}]*\}", @"\begin{matrix}");
            latex = Regex.Replace(latex, @"\\end\{array\*?\}", @"\end{matrix}");
            return latex;
        }

        private static string StripCommandWithArg(string latex, string cmd)
        {
            var sb = new StringBuilder();
            int i = 0;
            string trigger = @"\" + cmd + "{";

            while (i < latex.Length)
            {
                int idx = latex.IndexOf(trigger, i, StringComparison.Ordinal);
                if (idx < 0) { sb.Append(latex, i, latex.Length - i); break; }

                sb.Append(latex, i, idx - i);
                int pos = idx + trigger.Length, depth = 1;
                while (pos < latex.Length && depth > 0)
                {
                    if (latex[pos] == '{') depth++;
                    else if (latex[pos] == '}') depth--;
                    pos++;
                }
                i = pos;
            }

            return sb.Length == 0 ? latex : sb.ToString();
        }

        private static void BoldAllLabels(View root)
        {
            if (root is Label lbl)
            {
                lbl.FontAttributes |= FontAttributes.Bold;
                if (lbl.FormattedText != null)
                    foreach (var span in lbl.FormattedText.Spans)
                        span.FontAttributes |= FontAttributes.Bold;
                return;
            }

            IEnumerable<IView>? children = root switch
            {
                Layout layout => layout.Children,
                ContentView cv => cv.Content is View v1 ? new[] { v1 } : null,
                Border b => b.Content is View v2 ? new[] { v2 } : null,
                ScrollView sv => sv.Content is View v3 ? new[] { v3 } : null,
                _ => null
            };

            if (children == null) return;
            foreach (var child in children.OfType<View>())
                BoldAllLabels(child);
        }

        private static double GetHeadingMultiplier(int level) => level switch
        {
            1 => 2.0,
            2 => 1.6,
            3 => 1.3,
            4 => 1.1,
            _ => 1.0
        };
    }
}