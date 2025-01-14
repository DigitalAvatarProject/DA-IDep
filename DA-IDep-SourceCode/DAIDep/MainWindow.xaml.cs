using DAIDep.View;
using System;
using System.Data.SQLite;
using System.Windows;
using System.Windows.Input;
using System.Windows.Navigation;
using Forms = System.Windows.Forms;
using Drawing = System.Drawing;
using System.Windows.Controls;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Threading.Tasks;
using System.Threading;
using System.Security.Principal;
using System.Linq;


namespace DAIDep
{
    public partial class MainWindow : Window
    {
        private static Mutex _mutex;
        private Forms.NotifyIcon _notifyIcon;
        private SQLiteConnection connection;

        public MainWindow()
        {
            var args = Environment.GetCommandLineArgs();
            bool isMinimized = args.Contains("--minimized", StringComparer.OrdinalIgnoreCase);

            bool createdNew;
            _mutex = new Mutex(true, "DAIDep_SingleInstance", out createdNew);

            if (!createdNew)
            {
                if (isMinimized)
                {
                    NotifyExistingInstance("MinimizeToTray");
                }
                else
                {
                    NotifyExistingInstance("ShowWindow");
                }
                CloseApplication();
                return;
            }

            InitializeComponent();
            InitializeNotifyIcon();
            StartNamedPipeServer();

            ConfigPage configPage = new ConfigPage();
            configPage.UpdateSystemStatus();
            MainFrame.Content = configPage;

            if (isMinimized)
            {
                MinimizeToTray();
            }
        }

        private bool IsRunAsAdministrator()
        {
            try
            {
                WindowsIdentity id = WindowsIdentity.GetCurrent();
                WindowsPrincipal principal = new WindowsPrincipal(id);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch (Exception ex)
            {
                LogException(ex);
                return false;
            }
        }

        private void RelaunchAsStandardUser(string[] originalArgs)
        {
            try
            {
                string exePath = Process.GetCurrentProcess().MainModule.FileName;
                var newArgs = originalArgs.Skip(1)
                                         .Where(arg => !arg.Equals("--standard-user", StringComparison.OrdinalIgnoreCase))
                                         .ToList();
                newArgs.Add("--standard-user");
                string arguments = string.Join(" ", newArgs.Select(arg => $"\"{arg}\""));
                
                string taskName = "SZTSProgramGUIRelaunch";
                string taskCmd = $"schtasks /Create /TN \"{taskName}\" /TR \"\\\"{exePath}\\\" {arguments}\" /SC ONCE /ST 00:00 /RL LIMITED /F";
                string taskRun = $"schtasks /Run /TN \"{taskName}\"";
                string taskDelete = $"schtasks /Delete /TN \"{taskName}\" /F";
                
                bool taskExists = false;
                string checkTask = $"schtasks /Query /TN \"{taskName}\"";
                ProcessStartInfo checkTaskInfo = new ProcessStartInfo("cmd.exe", $"/C {checkTask}")
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true
                };

                using (Process checkTaskProcess = Process.Start(checkTaskInfo))
                {
                    checkTaskProcess.WaitForExit();
                    taskExists = (checkTaskProcess.ExitCode == 0);
                }
                if (!taskExists)
                {
                    ProcessStartInfo createTaskInfo = new ProcessStartInfo("cmd.exe", $"/C {taskCmd}")
                    {
                        CreateNoWindow = true,
                        UseShellExecute = false
                    };
                    using (Process createTask = Process.Start(createTaskInfo))
                    {
                        createTask.WaitForExit();
                    }
                }

                ProcessStartInfo runTaskInfo = new ProcessStartInfo("cmd.exe", $"/C {taskRun}")
                {
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                Process.Start(runTaskInfo);
                CloseApplication();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"以普通用户权限重启异常: {ex.Message}");
            }
        }

        private void InitializeNotifyIcon()
        {
            _notifyIcon = new Forms.NotifyIcon
            {
                Icon = new Drawing.Icon("C:\\SZTSProgramInstaller\\SZTSProgram\\finallogo2.ico"),
                Visible = false
            };
            _notifyIcon.MouseClick += NotifyIcon_MouseClick;

            var contextMenu = new Forms.ContextMenuStrip();
            var exitMenuItem = new Forms.ToolStripMenuItem("  退出", null, ExitMenuItem_Click);
            contextMenu.Items.Add(exitMenuItem);
            _notifyIcon.ContextMenuStrip = contextMenu;
        }
        private void NotifyExistingInstance(string command)
        {
            try
            {
                using (var client = new NamedPipeClientStream(".", "DAIDep_Pipe", PipeDirection.Out))
                {
                    client.Connect(1000);
                    using (var writer = new StreamWriter(client))
                    {
                        writer.WriteLine(command);
                        writer.Flush();
                    }
                }
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void StartNamedPipeServer()
        {
            System.Threading.Tasks.Task.Run(() =>
            {
                while (true)
                {
                    using (var server = new NamedPipeServerStream("DAIDep_Pipe", PipeDirection.In))
                    {
                        try
                        {
                            server.WaitForConnection();
                            using (var reader = new StreamReader(server))
                            {
                                string command = reader.ReadLine();
                                if (command == "ShowWindow")
                                {
                                    System.Windows.Application.Current.Dispatcher.Invoke(() => ShowWindowFromTray());
                                }
                                else if (command == "MinimizeToTray")
                                {
                                    System.Windows.Application.Current.Dispatcher.Invoke(() => MinimizeToTray());
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            LogException(ex);
                        }
                    }
                }
            });
        }

        private void LogException(Exception ex)
        {
            string logPath = "DAIDep_Log.txt";
            File.AppendAllText(logPath, $"[{DateTime.Now}] {ex}\n");
        }

        private void LogExceptionS(string logMessage)
        {
            string logPath = "DAIDep_Log.txt";
            File.AppendAllText(logPath, $"[{DateTime.Now}] {logMessage}\n");
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            MinimizeToTray();
        }

        private void MinimizeToTray()
        {
            this.Hide();
            _notifyIcon.Visible = true;
        }

        private void ShowWindowFromTray()
        {
            if (!this.IsVisible)
            {
                this.Show();
            }
            if (this.WindowState == WindowState.Minimized)
            {
                this.WindowState = WindowState.Normal;
            }
            _notifyIcon.Visible = false;
        }

        private void NotifyIcon_MouseClick(object sender, Forms.MouseEventArgs e)
        {
            if (e.Button == Forms.MouseButtons.Left)
            {
                ShowWindowFromTray();
            }
        }

        private void ExitMenuItem_Click(object sender, EventArgs e)
        {
            CloseApplication();
        }
        
        public void CloseApplication()
        {
            try
            {
                if (connection != null && connection.State == System.Data.ConnectionState.Open)
                {
                    connection.Close();
                    connection.Dispose();
                }
            }
            catch (Exception ex)
            {
                LogException(ex);
            }

            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
                _notifyIcon = null;
            }

            if (_mutex != null)
            {
                try
                {
                    _mutex.ReleaseMutex();
                }
                catch (Exception ex)
                {
                    LogException(ex);
                }
                finally
                {
                    _mutex = null;
                }
            }

            try
            {
                if (System.Windows.Application.Current != null)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        System.Windows.Application.Current.Shutdown();
                    });
                }
                else
                {
                   Environment.Exit(0);
                }
            }
            catch (Exception ex)
            {
                LogException(ex);
                try
                {
                    Environment.Exit(0);
                }
                catch
                {
                }
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            CloseApplication();
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        private void MainFrame_Navigated(object sender, NavigationEventArgs e)
        {
        }
    }

    public static class DisableNavigation
    {
        public static bool GetDisable(DependencyObject o)
        {
            return (bool)o.GetValue(DisableProperty);
        }

        public static void SetDisable(DependencyObject o, bool value)
        {
            o.SetValue(DisableProperty, value);
        }

        public static readonly DependencyProperty DisableProperty =
            DependencyProperty.RegisterAttached("Disable", typeof(bool), typeof(DisableNavigation), new PropertyMetadata(false, DisableChanged));

        public static void DisableChanged(object  sender, DependencyPropertyChangedEventArgs e)
        {
            var frame = (Frame)sender;
            frame.Navigated += DontNavigate;
            frame.NavigationUIVisibility = NavigationUIVisibility.Hidden;
        }

        public static void DontNavigate(object sender, NavigationEventArgs e)
        {
            ((Frame)sender).NavigationService.RemoveBackEntry();
        }
    }
}
