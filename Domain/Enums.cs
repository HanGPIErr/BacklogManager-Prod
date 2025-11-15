using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace BacklogManager.Domain
{
    public enum TypeDemande
    {
        Run,
        Dev
    }

    public enum Statut
    {
        Afaire,
        EnCours,
        Test,
        Termine
    }

    public enum Priorite
    {
        Urgent,
        Haute,
        Moyenne,
        Basse
    }

    [ValueConversion(typeof(Enum), typeof(IEnumerable<object>))]
    public class EnumToCollectionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Type enumType && enumType.IsEnum)
            {
                return Enum.GetValues(enumType).Cast<object>().ToList();
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }
}
