using System;
using System.Globalization;
using System.Windows.Data;
using BacklogManager.Services;

namespace BacklogManager.Converters
{
    /// <summary>
    /// Convertisseur pour accéder aux ressources localisées depuis XAML
    /// </summary>
    [ValueConversion(typeof(string), typeof(string))]
    public class LocalizationConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter == null)
                return string.Empty;

            string key = parameter.ToString();
            return LocalizationService.Instance[key];
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
