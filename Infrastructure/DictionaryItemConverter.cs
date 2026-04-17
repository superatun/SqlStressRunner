using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;

namespace SqlStressRunner.Infrastructure;

/// <summary>
/// Converter to enable two-way binding to dictionary items in DataGrid.
/// WPF doesn't support dictionary indexer binding out of the box.
/// </summary>
public class DictionaryItemConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is Dictionary<string, string> dictionary && parameter is string key)
        {
            return dictionary.TryGetValue(key, out var result) ? result : string.Empty;
        }
        return string.Empty;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // This converter doesn't support ConvertBack because we need the entire dictionary
        // to update it, which requires a multi-binding approach
        throw new NotImplementedException("Use DictionaryItemMultiConverter for two-way binding");
    }
}
