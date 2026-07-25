using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Text;
using Zubrium.Content;

namespace Zubrium.Maui.ViewModels
{
    public partial class BaseViewModel : ObservableObject, IQueryAttributable
    {

        protected readonly IContentRepository _repository;

        public BaseViewModel(IContentRepository repository)
        {
            _repository = repository;
        }

        public virtual async void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            throw new NotImplementedException();
        }
    }
}
