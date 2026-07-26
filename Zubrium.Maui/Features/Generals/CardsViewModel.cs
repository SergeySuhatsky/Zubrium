using System;
using System.Collections.Generic;
using System.Text;
using Zubrium.Content;
using Zubrium.Maui.ViewModels;

namespace Zubrium.Maui.Features.Generals
{
    public partial class CardsViewModel : BaseViewModel
    {

        public CardsViewModel(IContentRepository repository) : base(repository)
        {
        }

        public override void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            
        }
    }
}
