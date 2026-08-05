using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zubrium.Content.Parsing;
using Microsoft.Maui.ApplicationModel;
using Zubrium.Content.Repository;
using Zubrium.Domain;
using Zubrium.Maui.ViewModels;
using Zubrium.Persistence.Entities;

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
        public partial ParsedContentSet? ParsedContentSet { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SelectedCategoryText))]
        public partial CategoryDraft? SelectedCategory { get; set; }

        public string SelectedCategoryText =>
            string.IsNullOrWhiteSpace(SelectedCategory?.Name)
                ? "Выберите категорию"
                : SelectedCategory.Name;

        [ObservableProperty]
        public partial bool IsCodePreviewVisible { get; set; }

        [ObservableProperty]
        public partial string ImportButtonText { get; set; } = "Импортировать";

        [ObservableProperty]
        public partial bool IsImporting { get; set; }

        // Вычисляемые свойства для количества найденных элементов
        public int CardsFoundCount => ParsedContentSet?.Cards?.Count ?? 0;
        public int QuizzesFoundCount => ParsedContentSet?.Quizzes?.Count ?? 0;
        public int ArticlesFoundCount => ParsedContentSet?.Articles?.Count ?? 0;

        // Вычисляемые свойства для проверки наличия элементов
        public bool HasCards => CardsFoundCount > 0;
        public bool HasQuizzes => QuizzesFoundCount > 0;
        public bool HasArticles => ArticlesFoundCount > 0;

        // Вычисляемые свойства для предпросмотра всех найденных элементов
        public ObservableCollection<Card> PreviewCards { get; } = new();
        public ObservableCollection<QuizBlock> PreviewQuizzes { get; } = new();
        public ObservableCollection<Article> PreviewArticles { get; } = new();

        //Счетчики загруженных элементов
        private int _cardsLoaded = 0;
        private int _quizzesLoaded = 0;
        private int _articlesLoaded = 0;

        // 3. Метод CommunityToolkit, который автоматически вызывается при изменении ParsedContentSet
partial void OnParsedContentSetChanged(ParsedContentSet? value)
        {
            // Сбрасываем счетчики и очищаем списки при новом импорте
            _cardsLoaded = 0;
            _quizzesLoaded = 0;
            _articlesLoaded = 0;
            PreviewCards.Clear();
            PreviewQuizzes.Clear();
            PreviewArticles.Clear();

            // Если есть подсказка категории, пробуем её сопоставить или отложить создание
            if (!string.IsNullOrWhiteSpace(value?.CategoryHint))
            {
                _ = MatchOrCreateCategoryAsync(value.CategoryHint);
            }

            // Сразу подгружаем первую порцию для активной вкладки
            LoadNextChunk();

            // Обновляем состояние кнопки импорта
            MainThread.BeginInvokeOnMainThread(() =>
            {
                (ImportCommand as IRelayCommand)?.NotifyCanExecuteChanged();
                OnPropertyChanged(nameof(IsImportEnabled));
            });
        }

        // Также подгружаем первую порцию, если пользователь просто переключил пустую вкладку
        partial void OnIsCardsPreviewActiveChanged(bool value) { if (value && _cardsLoaded == 0) LoadNextChunk(); }
        partial void OnIsQuizzesPreviewActiveChanged(bool value) { if (value && _quizzesLoaded == 0) LoadNextChunk(); }
        partial void OnIsArticlesPreviewActiveChanged(bool value) { if (value && _articlesLoaded == 0) LoadNextChunk(); }

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
        [NotifyPropertyChangedFor(nameof(PreviewCards))]
        public partial bool IsCardsPreviewActive { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsAnyPreviewActive))]
        [NotifyPropertyChangedFor(nameof(PreviewQuizzes))] 
        public partial bool IsQuizzesPreviewActive { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsAnyPreviewActive))]
        [NotifyPropertyChangedFor(nameof(PreviewArticles))]
        public partial bool IsArticlesPreviewActive { get; set; }


        [ObservableProperty]
        public partial bool IsToastVisible { get; set; }

        [ObservableProperty]
        public partial string ToastMessage { get; set; } = string.Empty;


        // Вычисляемое свойство для отображения окна контента (заглушки)
        public bool IsAnyPreviewActive => IsCardsPreviewActive || IsQuizzesPreviewActive || IsArticlesPreviewActive;

        public override void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query.TryGetValue("SelectedCategory", out var categoryObj) && categoryObj is CategoryDraft category)
            {
                SelectedCategory = category;
            }
        }

        private async Task MatchOrCreateCategoryAsync(string categoryName)
        {
            if (string.IsNullOrWhiteSpace(categoryName))
            {
                return;
            }

            var existingCategory = await _repository.GetCategoryByNameAsync(categoryName.Trim());
            if (existingCategory != null)
            {
                SelectedCategory = new CategoryDraft
                {
                    Id = existingCategory.DbId,
                    Name = existingCategory.Name
                };
            }
            else
            {
                SelectedCategory = new CategoryDraft
                {
                    Name = categoryName.Trim()
                };
            }
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

                ShowToast("Код успешно вставлен!");
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
            var currentCategoryName = SelectedCategory?.Name ?? ParsedContentSet?.CategoryHint ?? string.Empty;
            var navigationParameter = new Dictionary<string, object>
            {
                { "CurrentCategoryName", currentCategoryName }
            };

            await Shell.Current.GoToAsync(nameof(CategorySelectionPage), navigationParameter);
        }

        [RelayCommand(CanExecute = nameof(CanImport))]
        public async Task Import()
        {
            IsImporting = true;

            try
            {
                var result = _parser.Parse(InputText);
                ParsedContentSet = result;

                if (SelectedCategory != null)
                {
                    string finalCategoryId = SelectedCategory.Id ?? Guid.NewGuid().ToString("N");

                    if (SelectedCategory.IsNew)
                    {
                        var newCategoryEntity = new CategoryEntity
                        {
                            DbId = finalCategoryId,
                            Name = SelectedCategory.Name
                        };

                        await _repository.SaveCategoryAsync(newCategoryEntity);
                        SelectedCategory.Id = finalCategoryId;
                    }

                    foreach (var article in result.Articles)
                    {
                        article.CategoryId = finalCategoryId;
                    }

                    foreach (var card in result.Cards)
                    {
                        card.CategoryId = finalCategoryId;
                    }

                    foreach (var quiz in result.Quizzes)
                    {
                        quiz.CategoryId = finalCategoryId;
                    }
                }

                await _repository.InsertContentSet(result);
                ShowToast("Контент успешно импортирован! Сейчас вы вернётесь обратно");
                await Task.Delay(1500);
                await GoBack();



            }
            catch (Exception ex)
            {
                
            }
            finally
            {
                IsImporting = false;
            }
        }

        private bool CanImport()
        {
            return !IsImporting && (HasCards || HasQuizzes || HasArticles);
        }

        partial void OnIsImportingChanged(bool value)
        {
            (ImportCommand as IRelayCommand)?.NotifyCanExecuteChanged();
            OnPropertyChanged(nameof(IsImportEnabled));
        }

        // Вспомогательное свойство для привязки состояния кнопки в XAML
        public bool IsImportEnabled => !IsImporting && (HasCards || HasQuizzes || HasArticles);

        private void ShowToast(string message)
        {
            ToastMessage = message;
            IsToastVisible = true;

            // Запускаем таймер в фоне, чтобы не блокировать UI
            Task.Run(async () =>
            {
                await Task.Delay(3000);
                // Обязательно возвращаемся в главный поток для изменения UI
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    IsToastVisible = false;
                });
            });
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

        // 4. Команда подгрузки следующего чанка
        [RelayCommand]
        public void LoadNextChunk()
        {
            if (ParsedContentSet == null) return;

            if (IsCardsPreviewActive && ParsedContentSet.Cards != null && _cardsLoaded < ParsedContentSet.Cards.Count)
            {
                // Берем следующие 5 карточек
                var chunk = ParsedContentSet.Cards.Skip(_cardsLoaded).Take(5).ToList();
                foreach (var item in chunk) PreviewCards.Add(item);
                _cardsLoaded += chunk.Count;
            }
            else if (IsQuizzesPreviewActive && ParsedContentSet.Quizzes != null && _quizzesLoaded < ParsedContentSet.Quizzes.Count)
            {
                // Берем следующие 3 квиза
                var chunk = ParsedContentSet.Quizzes.Skip(_quizzesLoaded).Take(3).ToList();
                foreach (var item in chunk) PreviewQuizzes.Add(item);
                _quizzesLoaded += chunk.Count;
            }
            else if (IsArticlesPreviewActive && ParsedContentSet.Articles != null && _articlesLoaded < ParsedContentSet.Articles.Count)
            {
                // Берем следующие 3 статьи
                var chunk = ParsedContentSet.Articles.Skip(_articlesLoaded).Take(3).ToList();
                foreach (var item in chunk) PreviewArticles.Add(item);
                _articlesLoaded += chunk.Count;
            }
        }
    }
}
