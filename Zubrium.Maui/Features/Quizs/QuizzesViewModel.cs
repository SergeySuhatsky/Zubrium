using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Text;
using Zubrium.Content;
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
        public async void Import()
        {
            App.Current.MainPage.DisplayAlert("Радыфв","sd","sda");
        }
    }
}
