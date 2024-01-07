using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Layout;
using Avalonia.Media;
using MinMe.Avalonia.Models;
using System.Globalization;

namespace MinMe.Avalonia.Views.Converters;

class InfoRowSizeConverter : IValueConverter
{
    private static readonly IBrush DangerBrush = new SolidColorBrush(Colors.IndianRed, 0.4);
    private static readonly IBrush? WarningBrush = new SolidColorBrush(Colors.Yellow, 0.4);

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        long? size = value switch
        {
            SlideInfoRow slideInfo => slideInfo.Size,
            PartInfoRow partInfo => partInfo.Size,
            _ => null,
        };
        if (size is null)
            return value;

        var cell = new TextBlock {
            Text = size.Value.ToString("0,0"),
            VerticalAlignment = VerticalAlignment.Center
        };

        cell.Background = size switch
        {
            > 5_000_000 => DangerBrush,
            > 200_000 => WarningBrush,
            _ => cell.Background
        };

        return cell;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
