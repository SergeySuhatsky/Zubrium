using System;
using System.Collections.Generic;
using System.Text;

namespace Zubrium.Tests.Parsing
{
    internal static class TestFixtures
    {
        // ---------- CARD ----------

        public const string CardWithTitleAttribute = """
            ::: card {title="Открытое множество"}
            Что называют открытым множеством?

            --- brief
            Любое множество из топологии.

            --- detailed
            Развёрнутое определение через топологию τ.
            :::
            """;

        public const string CardWithH1Fallback = """
            ::: card
            # Гомеоморфизм

            Когда пространства гомеоморфны?

            --- brief
            Когда есть непрерывная биекция с непрерывным обратным.

            --- detailed
            Три условия: биекция, непрерывность, непрерывность обратного.
            :::
            """;

        public const string CardWithoutTitleAndWithoutH1 = """
            ::: card
            Просто вопрос без заголовка и без front-matter title.

            --- brief
            Просто ответ.
            :::
            """;

        public const string CardWithoutDetailed = """
            ::: card {title="Без детального блока"}
            Вопрос без детального пояснения.

            --- brief
            Краткий ответ.
            :::
            """;

        public const string CardWithoutBrief = """
            ::: card {title="Без brief"}
            Вопрос сразу с детальным блоком, без brief.

            --- detailed
            Развёрнутый ответ без краткого.
            :::
            """;

        // То же самое, что CardWithTitleAttribute, но с CRLF-переводами строк —
        // имитация файла, сохранённого в Windows.
        public static readonly string CardWithCrlfLineEndings =
            CardWithTitleAttribute.Replace("\n", "\r\n");

        // ---------- QUIZ ----------

        public const string QuizWithTwoQuestions = """
            ::: quiz {title="Проверка знаний"}

            ## Первый вопрос?

            - [x] Верно
            - [ ] Неверно

            --- explanation
            Пояснение к первому вопросу.

            ---

            ## Второй вопрос?

            - [ ] Вариант A
            - [x] Вариант B
            - [ ] Вариант C

            :::
            """;

        // Второй quiz-блок без явного title — чтобы проверить, откуда
        // реально берётся H2 при поиске заголовка по умолчанию.
        public const string TwoQuizzesSecondWithoutTitleAttribute = """
            ::: quiz {title="Первый квиз"}

            ## Вопрос из первого квиза?

            - [x] Да
            - [ ] Нет

            :::

            ::: quiz
            ## Вопрос из второго квиза?

            - [x] Да
            - [ ] Нет

            :::
            """;

        // ---------- ARTICLE ----------

        public const string ArticleWithTitleAttribute = """
            ::: article {title="Заголовок из атрибута"}
            # Другой заголовок внутри текста

            Текст статьи.
            :::
            """;

        public const string ArticleWithH1Fallback = """
            ::: article
            # Заголовок статьи

            Текст статьи с формулой $\pi_1(S^1) \cong \mathbb{Z}$.
            :::
            """;

        // ---------- FULL DECK SOURCE (как реальный файл) ----------

        public const string FullSampleDeckSource = """
            ---
            category: Топология 🔢
            ---

            ::: card {title="Открытое множество"}
            Что называют открытым множеством?

            --- brief
            Любое множество из топологии.

            --- detailed
            Развёрнутое определение.
            :::

            ::: card
            # Гомеоморфизм

            Когда пространства гомеоморфны?

            --- brief
            Когда есть непрерывная биекция.
            :::

            ::: quiz {title="Итоговый квиз"}

            ## Вопрос номер один?

            - [x] Да
            - [ ] Нет

            --- explanation
            Потому что.

            ---

            ## Вопрос номер два?

            - [ ] A
            - [x] B

            :::

            ::: article {title="Статья про топологию"}
            # Статья про топологию

            Немного текста.
            :::
            """;
    }
}
