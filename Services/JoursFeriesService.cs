using System;
using System.Collections.Generic;

namespace BacklogManager.Services
{
    public static class JoursFeriesService
    {
        /// <summary>
        /// Retourne la liste des jours fériés en France pour une année donnée
        /// </summary>
        public static List<DateTime> GetJoursFeries(int annee)
        {
            var joursFeries = new List<DateTime>();

            // Jours fériés fixes
            joursFeries.Add(new DateTime(annee, 1, 1));   // Jour de l'an
            joursFeries.Add(new DateTime(annee, 5, 1));   // Fête du travail
            joursFeries.Add(new DateTime(annee, 5, 8));   // Victoire 1945
            joursFeries.Add(new DateTime(annee, 7, 14));  // Fête nationale
            joursFeries.Add(new DateTime(annee, 8, 15));  // Assomption
            joursFeries.Add(new DateTime(annee, 11, 1));  // Toussaint
            joursFeries.Add(new DateTime(annee, 11, 11)); // Armistice 1918
            joursFeries.Add(new DateTime(annee, 12, 25)); // Noël

            // Jours fériés mobiles (basés sur Pâques)
            DateTime paques = CalculerPaques(annee);
            joursFeries.Add(paques.AddDays(1));  // Lundi de Pâques
            joursFeries.Add(paques.AddDays(39)); // Ascension
            joursFeries.Add(paques.AddDays(50)); // Lundi de Pentecôte

            return joursFeries;
        }

        /// <summary>
        /// Vérifie si une date est un jour férié
        /// </summary>
        public static bool EstJourFerie(DateTime date)
        {
            var joursFeries = GetJoursFeries(date.Year);
            return joursFeries.Exists(jf => jf.Date == date.Date);
        }

        /// <summary>
        /// Vérifie si une date est un weekend
        /// </summary>
        public static bool EstWeekend(DateTime date)
        {
            return date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday;
        }

        /// <summary>
        /// Vérifie si une date est un jour ouvré
        /// </summary>
        public static bool EstJourOuvre(DateTime date)
        {
            return !EstWeekend(date) && !EstJourFerie(date);
        }

        /// <summary>
        /// Calcule la date de Pâques pour une année donnée (algorithme de Meeus)
        /// </summary>
        private static DateTime CalculerPaques(int annee)
        {
            int a = annee % 19;
            int b = annee / 100;
            int c = annee % 100;
            int d = b / 4;
            int e = b % 4;
            int f = (b + 8) / 25;
            int g = (b - f + 1) / 3;
            int h = (19 * a + b - d - g + 15) % 30;
            int i = c / 4;
            int k = c % 4;
            int l = (32 + 2 * e + 2 * i - h - k) % 7;
            int m = (a + 11 * h + 22 * l) / 451;
            int mois = (h + l - 7 * m + 114) / 31;
            int jour = ((h + l - 7 * m + 114) % 31) + 1;

            return new DateTime(annee, mois, jour);
        }

        /// <summary>
        /// Retourne le nom du jour férié si la date en est un
        /// </summary>
        public static string GetNomJourFerie(DateTime date)
        {
            if (!EstJourFerie(date)) return null;

            DateTime paques = CalculerPaques(date.Year);

            if (date.Date == new DateTime(date.Year, 1, 1).Date) return "Jour de l'an";
            if (date.Date == paques.AddDays(1).Date) return "Lundi de Pâques";
            if (date.Date == new DateTime(date.Year, 5, 1).Date) return "Fête du travail";
            if (date.Date == new DateTime(date.Year, 5, 8).Date) return "Victoire 1945";
            if (date.Date == paques.AddDays(39).Date) return "Ascension";
            if (date.Date == paques.AddDays(50).Date) return "Lundi de Pentecôte";
            if (date.Date == new DateTime(date.Year, 7, 14).Date) return "Fête nationale";
            if (date.Date == new DateTime(date.Year, 8, 15).Date) return "Assomption";
            if (date.Date == new DateTime(date.Year, 11, 1).Date) return "Toussaint";
            if (date.Date == new DateTime(date.Year, 11, 11).Date) return "Armistice 1918";
            if (date.Date == new DateTime(date.Year, 12, 25).Date) return "Noël";

            return "Jour férié";
        }

        /// <summary>
        /// Retourne la liste des jours ouvrés entre deux dates (exclut weekends et jours fériés)
        /// </summary>
        public static List<DateTime> GetJoursOuvres(DateTime dateDebut, DateTime dateFin)
        {
            var joursOuvres = new List<DateTime>();
            var currentDate = dateDebut.Date;

            while (currentDate <= dateFin.Date)
            {
                if (EstJourOuvre(currentDate))
                {
                    joursOuvres.Add(currentDate);
                }
                currentDate = currentDate.AddDays(1);
            }

            return joursOuvres;
        }

        /// <summary>
        /// Compte le nombre de jours ouvrés entre deux dates
        /// </summary>
        public static int CompterJoursOuvres(DateTime dateDebut, DateTime dateFin)
        {
            return GetJoursOuvres(dateDebut, dateFin).Count;
        }
    }
}
