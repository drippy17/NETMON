
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace NetworkMonitor
{
    public partial class TrafficWindow : Window
    {
        private readonly DispatcherTimer monitoringTimer = new();
        private NetworkInterface? selectedAdapter;

        private long previousReceived;
        private long previousSent;

        private long totalDownloaded;
        private long totalUploaded;

        private DateTime previousSampleTime;

        private readonly List<double> downloadHistory = new();
        private readonly List<double> uploadHistory = new();

        private const int MaxSamples = 60;

        public TrafficWindow()
        {
            InitializeComponent();

            monitoringTimer.Interval = TimeSpan.FromSeconds(1);
            monitoringTimer.Tick += MonitoringTimer_Tick;

            LoadAdapters();

            TrafficCanvas.SizeChanged += (s, e) => DrawGraph();
            Closed += (s, e) => monitoringTimer.Stop();
        }

        private void LoadAdapters()
        {
            monitoringTimer.Stop();

            AdapterComboBox.ItemsSource = null;

            var adapters = NetworkInterface.GetAllNetworkInterfaces()
                .Where(adapter =>
                    adapter.OperationalStatus == OperationalStatus.Up &&
                    adapter.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                    adapter.NetworkInterfaceType != NetworkInterfaceType.Tunnel)
                .ToList();

            var wifiAdapters = adapters
                .Where(adapter =>
                    adapter.NetworkInterfaceType ==
                    NetworkInterfaceType.Wireless80211)
                .ToList();

            // Prefer Wi-Fi adapters when available.
            var availableAdapters = wifiAdapters.Count > 0
                ? wifiAdapters
                : adapters;

            AdapterComboBox.ItemsSource = availableAdapters;
            AdapterComboBox.DisplayMemberPath = "Name";

            if (availableAdapters.Count > 0)
            {
                AdapterComboBox.SelectedIndex = 0;
                MonitoringStatusText.Text =
                    $"Found {availableAdapters.Count} available adapter(s).";
            }
            else
            {
                selectedAdapter = null;
                MonitoringStatusText.Text =
                    "No active network adapters were found.";
            }
        }

        private void RefreshAdapters_Click(
            object sender,
            RoutedEventArgs e)
        {
            LoadAdapters();
        }

        private void AdapterComboBox_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            monitoringTimer.Stop();

            selectedAdapter =
                AdapterComboBox.SelectedItem as NetworkInterface;

            ResetMeasurements();

            MonitoringStatusText.Text =
                selectedAdapter == null
                    ? "No adapter selected."
                    : $"Selected adapter: {selectedAdapter.Name}";
        }

        private void Start_Click(object sender, RoutedEventArgs e)
        {
            if (selectedAdapter == null)
            {
                MessageBox.Show("Please select a network adapter first.");
                return;
            }

            try
            {
                var statistics = selectedAdapter.GetIPv4Statistics();

                previousReceived = statistics.BytesReceived;
                previousSent = statistics.BytesSent;
                previousSampleTime = DateTime.UtcNow;

                monitoringTimer.Start();

                MonitoringStatusText.Text =
                    $"Monitoring {selectedAdapter.Name}";
            }
            catch (NetworkInformationException)
            {
                MessageBox.Show(
                    "Unable to read statistics from this adapter."
                );
            }
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            monitoringTimer.Stop();

            MonitoringStatusText.Text = "Monitoring stopped.";
        }

        private void MonitoringTimer_Tick(
            object? sender,
            EventArgs e)
        {
            if (selectedAdapter == null)
                return;

            try
            {
                var statistics = selectedAdapter.GetIPv4Statistics();

                long currentReceived = statistics.BytesReceived;
                long currentSent = statistics.BytesSent;

                DateTime currentTime = DateTime.UtcNow;

                double elapsedSeconds =
                    (currentTime - previousSampleTime).TotalSeconds;

                if (elapsedSeconds <= 0)
                    return;

                long receivedDifference =
                    Math.Max(0, currentReceived - previousReceived);

                long sentDifference =
                    Math.Max(0, currentSent - previousSent);

                double downloadMbps =
                    receivedDifference * 8.0 /
                    elapsedSeconds / 1_000_000;

                double uploadMbps =
                    sentDifference * 8.0 /
                    elapsedSeconds / 1_000_000;

                totalDownloaded += receivedDifference;
                totalUploaded += sentDifference;

                previousReceived = currentReceived;
                previousSent = currentSent;
                previousSampleTime = currentTime;

                DownloadSpeedText.Text =
                    $"{downloadMbps:F2} Mbps";

                UploadSpeedText.Text =
                    $"{uploadMbps:F2} Mbps";

                TotalDownloadedText.Text =
                    FormatBytes(totalDownloaded);

                TotalUploadedText.Text =
                    FormatBytes(totalUploaded);

                AddSample(downloadHistory, downloadMbps);
                AddSample(uploadHistory, uploadMbps);

                DrawGraph();
            }
            catch (NetworkInformationException)
            {
                monitoringTimer.Stop();

                MonitoringStatusText.Text =
                    "Unable to read adapter statistics.";
            }
        }

        private void AddSample(List<double> history, double value)
        {
            history.Add(value);

            if (history.Count > MaxSamples)
                history.RemoveAt(0);
        }

        private string FormatBytes(long bytes)
        {
            double megabytes = bytes / 1024.0 / 1024.0;

            if (megabytes >= 1024)
                return $"{megabytes / 1024:F2} GB";

            return $"{megabytes:F2} MB";
        }

        private void ResetMeasurements()
        {
            totalDownloaded = 0;
            totalUploaded = 0;

            downloadHistory.Clear();
            uploadHistory.Clear();

            DownloadSpeedText.Text = "0.00 Mbps";
            UploadSpeedText.Text = "0.00 Mbps";

            TotalDownloadedText.Text = "0 MB";
            TotalUploadedText.Text = "0 MB";

            DrawGraph();
        }

        private void DrawGraph()
        {
            if (TrafficCanvas == null)
                return;

            TrafficCanvas.Children.Clear();

            double width = TrafficCanvas.ActualWidth;
            double height = TrafficCanvas.ActualHeight;

            if (width < 20 || height < 20)
                return;

            // Horizontal reference lines.
            for (int i = 1; i <= 4; i++)
            {
                double y = height * i / 5;

                Line gridLine = new Line
                {
                    X1 = 0,
                    Y1 = y,
                    X2 = width,
                    Y2 = y,
                    Stroke = new SolidColorBrush(
                        Color.FromRgb(38, 52, 75)),
                    StrokeThickness = 1
                };

                TrafficCanvas.Children.Add(gridLine);
            }

            DrawHistory(downloadHistory, width, height, "#299DFF");
            DrawHistory(uploadHistory, width, height, "#A78BFA");
        }

        private void DrawHistory(
            List<double> history,
            double width,
            double height,
            string color)
        {
            if (history.Count < 2)
                return;

            double maxValue = Math.Max(
                1,
                Math.Max(
                    downloadHistory.DefaultIfEmpty(0).Max(),
                    uploadHistory.DefaultIfEmpty(0).Max()
                )
            );

            Polyline line = new Polyline
            {
                Stroke = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString(color)),
                StrokeThickness = 2.5,
                StrokeLineJoin = PenLineJoin.Round
            };

            for (int i = 0; i < history.Count; i++)
            {
                double x = width * i / (MaxSamples - 1);

                double y = height -
                    (history[i] / maxValue * (height * 0.85));

                line.Points.Add(new Point(x, y));
            }

            TrafficCanvas.Children.Add(line);
        }
    }
}
