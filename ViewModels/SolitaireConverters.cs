using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace projekt8plsdzialaj.ViewModels;

/// <summary>bool isRed → Brush (czerwony / czarny).</summary>
public sealed class RedBlackConverter : IValueConverter
{
    public static readonly RedBlackConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool isRed = value is bool b && b;
        return isRed ? Brushes.Crimson : Brushes.Black;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>bool isSelected → Brush (żółty / przezroczysty), do podświetlania zaznaczonej karty.</summary>
public sealed class SelectionBorderConverter : IValueConverter
{
    public static readonly SelectionBorderConverter Instance = new();
    private static readonly IBrush Selected = new SolidColorBrush(Color.FromRgb(0xFF, 0xD2, 0x4A));
    private static readonly IBrush Normal = new SolidColorBrush(Color.FromRgb(0x10, 0x10, 0x10));

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool sel = value is bool b && b;
        return sel ? Selected : Normal;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>obj != null → true.</summary>
public sealed class NotNullConverter : IValueConverter
{
    public static readonly NotNullConverter Instance = new();
    public object Convert(object? value, Type t, object? p, CultureInfo c) => value is not null;
    public object ConvertBack(object? value, Type t, object? p, CultureInfo c) => throw new NotSupportedException();
}

/// <summary>int == 0 → true.</summary>
public sealed class ZeroToTrueConverter : IValueConverter
{
    public static readonly ZeroToTrueConverter Instance = new();
    public object Convert(object? value, Type t, object? p, CultureInfo c) => value is int i && i == 0;
    public object ConvertBack(object? value, Type t, object? p, CultureInfo c) => throw new NotSupportedException();
}
