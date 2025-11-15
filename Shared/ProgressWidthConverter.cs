using System;
using System.Globalization;
using System.Windows.Data;

namespace BacklogManager.Shared
{
    public class ProgressWidthConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length != 2 || !(values[0] is double) || !(values[1] is double))
                return 0.0;

            var percentage = (double)values[0];
            var totalWidth = (double)values[1];

            return (percentage / 100.0) * totalWidth;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
