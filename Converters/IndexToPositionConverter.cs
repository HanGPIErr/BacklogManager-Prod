using System;
using System.Globalization;
using System.Windows.Data;

namespace BacklogManager.Converters
{
    public class IndexToPositionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int index)
            {
                // Chaque mois a une largeur de 100px
                return index * 100.0;
            }
            return 0.0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
