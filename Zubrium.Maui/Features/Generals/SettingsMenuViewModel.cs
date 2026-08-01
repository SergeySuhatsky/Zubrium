using System;
using System.Collections.Generic;
using System.Text;
using Zubrium.Content.Repository;
using Zubrium.Maui.ViewModels;

namespace Zubrium.Maui.Features.Generals
{
    public partial class SettingsMenuViewModel :BaseViewModel
    {
        public SettingsMenuViewModel(IContentRepository repository) : base(repository)
        {
        }

        public override void ApplyQueryAttributes(IDictionary<string, object> query)
        {

        }
    }
}
