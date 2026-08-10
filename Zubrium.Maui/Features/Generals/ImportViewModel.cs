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

        [ObservableProperty]
        public partial bool IsPromptOverlayVisible { get; set; }

        public string PromptText => "\r\n\r\nТвоя задача: создать образовательный контент для пользователя на основе Markdown синтаксиса. \r\nТы можешь создавать карточки для интервального повторения, квизы и статьи.\r\n**Глобальные правила синтаксиса:**\r\n1. **Метаданные:** В самом начале документа всегда должен быть блок YAML Frontmatter с указанием категории контента.\r\nПример:\r\n\r\n\r\n```yaml\r\n---\r\ncategory: Название категории\r\n---\r\n\r\n```\r\n\r\n\r\n2. **Контейнеры:** Основные логические блоки должны быть обернуты в кастомные контейнеры с помощью трех двоеточий (`:::`).\r\n\r\n\r\n3. **Атрибуты:** Для каждого контейнера можно задать ID и заголовок в формате `{#id title=\"Название\"}` сразу после открывающего тега.\r\n\r\n\r\n4. **Таблицы:** Внутри текста разрешено использовать стандартные Markdown-таблицы (Pipe Tables).\r\n\r\n5. **Возможности**: поддерживаются выделение слов, Latex выражения и таблицы\r\n\r\n\r\n\r\n**Типы поддерживаемых блоков и их правила:**\r\n**1. Блок Статьи (article)**\r\n\r\n* Оборачивается в `::: article ... :::`.\r\n\r\n\r\n* Если в атрибутах контейнера не указан `title`, первая значимая строка текста должна быть заголовком первого уровня (`# Название статьи`), парсер возьмет название оттуда.\r\n\r\n\r\n\r\n\r\n**2. Блок Карточки (card)**\r\n\r\n* Оборачивается в `::: card ... :::`.\r\n\r\n\r\n* Текст до первого разделителя — это лицевая сторона (вопрос). Из нее же берется заголовок карточки по первому тегу `#` (если нет атрибута `title`).\r\n\r\n\r\n* Маркер `--- brief` на отдельной строке отделяет краткий ответ.\r\n\r\n\r\n* Маркер `--- detailed` (опционально) на отдельной строке отделяет подробный ответ или объяснение.\r\n\r\n\r\n\r\n\r\n**3. Блок Квиза (quiz)**\r\n\r\n* Оборачивается в `::: quiz ... :::`.\r\n\r\n\r\n* Если в атрибутах нет `title`, используй заголовок второго уровня (`## Название квиза`) внутри текста, он станет названием.\r\n\r\n\r\n* Вопросы отделяются друг от друга строго тремя дефисами на отдельной строке (`---`).\r\n\r\n\r\n* Варианты ответов помечаются как Markdown-чекбоксы: `- [ ]` для неверных ответов и `- [x]` или `- [X]` для верных.\r\n\r\n\r\n* Многострочный текст ответа приклеивается к предыдущему варианту, если между ними нет пустых строк.\r\n\r\n\r\n* Маркер `--- explanation` в конце вопроса (до следующего разделителя `---`) позволяет добавить объяснение для пользователя.\r\n\r\n\r\n\r\nТы должен дать ответ в виде единого блока кода. \r\n\r\nПример синтаксиса, ты не обязан следовать его в плане наполения контента самой карточки:\r\n\r\n```\r\n---\r\ncategory: Топология\r\n---\r\n\r\n::: card {title=\"Открытое множество\"}\r\nЧто называют открытым множеством\r\nв топологическом пространстве $(X, \\tau)$?\r\n\r\n--- brief\r\nЛюбое множество $U \\in \\tau$ (элемент топологии).\r\n\r\n--- detailed\r\nТопология $\\tau$ на $X$ — семейство подмножеств,\r\nсодержащее $\\emptyset$ и $X$, замкнутое относительно\r\nпроизвольных объединений и конечных пересечений.\r\nЭлементы $\\tau$ по определению и называют открытыми.\r\n:::\r\n\r\n\r\n<!-- title берётся из первого # heading, атрибут не нужен -->\r\n\r\n::: card \r\nГомеоморфизм\r\n\r\nКогда два пространства $X$ и $Y$ называют **гомеоморфными**?\r\n![иллюстрация](images/homeo.png)\r\n\r\n--- brief\r\nКогда между ними существует непрерывная биекция\r\nс непрерывным обратным отображением.\r\n\r\n--- detailed\r\nОтображение $f: X \\to Y$ является гомеоморфизмом, если:\r\n\r\n1. $f$ — биекция\r\n2. $f$ непрерывна\r\n3. $f^{-1}$ непрерывна\r\n\r\nГомеоморфные пространства **топологически неразличимы** —\r\nкружка и бублик, квадрат и окружность.\r\n:::\r\n\r\n\r\n<!-- =========================================================\r\n     КВИЗ-БЛОК\r\n     Один ::: quiz = один блок с несколькими вопросами.\r\n     Вопросы разделяются голым --- (ThematicBreak в Markdig).\r\n     --- explanation — секция объяснения, НЕ разделитель.\r\n     ========================================================= -->\r\n\r\n::: quiz {title=\"Основы топологии — проверка знаний\"}\r\n\r\nЯвляется ли окружность $S^1$ гомеоморфной квадрату?\r\n\r\n- [x] Да\r\n- [ ] Нет\r\n- [ ] Только при определённой метрике\r\n\r\n--- explanation\r\nОба — компактные односвязные многообразия размерности 1.\r\nГомеоморфизм между ними существует, хотя явно построить\r\nего не так просто.\r\n\r\n---\r\n\r\nПусть $d(x,y) = |x-y|^2$ для $x,y \\in \\mathbb{R}$.\r\nЯвляется ли $d$ метрикой?\r\n\r\n- [ ] Да, удовлетворяет всем аксиомам\r\n- [x] Нет, нарушается неравенство треугольника\r\n- [ ] Нет, нарушается симметричность\r\n\r\n--- explanation\r\nКонтрпример: $d(0,2)=4$, но\r\n$d(0,1)+d(1,2) = 1+1 = 2 < 4$.\r\nНеравенство треугольника нарушено.\r\n\r\n---\r\n\r\nЧто из перечисленного является топологией\r\nна $X = \\{a, b, c\\}$?\r\n\r\n- [ ] $\\tau = \\{\\emptyset,\\, \\{a\\},\\, \\{b\\},\\, X\\}$\r\n- [x] $\\tau = \\{\\emptyset,\\, \\{a\\},\\, \\{a,b\\},\\, X\\}$\r\n- [ ] $\\tau = \\{\\{a\\},\\, \\{b,c\\},\\, X\\}$\r\n\r\n--- explanation\r\n**Первый** нарушает замкнутость объединений:\r\n$\\{a\\} \\cup \\{b\\} = \\{a,b\\} \\notin \\tau$.\r\n\r\n**Второй** корректен: все объединения и пересечения\r\nэлементов $\\tau$ остаются в $\\tau$.\r\n\r\n**Третий** не содержит $\\emptyset$.\r\n\r\n:::\r\n\r\n\r\n<!-- =========================================================\r\n     СТАТЬЯ\r\n     title= опционален — иначе из первого # в body.\r\n     ========================================================= -->\r\n\r\n::: article {title=\"Что такое топология и зачем она нужна\"}\r\n\r\n# Что такое топология и зачем она нужна\r\n\r\nТопология изучает **свойства пространств**, сохраняющиеся\r\nпри непрерывных деформациях — растяжениях, сжатиях,\r\nизгибах, но не при разрывах и склейках.\r\n\r\n## Интуиция через пластилин\r\n\r\nПредставьте, что все фигуры сделаны из пластилина.\r\nКружку можно деформировать в бублик — у обоих одна дырка.\r\nНо шар в бублик превратить нельзя: пришлось бы рвать.\r\n\r\nИменно это «нельзя» и изучает топология.\r\n\r\n## Формальное определение\r\n\r\nПара $(X, \\tau)$ называется **топологическим пространством**,\r\nесли $\\tau \\subseteq 2^X$ удовлетворяет аксиомам:\r\n\r\n$$\\emptyset \\in \\tau,\\quad X \\in \\tau$$\r\n$$U_\\alpha \\in \\tau \\Rightarrow \\bigcup_\\alpha U_\\alpha \\in \\tau$$\r\n$$U, V \\in \\tau \\Rightarrow U \\cap V \\in \\tau$$\r\n\r\n## Фундаментальная группа\r\n\r\nОдним из главных инвариантов является фундаментальная группа.\r\nДля окружности:\r\n\r\n$$\\pi_1(S^1) \\cong \\mathbb{Z}$$\r\n\r\nЭто означает, что петли вокруг окружности классифицируются\r\nцелым числом — количеством оборотов.\r\n\r\n:::\r\n```\r\n\r\n\r\n";
        [RelayCommand]
        public void TogglePromptOverlay() => IsPromptOverlayVisible = !IsPromptOverlayVisible;

        [RelayCommand]
        public async Task CopyPrompt()
        {
            await Clipboard.Default.SetTextAsync(PromptText);
            TogglePromptOverlay();
            ShowToast("Промпт скопирован в буфер обмена!");
        }

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
