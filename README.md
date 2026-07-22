# Дорожная карта разработки

## Часть 1. MAUI-страницы — режим карточек (без данных)

- [x] 1. Создание solution: `App.Domain`, `App.Content`, `App.Persistence`, `App.Rendering` (существующий), `App.Maui` — отдельные проекты, чтобы Domain/Content тестировались без MAUI-раннера.
- [ ] 2. Подключение NuGet: `Markdig`, `CommunityToolkit.Mvvm`, `CommunityToolkit.Maui`, `sqlite-net-pcl`, тест-проект `xUnit`.
- [ ] 3. `CardStudyPage.xaml` с захардкоженным текстом во всех трёх секциях (front/brief/detailed): никакого биндинга, просто `MarkdownView Text="..."` прямо в XAML, чтобы сразу убедиться, что рендер MarkdownRenderService работает внутри этой страницы.
- [ ] 4. `SwipeableCardView` — кастомный `ContentView` на `PanGestureRecognizer`: слежение за `TranslationX`, наклон карточки пропорционально смещению, визуальные индикаторы «✓» и «✗» по краям, которые появляются при смещении. Логика свайпа полностью захардкожена — события `SwipedLeft`/`SwipedRight` только пишут в консоль.
- [ ] 5. Анимация возврата карточки (`TranslateTo`) при недостаточном смещении.
- [ ] 6. Тап по карточке — переключение brief/detailed: `IsDetailedMode` — локальная переменная прямо во view, `BriefContent.IsVisible` меняется без всякого ViewModel.
- [ ] 7. Кнопка-индикатор «развернуть» поверх карточки, fade-анимация смены brief→detailed.
- [ ] 8. Заглушка прогресса сессии: статичная строка «Карточка 1 из 5» и финальная заглушка-экран «Сессия завершена».

---

## Часть 2. MAUI-страницы — режим квиза (без данных)

- [ ] 9. `QuizPage.xaml` с захардкоженным вопросом и четырьмя вариантами ответа, никакого биндинга.
- [ ] 10. `AnswerOptionView` — `ContentView` с `MarkdownView` внутри для текста варианта (сразу проверяем, что формула в варианте рендерится), `TapGestureRecognizer`.
- [ ] 11. `VisualStateManager` на `AnswerOptionView`: состояния `Normal / Selected / Correct / IncorrectSelected / Dimmed`. Переключение состояний при тапе — локальный код, без ViewModel.
- [ ] 12. Мгновенная подсветка правильного/неправильного ответа при тапе.
- [ ] 13. `explanation`-блок: `MarkdownView`, скрыт до выбора ответа, появляется с fade-анимацией после тапа.
- [ ] 14. Поддержка multi-select: вторая захардкоженная заглушка-вопрос, где выбраны два правильных — проверка, что UI переключается с радио на чекбоксы.
- [ ] 15. Статичный экран результатов квиза: «3 из 5 правильно», кнопка-заглушка «Пройти ошибки заново».

---

## Часть 3. MAUI-страницы — режим статьи (без данных)

- [ ] 16. `ArticlePage.xaml`: `ScrollView` + `MarkdownView` с захардкоженным длинным текстом, включая блочные `$$...$$` формулы, таблицы, заголовки — проверяем типографику и читаемость.
- [ ] 17. Кнопки в конце статьи «Перейти к колоде» / «Перейти к квизу» — пока просто `DisplayAlert("скоро")`.
- [ ] 18. Стилизация типографики статьи: заголовки, отступы между абзацами, таблицы — отдельно от компактного стиля карточек.

**🏁 Чекпоинт A — UI-прототип.**
Все три экрана полностью верстаны и анимированы: свайпы на карточках с визуальным фидбэком, переключение brief/detailed, подсветка ответов в квизе, читаемая статья с формулами. Никаких реальных данных — все тексты захардкожены в XAML. Можно показывать дизайн и собирать фидбэк по UX.

---

## Часть 4. Domain-модели и DSL-парсер (без UI, без БД)

- [ ] 19. Domain-модели: `Card`, `QuizQuestion`, `AnswerOption`, `Article`, `Category`, `CategoryResolution`, `ParsedContentSet`, `ImportableCard/Quiz/Article`, `ParseWarning`. Чистые POCO + базовые equality-тесты.
- [ ] 20. `ContentMarkdownPipeline.cs` — сборка Markdig pipeline (`CustomContainers`, `TaskLists`, `GenericAttributes`, `YamlFrontMatter`, `PipeTables`). Тест: pipeline парсит «пустой» валидный файл без исключений.
- [ ] 21. `SectionSplitterBlockParser` — кастомный `IBlockParser` для `--- brief / --- detailed / --- explanation`. Юнит-тесты: только front, front+brief, front+brief+detailed, detailed без brief (должно давать `ParseWarning`, не исключение).
- [ ] 22. `FrontMatterReader` — извлечение `category/title/tags` из YAML. Тест с unicode/emoji в `category`.
- [ ] 23. `CardBuilder` — сборка `Card` из `CustomContainer`, вырезание markdown-подстрок по `Span`. Тесты на карточку без `#id`, с `#id`, с картинкой в front/brief/detailed.
- [ ] 24. `QuizBuilder` — сборка `QuizQuestion`/`AnswerOption` из task-list. Тесты: 1 правильный ответ, 2+ правильных (multi-select), 0 правильных (warning, пропускается).
- [ ] 25. `ArticleBuilder` — сборка `Article` с резолвом атрибутов `deck=`/`quiz=` на `#id` внутри файла. Тест на битую ссылку.
- [ ] 26. `DeckSourceParser.Parse(string)` — единая точка входа. Edge-case тесты: файл без frontmatter, дублирующиеся `#id`, вложенные контейнеры (ошибка), смешанный файл card+quiz+article.
- [ ] 27. `ContentFingerprint` (SHA256 по нормализованному тексту) — юнит-тесты на идентичный/изменённый контент.

---

## Часть 5. Персистентность

- [ ] 28. `AppDb.cs` на `sqlite-net-pcl`: схема `CategoryEntity`, `CardEntity`, `CardReviewStateEntity`, `QuizEntity`, `AnswerOptionEntity`, `QuizAttemptEntity`, `ArticleEntity` — все таблицы с fingerprint-колонкой.
- [ ] 29. `IContentRepository` интерфейс: `FindCategoryByNameAsync`, `CreateCategoryAsync`, `GetCategoriesAsync`, `SaveContentAsync`, `GetDueCardsAsync`, `GetCardsByCategoryAsync` и т.п.
- [ ] 30. `SqliteContentRepository.SaveContentAsync(...)` — транзакционная запись карточек/квизов/статей с привязкой к `categoryId`.
- [ ] 31. `DuplicateChecker.CheckAsync(ParsedContentSet)` — один батч-запрос к БД, проставление `IsDuplicate` на `ImportableX`.
- [ ] 32. Интеграционные тесты репозитория на in-memory sqlite: создание категории, сохранение карточек, повторный импорт того же файла → корректная дедупликация.

---

## Часть 6. Подключение данных к UI

- [ ] 33. Сидирование тестовых данных: 2-3 `.md`-файла в `Assets/SeedData`, загружаются при первом запуске в БД (временный код, удалится после части 9).
- [ ] 34. `CardStudyViewModel` — заменяем захардкоженный текст на биндинг: `Card.FrontMarkdown`, `Card.BriefBackMarkdown`, `Card.DetailedBackMarkdown` из БД. Карточки берутся из `GetDueCardsAsync` вместо статичного списка.
- [ ] 35. Интеграция `SwipedLeft`/`SwipedRight` с `Sm2SchedulingService` вместо `Console.WriteLine`.
- [ ] 36. `ISchedulingAlgorithm`/`Sm2SchedulingService` — реализация SM-2, юнит-тесты на интервалы (easy/repeat, граничные EaseFactor).
- [ ] 37. Счётчик прогресса и экран завершения сессии — теперь живые данные вместо заглушек.
- [ ] 38. `QuizViewModel` — биндинг `QuizQuestion.QuestionMarkdown` и `AnswerOption.TextMarkdown`, логика проверки, `ExplanationMarkdown` после ответа.
- [ ] 39. Запись `QuizAttempt` в БД при завершении квиза, живой счётчик правильных ответов.
- [ ] 40. `ArticleViewModel` — биндинг `Article.BodyMarkdown`, резолв `LinkedDeckId`/`LinkedQuizId` в реальные записи БД, переходы по кнопкам «К колоде» / «К квизу».

**🏁 Чекпоинт B — Полный функциональный прототип на сидированных данных.**
Все три режима работают с реальными данными из БД, SM-2 считает интервалы, переходы между экранами настоящие. Единственное, чего нет — способа добавить свой контент. Можно отдавать тестировщикам.

---

## Часть 7. Импорт — базовый текстовый flow

- [ ] 41. `ImportPage`: табы «File» / «Paste text», `FilePicker` для `.md`.
- [ ] 42. Live-парсинг вкладки Paste text: `TextChanged` → debounce 500мс через `CancellationTokenSource` → фоновый `DeckSourceParser.Parse`.
- [ ] 43. `ImportSettingsPage`: счётчики по типам (`Cards.Count`, `Quizzes.Count`, `Articles.Count`) из `ParsedContentSet`.
- [ ] 44. `CategoryPickerPopup` (`CommunityToolkit.Maui.Popup`): список существующих категорий + поле «New category».
- [ ] 45. Автодетект категории: при открытии экрана `CategoryHint` сверяется с БД → `Existing`/`Pending` без записи в БД.
- [ ] 46. Три чекбокса Include Cards/Quiz/Article с инвертированной логикой кнопки Preview (отмеченные типы деактивируют Preview).
- [ ] 47. `ImportButtonLabel` computed property + `CanExecute` (категория выбрана + хотя бы один чекбокс).

---

## Часть 8. Превью и дедупликация

- [ ] 48. `ContentPreviewPage`: горизонтальный `CollectionView` миниатюр (`LinearItemsLayout(Horizontal)`) + detail-панель под ним, переключаемая по `SelectedItem`.
- [ ] 49. Шаблоны превью для трёх типов: Card (front + toggle brief/detailed), Quiz (вопрос + варианты), Article (полный текст).
- [ ] 50. Интеграция `DuplicateChecker`: бейдж «Already exists», `IsExcluded` тоггл, состояния `Duplicate/DuplicateEnabled` в `VisualStateManager`.
- [ ] 51. Отображение `ParseWarning` как мягких предупреждений (дубль `#id`, квиз без правильного ответа, битая ссылка) — список под превью, не блокирует импорт остального контента.

**🏁 Чекпоинт C — Полный пользовательский цикл без медиа.**
Пользователь может сам написать `.md` по документации синтаксиса, импортировать его, проверить превью, разрешить дубликаты, выбрать категорию и начать учиться. Это уже законченное приложение для текстового контента.

---

## Часть 9. Изображения и ZIP-пакеты

- [ ] 52. `IContentSource` абстракция, рефакторинг `FilePicker`-кода под `PlainMarkdownFileSource`/`PastedTextSource`.
- [ ] 53. `ZipPackageSource`: распаковка во временный staging (`CacheDirectory/import-staging/{guid}`), поиск `.md` в корне, конкатенация при нескольких файлах.
- [ ] 54. `StagingImageResolver` + подключение в рендер превью — картинки видны уже на этапе предпросмотра.
- [ ] 55. Лимиты размера архива и одной картинки, понятная ошибка в UI при превышении.
- [ ] 56. `MediaCommitService.CommitMediaAsync` — перенос файлов из staging в `AppDataDirectory/media/{contentId}` строго внутри транзакции `ImportAsync`.
- [ ] 57. `PersistedImageResolver` — подмена резолвера после импорта во всех трёх режимах изучения.
- [ ] 58. Очистка staging-каталога при отмене импорта (`Directory.Delete(recursive: true)`).
- [ ] 59. Визуальное QA: при отсутствии `![]()` в секции `Image`-контрол не создаётся, текст занимает всю высоту без пустых отступов — проверка в каждом из трёх режимов.
- [ ] 60. Удаление seed-кода из `MauiProgram.cs` — теперь он больше не нужен.

**🏁 Чекпоинт D — Полноценный мультимедийный прототип.**
Поддержан полный мультимедийный контент: текст, математика, изображения из `.md` и `.zip`, во всех режимах, с корректным освобождением места под текст.

---

## Часть 10. Навигация и каркас приложения

- [ ] 61. `AppShell`: Home (список категорий) → Category detail (список колод/квизов/статей) → конкретный режим изучения.
- [ ] 62. Главный экран: сколько карточек due сегодня, streak, общее количество контента по категориям.
- [ ] 63. Удаление и редактирование категорий/контента с подтверждением и cascade-удалением `CardReviewState`/`QuizAttempt`.
- [ ] 64. Поиск по контенту (LIKE-запрос по тексту карточек/квизов).

---

## Часть 11. Полировка UX

- [ ] 65. Единая дизайн-система (цвета, типографика, отступы) для всех трёх режимов, светлая/тёмная тема.
- [ ] 66. Анимации переходов между экранами Shell, более плавные транзишены свайпа.
- [ ] 67. Haptic feedback на свайпах и на правильном/неправильном ответе в квизе.
- [ ] 68. Пустые состояния: «Нет карточек на сегодня», «Категория пуста», «Нет результатов поиска».
- [ ] 69. Человекочитаемая обработка ошибок парсинга: toast/alert вместо raw exception, список `ParseWarning` в UI.

---

## Часть 12. Тестирование, производительность, релиз

- [ ] 70. Покрытие unit-тестами парсера/дедупа/SM-2 до целевого порога; интеграционные тесты репозитория.
- [ ] 71. UI-тесты ключевых экранов (импорт end-to-end, свайп-сессия).
- [ ] 72. Профилирование рендера больших файлов (100+ карточек): виртуализация `CollectionView`, ленивый парсинг по требованию.
- [ ] 73. Подготовка релизных сборок Android/iOS: иконки, splash, permissions для `FilePicker`/zip.
- [ ] 74. Бета-тестирование, фикс по приоритету (импорт > изучение > полировка).
- [ ] 75. Релиз 1.0.

