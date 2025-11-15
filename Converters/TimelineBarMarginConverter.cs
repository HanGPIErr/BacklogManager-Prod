using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace BacklogManager.Converters
{
    public class TimelineBarMarginConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 3) return new Thickness(0);

            if (values[0] is double leftPercent && values[1] is double widthPercent && values[2] is double containerWidth)
            {
                if (containerWidth <= 0) return new Thickness(0);

                var leftPixels = (leftPercent / 100.0) * containerWidth;
                var widthPixels = (widthPercent / 100.0) * containerWidth;
                var rightPixels = containerWidth - leftPixels - widthPixels;

                return new Thickness(leftPixels, 2, Math.Max(0, rightPixels), 2);
            }

            return new Thickness(0);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
