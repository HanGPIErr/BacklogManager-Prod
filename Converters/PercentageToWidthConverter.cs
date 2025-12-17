using System;
using System.Globalization;
using System.Windows.Data;

namespace BacklogManager.Converters
{
    public class PercentageToWidthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double percentage && parameter != null)
            {
                // Si parameter est fourni, c'est la largeur max
                if (double.TryParse(parameter.ToString(), out double maxWidth))
                {
                    return (percentage / 100.0) * maxWidth;
                }
            }
            
            // Sinon retourner le pourcentage tel quel (pour Width=pourcentage%)
            if (value is double perc)
            {
                return perc;
            }
            return 0.0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
