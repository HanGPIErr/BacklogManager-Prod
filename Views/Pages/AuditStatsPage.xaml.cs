using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media;
using BacklogManager.Domain;
using BacklogManager.Services;

namespace BacklogManager.Views.Pages
{
    public partial class AuditStatsPage : Page
    {
        private readonly IDatabase _database;

        public AuditStatsPage(IDatabase database)
        {
            InitializeComponent();
            _database = database;
            LoadStats();
        }

        private void LoadStats()
        {
            try
            {
                var logs = _database.GetAuditLogs();
                if (logs == null || logs.Count == 0)
                {
                    TxtTotalActions.Text = "0";
                    TxtActiveUsers.Text = "0";
                    TxtTodayActions.Text = "0";
                    TxtWeekActions.Text = "0";
                    return;
                }

                var today = DateTime.Today;
                var weekStart = today.AddDays(-(int)today.DayOfWeek + (int)DayOfWeek.Monday);
                if (today.DayOfWeek == DayOfWeek.Sunday) weekStart = weekStart.AddDays(-7);

                // KPIs
                TxtTotalActions.Text = logs.Count.ToString("N0");
                TxtActiveUsers.Text = logs.Select(l => l.Username).Distinct().Count().ToString();
                TxtTodayActions.Text = logs.Count(l => l.DateAction.Date == today).ToString("N0");
                TxtWeekActions.Text = logs.Count(l => l.DateAction.Date >= weekStart).ToString("N0");

                // Actions by type
                var actionGroups = logs.GroupBy(l => l.Action ?? "N/A")
                    .Select(g => new { Label = g.Key, Count = g.Count() })
                    .OrderByDescending(g => g.Count)
                    .Take(10)
                    .ToList();

                var maxAction = actionGroups.Any() ? actionGroups.Max(a => a.Count) : 1;
                var actionColors = new[] { "#00915A", "#2196F3", "#FF9800", "#9C27B0", "#F44336", "#009688", "#795548", "#607D8B", "#E91E63", "#3F51B5" };

                ActionsByTypeList.ItemsSource = actionGroups.Select((g, i) => new BarItem
                {
                    Label = g.Label,
                    Count = g.Count.ToString(),
                    BarWidth = Math.Max(10, (double)g.Count / maxAction * 200),
                    Color = new SolidColorBrush((Color)ColorConverter.ConvertFromString(actionColors[i % actionColors.Length]))
                }).ToList();

                // Top users
                var userGroups = logs.GroupBy(l => l.Username ?? "N/A")
                    .Select(g => new { Username = g.Key, Count = g.Count() })
                    .OrderByDescending(g => g.Count)
                    .Take(10)
                    .ToList();

                var maxUser = userGroups.Any() ? userGroups.Max(u => u.Count) : 1;
                var userColors = new[] { "#00915A", "#2196F3", "#FF9800", "#9C27B0", "#F44336" };

                TopUsersList.ItemsSource = userGroups.Select((g, i) => new UserBarItem
                {
                    Rank = $"#{i + 1}",
                    Username = g.Username,
                    Count = g.Count.ToString(),
                    BarWidth = Math.Max(10, (double)g.Count / maxUser * 150),
                    Color = new SolidColorBrush((Color)ColorConverter.ConvertFromString(userColors[i % userColors.Length]))
                }).ToList();

                // Entity types
                var entityGroups = logs.Where(l => !string.IsNullOrEmpty(l.EntityType))
                    .GroupBy(l => l.EntityType)
                    .Select(g => new { Label = g.Key, Count = g.Count() })
                    .OrderByDescending(g => g.Count)
                    .Take(10)
                    .ToList();

                var maxEntity = entityGroups.Any() ? entityGroups.Max(e => e.Count) : 1;
                var entityColors = new[] { "#009688", "#3F51B5", "#FF5722", "#8BC34A", "#FFC107", "#00BCD4", "#673AB7", "#CDDC39", "#E91E63", "#795548" };

                EntityTypesList.ItemsSource = entityGroups.Select((g, i) => new BarItem
                {
                    Label = g.Label,
                    Count = g.Count.ToString(),
                    BarWidth = Math.Max(10, (double)g.Count / maxEntity * 300),
                    Color = new SolidColorBrush((Color)ColorConverter.ConvertFromString(entityColors[i % entityColors.Length]))
                }).ToList();

                // Activity last 7 days
                var last7Days = Enumerable.Range(0, 7)
                    .Select(i => today.AddDays(-6 + i))
                    .ToList();

                var dailyCounts = last7Days.Select(d => new
                {
                    Date = d,
                    Count = logs.Count(l => l.DateAction.Date == d)
                }).ToList();

                var maxDaily = dailyCounts.Any() ? Math.Max(1, dailyCounts.Max(d => d.Count)) : 1;

                RecentActivityList.ItemsSource = dailyCounts.Select(d => new BarItem
                {
                    Label = d.Date.ToString("ddd dd/MM", CultureInfo.CurrentCulture),
                    Count = d.Count.ToString(),
                    BarWidth = Math.Max(5, (double)d.Count / maxDaily * 400),
                    Color = new SolidColorBrush((Color)ColorConverter.ConvertFromString(d.Date == today ? "#00915A" : "#90CAF9"))
                }).ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur chargement stats audit: {ex.Message}");
            }
        }

        public class BarItem
        {
            public string Label { get; set; }
            public string Count { get; set; }
            public double BarWidth { get; set; }
            public SolidColorBrush Color { get; set; }
        }

        public class UserBarItem
        {
            public string Rank { get; set; }
            public string Username { get; set; }
            public string Count { get; set; }
            public double BarWidth { get; set; }
            public SolidColorBrush Color { get; set; }
        }
    }
}
