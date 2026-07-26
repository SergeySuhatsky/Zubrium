using System;
using System.Collections.Generic;
using System.Text;
using Zubrium.Content;
using Zubrium.Maui.ViewModels;

namespace Zubrium.Maui.Features.Quizs
{
    public partial class PickQuizViewModel: BaseViewModel
    {

        public PickQuizViewModel(IContentRepository repository) : base(repository)
        {
        }

        public override void ApplyQueryAttributes(IDictionary<string, object> query)
        {

        }
    }
}
