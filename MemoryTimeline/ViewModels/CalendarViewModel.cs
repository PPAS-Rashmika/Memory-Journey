using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MemoryTimeline.Models;
using MemoryTimeline.Repositories;
using System.Collections.ObjectModel;

namespace MemoryTimeline.ViewModels;

public partial class CalendarViewModel : ObservableObject
{
    private readonly IMemoryRepository _memoryRepo;

    public enum CalendarView { Year, Month, Week }

    [ObservableProperty] private CalendarView _currentView = CalendarView.Month;
    [ObservableProperty] private int _currentYear = DateTime.Today.Year;
    [ObservableProperty] private int _currentMonth = DateTime.Today.Month;
    [ObservableProperty] private DateTime _currentWeekStart = GetMonday(DateTime.Today);
    [ObservableProperty] private List<Memory> _selectedDateMemories = new();
    [ObservableProperty] private DateTime? _selectedDate;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private ObservableCollection<CalendarDay> _calendarDays = new();
    [ObservableProperty] private ObservableCollection<YearMonthSummary> _yearSummary = new();
    [ObservableProperty] private ObservableCollection<WeekDay> _weekDays = new();

    public string CurrentMonthName => new DateTime(CurrentYear, CurrentMonth, 1).ToString("MMMM yyyy");
    public string CurrentWeekLabel =>
        $"{CurrentWeekStart:MMM d} – {CurrentWeekStart.AddDays(6):MMM d, yyyy}";

    public CalendarViewModel(IMemoryRepository memoryRepo)
    {
        _memoryRepo = memoryRepo;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            switch (CurrentView)
            {
                case CalendarView.Year: await BuildYearViewAsync(); break;
                case CalendarView.Month: await BuildMonthViewAsync(); break;
                case CalendarView.Week: await BuildWeekViewAsync(); break;
            }
        }
        finally { IsLoading = false; }
    }

    private async Task BuildYearViewAsync()
    {
        var summaries = new ObservableCollection<YearMonthSummary>();
        for (int m = 1; m <= 12; m++)
        {
            var memories = await _memoryRepo.GetByMonthAsync(CurrentYear, m);
            summaries.Add(new YearMonthSummary
            {
                Month = m,
                MonthName = new DateTime(CurrentYear, m, 1).ToString("MMM"),
                MemoryCount = memories.Count,
                HasMilestone = memories.Any(x => x.IsMilestone)
            });
        }
        YearSummary = summaries;
    }

    private async Task BuildMonthViewAsync()
    {
        var days = new ObservableCollection<CalendarDay>();
        var firstDay = new DateTime(CurrentYear, CurrentMonth, 1);
        var daysInMonth = DateTime.DaysInMonth(CurrentYear, CurrentMonth);
        var memories = await _memoryRepo.GetByMonthAsync(CurrentYear, CurrentMonth);

        // Fill leading empty days
        for (int i = 0; i < (int)firstDay.DayOfWeek; i++)
            days.Add(new CalendarDay { IsEmpty = true });

        for (int d = 1; d <= daysInMonth; d++)
        {
            var date = new DateTime(CurrentYear, CurrentMonth, d);
            var dayMems = memories.Where(m => m.MemoryDate.Date == date).ToList();
            days.Add(new CalendarDay
            {
                Date = date,
                DayNumber = d,
                IsToday = date == DateTime.Today,
                IsSelected = SelectedDate.HasValue && SelectedDate.Value.Date == date,
                HasMemories = dayMems.Any(),
                HasMilestone = dayMems.Any(m => m.IsMilestone),
                MemoryCount = dayMems.Count,
                Memories = dayMems
            });
        }
        CalendarDays = days;
    }

    private async Task BuildWeekViewAsync()
    {
        var weekDays = new ObservableCollection<WeekDay>();
        for (int i = 0; i < 7; i++)
        {
            var date = CurrentWeekStart.AddDays(i);
            var memories = await _memoryRepo.GetByDateAsync(date);
            weekDays.Add(new WeekDay
            {
                Date = date,
                DayName = date.ToString("ddd"),
                DayNumber = date.Day,
                IsToday = date == DateTime.Today,
                Memories = memories
            });
        }
        WeekDays = weekDays;
    }

    [RelayCommand]
    private async Task SelectDateAsync(CalendarDay day)
    {
        if (day.IsEmpty || day.Date == default) return;

        foreach (var d in CalendarDays)
        {
            d.IsSelected = (d == day);
        }

        SelectedDate = day.Date;
        SelectedDateMemories = await _memoryRepo.GetByDateAsync(day.Date);
    }

    [RelayCommand]
    private async Task SelectWeekDayAsync(WeekDay day)
    {
        SelectedDate = day.Date;
        SelectedDateMemories = await _memoryRepo.GetByDateAsync(day.Date);
    }

    [RelayCommand]
    private async Task SelectYearMonthAsync(YearMonthSummary summary)
    {
        CurrentMonth = summary.Month;
        CurrentView = CalendarView.Month;
        await LoadAsync();
    }

    [RelayCommand]
    private async Task GoToPreviousAsync()
    {
        switch (CurrentView)
        {
            case CalendarView.Year: CurrentYear--; break;
            case CalendarView.Month:
                if (CurrentMonth == 1) { CurrentMonth = 12; CurrentYear--; }
                else CurrentMonth--;
                OnPropertyChanged(nameof(CurrentMonthName));
                break;
            case CalendarView.Week:
                CurrentWeekStart = CurrentWeekStart.AddDays(-7);
                OnPropertyChanged(nameof(CurrentWeekLabel));
                break;
        }
        await LoadAsync();
    }

    [RelayCommand]
    private async Task GoToNextAsync()
    {
        switch (CurrentView)
        {
            case CalendarView.Year: CurrentYear++; break;
            case CalendarView.Month:
                if (CurrentMonth == 12) { CurrentMonth = 1; CurrentYear++; }
                else CurrentMonth++;
                OnPropertyChanged(nameof(CurrentMonthName));
                break;
            case CalendarView.Week:
                CurrentWeekStart = CurrentWeekStart.AddDays(7);
                OnPropertyChanged(nameof(CurrentWeekLabel));
                break;
        }
        await LoadAsync();
    }

    [RelayCommand]
    private async Task GoToTodayAsync()
    {
        CurrentYear = DateTime.Today.Year;
        CurrentMonth = DateTime.Today.Month;
        CurrentWeekStart = GetMonday(DateTime.Today);
        OnPropertyChanged(nameof(CurrentMonthName));
        OnPropertyChanged(nameof(CurrentWeekLabel));
        await LoadAsync();
    }

    [RelayCommand]
    public async Task SwitchViewAsync(string viewName)
    {
        CurrentView = viewName switch
        {
            "Year" => CalendarView.Year,
            "Week" => CalendarView.Week,
            _ => CalendarView.Month
        };
        await LoadAsync();
    }

    private static DateTime GetMonday(DateTime date)
    {
        int diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
        return date.AddDays(-diff).Date;
    }
}

public partial class CalendarDay : ObservableObject
{
    public DateTime Date { get; set; }
    public int DayNumber { get; set; }
    public bool IsToday { get; set; }
    [ObservableProperty] private bool _isSelected;
    public bool IsEmpty { get; set; }
    public bool HasMemories { get; set; }
    public bool HasMilestone { get; set; }
    public int MemoryCount { get; set; }
    public List<Memory> Memories { get; set; } = new();

    public IEnumerable<Memory> DisplayMemories => Memories.Take(2);
    public int ExtraMemoriesCount => Math.Max(0, Memories.Count - 2);
    public bool HasExtraMemories => ExtraMemoriesCount > 0;
    public string ExtraMemoriesLabel => $"+{ExtraMemoriesCount} more";
}

public class YearMonthSummary : ObservableObject
{
    public int Month { get; set; }
    public string MonthName { get; set; } = string.Empty;
    public int MemoryCount { get; set; }
    public bool HasMilestone { get; set; }
}

public class WeekDay : ObservableObject
{
    public DateTime Date { get; set; }
    public string DayName { get; set; } = string.Empty;
    public int DayNumber { get; set; }
    public bool IsToday { get; set; }
    public List<Memory> Memories { get; set; } = new();
}
