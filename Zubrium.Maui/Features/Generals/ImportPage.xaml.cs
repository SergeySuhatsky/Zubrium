using Zubrium.Maui.Attributes;

namespace Zubrium.Maui.Features.Generals;

[ShellRoute]
public partial class ImportPage : ContentPage
{
	public ImportPage(ImportViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;

    }

    private void OnScrollViewScrolled(object sender, ScrolledEventArgs e)
    {
        if (sender is ScrollView scrollView)
        {
            // Проверяем: Текущая позиция Y >= (Вся высота контента - Высота видимого окна - 200 пикселей запаса)
            if (scrollView.ScrollY >= scrollView.ContentSize.Height - scrollView.Height - 200)
            {
                if (BindingContext is ImportViewModel vm)
                {
                    // Вызываем команду подгрузки
                    vm.LoadNextChunkCommand.Execute(null);
                }
            }
        }
    }
}