using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using Ametatsu.StatusLightClient.Models;

namespace Ametatsu.StatusLightClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class StatusWindow : Window
    {
        public StatusWindow()
        {
            InitializeComponent();
            DataContext = this;
            StatusListView.ItemsSource = null;
        }

        public void UpdateStatuses(IEnumerable<AppStatus> appStatuses)
        {
            if (appStatuses == null)
            {
                Dispatcher.Invoke(() =>
                {
                    StatusListView.ItemsSource = null;
                });
            }
            else
            {
                var activeApps = appStatuses.Where(a => a.Active);
                var viewItems = activeApps.Select(
                    a => new StatusListViewModel
                    {
                        AppName = a.AppName,
                        Category = a.Category,
                        MicStatus = a.MicActive ? $"Active for {a.MicActiveDuration?.ToString(@"hh\:mm\:ss")}" : "Inactive",
                        CamStatus = a.CamActive ? $"Active for {a.CamActiveDuration?.ToString(@"hh\:mm\:ss")}" : "Inactive"
                    }
                );

                Dispatcher.Invoke(() =>
                {
                    StatusListView.ItemsSource = viewItems;
                });
                
            }
        }

        private static readonly DependencyProperty ActiveCategoryProperty = DependencyProperty.Register("ActiveCategory", typeof(string), typeof(StatusWindow));
        private string ActiveCategory
        {
            get { return (string)GetValue(ActiveCategoryProperty); }
            set { SetValue(ActiveCategoryProperty, value); }
        }

        private static readonly DependencyProperty ActiveCategoryIconProperty = DependencyProperty.Register("ActiveCategoryIcon", typeof(string), typeof(StatusWindow));
        private string ActiveCategoryIcon
        {
            get { return (string)GetValue(ActiveCategoryIconProperty); }
            set { SetValue(ActiveCategoryIconProperty, value); }
        }

        public void UpdateActiveCategory(string activeCategory, string activeCategoryColor)
        {
            Dispatcher.Invoke(() =>
            {
                ActiveCategory = activeCategory;
                ActiveCategoryIcon = @$"Resources/icon{(!string.IsNullOrEmpty(activeCategoryColor) ? "-" + activeCategoryColor : null)}.ico";
            });
        }

        private void StatusWindow_Closing(object sender, CancelEventArgs e)
        {
            Hide();
            e.Cancel = true;
        }

    }

    public class StatusListViewModel
    {
        public string AppName { get; set; }
        public string MicStatus { get; set; }
        public string CamStatus { get; set; }
        public string Category { get; set; }
    }
}
