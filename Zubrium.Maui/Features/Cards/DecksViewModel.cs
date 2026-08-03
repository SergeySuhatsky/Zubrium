using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Text;
using Zubrium.Content.Repository;
using Zubrium.Domain;
using Zubrium.Maui.ViewModels;

namespace Zubrium.Maui.Features.Cards;

public partial class DecksViewModel : BaseViewModel
{
    [ObservableProperty]
    public partial string Title { get; set; } = "sdads";

    [ObservableProperty]
    public partial string BodyMarkdown { get; set; } = "asdasadca $x^3+2/5$ \n asdas dsve";

    public DecksViewModel(IContentRepository repository): base(repository) { }

    public override async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("articleId", out var articleIdObj) && articleIdObj is string articleId)
        {
            var article = await _repository.GetArticleAsync(articleId);

            Title = article.Title;
            BodyMarkdown = article.BodyMarkdown;
        }
    }


    [RelayCommand]
    public async Task LoadArticle(string articleId)
    {
        var article = await _repository.GetArticleAsync("2");

        Title = article.Title;
        BodyMarkdown = article.BodyMarkdown;

    }

}
