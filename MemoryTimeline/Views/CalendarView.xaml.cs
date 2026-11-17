using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MemoryTimeline.Models;
using MemoryTimeline.ViewModels;

namespace MemoryTimeline.Views;

public partial class CalendarView : UserControl
{
    private readonly CalendarViewModel _vm;

    public CalendarView(CalendarViewModel vm)
    {
        _vm = vm;
        DataContext = _vm;
        InitializeComponent();
        Loaded += async (s, e) => await _vm.LoadAsync();

        YearViewBtn.Click += async (s, e) =>
        {
            SetViewButtons("Year");
            await _vm.SwitchViewCommand.ExecuteAsync("Year");
            UpdatePeriodLabel();
        };
        MonthViewBtn.Click += async (s, e) =>
        {
            SetViewButtons("Month");
            await _vm.SwitchViewCommand.ExecuteAsync("Month");
            UpdatePeriodLabel();
        };
        WeekViewBtn.Click += async (s, e) =>
        {
            SetViewButtons("Week");
            await _vm.SwitchViewCommand.ExecuteAsync("Week");
            UpdatePeriodLabel();
        };

        _vm.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(_vm.CurrentView)) UpdateGridVisibility();
            if (e.PropertyName is nameof(_vm.CurrentMonthName) or
                nameof(_vm.CurrentYear) or nameof(_vm.CurrentWeekLabel))
                UpdatePeriodLabel();
        };

        UpdatePeriodLabel();
        UpdateGridVisibility();
    }

    private void UpdateGridVisibility()
    {
        YearGrid.Visibility = _vm.CurrentView == CalendarViewModel.CalendarView.Year
            ? Visibility.Visible : Visibility.Collapsed;
        MonthGrid.Visibility = _vm.CurrentView == CalendarViewModel.CalendarView.Month
            ? Visibility.Visible : Visibility.Collapsed;
        WeekGrid.Visibility = _vm.CurrentView == CalendarViewModel.CalendarView.Week
            ? Visibility.Visible : Visibility.Collapsed;
    }

    private void UpdatePeriodLabel()
    {
        PeriodLabel.Text = _vm.CurrentView switch
        {
            CalendarViewModel.CalendarView.Year => _vm.CurrentYear.ToString(),
            CalendarViewModel.CalendarView.Week => _vm.CurrentWeekLabel,
            _ => _vm.CurrentMonthName
        };
    }

    private void SetViewButtons(string active)
    {
        YearViewBtn.Opacity = active == "Year" ? 1.0 : 0.6;
        MonthViewBtn.Opacity = active == "Month" ? 1.0 : 0.6;
        WeekViewBtn.Opacity = active == "Week" ? 1.0 : 0.6;
    }

    private async void OnMonthCardClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement el && el.DataContext is YearMonthSummary summary)
        {
            SetViewButtons("Month");
            await _vm.SelectYearMonthCommand.ExecuteAsync(summary);
            UpdatePeriodLabel();
        }
    }

    private async void OnDayClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement el && el.DataContext is CalendarDay day)
        {
            await _vm.SelectDateCommand.ExecuteAsync(day);
        }
    }

    private async void OnWeekDayClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement el && el.DataContext is WeekDay weekDay)
        {
            await _vm.SelectWeekDayCommand.ExecuteAsync(weekDay);
        }
    }

    private void OnCalendarMemoryClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement el && el.DataContext is Memory memory)
        {
            var detailWin = new MemoryDetailWindow(memory) { Owner = App.Current.MainWindow };
            detailWin.ShowDialog();
            if (detailWin.MemoryModified)
            {
                _ = _vm.LoadAsync();
            }
        }
    }
}
