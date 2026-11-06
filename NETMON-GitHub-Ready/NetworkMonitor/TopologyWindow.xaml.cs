
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace NetworkMonitor
{
    public partial class TopologyWindow : Window
    {
        public TopologyWindow()
        {
            InitializeComponent();

            Loaded += (sender, e) => DrawNetwork();
            SizeChanged += (sender, e) => DrawNetwork();
        }

        private void RefreshMap_Click(object sender, RoutedEventArgs e)
        {
            DrawNetwork();
        }

        private void DrawNetwork()
        {
            NetworkCanvas.Children.Clear();

            double width = NetworkCanvas.ActualWidth;
            double height = NetworkCanvas.ActualHeight;

            if (width < 200 || height < 200)
                return;

            var devices = DevicesWindow.Devices.ToList();

            DeviceCountText.Text = devices.Count.ToString();

            double centerX = width / 2;
            double centerY = height / 2;

            double centerSize = 95;

            // Draw central network node.
            DrawConnection(
                centerX,
                centerY,
                centerX,
                centerY
            );

            // Central network device.
            DrawNode(
                centerX,
                centerY,
                centerSize,
                "#1677D2",
                "NETWORK",
                "MONITOR"
            );

            if (devices.Count == 0)
            {
                AddText(
                    "No devices configured yet.",
                    centerX - 105,
                    centerY + 85,
                    14,
                    "#8795AC"
                );

                AddText(
                    "Add devices in the Network Devices section.",
                    centerX - 160,
                    centerY + 110,
                    11,
                    "#65748C"
                );

                return;
            }

            double radius = Math.Min(
                Math.Min(width * 0.37, height * 0.37),
                250
            );

            for (int i = 0; i < devices.Count; i++)
            {
                var device = devices[i];

                double angle =
                    (2 * Math.PI * i / devices.Count)
                    - (Math.PI / 2);

                double x = centerX + radius * Math.Cos(angle);
                double y = centerY + radius * Math.Sin(angle);

                // Draw connection to central node.
                DrawConnection(centerX, centerY, x, y);

                // Choose node colour based on availability.
                string nodeColor = device.Status == "Online"
                    ? "#16834D"
                    : device.Status == "Offline"
                        ? "#A83242"
                        : "#526078";

                DrawNode(
                    x,
                    y,
                    75,
                    nodeColor,
                    device.Name,
                    device.IPAddress
                );
            }
        }

        private void DrawConnection(
            double x1,
            double y1,
            double x2,
            double y2)
        {
            Line line = new Line
            {
                X1 = x1,
                Y1 = y1,
                X2 = x2,
                Y2 = y2,
                Stroke = new SolidColorBrush(
                    Color.FromRgb(41, 157, 255)
                ),
                StrokeThickness = 2,
                Opacity = 0.55
            };

            NetworkCanvas.Children.Add(line);
        }

        private void DrawNode(
            double centerX,
            double centerY,
            double size,
            string color,
            string title,
            string subtitle)
        {
            Ellipse node = new Ellipse
            {
                Width = size,
                Height = size,
                Fill = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString(color)
                ),
                Stroke = Brushes.White,
                StrokeThickness = 1.5
            };

            Canvas.SetLeft(node, centerX - size / 2);
            Canvas.SetTop(node, centerY - size / 2);

            NetworkCanvas.Children.Add(node);

            TextBlock titleText = new TextBlock
            {
                Text = title,
                Foreground = Brushes.White,
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center,
                Width = size + 30
            };

            Canvas.SetLeft(titleText, centerX - (size + 30) / 2);
            Canvas.SetTop(titleText, centerY - 9);

            NetworkCanvas.Children.Add(titleText);

            TextBlock subtitleText = new TextBlock
            {
                Text = subtitle,
                Foreground = Brushes.White,
                FontSize = 9,
                TextAlignment = TextAlignment.Center,
                Width = size + 50
            };

            Canvas.SetLeft(subtitleText, centerX - (size + 50) / 2);
            Canvas.SetTop(subtitleText, centerY + 9);

            NetworkCanvas.Children.Add(subtitleText);
        }

        private void AddText(
            string text,
            double x,
            double y,
            double fontSize,
            string color)
        {
            TextBlock block = new TextBlock
            {
                Text = text,
                Foreground = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString(color)
                ),
                FontSize = fontSize
            };

            Canvas.SetLeft(block, x);
            Canvas.SetTop(block, y);

            NetworkCanvas.Children.Add(block);
        }
    }
}
