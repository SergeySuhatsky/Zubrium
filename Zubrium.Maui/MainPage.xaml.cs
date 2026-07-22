using Zubrium.Maui.Services.MarkdownRender;

namespace Zubrium.Maui
{
    public partial class MainPage : ContentPage
    {


        private readonly double _swipeThreshold = 120;

        public MainPage()
        {

            InitializeComponent();

            // Рендеру начальный markdown
            OnRenderClicked(this, EventArgs.Empty);
        }


        private void OnRenderClicked(object sender, EventArgs e)
        {
            MarkdownRenderer1.Text = MarkdownInput.Text;
        }


        private async void OnCardPanUpdated(object sender, PanUpdatedEventArgs e)
        {
            var card = sender as View;
            if (card == null) return;

            switch (e.StatusType)
            {
                case GestureStatus.Running:
                    // 1. Двигаем карточку за пальцем
                    card.TranslationX = e.TotalX;
                    card.TranslationY = e.TotalY;

                    // 2. Добавляем легкое вращение в зависимости от сдвига по оси X
                    card.Rotation = e.TotalX * 0.05;
                    break;

                case GestureStatus.Completed:
                    // Пользователь отпустил карточку
                    if (Math.Abs(card.TranslationX) > _swipeThreshold)
                    {
                        // Карточка ушла достаточно далеко — делаем финальный свайп
                        await SwipeOutCard(card, card.TranslationX > 0);
                    }
                    else
                    {
                        // Сдвиг слишком маленький — возвращаем карточку в центр
                        await ResetCardPosition(card);
                    }
                    break;
            }
        }

        private async Task SwipeOutCard(View card, bool isSwipeRight)
        {
            // Вычисляем, куда должна улететь карточка (за пределы экрана)
            double screenWidth = DeviceDisplay.MainDisplayInfo.Width / DeviceDisplay.MainDisplayInfo.Density;
            double finalX = isSwipeRight ? screenWidth : -screenWidth;

            // Анимируем уход за экран
            await card.TranslateTo(finalX, card.TranslationY, 250, Easing.Linear);

            // Скрываем карточку (в реальном приложении здесь вы бы удалили ее из коллекции или подгрузили следующую)
            card.IsVisible = false;

            // Обработка результата
            if (isSwipeRight)
                Console.WriteLine("Свайп вправо (Лайк)!");
            else
                Console.WriteLine("Свайп влево (Дизлайк)!");
        }

        private async Task ResetCardPosition(View card)
        {
            // Запускаем две анимации параллельно: возврат позиции и сброс поворота
            await Task.WhenAll(
                card.TranslateTo(0, 0, 250, Easing.SpringOut),
                card.RotateTo(0, 250, Easing.SpringOut)
            );
        }

    }
}
