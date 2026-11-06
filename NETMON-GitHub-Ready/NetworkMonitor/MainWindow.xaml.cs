
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace NetworkMonitor
{
    public partial class MainWindow : Window
    {
        private readonly DispatcherTimer monitoringTimer;

        public int MonitoringIntervalSeconds { get; private set; } = 30;

        public bool IsMonitoringActive => monitoringTimer.IsEnabled;

        public MainWindow()
        {
            InitializeComponent();

            monitoringTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(MonitoringIntervalSeconds)
            };

            monitoringTimer.Tick += MonitoringTimer_Tick;

            UpdateDashboard();

            monitoringTimer.Start();

            Closed += MainWindow_Closed;
        }

        // Change automatic monitoring interval.
        public void SetMonitoringInterval(int seconds)
        {
            if (seconds <= 0)
                return;

            MonitoringIntervalSeconds = seconds;

            monitoringTimer.Interval =
                TimeSpan.FromSeconds(seconds);

            UpdateDashboard();
        }

        // Pause automatic monitoring.
        public void PauseMonitoring()
        {
            monitoringTimer.Stop();

            UpdateDashboard();
        }

        // Resume automatic monitoring.
        public void ResumeMonitoring()
        {
            monitoringTimer.Start();

            UpdateDashboard();
        }

        // Automatically check registered devices.
        private async void MonitoringTimer_Tick(
            object? sender,
            EventArgs e)
        {
            bool completed =
                await DevicesWindow.CheckAllDevicesAsync();

            if (completed)
            {
                UpdateDashboard();

                EventsList.Items.Insert(
                    0,
                    new ListBoxItem
                    {
                        Content =
                            $"Automatic network check completed at {DateTime.Now:HH:mm:ss}",
                        Padding = new Thickness(8)
                    }
                );
            }
        }

        // Stop timer when application closes.
        private void MainWindow_Closed(
            object? sender,
            EventArgs e)
        {
            monitoringTimer.Stop();
        }

        // Update dashboard information.
        private void UpdateDashboard()
        {
            int total = DevicesWindow.Devices.Count;

            int online = DevicesWindow.Devices.Count(
                d => d.Status == "Online");

            int offline = DevicesWindow.Devices.Count(
                d => d.Status == "Offline");

            DevicesCountText.Text = total.ToString();

            OnlineCountText.Text = online.ToString();

            OfflineCountText.Text = offline.ToString();

            AlertsCountText.Text =
                DevicesWindow.Alerts.Count.ToString();

            DownloadText.Text = "Open traffic monitor";

            UploadText.Text = "Open traffic monitor";

            EngineStatusText.Text = IsMonitoringActive
                ? $"Active - {MonitoringIntervalSeconds}s checks"
                : "Monitoring Paused";

            LastUpdateText.Text =
                DateTime.Now.ToString("dd MMM yyyy, HH:mm:ss");
        }

        // Refresh dashboard.
        private void Refresh_Click(
            object sender,
            RoutedEventArgs e)
        {
            UpdateDashboard();

            EventsList.Items.Insert(
                0,
                new ListBoxItem
                {
                    Content =
                        $"Dashboard refreshed at {DateTime.Now:HH:mm:ss}",
                    Padding = new Thickness(8)
                }
            );
        }

        // Dashboard navigation.
        private void Dashboard_Click(
            object sender,
            RoutedEventArgs e)
        {
            PageTitle.Text = "Network Dashboard";

            PageSubtitle.Text =
                "Monitor your network infrastructure and device activity.";

            UpdateDashboard();
        }

        // Device management.
        private void Devices_Click(
            object sender,
            RoutedEventArgs e)
        {
            DevicesWindow devicesWindow = new DevicesWindow
            {
                Owner = this
            };

            devicesWindow.ShowDialog();

            UpdateDashboard();
        }

        // Network topology.
        private void Topology_Click(
            object sender,
            RoutedEventArgs e)
        {
            TopologyWindow topologyWindow = new TopologyWindow
            {
                Owner = this
            };

            topologyWindow.ShowDialog();
        }

        // Network traffic monitoring.
        private void Traffic_Click(
            object sender,
            RoutedEventArgs e)
        {
            TrafficWindow trafficWindow = new TrafficWindow
            {
                Owner = this
            };

            trafficWindow.ShowDialog();
        }

        // Alerts and events.
        private void Alerts_Click(
            object sender,
            RoutedEventArgs e)
        {
            PageTitle.Text = "Alerts and Events";

            PageSubtitle.Text =
                "Review network events and monitoring alerts.";
        }

        // Reports.
        private void Reports_Click(
            object sender,
            RoutedEventArgs e)
        {
            ReportsWindow reportsWindow = new ReportsWindow
            {
                Owner = this
            };

            reportsWindow.ShowDialog();

            UpdateDashboard();
        }

        // Monitoring settings.
        private void Settings_Click(
            object sender,
            RoutedEventArgs e)
        {
            MonitoringSettingsWindow settingsWindow =
                new MonitoringSettingsWindow(this)
                {
                    Owner = this
                };

            settingsWindow.ShowDialog();

            UpdateDashboard();
        }
    }
}
