using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Zubrium.Content.Repository;
using Zubrium.Persistence.Entities;
using Zubrium.Maui.ViewModels;

namespace Zubrium.Maui.Features.Generals
{
    public partial class CategorySelectionViewModel : BaseViewModel
    {
        private List<CategoryEntity> _allDbCategories = new();

        [ObservableProperty]
        public partial string SearchText { get; set; } = string.Empty;

        [ObservableProperty]
        public partial ObservableCollection<CategoryDraft> Categories { get; set; } = new();

        public CategorySelectionViewModel(IContentRepository repository) : base(repository)
        {
        }

        public override async void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query.TryGetValue("CurrentCategoryName", out var nameObj) && nameObj is string name)
            {
                SearchText = name;
            }

            await LoadCategoriesAsync();
        }

        private async Task LoadCategoriesAsync()
        {
            _allDbCategories = await _repository.GetAllCategoriesAsync();
            FilterCategories();
        }

        partial void OnSearchTextChanged(string value)
        {
            FilterCategories();
        }

        private void FilterCategories()
        {
            Categories.Clear();
            var query = SearchText?.Trim() ?? string.Empty;
            bool exactMatchFound = false;

            foreach (var category in _allDbCategories.OrderBy(c => c.Name))
            {
                if (string.IsNullOrEmpty(query) || category.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
                {
                    Categories.Add(new CategoryDraft
                    {
                        Id = category.DbId,
                        Name = category.Name
                    });

                    if (string.Equals(category.Name, query, StringComparison.OrdinalIgnoreCase))
                    {
                        exactMatchFound = true;
                    }
                }
            }

            if (!exactMatchFound && !string.IsNullOrWhiteSpace(query))
            {
                Categories.Insert(0, new CategoryDraft
                {
                    Name = query.Trim()
                });
            }
        }

        [RelayCommand]
        public async Task Select(CategoryDraft selected)
        {
            if (selected == null) return;

            var navigationParameter = new Dictionary<string, object>
            {
                { "SelectedCategory", selected }
            };

            await Shell.Current.GoToAsync("..", navigationParameter);
        }
    }
}
