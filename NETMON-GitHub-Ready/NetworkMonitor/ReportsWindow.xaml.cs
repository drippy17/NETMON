
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using Microsoft.Win32;

namespace NetworkMonitor
{
    public partial class ReportsWindow : Window
    {
        public ReportsWindow()
        {
            InitializeComponent();
            LoadReport();
        }

        private void LoadReport()
        {
            var devices = DevicesWindow.Devices;
            var alerts = DevicesWindow.Alerts;

            TotalDevicesText.Text = devices.Count.ToString();

            OnlineDevicesText.Text =
                devices.Count(d => d.Status == "Online").ToString();

            OfflineDevicesText.Text =
                devices.Count(d => d.Status == "Offline").ToString();

            TotalAlertsText.Text = alerts.Count.ToString();

            HighAlertsText.Text =
                alerts.Count(a =>
                    a.Severity.Equals(
                        "High",
                        StringComparison.OrdinalIgnoreCase)).ToString();

            ReportsDataGrid.ItemsSource = null;
            ReportsDataGrid.ItemsSource = alerts.ToList();
        }

        private void RefreshReport_Click(object sender, RoutedEventArgs e)
        {
            LoadReport();

            MessageBox.Show(
                "Report refreshed successfully.",
                "Network Reports",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void ExportCsv_Click(object sender, RoutedEventArgs e)
        {
            var alerts = DevicesWindow.Alerts;

            SaveFileDialog saveDialog = new SaveFileDialog
            {
                Title = "Export Network Report",
                Filter = "CSV Files (*.csv)|*.csv",
                FileName = "NetworkMonitor_Report.csv"
            };

            if (saveDialog.ShowDialog() != true)
            {
                return;
            }

            StringBuilder csv = new StringBuilder();

            csv.AppendLine(
                "Timestamp,Device Name,IP Address,Severity,Description,Status");

            foreach (var alert in alerts)
            {
                csv.AppendLine(string.Join(",",
                    EscapeCsv(alert.Timestamp),
                    EscapeCsv(alert.DeviceName),
                    EscapeCsv(alert.IPAddress),
                    EscapeCsv(alert.Severity),
                    EscapeCsv(alert.Description),
                    EscapeCsv(alert.InvestigationStatus)));
            }

            try
            {
                File.WriteAllText(
                    saveDialog.FileName,
                    csv.ToString(),
                    Encoding.UTF8);

                MessageBox.Show(
                    "Report exported successfully.",
                    "Export Complete",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Could not export report: " + ex.Message,
                    "Export Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private string EscapeCsv(string value)
        {
            value ??= "";

            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }
    }
}
