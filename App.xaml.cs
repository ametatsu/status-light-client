using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Timers;
using System.Windows;
using Ametatsu.StatusLightClient.Models;
using Ametatsu.StatusLightClient.Services;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using Forms = System.Windows.Forms;

namespace Ametatsu.StatusLightClient
{
    public partial class App : Application
    {
        private readonly CategoryConfig _categoryConfig;
        
        private readonly Win10MicConsentService _micConsentService;

        private readonly Forms.NotifyIcon _trayIcon;
        private readonly StatusWindow _statusWindow;

        private IEnumerable<Win10ConsentStatus> _micConsentStatuses;
        private IEnumerable<Win10ConsentStatus> _camConsentStatuses;

        private readonly List<AppStatus> _appStatuses;

        private string _activeCategory;
        private string _activeCategoryColor;

        public event EventHandler ActiveStatusChanged;
        public event EventHandler ActiveCategoryChanged;
        public event EventHandler StatusUpdated;

        public App()
        {
            var yamlDeserializer = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .Build();
            _categoryConfig =
                yamlDeserializer.Deserialize<CategoryConfig>(System.IO.File.ReadAllText("category.config.yaml"));

            // Initialize variables
            _appStatuses = new List<AppStatus>();

            // Initialize services
            _micConsentService = new Win10MicConsentService();

            // Initialize status window
            _statusWindow = new StatusWindow();
            StatusUpdated += (s, e) => { _statusWindow.UpdateStatuses(_appStatuses); };
            ActiveCategoryChanged += (s, e) => { _statusWindow.UpdateActiveCategory(_activeCategory, _activeCategoryColor); };

            // Initialize the Tray Icon
            _trayIcon = new Forms.NotifyIcon
            {
                Icon = new System.Drawing.Icon("Resources/icon.ico"),
                Text = "Status Light Client",
                Visible = true,
                ContextMenuStrip = new Forms.ContextMenuStrip()
            };

            _trayIcon.Click += ShowStatus;

            _trayIcon.ContextMenuStrip.Items.Add("Show status", null, ShowStatus);
            _trayIcon.ContextMenuStrip.Items.Add("Exit", null, ExitApp);

            // Update tray icon and text color when status changes
            ActiveCategoryChanged += (s, e) => {
                _trayIcon.Icon = new System.Drawing.Icon(@$"Resources/icon{(!string.IsNullOrEmpty(_activeCategoryColor) ? "-" + _activeCategoryColor : null)}.ico");
                _trayIcon.Text = $"Status Light Client\nActive Status: {_activeCategory}";
            };
        }

        private void ApplicationStartup(object sender, StartupEventArgs e)
        {
            // For an initial status update
            FetchUpdatedStatuses();

            // Background polling update task
            var timer = new Timer(5000);
            timer.Elapsed += OnBackgroundUpdate;
            timer.Enabled = true;
        }

        private void ShowStatus(object source, EventArgs e)
        {
            // Prevent right click from opening the window as well
            if (e.GetType() == typeof(Forms.MouseEventArgs) && ((Forms.MouseEventArgs)e).Button != Forms.MouseButtons.Left) return;

            _statusWindow.Show();
        }

        private void OnBackgroundUpdate(object source, ElapsedEventArgs e)
        {
            FetchUpdatedStatuses();
        }

        private void FetchUpdatedStatuses()
        {
            _micConsentStatuses = _micConsentService.GetConsentStatuses("microphone");
            _camConsentStatuses = _micConsentService.GetConsentStatuses("webcam");
            UpdateAppStatuses();
        }

        private void UpdateAppStatuses()
        {
            var activeStatusChange = false;
            var statusUpdated = false;

            foreach (var status in _micConsentStatuses.Concat(_camConsentStatuses))
            {
                var ignoredApp = false;
                var appCategory = "Other";
                if (_categoryConfig.IgnoredApps.Any(i => i.Contains(status.AppName, StringComparison.InvariantCultureIgnoreCase)))
                {
                    ignoredApp = true;
                    appCategory = "Ignored";
                }
                else
                {
                    var categoryMap = _categoryConfig.CategoryMapping.FirstOrDefault(c =>
                        c.Key.Contains(status.AppName, StringComparison.InvariantCultureIgnoreCase));
                    if (!string.IsNullOrEmpty(categoryMap.Key)) appCategory = categoryMap.Value;
                }

                var appStatusIndex = _appStatuses.FindIndex(a => a.AppName == status.AppName);
                if (appStatusIndex > 0)
                {
                    switch (status.ConsentDomain)
                    {
                        case "microphone":
                            if (_appStatuses[appStatusIndex].MicActive != status.Active) activeStatusChange = true;
                            _appStatuses[appStatusIndex].MicLastUsedTimeStart = status.LastUsedTimeStart;
                            _appStatuses[appStatusIndex].MicLastUsedTimeStop = status.LastUsedTimeStop;
                            break;
                        case "webcam":
                            if (_appStatuses[appStatusIndex].CamActive != status.Active) activeStatusChange = true;
                            _appStatuses[appStatusIndex].CamLastUsedTimeStart = status.LastUsedTimeStart;
                            _appStatuses[appStatusIndex].CamLastUsedTimeStop = status.LastUsedTimeStop;
                            break;
                    }

                    statusUpdated = true;
                }
                else
                {
                    switch (status.ConsentDomain)
                    {
                        case "microphone":
                            _appStatuses.Add(new AppStatus
                            {
                                AppName = status.AppName,
                                Category = appCategory,
                                IgnoredApp = ignoredApp,
                                MicLastUsedTimeStart = status.LastUsedTimeStart,
                                MicLastUsedTimeStop = status.LastUsedTimeStop
                            });
                            break;
                        case "webcam":
                            _appStatuses.Add(new AppStatus
                            {
                                AppName = status.AppName,
                                Category = appCategory,
                                IgnoredApp = ignoredApp,
                                CamLastUsedTimeStart = status.LastUsedTimeStart,
                                CamLastUsedTimeStop = status.LastUsedTimeStop
                            });
                            break;
                    }
                    statusUpdated = true;
                    activeStatusChange = true;
                }
            }

            UpdateActiveCategory();

            if (activeStatusChange)
            {
                var onActiveStatusChange = ActiveStatusChanged;
                onActiveStatusChange?.Invoke(this, EventArgs.Empty);
            }

            if (statusUpdated)
            {
                var onStatusUpdated = StatusUpdated;
                onStatusUpdated?.Invoke(this, EventArgs.Empty);
            }
        }

        private void UpdateActiveCategory()
        {
            var activeCategory = "None";

            var activeApps = _appStatuses.Where(a => a.Active && !a.IgnoredApp);
            if (activeApps.Any())
            {
                activeApps = activeApps.OrderBy(a =>
                {
                    var orderIndex = _categoryConfig.CategoryPriority.IndexOf(a.Category);
                    if (orderIndex >= 0) return orderIndex;
                    else return 9999999;
                });

                var primaryActiveApp = activeApps.FirstOrDefault();

                activeCategory = primaryActiveApp?.Category;
            }

            var activeCategoryChanged = _activeCategory != activeCategory;
            _activeCategory = activeCategory;

            var categoryIconMap = _categoryConfig.CategoryColors.FirstOrDefault(c => c.Key == activeCategory);
            if (!string.IsNullOrEmpty(categoryIconMap.Key)) _activeCategoryColor = categoryIconMap.Value;
            else _activeCategoryColor = null;

            // Fire event if changed
            if (activeCategoryChanged)
            {
                var onActiveCategoryChanged = ActiveCategoryChanged;
                onActiveCategoryChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public void ExitApp(object sender, EventArgs e)
        {
            _trayIcon.Visible = false;
            _trayIcon.Dispose();

            Shutdown();
        }

    }
}
