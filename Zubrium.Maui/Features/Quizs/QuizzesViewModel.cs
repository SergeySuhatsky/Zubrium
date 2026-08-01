using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Text;
using Zubrium.Content.Repository;
using Zubrium.Maui.ViewModels;

namespace Zubrium.Maui.Features.Quizs
{
    public partial class QuizzesViewModel : BaseViewModel
    {

        public QuizzesViewModel(IContentRepository repository) : base(repository)
        {
        }

        public override void ApplyQueryAttributes(IDictionary<string, object> query)
        {

        }

        [RelayCommand]
        public async Task Import()
        {
            await Shell.Current.GoToAsync(nameof(Generals.ImportPage));
        }
    }
}
