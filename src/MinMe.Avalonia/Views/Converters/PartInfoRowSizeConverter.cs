using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Media;
using MinMe.Avalonia.Models;
using System;
using System.Globalization;
using System.Text;

namespace MinMe.Avalonia.Views.Converters
{
    class PartInfoRowSizeConverter : IValueConverter
    {
        private static IBrush DangerBrush = new SolidColorBrush(Colors.IndianRed, 0.4);
        private static IBrush WarningBrush = new SolidColorBrush(Colors.Yellow, 0.4);

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var partInfo = value as PartInfoRow;
            if (partInfo is null)
                return value;

            var cell = new TextBlock {
                Text = partInfo.Size.ToString("0,0")
            };

            if (partInfo.Size > 5_000_000)
                cell.Background = DangerBrush;
            else if (partInfo.Size > 100_000)
                cell.Background = WarningBrush;

            return cell;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
