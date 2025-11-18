using System;
using System.Globalization;
using System.Windows.Data;

namespace BacklogManager.Converters
{
    /// <summary>
    /// Convertit un pourcentage en hauteur proportionnelle pour les barres de tâches
    /// </summary>
    public class PercentageToHeightConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2) return 0.0;

            if (values[0] is double pourcentage && values[1] is double containerHeight)
            {
                if (containerHeight <= 0 || pourcentage <= 0)
                    return 0.0;

                // Retourne la hauteur en pixels basée sur le pourcentage du container
                return (pourcentage / 100.0) * containerHeight;
            }

            return 0.0;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
