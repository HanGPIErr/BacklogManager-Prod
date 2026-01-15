using System.ComponentModel;
using BacklogManager.Services;

namespace BacklogManager.ViewModels
{
    /// <summary>
    /// ViewModel de base qui expose le service de localisation
    /// </summary>
    public class LocalizableViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Service de localisation pour l'accès aux chaînes traduites
        /// </summary>
        public LocalizationService Localization => LocalizationService.Instance;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public LocalizableViewModel()
        {
            // S'abonner aux changements de langue
            LocalizationService.Instance.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == "Item[]")
                {
                    // Notifier que toutes les propriétés ont changé
                    OnPropertyChanged(string.Empty);
                }
            };
        }
    }
}
