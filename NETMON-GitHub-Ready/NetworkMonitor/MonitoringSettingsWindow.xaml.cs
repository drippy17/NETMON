
using System.Windows;
using System.Windows.Controls;

namespace NetworkMonitor
{
    public partial class MonitoringSettingsWindow : Window
    {
        private readonly MainWindow mainWindow;

        public MonitoringSettingsWindow(MainWindow owner)
        {
            InitializeComponent();

            mainWindow = owner;

            LoadSettings();
        }

        private void LoadSettings()
        {
            int currentInterval =
                mainWindow.MonitoringIntervalSeconds;

            foreach (ComboBoxItem item in IntervalComboBox.Items)
            {
                if (int.TryParse(item.Tag?.ToString(), out int seconds)
                    && seconds == currentInterval)
                {
                    IntervalComboBox.SelectedItem = item;
                    break;
                }
            }

            UpdateStatus();
        }

        private void UpdateStatus()
        {
            if (mainWindow.IsMonitoringActive)
            {
                StatusText.Text =
                    $"Monitoring active - every {mainWindow.MonitoringIntervalSeconds} seconds";

                StatusText.Foreground =
                    System.Windows.Media.Brushes.SeaGreen;

                PauseResumeButton.Content = "Pause Monitoring";
            }
            else
            {
                StatusText.Text = "Monitoring paused";

                StatusText.Foreground =
                    System.Windows.Media.Brushes.IndianRed;

                PauseResumeButton.Content = "Resume Monitoring";
            }
        }

        private void PauseResume_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (mainWindow.IsMonitoringActive)
            {
                mainWindow.PauseMonitoring();
            }
            else
            {
                mainWindow.ResumeMonitoring();
            }

            UpdateStatus();
        }

        private void SaveSettings_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (IntervalComboBox.SelectedItem is not ComboBoxItem selectedItem)
            {
                MessageBox.Show("Please select a monitoring interval.");
                return;
            }

            if (!int.TryParse(selectedItem.Tag?.ToString(), out int seconds))
            {
                MessageBox.Show("Invalid monitoring interval.");
                return;
            }

            mainWindow.SetMonitoringInterval(seconds);

            MessageBox.Show(
                $"Monitoring interval updated to {seconds} seconds.",
                "Settings Saved",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );

            Close();
        }
    }
}
