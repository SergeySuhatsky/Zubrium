using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Zubrium.Content.Repository;
using Zubrium.Maui.ViewModels;
using Zubrium.Persistence.Mappers;

namespace Zubrium.Maui.Features.Articles
{
    public partial class ArticleReaderViewModel : BaseViewModel
    {
        [ObservableProperty] 
        public partial string ArticleTitle { get; set; } = string.Empty;

        [ObservableProperty] 
        public partial string ArticleMarkdown { get; set; } = string.Empty;

        public ArticleReaderViewModel(IContentRepository repository) : base(repository)
        {
        }

        public override async void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query.TryGetValue("ArticleId", out var idObj) && idObj is string articleId)
            {
                var articleEntity = await _repository.GetArticleAsync(articleId);
                if (articleEntity != null)
                {
                    var article = articleEntity.ToDomain();
                    ArticleTitle = article.Title;
                    ArticleMarkdown = article.BodyMarkdown;
                }
            }
        }

        [RelayCommand]
        public async Task FinishReading()
        {
            await GoBack();
        }
    }
}
