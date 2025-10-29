// MainWindow.xaml.cs
using CaloriesTracker.Data;
using CaloriesTracker.Models;
using CaloriesTracker.Services;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace CaloriesTracker;

public partial class MainWindow : Window
{
    private readonly CalorieService _service;

    public MainWindow()
    {
        InitializeComponent();

        var db = new CaloriesDbContext();
        _service = new CalorieService(db);

        datePicker.SelectedDateChanged += (s, e) => LoadData();
        Loaded += (s, e) => LoadData();
    }

    private void LoadData()
    {
        var date = DateOnly.FromDateTime(datePicker.SelectedDate ?? DateTime.Today);
        entryList.ItemsSource = _service.GetByDate(date);
        summaryText.Text = $"Net: {_service.GetNet(date):F0} cal";
    }

    private async void Add_Click(object sender, RoutedEventArgs e)
    {
        if (!double.TryParse(consumedBox.Text, out var c) ||
            !double.TryParse(burnedBox.Text, out var b))
            return;

        var entry = new CalorieEntry
        {
            Date = DateOnly.FromDateTime(datePicker.SelectedDate ?? DateTime.Today),
            Consumed = c,
            Burned = b,
            Notes = notesBox.Text
        };

        var error = await _service.AddAsync(entry);
        if (error != null)
            MessageBox.Show(error);
        else
        {
            LoadData();
            consumedBox.Text = "0";
            burnedBox.Text = "0";
            notesBox.Text = "";
        }
    }

    private async void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && int.TryParse(btn.Tag?.ToString(), out var id))
        {
            await _service.DeleteAsync(id);
            LoadData();
        }
    }

    private void Chart_Click(object sender, RoutedEventArgs e)
    {
        var now = DateTime.Now;
        var data = _service.GetMonthly(now.Year, now.Month);
        var totalConsumed = data.Values.Sum(x => x.c);
        var totalBurned   = data.Values.Sum(x => x.b);

        var chart = new LiveChartsCore.SkiaSharpView.WPF.PieChart
        {
            Series = new ISeries[]
            {
                new PieSeries<double>
                {
                    Values = new[] { totalConsumed },
                    Name   = "Consumed",
                    Fill   = new SolidColorPaint(SKColors.LightGreen)
                },
                new PieSeries<double>
                {
                    Values = new[] { totalBurned },
                    Name   = "Burned",
                    Fill   = new SolidColorPaint(SKColors.IndianRed)
                }
            }
        };

        var win = new Window
        {
            Title   = "Monthly Chart",
            Width   = 500,
            Height  = 400,
            Content = chart
        };
        win.Show();
    }
}

/* -------------------------------------------------
   Converters – MUST be OUTSIDE the MainWindow class
   ------------------------------------------------- */
[ValueConversion(typeof(double), typeof(string))]
public class NetConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => $"{(double)value:+0;-0;0}";

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

[ValueConversion(typeof(double), typeof(Brush))]
public class ColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => (double)value >= 0 ? Brushes.Green : Brushes.Red;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}