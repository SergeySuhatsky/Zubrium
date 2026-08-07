using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.Maui.Graphics;
using Zubrium.Content.Repository;
using Zubrium.Domain;
using Zubrium.Maui.ViewModels;
using FSRS.Core.Enums;

namespace Zubrium.Maui.Features.Generals
{
    public partial class SettingsMenuViewModel : BaseViewModel
    {
        private readonly IStudySettings _settings;

        [ObservableProperty] public partial int TargetMasteryDays { get; set; }
        [ObservableProperty] public partial int MinRepetitions { get; set; }
        [ObservableProperty] public partial int DailyNewCardsTarget { get; set; }
        [ObservableProperty] public partial int DailyReviewCardsTarget { get; set; }

        [ObservableProperty]
        public partial double DesiredRetention { get; set; } = 0.90;

        [ObservableProperty]
        public partial ObservableCollection<SimulationStep> SimulationSteps { get; set; } = new();

        [ObservableProperty] public partial string DifficultyPathData { get; set; } = "";
        [ObservableProperty] public partial string StabilityPathData { get; set; } = "";

        public SettingsMenuViewModel(IContentRepository repository, IStudySettings settings) : base(repository)
        {
            _settings = settings;

            TargetMasteryDays = _settings.TargetMasteryDays;
            MinRepetitions = _settings.MinRepetitions;
            DailyNewCardsTarget = _settings.DailyNewCardsTarget;
            DailyReviewCardsTarget = _settings.DailyReviewCardsTarget;
            DesiredRetention = _settings.DesiredRetention;

            // Запускаем пустой симулятор для отрисовки пустых графиков
            RunSimulation();
        }

        partial void OnTargetMasteryDaysChanged(int value) => _settings.TargetMasteryDays = value;
        partial void OnMinRepetitionsChanged(int value) => _settings.MinRepetitions = value;
        partial void OnDailyNewCardsTargetChanged(int value) => _settings.DailyNewCardsTarget = value;
        partial void OnDailyReviewCardsTargetChanged(int value) => _settings.DailyReviewCardsTarget = value;

        partial void OnDesiredRetentionChanged(double value)
        {
            _settings.DesiredRetention = value;
            RunSimulation();
        }

        [RelayCommand]
        public void AddRating(string ratingStr)
        {
            SimulationSteps.Add(new SimulationStep
            {
                RatingName = ratingStr,
                StepNumber = SimulationSteps.Count + 1
            });
            RunSimulation();
        }

        [RelayCommand]
        public void UndoStep()
        {
            if (SimulationSteps.Count > 0)
            {
                SimulationSteps.RemoveAt(SimulationSteps.Count - 1);
                RunSimulation();
            }
        }

        [RelayCommand]
        public void ClearSimulation()
        {
            SimulationSteps.Clear();
            RunSimulation();
        }

        private void RunSimulation()
        {
            var options = new FSRS.Core.Configurations.SchedulerOptions();
            var prop = options.GetType().GetProperty("TargetRetention") ?? options.GetType().GetProperty("DesiredRetention");
            if (prop != null && prop.CanWrite)
            {
                prop.SetValue(options, prop.PropertyType == typeof(float) ? (float)DesiredRetention : DesiredRetention);
            }
            var scheduler = new FSRS.Core.Services.SchedulerFactory(options).CreateScheduler();

            // Создаем чистую карточку, именно так, как требует библиотека (State.Learning и S/D = null)
            var fsrsCard = new FSRS.Core.Models.Card(
                cardId: Guid.NewGuid(),
                state: State.Learning,
                step: 0,
                stability: null,
                difficulty: null,
                due: DateTime.UtcNow,
                lastReview: null
            );

            double maxS = 1.0;

            foreach (var step in SimulationSteps)
            {
                Rating r = step.RatingName switch
                {
                    "Снова" => Rating.Again,
                    "Трудно" => Rating.Hard,
                    "Хорошо" => Rating.Good,
                    "Легко" => Rating.Easy,
                    _ => Rating.Good
                };

                // Выполняем обзор точно в срок (перематываем время к Due)
                var simTime = fsrsCard.Due;
                var (updated, _) = scheduler.ReviewCard(fsrsCard, r, simTime);

                // Передаем обновленное состояние в следующую итерацию
                fsrsCard = updated;

                step.Difficulty = fsrsCard.Difficulty ?? 0;
                step.Stability = fsrsCard.Stability ?? 0;

                maxS = Math.Max(maxS, step.Stability);
            }

            UpdateCharts(maxS);
        }

        private void UpdateCharts(double maxS)
        {
            if (SimulationSteps.Count == 0)
            {
                DifficultyPathData = ""; StabilityPathData = "";
                return;
            }

            var dValues = SimulationSteps.Select(s => s.Difficulty).ToList();
            DifficultyPathData = GeneratePath(dValues, 1, 10);

            // Stability логарифмическая шкала (для визуального удобства)
            double logMinS = 0; // log10(1)
            double logMaxS = Math.Log10(Math.Max(10, maxS * 1.2)); // с запасом
            var sValues = SimulationSteps.Select(s => Math.Log10(Math.Max(1, s.Stability))).ToList();
            StabilityPathData = GeneratePath(sValues, logMinS, logMaxS);

            // Вычисляем пропорциональные координаты для точек (от 0 до 1)
            for (int i = 0; i < SimulationSteps.Count; i++)
            {
                double xProp = SimulationSteps.Count <= 1 ? 0.5 : (double)i / (SimulationSteps.Count - 1);

                double dProp = ScaleY(dValues[i], 1, 10) / 100.0;
                SimulationSteps[i].DifficultyBounds = new Rect(xProp, dProp, 10, 10);

                double sProp = ScaleY(sValues[i], logMinS, logMaxS) / 100.0;
                SimulationSteps[i].StabilityBounds = new Rect(xProp, sProp, 10, 10);
            }
        }

        private string GeneratePath(List<double> values, double minY, double maxY)
        {
            if (values.Count == 1) return $"M 0,{ScaleY(values[0], minY, maxY).ToString(CultureInfo.InvariantCulture)} L 100,{ScaleY(values[0], minY, maxY).ToString(CultureInfo.InvariantCulture)}";

            var sb = new StringBuilder();
            for (int i = 0; i < values.Count; i++)
            {
                double x = (i / (double)(values.Count - 1)) * 100.0;
                double y = ScaleY(values[i], minY, maxY);
                sb.Append(i == 0 ? "M " : "L ");
                sb.Append($"{x.ToString(CultureInfo.InvariantCulture)},{y.ToString(CultureInfo.InvariantCulture)} ");
            }
            return sb.ToString();
        }

        private double ScaleY(double val, double min, double max)
        {
            if (max == min) return 50.0;
            return 100.0 - ((val - min) / (max - min) * 100.0); // Переворачиваем ось Y
        }

        public override void ApplyQueryAttributes(IDictionary<string, object> query) { }
    }

    public partial class SimulationStep : ObservableObject
    {
        [ObservableProperty, NotifyPropertyChangedFor(nameof(ShortTitle))]
        public partial int StepNumber { get; set; }

        [ObservableProperty, NotifyPropertyChangedFor(nameof(ShortTitle)), NotifyPropertyChangedFor(nameof(RatingColor))]
        public partial string RatingName { get; set; } = string.Empty;

        [ObservableProperty, NotifyPropertyChangedFor(nameof(DifficultyText))]
        public partial double Difficulty { get; set; }

        [ObservableProperty, NotifyPropertyChangedFor(nameof(StabilityText))]
        public partial double Stability { get; set; }

        public string ShortTitle => $"{StepNumber}. {RatingName}";
        public string DifficultyText => Difficulty.ToString("0.00");
        public string StabilityText => Stability.ToString("0.00");

        [ObservableProperty] public partial Rect DifficultyBounds { get; set; }
        [ObservableProperty] public partial Rect StabilityBounds { get; set; }

        public Color RatingColor => RatingName switch
        {
            "Снова" => Color.FromArgb("#FF3B30"),
            "Трудно" => Color.FromArgb("#FF9500"),
            "Хорошо" => Color.FromArgb("#34C759"),
            "Легко" => Color.FromArgb("#6278E6"),
            _ => Colors.Gray
        };
    }
}