using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Zubrium.Content.Parsing;
using Zubrium.Content.Repository;
using Zubrium.Domain;
using Zubrium.Maui.ViewModels;

namespace Zubrium.Maui.Features.Generals
{
    public partial class ImportViewModel: BaseViewModel
    {
        private readonly IDeckSourceParser _parser;

        // ==========================================
        // СВОЙСТВА РЕЖИМОВ И ТЕКСТА
        // ==========================================

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsFileMode))]
        public partial bool IsPasteTextMode { get; set; } = true;

        // Вычисляемое свойство: если не текст, значит режим файла
        public bool IsFileMode => !IsPasteTextMode;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasContent))]
        public partial string InputText { get; set; } = string.Empty;

        // Кнопка "Посмотреть код контента" будет активна только если есть текст
        public bool HasContent => !string.IsNullOrEmpty(InputText);

        // Хранение результата парсирования для получения количества элементов
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CardsFoundCount))]
        [NotifyPropertyChangedFor(nameof(QuizzesFoundCount))]
        [NotifyPropertyChangedFor(nameof(ArticlesFoundCount))]
        [NotifyPropertyChangedFor(nameof(HasCards))]
        [NotifyPropertyChangedFor(nameof(HasQuizzes))]
        [NotifyPropertyChangedFor(nameof(HasArticles))]
        [NotifyPropertyChangedFor(nameof(PreviewCards))]
        [NotifyPropertyChangedFor(nameof(PreviewQuizzes))]
        [NotifyPropertyChangedFor(nameof(PreviewArticles))]
        public partial ParsedContentSet? ParsedContentSet { get; set; }

        [ObservableProperty]
        public partial bool IsCodePreviewVisible { get; set; }

        [ObservableProperty]
        public partial string ImportButtonText { get; set; } = "Импортировать";

        // Вычисляемые свойства для количества найденных элементов
        public int CardsFoundCount => ParsedContentSet?.Cards?.Count ?? 0;
        public int QuizzesFoundCount => ParsedContentSet?.Quizzes?.Count ?? 0;
        public int ArticlesFoundCount => ParsedContentSet?.Articles?.Count ?? 0;

        // Вычисляемые свойства для проверки наличия элементов
        public bool HasCards => CardsFoundCount > 0;
        public bool HasQuizzes => QuizzesFoundCount > 0;
        public bool HasArticles => ArticlesFoundCount > 0;

        // Вычисляемые свойства для предпросмотра всех найденных элементов
        public IEnumerable<Card>? PreviewCards => ParsedContentSet?.Cards;
        public IEnumerable<QuizBlock>? PreviewQuizzes => ParsedContentSet?.Quizzes;
        public IEnumerable<Article>? PreviewArticles => ParsedContentSet?.Articles;

        // ==========================================
        // СВОЙСТВА ДЛЯ СЕКЦИИ "ЧТО ИМПОРТИРОВАТЬ"
        // ==========================================

        [ObservableProperty]
        public partial bool IsCardsImportSelected { get; set; } = true;

        [ObservableProperty]
        public partial bool IsQuizzesImportSelected { get; set; } = true;

        [ObservableProperty]
        public partial bool IsArticlesImportSelected { get; set; } = true;

        // ==========================================
        // СВОЙСТВА ДЛЯ СЕКЦИИ "ПРЕДПРОСМОТР"
        // ==========================================

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsAnyPreviewActive))]
        public partial bool IsCardsPreviewActive { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsAnyPreviewActive))]
        public partial bool IsQuizzesPreviewActive { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsAnyPreviewActive))]
        public partial bool IsArticlesPreviewActive { get; set; }

        // Вычисляемое свойство для отображения окна контента (заглушки)
        public bool IsAnyPreviewActive => IsCardsPreviewActive || IsQuizzesPreviewActive || IsArticlesPreviewActive;

        public override void ApplyQueryAttributes(IDictionary<string, object> query)
        {

        }

        public ImportViewModel(IContentRepository repository, IDeckSourceParser parser) : base(repository)
        {
            _parser = parser;
        }


        // ==========================================
        // НАВИГАЦИЯ И ВКЛАДКИ (СВЕРХУ)
        // ==========================================


        [RelayCommand]
        public void SwitchToPasteText()
        {
            IsPasteTextMode = true;
        }

        [RelayCommand]
        public void SwitchToFile()
        {
            IsPasteTextMode = false;
        }

        // ==========================================
        // ДЕЙСТВИЯ С КОНТЕНТОМ (КНОПКИ И ПОПАП)
        // ==========================================

        [RelayCommand]
        public async Task PasteFromClipboard()
        {
            if (Clipboard.Default.HasText)
            {
                InputText = await Clipboard.Default.GetTextAsync();
                // Парсируем контент при вставке текста
                ParsedContentSet = _parser.Parse(InputText);
            }
        }

        [RelayCommand]
        public async Task PickFile()
        {
            try
            {
                var result = await FilePicker.Default.PickAsync();
                if (result != null)
                {
                    // Читаем текст из файла
                    InputText = await File.ReadAllTextAsync(result.FullPath);
                    // Парсируем контент при выборе файла
                    ParsedContentSet = _parser.Parse(InputText);
                    // Показываем окно предпросмотра кода
                    IsCodePreviewVisible = true;
                }
            }
            catch (Exception)
            {
                // Обработка отмены выбора файла
            }
        }

        [RelayCommand]
        public void OpenCodePreview()
        {
            IsCodePreviewVisible = true;
        }

        [RelayCommand]
        public void CloseCodePreview()
        {
            IsCodePreviewVisible = false;
        }

        // ==========================================
        // КАТЕГОРИИ И ИМПОРТ
        // ==========================================

        [RelayCommand]
        public async Task SelectCategory()
        {
            // Логика вызова окна/bottom sheet с выбором категории
            await Task.CompletedTask;
        }

        [RelayCommand]
        public async Task Import()
        {
            // Парсируем контент и сохраняем результат для отображения количества элементов
            var result = _parser.Parse(InputText);
            ParsedContentSet = result;

            await _repository.InsertContentSet(result);

            // Логика финального импорта
            await Task.CompletedTask;
        }

        // ==========================================
        // КОМАНДЫ ДЛЯ КАРТОЧЕК ИМПОРТА
        // ==========================================

        [RelayCommand]
        public void ToggleCardsImport()
        {
            // Активируем только если есть найденные карточки
            if (!HasCards) return;
            IsCardsImportSelected = !IsCardsImportSelected;
            if (!IsCardsImportSelected) IsCardsPreviewActive = false;
        }

        [RelayCommand]
        public void ToggleQuizzesImport()
        {
            // Активируем только если есть найденные квизы
            if (!HasQuizzes) return;
            IsQuizzesImportSelected = !IsQuizzesImportSelected;
            if (!IsQuizzesImportSelected) IsQuizzesPreviewActive = false;
        }

        [RelayCommand]
        public void ToggleArticlesImport()
        {
            // Активируем только если есть найденные статьи
            if (!HasArticles) return;
            IsArticlesImportSelected = !IsArticlesImportSelected;
            if (!IsArticlesImportSelected) IsArticlesPreviewActive = false;
        }

        // ==========================================
        // КОМАНДЫ ДЛЯ ТАБОВ ПРЕДПРОСМОТРА
        // ==========================================

        [RelayCommand]
        public void ToggleCardsPreview()
        {
            if (!IsCardsImportSelected) return;

            if (IsCardsPreviewActive)
            {
                IsCardsPreviewActive = false;
                return;
            }

            IsCardsPreviewActive = true;
            IsQuizzesPreviewActive = false;
            IsArticlesPreviewActive = false;
        }

        [RelayCommand]
        public void ToggleQuizzesPreview()
        {
            if (!IsQuizzesImportSelected) return;

            if (IsQuizzesPreviewActive)
            {
                IsQuizzesPreviewActive = false;
                return;
            }

            IsQuizzesPreviewActive = true;
            IsCardsPreviewActive = false;
            IsArticlesPreviewActive = false;
        }

        [RelayCommand]
        public void ToggleArticlesPreview()
        {
            if (!IsArticlesImportSelected) return;

            if (IsArticlesPreviewActive)
            {
                IsArticlesPreviewActive = false;
                return;
            }

            IsArticlesPreviewActive = true;
            IsCardsPreviewActive = false;
            IsQuizzesPreviewActive = false;
        }
    }
}
