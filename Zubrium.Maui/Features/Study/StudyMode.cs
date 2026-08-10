namespace Zubrium.Maui.Features.Study
{
    public enum StudyMode
    {
        NewCards,   // Только новые
        Review,     // Только повторение
        Mixed       // Смешанный режим
    }

    public enum StudyCardPhase
    {
        Discovery,  // Этап 1: Знакомство (Свайп влево = Уже знаю)
        Review,     // Этап 2: Изучение (Свайп влево = Good)
        LocalQueue  // Микро-очередь сессии
    }
}
