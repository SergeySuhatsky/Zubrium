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
        private List<string> _preSelectedIds = new(); // Для запоминания отмеченных галочек

        [ObservableProperty]
        public partial string SearchText { get; set; } = string.Empty;

        [ObservableProperty]
        public partial ObservableCollection<CategoryDraft> Categories { get; set; } = new();

        [ObservableProperty]
        public partial bool IsMultiSelect { get; set; } // Флаг режима

        public CategorySelectionViewModel(IContentRepository repository) : base(repository)
        {
        }

        public override async void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query.TryGetValue("CurrentCategoryName", out var nameObj) && nameObj is string name)
                SearchText = name;

            if (query.TryGetValue("IsMultiSelect", out var multiObj) && multiObj is bool multi)
                IsMultiSelect = multi;

            if (query.TryGetValue("PreSelectedIds", out var preSelObj) && preSelObj is List<string> preSelected)
                _preSelectedIds = preSelected;

            await LoadCategoriesAsync();
        }

        private async Task LoadCategoriesAsync()
        {
            _allDbCategories = await _repository.GetAllCategoriesAsync();
            FilterCategories();
        }

        partial void OnSearchTextChanged(string value) => FilterCategories();

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
                        Name = category.Name,
                        IsSelected = _preSelectedIds.Contains(category.DbId) // Восстанавливаем галочку
                    });

                    if (string.Equals(category.Name, query, StringComparison.OrdinalIgnoreCase))
                        exactMatchFound = true;
                }
            }

            // Предлагаем создать новую только в одиночном режиме (для импорта)
            if (!exactMatchFound && !string.IsNullOrWhiteSpace(query) && !IsMultiSelect)
            {
                Categories.Insert(0, new CategoryDraft { Name = query.Trim() });
            }
        }

        [RelayCommand]
        public async Task Select(CategoryDraft selected)
        {
            if (selected == null) return;

            if (IsMultiSelect)
            {
                // В режиме мультивыбора клик по категории просто ставит/снимает галочку
                selected.IsSelected = !selected.IsSelected;
            }
            else
            {
                // В одиночном режиме клик сразу возвращает результат (как было раньше)
                var navigationParameter = new Dictionary<string, object> { { "SelectedCategory", selected } };
                await Shell.Current.GoToAsync("..", navigationParameter);
            }
        }

        [RelayCommand]
        public async Task ApplySelection()
        {
            // Кнопка "Применить" доступна только в режиме мультивыбора
            var selectedList = Categories.Where(c => c.IsSelected && !c.IsNew).ToList();
            var navigationParameter = new Dictionary<string, object>
            {
                { "SelectedCategories", selectedList }
            };
            await Shell.Current.GoToAsync("..", navigationParameter);
        }
    }
}
