using System;
using System.Globalization;

using Avalonia.Data.Converters;

namespace TheCabinetGroup.Converters;

/// <summary>
/// Returns true when the bound string value equals the converter parameter.
/// Used to show/hide panels based on a CurrentView string property.
///
/// Usage in XAML:
///   IsVisible="{Binding CurrentView,
///       Converter={x:Static conv:StringEqualityConverter.Instance},
///       ConverterParameter=Login}"
/// </summary>
public class StringEqualityConverter: IValueConverter
{
    public static readonly StringEqualityConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is string s && s == parameter as string;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
