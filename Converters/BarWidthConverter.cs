using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace BacklogManager.Converters
{
    public class BarWidthConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length != 2 || values[0] == DependencyProperty.UnsetValue || values[1] == DependencyProperty.UnsetValue)
                return 0.0;

            try
            {
                var nbMembres = System.Convert.ToDouble(values[0]);
                var maxMembres = System.Convert.ToDouble(values[1]);

                if (maxMembres == 0)
                    return 20.0;  // Hauteur minimale même si pas de max

                // Hauteur maximale disponible pour les barres verticales
                const double maxHeight = 250.0;
                
                if (nbMembres == 0)
                    return 20.0;  // Hauteur minimale pour équipes sans membres
                
                var ratio = nbMembres / maxMembres;
                var height = maxHeight * ratio;

                // Hauteur minimale pour visibilité (au moins 20px)
                return Math.Max(height, 20.0);
            }
            catch
            {
                return 20.0;  // Hauteur par défaut en cas d'erreur
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
