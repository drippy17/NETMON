
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace NetworkMonitor
{
    public partial class DevicesWindow : Window
    {
        private static readonly string StorageFolder =
            Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData),
                "NetworkMonitor"
            );

        private static readonly string StorageFile =
            Path.Combine(StorageFolder, "netmon-data.json");

        public static ObservableCollection<NetworkDevice> Devices { get; }
            = new ObservableCollection<NetworkDevice>();

        public static ObservableCollection<NetworkMonitorAlert> Alerts { get; }
            = new ObservableCollection<NetworkMonitorAlert>();

        private static readonly SemaphoreSlim checkLock =
            new SemaphoreSlim(1, 1);

        // Load saved devices and alerts when the class is first accessed.
        static DevicesWindow()
        {
            LoadData();
        }

        public DevicesWindow()
        {
            InitializeComponent();

            DevicesDataGrid.ItemsSource = Devices;

            UpdateDeviceCount();
        }

        // ==========================================
        // PERSISTENT STORAGE
        // ==========================================

        public static void LoadData()
        {
            try
            {
                if (!File.Exists(StorageFile))
                    return;

                string json = File.ReadAllText(StorageFile);

                NetworkMonitorData? savedData =
                    JsonSerializer.Deserialize<NetworkMonitorData>(json);

                if (savedData == null)
                    return;

                Devices.Clear();
                Alerts.Clear();

                foreach (NetworkDevice device in savedData.Devices)
                {
                    Devices.Add(device);
                }

                foreach (NetworkMonitorAlert alert in savedData.Alerts)
                {
                    alert.PropertyChanged += Alert_PropertyChanged;

                    Alerts.Add(alert);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "NETMON could not load its saved data.\n\n" + ex.Message,
                    "Storage Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
            }
        }

        public static void SaveData()
        {
            try
            {
                Directory.CreateDirectory(StorageFolder);

                NetworkMonitorData data = new NetworkMonitorData
                {
                    Devices = Devices.ToList(),
                    Alerts = Alerts.ToList()
                };

                JsonSerializerOptions options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };

                string json = JsonSerializer.Serialize(data, options);

                string temporaryFile = StorageFile + ".tmp";

                File.WriteAllText(temporaryFile, json);

                File.Move(temporaryFile, StorageFile, true);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    "NETMON storage error: " + ex.Message
                );
            }
        }

        private static void Alert_PropertyChanged(
            object? sender,
            PropertyChangedEventArgs e)
        {
            SaveData();
        }

        // ==========================================
        // ADD DEVICE
        // ==========================================

        private void AddDevice_Click(
            object sender,
            RoutedEventArgs e)
        {
            string name = DeviceNameBox.Text.Trim();
            string ip = IPAddressBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Please enter a device name.");
                return;
            }

            if (!IPAddress.TryParse(ip, out _))
            {
                MessageBox.Show("Please enter a valid IP address.");
                return;
            }

            if (Devices.Any(d => d.IPAddress == ip))
            {
                MessageBox.Show(
                    "This IP address is already configured."
                );
                return;
            }

            Devices.Add(new NetworkDevice
            {
                Name = name,
                IPAddress = ip,
                Status = "Not checked",
                ResponseTime = "-",
                LastChecked = "-"
            });

            SaveData();

            DeviceNameBox.Clear();
            IPAddressBox.Clear();

            UpdateDeviceCount();
        }

        // ==========================================
        // MANUAL DEVICE CHECK
        // ==========================================

        private async void CheckAll_Click(
            object sender,
            RoutedEventArgs e)
        {
            bool completed = await CheckAllDevicesAsync();

            if (!completed)
            {
                MessageBox.Show(
                    "A device check is already running, or no devices are registered."
                );
            }

            UpdateDeviceCount();
        }

        // Shared checking method used by automatic and manual monitoring.
        public static async Task<bool> CheckAllDevicesAsync()
        {
            if (!await checkLock.WaitAsync(0))
            {
                return false;
            }

            try
            {
                NetworkDevice[] devicesToCheck = Devices.ToArray();

                if (devicesToCheck.Length == 0)
                {
                    return false;
                }

                foreach (NetworkDevice device in devicesToCheck)
                {
                    if (Devices.Contains(device))
                    {
                        await CheckDeviceAsync(device);
                    }
                }

                SaveData();

                return true;
            }
            finally
            {
                checkLock.Release();
            }
        }

        // ==========================================
        // CHECK INDIVIDUAL DEVICE
        // ==========================================

        private static async Task CheckDeviceAsync(
            NetworkDevice device)
        {
            string previousStatus = device.Status;

            device.Status = "Checking...";
            device.ResponseTime = "-";

            device.LastChecked =
                DateTime.Now.ToString("dd MMM yyyy HH:mm:ss");

            string newStatus;

            try
            {
                using Ping ping = new Ping();

                PingReply reply = await ping.SendPingAsync(
                    device.IPAddress,
                    2000
                );

                if (reply.Status == IPStatus.Success)
                {
                    newStatus = "Online";

                    device.ResponseTime =
                        $"{reply.RoundtripTime} ms";
                }
                else
                {
                    newStatus = "Offline";
                    device.ResponseTime = "-";
                }
            }
            catch
            {
                newStatus = "Offline";
                device.ResponseTime = "-";
            }

            device.Status = newStatus;

            // Detect established status changes.
            if (previousStatus == "Online" &&
                newStatus == "Offline")
            {
                AddAlert(
                    device,
                    "High",
                    "Device went offline. The device is no longer responding to ping."
                );
            }
            else if (previousStatus == "Offline" &&
                     newStatus == "Online")
            {
                AddAlert(
                    device,
                    "Low",
                    "Device recovered. The device is responding to ping again."
                );
            }

            device.LastChecked =
                DateTime.Now.ToString("dd MMM yyyy HH:mm:ss");
        }

        // ==========================================
        // ALERT MANAGEMENT
        // ==========================================

        private static void AddAlert(
            NetworkDevice device,
            string severity,
            string description)
        {
            NetworkMonitorAlert alert = new NetworkMonitorAlert
            {
                Timestamp =
                    DateTime.Now.ToString("dd MMM yyyy HH:mm:ss"),

                DeviceName = device.Name,
                IPAddress = device.IPAddress,
                Severity = severity,
                Description = description,
                InvestigationStatus = "New"
            };

            alert.PropertyChanged += Alert_PropertyChanged;

            Alerts.Insert(0, alert);

            SaveData();
        }

        // ==========================================
        // REMOVE DEVICE
        // ==========================================

        private void RemoveDevice_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (DevicesDataGrid.SelectedItem is not NetworkDevice selected)
            {
                MessageBox.Show("Please select a device first.");
                return;
            }

            MessageBoxResult result = MessageBox.Show(
                $"Remove {selected.Name} from NETMON?",
                "Confirm Removal",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );

            if (result != MessageBoxResult.Yes)
                return;

            Devices.Remove(selected);

            SaveData();

            UpdateDeviceCount();
        }

        // ==========================================
        // UPDATE DEVICE COUNT
        // ==========================================

        private void UpdateDeviceCount()
        {
            DeviceCountText.Text =
                $"{Devices.Count} devices configured";
        }
    }

    // ==============================================
    // SAVED DATA MODEL
    // ==============================================

    public class NetworkMonitorData
    {
        public List<NetworkDevice> Devices { get; set; }
            = new List<NetworkDevice>();

        public List<NetworkMonitorAlert> Alerts { get; set; }
            = new List<NetworkMonitorAlert>();
    }

    // ==============================================
    // NETWORK DEVICE MODEL
    // ==============================================

    public class NetworkDevice : INotifyPropertyChanged
    {
        private string status = "Not checked";
        private string responseTime = "-";
        private string lastChecked = "-";

        public string Name { get; set; } = "";

        public string IPAddress { get; set; } = "";

        public string Status
        {
            get => status;

            set
            {
                if (status == value)
                    return;

                status = value;

                OnPropertyChanged(nameof(Status));
            }
        }

        public string ResponseTime
        {
            get => responseTime;

            set
            {
                if (responseTime == value)
                    return;

                responseTime = value;

                OnPropertyChanged(nameof(ResponseTime));
            }
        }

        public string LastChecked
        {
            get => lastChecked;

            set
            {
                if (lastChecked == value)
                    return;

                lastChecked = value;

                OnPropertyChanged(nameof(LastChecked));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(propertyName)
            );
        }
    }

    // ==============================================
    // NETWORK ALERT MODEL
    // ==============================================

    public class NetworkMonitorAlert : INotifyPropertyChanged
    {
        private string investigationStatus = "New";

        public string Timestamp { get; set; } = "";

        public string DeviceName { get; set; } = "";

        public string IPAddress { get; set; } = "";

        public string Severity { get; set; } = "";

        public string Description { get; set; } = "";

        public string InvestigationStatus
        {
            get => investigationStatus;

            set
            {
                if (investigationStatus == value)
                    return;

                investigationStatus = value;

                PropertyChanged?.Invoke(
                    this,
                    new PropertyChangedEventArgs(
                        nameof(InvestigationStatus)
                    )
                );
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
