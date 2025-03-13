using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using LibreHardwareMonitor.Hardware;
using SystemWindowsForms = System.Windows.Forms;
using SystemDrawing = System.Drawing;
using System.Configuration;
using System.ComponentModel;

namespace EngineStatsClient
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
    {
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TRANSPARENT = 0x00000020;
		private const int WS_EX_LAYERED = 0x00080000;

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hwnd, int nIndex, int dwNewLong);
		[DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hwnd, int nIndex);

		private Computer _computer;
		private DispatcherTimer _timer;
		private float[] cpuHistory = new float[3];
		private int cpuIndex = 0;
		private bool isDragging = false;
		private System.Windows.Point startPoint;
		private NotifyIcon trayIcon;
		private ToolStripMenuItem showHideMenuItem;

		public MainWindow()
        {
            InitializeComponent();
            Left = Properties.Settings.Default.WindowLeft;
            Top =  Properties.Settings.Default.WindowTop;
			Loaded += MainWindow_Loaded; // This fires when the window is loaded
			Deactivated += Window_Deactivated; // Deactivated fires when the window loses focus

			trayIcon = new NotifyIcon()
			{
				Icon = new SystemDrawing.Icon("EngineStatsIcon.ico"),
				Visible = true,
				Text = "Engine Stats"
			};
			trayIcon.DoubleClick += TrayIcon_DoubleClick;

			ContextMenuStrip menu = new ContextMenuStrip();
			showHideMenuItem = new ToolStripMenuItem("Hide", null, ToggleShowHide); // We do it this way to have a reference so we can change the text
			menu.Items.Add(showHideMenuItem);
			menu.Items.Add("Exit", null, (s, e) => CloseApp());
			trayIcon.ContextMenuStrip = menu;

		}
		private void Window_Deactivated(object sender, EventArgs e)
		{
			// Force back to topmost and visible when focus is lost
			Topmost = false; // Briefly reset
			Topmost = true; // Then set it back
			Visibility = Visibility.Visible;
			Activate();
		}
		private void MainWindow_Loaded(object sender, RoutedEventArgs e)
		{
			try
			{
				if (!IsAdministrator())
				{
					System.Windows.MessageBox.Show("Please run this application as an administrator for full hardware access.", 
						"Warning",
						MessageBoxButton.OK, MessageBoxImage.Warning);
				}
				// Setup LibreHardwareMonitor
				_computer = new Computer()
				{
					IsCpuEnabled = true, // Enable CPU monitoring
					IsGpuEnabled = true, // Enable GPU monitoring
					IsMemoryEnabled = true, // Enable Memory monitoring
				};
				_computer.Open(); // Start monitoring

				// Update UI with a timer
				_timer = new DispatcherTimer()
				{
					Interval = TimeSpan.FromSeconds(1)
				};
				_timer.Tick += (s, args) => UpdatePerformanceData(); // Update performance data every second. Lambda expression
				_timer.Start();
			}
			catch (Exception ex)
			{
				System.Windows.MessageBox.Show($"Error: {ex.Message}");
			}
		}
		private bool IsAdministrator()
		{
			using (var identity = System.Security.Principal.WindowsIdentity.GetCurrent())
			{
				var principal = new System.Security.Principal.WindowsPrincipal(identity);
				return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
			}
		}
		private void UpdatePerformanceData()
		{
			try
			{
				_computer.Accept(new UpdateVisitor());

				float cpuUsage = 0, gpuUsage = float.NaN, ramUsage = 0;

				foreach (var hardware in _computer.Hardware)
				{
					hardware.Update(); // Update hardware data

					if (hardware.HardwareType == HardwareType.Cpu)
					{
						foreach (var sensor in hardware.Sensors)
						{
							if (sensor.SensorType == SensorType.Load && sensor.Name.Contains("CPU Total"))
							{
								cpuUsage = sensor.Value ?? 0;
								cpuHistory[cpuIndex % 3] = cpuUsage; // Store the last 3 CPU usages
								cpuIndex++;
								cpuUsage = cpuHistory.Take(Math.Min(cpuIndex, 3)).Average(); // Calculate the average of the last 3 CPU usages
							}
						}
					}
					else if (hardware.HardwareType == HardwareType.GpuNvidia || hardware.HardwareType == HardwareType.GpuAmd)
					{
						foreach (var sensor in hardware.Sensors)
						{
							if (sensor.SensorType == SensorType.Load)
							{
								gpuUsage = sensor.Value ?? 0;
							}
						}
					}
					else if (hardware.HardwareType == HardwareType.Memory)
					{
						foreach (var sensor in hardware.Sensors)
						{
							if (sensor.SensorType == SensorType.Data && (sensor.Name == "Used Memory" || sensor.Name.Contains("Memory Used")))
							{
								ramUsage = sensor.Value ?? 0;
							}
						}
					}
				}
				cpuUsage = (float)Math.Round(cpuUsage, 1);
				gpuUsage = float.IsNaN(gpuUsage) ? gpuUsage : (float)Math.Round(gpuUsage, 1);
				ramUsage = (float)Math.Round(ramUsage, 1);
				PerformanceText.Text = $"CPU: {cpuUsage:F1}% | GPU: {gpuUsage:F1}% | RAM: {ramUsage:F1}GB";
			}
			catch (Exception ex)
			{
				PerformanceText.Text = $"Error: {ex.Message}";
			}
		}

		// Mouse drag events
		private void Border_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			isDragging = true;
			startPoint = e.GetPosition(this);
			((Border)sender).CaptureMouse();
		}
		private void Border_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
		{
			if (isDragging)
			{
				System.Windows.Point currentPoint = e.GetPosition(this);
				double deltaX = currentPoint.X - startPoint.X;
				double deltaY = currentPoint.Y - startPoint.Y;
				Left += deltaX;
				Top += deltaY;
			}
		}
		private void Border_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			isDragging = false;
			((Border)sender).ReleaseMouseCapture();

			// Save the new position when dragging stops
			Properties.Settings.Default.WindowLeft = Left;
			Properties.Settings.Default.WindowTop = Top;
			Properties.Settings.Default.Save();
		}

		private void TrayIcon_DoubleClick(object sender, EventArgs e)
		{
			if (Visibility == Visibility.Hidden)
			{
				Visibility = Visibility.Visible; 
				showHideMenuItem.Text = "Hide";
			}
		}
		private void ToggleShowHide(object sender, EventArgs e)
		{
			if (Visibility == Visibility.Visible)
			{
				Visibility = Visibility.Hidden;
				showHideMenuItem.Text = "Show";
			}
			else
			{
				Visibility = Visibility.Visible;
				showHideMenuItem.Text = "Hide";
			}
		}
		private void CloseApp()
		{
			_computer?.Close();
			trayIcon.Visible = false;
			trayIcon.Dispose();
			System.Windows.Application.Current.Shutdown();
		}
	}

	public class UpdateVisitor : IVisitor
	{
		public void VisitComputer(IComputer computer)
		{
			computer.Traverse(this);
		}

		public void VisitHardware(IHardware hardware)
		{
			hardware.Update();
			foreach (IHardware subHardware in hardware.SubHardware)
			{
				subHardware.Accept(this);
			}
		}

		public void VisitParameter(IParameter parameter)
		{
		}

		public void VisitSensor(ISensor sensor)
		{
		}
	}
}