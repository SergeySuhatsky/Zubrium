using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Text;
using Zubrium.Content;
using Zubrium.Maui.ViewModels;

namespace Zubrium.Maui.Features.Generals
{
    public partial class StudyViewModel : BaseViewModel
    {

        public StudyViewModel(IContentRepository repository) : base(repository)
        {
        }

        public override void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            
        }

        [RelayCommand]
        public async Task TakeQuiz() 
        {
            await Shell.Current.GoToAsync(nameof(Quizs.PickQuizPage));
        }
    }
}
