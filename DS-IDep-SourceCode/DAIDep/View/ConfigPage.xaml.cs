using Microsoft.Win32;
using DAIDep.View.Configs;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static DAIDep.View.Configs.FileFoldsPage;
using System.IO;
using Newtonsoft.Json;
using System.Reflection.PortableExecutable;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Data.SQLite;
using System.IO;
using System.Text.Json.Nodes;
using System.Windows.Forms;


namespace DAIDep.View
{
    public partial class ConfigPage : Page
    {
        private string dbFilePath = @"C:\SZTSProgramInstaller\SZTSProgram\test.db";
        static string currentDate = DateTime.Now.ToString("yyyy-MM-dd");
        private string logFilePath = $"C:\\SZTSProgramInstaller\\SZTSConfig\\sztsfs_gui_logs\\{currentDate}_sztsfs_gui_log.txt";

        private string connectionString;
        private SQLiteConnection connection;
        private string line = "                                       ";
        private bool isButtonClicked = false;

        public ConfigPage()
        {
            InitializeComponent();
            UpdateSystemStatus();
            NaviFrame.Navigate(new FileFoldsPage());

            connectionString = $"Data Source={dbFilePath};Version=3;";
        }

        private void InitializeDatabase()
        {
            try
            {
                connection = new SQLiteConnection(connectionString);
                connection.Open();

                string createTableQuery = @"
                    CREATE TABLE IF NOT EXISTS filesystem(id INTEGER PRIMARY KEY,
                        'Date & Time' TEXT, 'Process Name' TEXT, 'PID' TEXT, 'Operation' TEXT, 'Result' TEXT,
                        'Detail' TEXT, 'Sequence' TEXT, 'Company' TEXT, 'Description' TEXT, 'Command Line' TEXT, 'User' TEXT, 'Image Path' TEXT,
                        'Session' TEXT, 'Path' TEXT, 'TID' TEXT, 'Relative Time' TEXT, 'Duration' TEXT, 'Time of Day' TEXT, 'Version' TEXT,
                        'Event Class' TEXT, 'Authentication ID' TEXT, 'Virtualized' TEXT, 'Integrity' TEXT, 'Category' TEXT, 'Parent PID' TEXT,
                        'Architecture' TEXT, 'Completion Time' TEXT);

                    CREATE TABLE IF NOT EXISTS network(id INTEGER PRIMARY KEY,
                        'Date & Time' TEXT, 'Process Name' TEXT, 'PID' TEXT, 'Operation' TEXT, 'Result' TEXT,
                        'Detail' TEXT, 'Sequence' TEXT, 'Company' TEXT, 'Description' TEXT, 'Command Line' TEXT, 'User' TEXT, 'Image Path' TEXT,
                        'Session' TEXT, 'Path' TEXT, 'TID' TEXT, 'Relative Time' TEXT, 'Duration' TEXT, 'Time of Day' TEXT, 'Version' TEXT,
                        'Event Class' TEXT, 'Authentication ID' TEXT, 'Virtualized' TEXT, 'Integrity' TEXT, 'Category' TEXT, 'Parent PID' TEXT,
                        'Architecture' TEXT, 'Completion Time' TEXT);

                    CREATE TABLE IF NOT EXISTS process(id INTEGER PRIMARY KEY,
                        'Date & Time' TEXT, 'Process Name' TEXT, 'PID' TEXT, 'Operation' TEXT, 'Result' TEXT,
                        'Detail' TEXT, 'Sequence' TEXT, 'Company' TEXT, 'Description' TEXT, 'Command Line' TEXT, 'User' TEXT, 'Image Path' TEXT,
                        'Session' TEXT, 'Path' TEXT, 'TID' TEXT, 'Relative Time' TEXT, 'Duration' TEXT, 'Time of Day' TEXT, 'Version' TEXT,
                        'Event Class' TEXT, 'Authentication ID' TEXT, 'Virtualized' TEXT, 'Integrity' TEXT, 'Category' TEXT, 'Parent PID' TEXT,
                        'Architecture' TEXT, 'Completion Time' TEXT);

                    CREATE TABLE IF NOT EXISTS malicious(id INTEGER PRIMARY KEY,
                        'Date & Time' TEXT, 'Process Name' TEXT, 'PID' TEXT, 'Operation' TEXT, 'Result' TEXT,
                        'Detail' TEXT, 'Sequence' TEXT, 'Company' TEXT, 'Description' TEXT, 'Command Line' TEXT, 'User' TEXT, 'Image Path' TEXT,
                        'Session' TEXT, 'Path' TEXT, 'TID' TEXT, 'Relative Time' TEXT, 'Duration' TEXT, 'Time of Day' TEXT, 'Version' TEXT,
                        'Event Class' TEXT, 'Authentication ID' TEXT, 'Virtualized' TEXT, 'Integrity' TEXT, 'Category' TEXT, 'Parent PID' TEXT,
                        'Architecture' TEXT, 'Completion Time' TEXT, 'Malicious File Path' TEXT);

                    CREATE TABLE IF NOT EXISTS operations (
                        'Time of Day' TEXT,
                        'Operation' TEXT,
                        'Result' TEXT
                    )";

                using (SQLiteCommand command = new SQLiteCommand(createTableQuery, connection))
                {
                    command.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"日志数据库初始化失败: {ex.Message}");
            }
        }
        
        private void LogOperationToFile(string timeOfDay, string operation, string result)
        {
            try
            {
                string logMessage = $"{timeOfDay},{operation},{result}";

                string directoryPath = System.IO.Path.GetDirectoryName(logFilePath);
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                if (!File.Exists(logFilePath))
                {
                    File.Create(logFilePath).Dispose();
                }

                using (FileStream fs = new FileStream(logFilePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
                using (StreamWriter sw = new StreamWriter(fs))
                {
                    sw.WriteLine(logMessage);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"用户操作行为写入日志文件失败: {ex.Message}");
            }
        }

        public async Task<(bool, List<string>)> CheckOpenFilesAsync(string logFilePath)
        {
            string tempLogFilePath = logFilePath + "_tmp";

            try
            {
                File.Copy(logFilePath, tempLogFilePath, true);
                Dictionary<string, int> fileCounts = new Dictionary<string, int>();
                string[] lines = await File.ReadAllLinesAsync(tempLogFilePath);
                for (int i = lines.Length - 1; i >= 0; i--)
                {
                    string line = lines[i];
                    string[] parts = line.Split(',');
                    if (parts.Length == 2)
                    {
                        string filePath = parts[0].Trim();
                        if (int.TryParse(parts[1].Trim(), out int count))
                        {
                            if (!fileCounts.ContainsKey(filePath))
                            {
                                fileCounts[filePath] = count;
                            }
                        }
                    }
                }

                List<string> openFiles = new List<string>();
                foreach (var fileEntry in fileCounts)
                {
                    if (fileEntry.Value != 0 && !fileEntry.Key.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                    {
                        string fileName = System.IO.Path.GetFileName(fileEntry.Key);
                        openFiles.Add(fileName);
                    }
                }
                return (openFiles.Count > 0, openFiles);
            }
            catch (IOException ex)
            {
                System.Windows.MessageBox.Show($"无法访问日志文件：{ex.Message}");
                return (false, null);
            }
            finally
            {
                if (File.Exists(tempLogFilePath))
                {
                    try
                    {
                        File.Delete(tempLogFilePath);
                    }
                    catch (IOException ex)
                    {
                        System.Windows.MessageBox.Show($"无法删除临时日志文件：{ex.Message}");
                    }
                }
            }
        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var listBox = sender as System.Windows.Controls.ListBox;
            var selectedItem = listBox?.SelectedItem as ListBoxItem;
            switch (selectedItem.Content)
            {
                case "数字替身系统配置":
                    NaviFrame.Navigate(new ProtectPwdPage());
                    break;
                case "防护文件列表配置":
                    NaviFrame.Navigate(new FileFoldsPage());
                    break;
                case "日志展示":
                    NavigationService.GetNavigationService(this).Navigate(new DataBasePage());
                    break;
                default:
                    break;
            }
        }

        private async void LaunchExeButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string DrivePath = "C:\\SZTSProgramInstaller\\SZTSConfig\\szts_encdir";
                string DriveLetter = "V:";
                string DrivePassword = "12345678";

                string jsonPath = @"C:\SZTSProgramInstaller\SZTSConfig\forms.json";
                if (File.Exists(jsonPath))
                {
                    string jsonStr = File.ReadAllText(jsonPath);
                    dynamic jsonObj = JsonConvert.DeserializeObject(jsonStr);

                    if (jsonObj.driveLetter != null)
                    {
                        DriveLetter = jsonObj.driveLetter;
                    }
                }

                if (Directory.Exists(DriveLetter + "//"))
                {
                    System.Windows.MessageBox.Show("当前文件系统已挂载到 " + DriveLetter[0] + " 盘！");
                }
                else
                {
                    string exePath = @"unionFileSubstituteSystem.exe";
                    string arguments = $"--mode m";

                    ProcessStartInfo startInfo = new ProcessStartInfo
                    {
                        FileName = exePath,
                        Arguments = arguments,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true,
                        UseShellExecute = false
                    };

                    using (Process process = new Process { StartInfo = startInfo })
                    {
                        process.StartInfo.StandardOutputEncoding = Encoding.UTF8;
                        process.Start();

                        string result = await process.StandardOutput.ReadToEndAsync();
                        await process.WaitForExitAsync();

                        if (result.Trim().StartsWith("Success"))
                        {
                            System.Windows.MessageBox.Show("已成功挂载文件系统到 " + DriveLetter[0] + " 盘！");

                            string currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                            string operation = "用户挂载文件系统到 " + DriveLetter[0] + " 盘" + line;
                            string cur_result = "成功";
                            LogOperationToFile(currentTime, operation, cur_result);
                        }

                        if (result.Trim().StartsWith("Error"))
                        {
                            System.Windows.MessageBox.Show("挂载文件系统失败: " + result);

                            string currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                            string operation = "用户挂载文件系统到 " + DriveLetter[0] + " 盘" + line;
                            string cur_result = "失败: " + result;
                            LogOperationToFile(currentTime, operation, cur_result);
                        }
                    }
                    UpdateSystemStatus();
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("挂载文件系统失败: " + ex.Message);

                string currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                string operation = "用户挂载文件系统" + line;
                string cur_result = "失败: " + ex.Message;
                LogOperationToFile(currentTime, operation, cur_result);
            }
        }

        private void ShutDwExeButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string exePath = @"C:\SZTSProgramInstaller\SZTSProgram\unionFileSubstituteSystem.exe";
                string DriveLetter = "V:";
                string jsonPath = @"C:\SZTSProgramInstaller\SZTSConfig\forms.json";
                if (File.Exists(jsonPath))
                {
                    string jsonStr = File.ReadAllText(jsonPath);
                    dynamic jsonObj = JsonConvert.DeserializeObject(jsonStr);

                    if (jsonObj.driveLetter != null)
                    {
                        DriveLetter = jsonObj.driveLetter;
                    }
                }

                if (!Directory.Exists(DriveLetter + "//"))
                {
                    System.Windows.MessageBox.Show("当前尚未挂载文件系统到 " + DriveLetter + " 盘！");
                }
                else
                {
                    string arguments = $"--mode u";

                    ProcessStartInfo startInfo = new ProcessStartInfo();
                    startInfo.FileName = exePath;
                    startInfo.Arguments = arguments;
                    startInfo.RedirectStandardOutput = true;
                    startInfo.CreateNoWindow = true;
                    startInfo.UseShellExecute = false;

                    using (Process process = Process.Start(startInfo))
                    {
                        using (StreamReader reader = process.StandardOutput)
                        {
                            string result = reader.ReadToEnd();
                            process.WaitForExit();

                            var encfsProcesses = Process.GetProcessesByName("encfs");
                            foreach (var a_process in encfsProcesses)
                            {
                                a_process.Kill();
                                a_process.WaitForExit();
                            }

                            if (result.Trim().StartsWith("Success"))
                            {
                                System.Windows.MessageBox.Show("已成功卸载文件系统！");
                                string currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                                string operation = "用户卸载" + DriveLetter + " 盘" + line;
                                string cur_result = "成功";
                                LogOperationToFile(currentTime, operation, cur_result);
                            }
                            else
                            {
                                System.Windows.MessageBox.Show("卸载文件系统失败: " + result);

                                string currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                                string operation = "用户卸载" + DriveLetter + " 盘" + line;
                                string cur_result = "失败: " + result;
                                LogOperationToFile(currentTime, operation, cur_result);
                            }
                        }
                    }
                }
                UpdateSystemStatus();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("卸载文件系统失败: " + ex.Message);

                string currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                string operation = "用户卸载文件系统" + line;
                string cur_result = "失败: " + ex.Message;
                LogOperationToFile(currentTime, operation, cur_result);
            }
        }

        private void StartProcButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string folderPath = @"C:\\SZTSProgramInstaller\\SZTSProgram\\malicious_file";
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                string LogKeywords = "敏感文件资料";
                int LogStatus = 0;
                string jsonPath = @"C:\SZTSProgramInstaller\SZTSConfig\forms.json";
                if (File.Exists(jsonPath))
                {
                    string jsonStr = File.ReadAllText(jsonPath);
                    dynamic jsonObj = JsonConvert.DeserializeObject(jsonStr);

                    if (jsonObj.logKeywords != null)
                    {
                        LogKeywords = jsonObj.logKeywords;
                    }

                    if (jsonObj.LogStatus != null)
                    {
                        LogStatus = jsonObj.LogStatus;
                    }
                }

                var pmlwatchdog_process = Process.GetProcessesByName("pmlwatchdog");
                if (LogStatus == 1 && pmlwatchdog_process.Any())
                {
                    System.Windows.MessageBox.Show("当前已开启日志采集！");
                }
                else
                {
                    string exePath = @"C:\\SZTSProgramInstaller\\SZTSProgram\\SZTSLogCollectionProgram.exe";

                    var procmon_process = Process.GetProcessesByName("Procmon");
                    var procmon64_process = Process.GetProcessesByName("Procmon64");
                    if (procmon_process.Any() || procmon64_process.Any())
                    {
                        string end_arguments = $"--end";
                        ProcessStartInfo endInfo = new ProcessStartInfo();
                        endInfo.FileName = exePath;
                        endInfo.Verb = "runas";
                        endInfo.Arguments = end_arguments;
                        endInfo.CreateNoWindow = true;
                        endInfo.UseShellExecute = true;
                        endInfo.WindowStyle = ProcessWindowStyle.Hidden;

                        using (Process end_process = Process.Start(endInfo))
                        {
                            end_process.WaitForExit();
                        }

                        System.Threading.Thread.Sleep(7000);
                    }

                    string jsonString = File.ReadAllText(jsonPath);
                    JObject jsonObject = JObject.Parse(jsonString);
                    jsonObject["LogStatus"] = 1;

                    string arguments = $"--start";
                    ProcessStartInfo startInfo = new ProcessStartInfo();
                    startInfo.FileName = exePath;
                    startInfo.Verb = "runas";
                    startInfo.Arguments = arguments;
                    startInfo.CreateNoWindow = true;
                    startInfo.UseShellExecute = true;
                    startInfo.WindowStyle = ProcessWindowStyle.Hidden;

                    using (Process process = Process.Start(startInfo))
                    {
                        process.WaitForExit();
                        System.Windows.MessageBox.Show("已成功启动日志采集！");
                        File.WriteAllText(jsonPath, jsonObject.ToString(Formatting.Indented));

                        string currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                        string operation = "用户启动日志采集" + line;
                        string cur_result = "成功";
                        LogOperationToFile(currentTime, operation, cur_result);
                    }

                    var after_pmlwatchdog_process = Process.GetProcessesByName("pmlwatchdog");
                    if (after_pmlwatchdog_process.Any())
                    {
                        UpdateSystemStatus();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("日志采集启动失败: " + ex.Message);

                string currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                string operation = "用户启动日志采集" + line;
                string cur_result = "失败: " + ex.Message;
                LogOperationToFile(currentTime, operation, cur_result);
            }
        }

        private void KillProcButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int LogStatus = 0;
                string jsonPath = @"C:\SZTSProgramInstaller\SZTSConfig\forms.json";
                if (File.Exists(jsonPath))
                {
                    string jsonStr = File.ReadAllText(jsonPath);
                    dynamic jsonObj = JsonConvert.DeserializeObject(jsonStr);

                    if (jsonObj.LogStatus != null)
                    {
                        LogStatus = jsonObj.LogStatus;
                    }
                }

                var pmlwatchdog_process = Process.GetProcessesByName("pmlwatchdog");
                if (LogStatus == 0 && !pmlwatchdog_process.Any())
                {
                    System.Windows.MessageBox.Show("当前尚未开启日志采集！");
                }
                else
                {
                    string jsonString = File.ReadAllText(jsonPath);
                    JObject jsonObject = JObject.Parse(jsonString);
                    jsonObject["LogStatus"] = 0;

                    string exePath = @"C:\\SZTSProgramInstaller\\SZTSProgram\\SZTSLogCollectionProgram.exe";

                    string arguments = $"--end";

                    ProcessStartInfo startInfo = new ProcessStartInfo();
                    startInfo.FileName = exePath;
                    startInfo.Verb = "runas";
                    startInfo.Arguments = arguments;
                    startInfo.CreateNoWindow = true;
                    startInfo.UseShellExecute = true;
                    startInfo.WindowStyle = ProcessWindowStyle.Hidden;

                    using (Process process = Process.Start(startInfo))
                    {
                        process.WaitForExit();
                        System.Windows.MessageBox.Show("已成功终止日志采集！");
                        File.WriteAllText(jsonPath, jsonObject.ToString(Formatting.Indented));

                        string currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                        string operation = "用户终止日志采集" + line;
                        string cur_result = "成功";
                        LogOperationToFile(currentTime, operation, cur_result);
                    }

                    var after_pmlwatchdog_process = Process.GetProcessesByName("pmlwatchdog");
                    if (!after_pmlwatchdog_process.Any())
                    {
                        UpdateSystemStatus();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("日志采集终止失败: " + ex.Message);

                string currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                string operation = "用户终止日志采集" + line;
                string cur_result = "失败: " + ex.Message;
                LogOperationToFile(currentTime, operation, cur_result);
            }
        }
        private async void ConfigButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string exePath = @"C:\SZTSProgramInstaller\SZTSProgram\unionFileSubstituteSystem.exe";
                string Shut_arguments = $"--mode u";
                ProcessStartInfo Shut_startInfo = new ProcessStartInfo();
                Shut_startInfo.FileName = exePath;
                Shut_startInfo.Arguments = Shut_arguments;
                Shut_startInfo.RedirectStandardOutput = true;
                Shut_startInfo.CreateNoWindow = true;
                Shut_startInfo.UseShellExecute = false;

                using (Process process = Process.Start(Shut_startInfo))
                {
                    using (StreamReader reader = process.StandardOutput)
                    {
                        string result = reader.ReadToEnd();
                        process.WaitForExit();
                    }
                }

                var encfsProcesses = Process.GetProcessesByName("encfs");
                foreach (var a_process in encfsProcesses)
                {
                    a_process.Kill();
                    a_process.WaitForExit();
                }

                string Launch_arguments = $"--mode m";
                ProcessStartInfo Launch_startInfo = new ProcessStartInfo
                {
                    FileName = exePath,
                    Arguments = Launch_arguments,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                    UseShellExecute = false
                };

                using (Process process = new Process { StartInfo = Launch_startInfo })
                {
                    process.StartInfo.StandardOutputEncoding = Encoding.UTF8;
                    process.Start();
                    string result = await process.StandardOutput.ReadToEndAsync();
                    await process.WaitForExitAsync();

                    if (result.Trim().StartsWith("Success"))
                    {
                        System.Windows.MessageBox.Show("已成功更新系统配置并重启文件系统！");

                        string currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                        string operation = "用户更新系统配置并重启文件系统" + line;
                        string cur_result = "成功";
                        LogOperationToFile(currentTime, operation, cur_result);
                    }
                    if (result.Trim().StartsWith("Error"))
                    {
                        System.Windows.MessageBox.Show("更新系统配置并重启文件系统失败:" + result);

                        string currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                        string operation = "用户更新系统配置并重启文件系统" + line;
                        string cur_result = "失败: " + result;
                        LogOperationToFile(currentTime, operation, cur_result);
                    }
                }
                UpdateSystemStatus();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("更新系统配置并重启文件系统失败: " + ex.Message);

                string currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                string operation = "用户更新系统配置并重启文件系统" + line;
                string cur_result = "失败: " + ex.Message;
                LogOperationToFile(currentTime, operation, cur_result);
            }
        }

        private async void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            if (isButtonClicked)
            {
                return;
            }

            try
            {
                isButtonClicked = true;
                Process[] poppwdProcesses = Process.GetProcessesByName("PopPwd");
                if (poppwdProcesses.Length > 0)
                {
                    System.Windows.MessageBox.Show("当前用户身份验证尚未结束，请完成本次验证后再发起重置！");
                    return;
                }

                string exePath = @"C:\SZTSProgramInstaller\SZTSProgram\unionFileSubstituteSystem.exe";
                string Shut_arguments = $"--mode u";

                ProcessStartInfo Shut_startInfo = new ProcessStartInfo();
                Shut_startInfo.FileName = exePath;
                Shut_startInfo.Arguments = Shut_arguments;
                Shut_startInfo.RedirectStandardOutput = true;
                Shut_startInfo.CreateNoWindow = true;
                Shut_startInfo.UseShellExecute = false;

                using (Process process = Process.Start(Shut_startInfo))
                {
                    using (StreamReader reader = process.StandardOutput)
                    {
                        string result = reader.ReadToEnd();
                        process.WaitForExit();
                    }
                }

                var encfsProcesses = Process.GetProcessesByName("encfs");
                foreach (var a_process in encfsProcesses)
                {
                    a_process.Kill();
                    a_process.WaitForExit();
                }

                string Launch_arguments = $"--mode m";

                ProcessStartInfo Launch_startInfo = new ProcessStartInfo
                {
                    FileName = exePath,
                    Arguments = Launch_arguments,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                    UseShellExecute = false
                };

                using (Process process = new Process { StartInfo = Launch_startInfo })
                {
                    process.StartInfo.StandardOutputEncoding = Encoding.UTF8;
                    process.Start();
                    string result = await process.StandardOutput.ReadToEndAsync();
                    await process.WaitForExitAsync();

                    if (result.Trim().StartsWith("Success"))
                    {
                        System.Windows.MessageBox.Show("身份验证状态已重置！");

                        string currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                        string operation = "用户点击重置身份验证" + line;
                        string cur_result = "成功";
                        LogOperationToFile(currentTime, operation, cur_result);
                    }
                    if (result.Trim().StartsWith("Error"))
                    {
                        System.Windows.MessageBox.Show("重置身份验证失败:" + result);

                        string currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                        string operation = "用户点击重置身份验证" + line;
                        string cur_result = "失败: " + result;
                        LogOperationToFile(currentTime, operation, cur_result);
                    }
                }
                UpdateSystemStatus();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("重置身份验证失败: " + ex.Message);

                string currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                string operation = "用户点击重置身份验证" + line;
                string cur_result = "失败: " + ex.Message;
                LogOperationToFile(currentTime, operation, cur_result);
            }
            finally
            {
                isButtonClicked = false;
            }
        }

        private async void DefaultConfigButton_Click(object sender, RoutedEventArgs e)
        {
            string OpenCountlogFilePath = @"C:\SZTSProgramInstaller\SZTSConfig\sztsfs_openfilecount_log.txt";
            string sourceDirectory = @"C:\SZTSProgramInstaller\SZTSConfig\default_dir";
            string targetDirectory = @"C:\SZTSProgramInstaller\SZTSConfig";
            string[] filesToCopy = { "docs.json", "f_secure.json", "forms.json", "processes_list.json" };
            string popPwdPath = @"C:\SZTSProgramInstaller\SZTSProgram\PopPwd2.exe";

            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = popPwdPath,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (Process popPwdProcess = Process.Start(startInfo))
                {
                    await Task.Run(() => popPwdProcess.WaitForExit());
                    int result = popPwdProcess.ExitCode;
                    if (result == 1)
                    {
                        System.Windows.MessageBox.Show("身份验证失败，无法恢复默认配置！");
                        return;
                    }
                }

                var openFileCheckResult = await CheckOpenFilesAsync(OpenCountlogFilePath);
                bool hasOpenFiles = openFileCheckResult.Item1;
                List<string> openFiles = openFileCheckResult.Item2;

                if (hasOpenFiles && openFiles != null)
                {
                    string fileList = string.Join(", ", openFiles);
                    System.Windows.MessageBox.Show($"当前无法恢复默认配置，有文件未关闭：{fileList}");
                }

                else
                {
                    int rawLogStatus = 0;
                    string rawjsonPath = @"C:\SZTSProgramInstaller\SZTSConfig\forms.json";
                    if (File.Exists(rawjsonPath))
                    {
                        string jsonStr = File.ReadAllText(rawjsonPath);
                        dynamic jsonObj = JsonConvert.DeserializeObject(jsonStr);

                        if (jsonObj.LogStatus != null)
                        {
                            rawLogStatus = jsonObj.LogStatus;
                        }
                    }

                    foreach (var fileName in filesToCopy)
                    {
                        string sourceFilePath = System.IO.Path.Combine(sourceDirectory, fileName);
                        string targetFilePath = System.IO.Path.Combine(targetDirectory, fileName);
                        using (FileStream sourceStream = new FileStream(sourceFilePath, FileMode.Open, FileAccess.Read))
                        {
                            using (FileStream destinationStream = new FileStream(targetFilePath, FileMode.Create, FileAccess.Write))
                            {
                                await sourceStream.CopyToAsync(destinationStream);
                            }
                        }
                    }

                    string jsonPath = @"C:\SZTSProgramInstaller\SZTSConfig\forms.json";
                    if (File.Exists(jsonPath))
                    {
                        if (rawLogStatus == 1)
                        {
                            string jsonString = File.ReadAllText(jsonPath);
                            JObject jsonObject = JObject.Parse(jsonString);
                            jsonObject["LogStatus"] = 1;
                            File.WriteAllText(jsonPath, jsonObject.ToString(Formatting.Indented));
                        }

                        string jsonStr = File.ReadAllText(jsonPath);
                        dynamic jsonObj = JsonConvert.DeserializeObject(jsonStr);
                    }

                    string exePath = @"C:\SZTSProgramInstaller\SZTSProgram\unionFileSubstituteSystem.exe";
                    string Shut_arguments = $"--mode u";

                    ProcessStartInfo Shut_startInfo = new ProcessStartInfo();
                    Shut_startInfo.FileName = exePath;
                    Shut_startInfo.Arguments = Shut_arguments;
                    Shut_startInfo.RedirectStandardOutput = true;
                    Shut_startInfo.CreateNoWindow = true;
                    Shut_startInfo.UseShellExecute = false;

                    using (Process process = Process.Start(Shut_startInfo))
                    {
                        using (StreamReader reader = process.StandardOutput)
                        {
                            string result = reader.ReadToEnd();
                            process.WaitForExit();
                        }
                    }

                    var encfsProcesses = Process.GetProcessesByName("encfs");
                    foreach (var a_process in encfsProcesses)
                    {
                        a_process.Kill();
                        a_process.WaitForExit();
                    }

                    string Launch_arguments = $"--mode m";

                    ProcessStartInfo Launch_startInfo = new ProcessStartInfo
                    {
                        FileName = exePath,
                        Arguments = Launch_arguments,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true,
                        UseShellExecute = false
                    };

                    using (Process process = new Process { StartInfo = Launch_startInfo })
                    {
                        process.Start();
                        string result = await process.StandardOutput.ReadToEndAsync();
                        await process.WaitForExitAsync();

                        if (result.Trim().StartsWith("Success"))
                        {
                            System.Windows.MessageBox.Show("已成功恢复默认配置并重启文件系统！");
                            if (ProtectPwdPage.Instance != null)
                            {
                                ProtectPwdPage.Instance.LoadData();
                            }
                            else
                            {
                                System.Windows.MessageBox.Show("未找到 ProtectPwdPage 的实例！");
                            }

                            string currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                            string operation = "用户恢复默认配置并重启文件系统" + line;
                            string cur_result = "成功";
                            LogOperationToFile(currentTime, operation, cur_result);
                        }
                        if (result.Trim().StartsWith("Error"))
                        {
                            System.Windows.MessageBox.Show("恢复默认配置并重启文件系统失败:" + result);

                            string currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                            string operation = "用户恢复默认配置并重启文件系统" + line;
                            string cur_result = "失败: " + result;
                            LogOperationToFile(currentTime, operation, cur_result);
                        }
                    }
                    UpdateSystemStatus();
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("恢复默认配置并重启文件系统失败: " + ex.Message);

                string currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                string operation = "用户恢复默认配置并重启文件系统" + line;
                string cur_result = "失败: " + ex.Message;
                LogOperationToFile(currentTime, operation, cur_result);
            }
        }
        private void NaviFrame_Navigated(object sender, NavigationEventArgs e)
        {

        }

        private async void StartNodisturbButton_Click(object sender, RoutedEventArgs e)
        {
            string popPwdPath = @"C:\SZTSProgramInstaller\SZTSProgram\PopPwd2.exe";
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = popPwdPath,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (Process popPwdProcess = Process.Start(startInfo))
                {
                    await Task.Run(() => popPwdProcess.WaitForExit());
                    int result = popPwdProcess.ExitCode;
                    if (result == 1)
                    {
                        System.Windows.MessageBox.Show("身份验证失败，无法开启勿扰模式！");
                        return;
                    }
                }

                string OpenCountlogFilePath = @"C:\SZTSProgramInstaller\SZTSConfig\sztsfs_openfilecount_log.txt";
                string jsonPath = @"C:\SZTSProgramInstaller\SZTSConfig\forms.json";

                string DrivePath = "C:\\SZTSProgramInstaller\\SZTSConfig\\szts_encdir";
                string DriveLetter = "V:";
                string DrivePassword = "12345678";
                int NoDisturb = 0;
                int TrappingTag = 0;
                if (File.Exists(jsonPath))
                {
                    string jsonStr = File.ReadAllText(jsonPath);
                    dynamic jsonObj = JsonConvert.DeserializeObject(jsonStr);

                    if (jsonObj.driveLetter != null)
                    {
                        DriveLetter = jsonObj.driveLetter;
                    }

                    if (jsonObj.drivePath != null)
                    {
                        DrivePath = jsonObj.drivePath;
                    }

                    if (jsonObj.Nodisturb != null)
                    {
                        NoDisturb = jsonObj.Nodisturb;
                    }

                    if (jsonObj.Trapping != null)
                    {
                        TrappingTag = jsonObj.Trapping;
                    }
                }

                if (NoDisturb == 1)
                {
                    System.Windows.MessageBox.Show("当前已处于勿扰模式！");
                }

                else if (TrappingTag == 1)
                {
                    System.Windows.MessageBox.Show("当前已处于诱捕模式，请取消诱捕模式后再开启勿扰模式！");
                }

                else
                {
                    var openFileCheckResult = await CheckOpenFilesAsync(OpenCountlogFilePath);
                    bool hasOpenFiles = openFileCheckResult.Item1;
                    List<string> openFiles = openFileCheckResult.Item2;

                    if (hasOpenFiles && openFiles != null)
                    {
                        string fileList = string.Join(", ", openFiles);
                        System.Windows.MessageBox.Show($"当前无法切换模式，有文件未关闭：{fileList}");
                    }

                    else
                    {
                        string jsonString = File.ReadAllText(jsonPath);
                        JObject jsonObject = JObject.Parse(jsonString);
                        jsonObject["Nodisturb"] = 1;
                        File.WriteAllText(jsonPath, jsonObject.ToString(Formatting.Indented));

                        string exePath = @"unionFileSubstituteSystem.exe";
                        //string Shut_arguments = $"--mode u --drive_letter {DriveLetter}";
                        string Shut_arguments = $"--mode u";

                        ProcessStartInfo Shut_startInfo = new ProcessStartInfo();
                        Shut_startInfo.FileName = exePath;
                        Shut_startInfo.Arguments = Shut_arguments;
                        Shut_startInfo.RedirectStandardOutput = true;
                        Shut_startInfo.CreateNoWindow = true;
                        Shut_startInfo.UseShellExecute = false;

                        using (Process process = Process.Start(Shut_startInfo))
                        {
                            using (StreamReader reader = process.StandardOutput)
                            {
                                string result = reader.ReadToEnd();
                                process.WaitForExit();
                            }
                        }

                        var encfsProcesses = Process.GetProcessesByName("encfs");
                        foreach (var a_process in encfsProcesses)
                        {
                            a_process.Kill();
                            a_process.WaitForExit();
                        }

                        string Launch_arguments = $"--mode m";
                        ProcessStartInfo Launch_startInfo = new ProcessStartInfo
                        {
                            FileName = exePath,
                            Arguments = Launch_arguments,
                            RedirectStandardOutput = true,
                            CreateNoWindow = true,
                            UseShellExecute = false
                        };

                        using (Process process = new Process { StartInfo = Launch_startInfo })
                        {
                            process.StartInfo.StandardOutputEncoding = Encoding.UTF8;
                            process.Start();
                            string result = await process.StandardOutput.ReadToEndAsync();
                            await process.WaitForExitAsync();

                            if (result.Trim().StartsWith("Success"))
                            {
                                System.Windows.MessageBox.Show("已成功开启勿扰模式！");

                                string currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                                string operation = "用户开启勿扰模式" + line;
                                string cur_result = "成功";
                                LogOperationToFile(currentTime, operation, cur_result);
                            }
                            if (result.Trim().StartsWith("Error"))
                            {
                                System.Windows.MessageBox.Show("开启勿扰模式失败:" + result);

                                string currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                                string operation = "用户开启勿扰模式" + line;
                                string cur_result = "失败: " + result;
                                LogOperationToFile(currentTime, operation, cur_result);
                            }
                        }
                        UpdateSystemStatus();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"开启勿扰模式失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);

                string currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                string operation = "用户开启勿扰模式" + line;
                string cur_result = "失败: " + ex.Message;
                LogOperationToFile(currentTime, operation, cur_result);
            }
        }
        private async void StopNodisturbButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string OpenCountlogFilePath = @"C:\SZTSProgramInstaller\SZTSConfig\sztsfs_openfilecount_log.txt";
                string jsonPath = @"C:\SZTSProgramInstaller\SZTSConfig\forms.json";

                string DrivePath = "C:\\SZTSProgramInstaller\\SZTSConfig\\szts_encdir";
                string DriveLetter = "V:";
                string DrivePassword = "12345678";
                int NoDisturb = 0;
                int TrappingTag = 0;
                if (File.Exists(jsonPath))
                {
                    string jsonStr = File.ReadAllText(jsonPath);
                    dynamic jsonObj = JsonConvert.DeserializeObject(jsonStr);

                    if (jsonObj.driveLetter != null)
                    {
                        DriveLetter = jsonObj.driveLetter;
                    }

                    if (jsonObj.drivePath != null)
                    {
                        DrivePath = jsonObj.drivePath;
                    }

                    if (jsonObj.Nodisturb != null)
                    {
                        NoDisturb = jsonObj.Nodisturb;
                    }

                    if (jsonObj.Trapping != null)
                    {
                        TrappingTag = jsonObj.Trapping;
                    }
                }

                if (NoDisturb == 0)
                {
                    System.Windows.MessageBox.Show("当前未处于勿扰模式！");
                }

                else
                {
                    var openFileCheckResult = await CheckOpenFilesAsync(OpenCountlogFilePath);
                    bool hasOpenFiles = openFileCheckResult.Item1;
                    List<string> openFiles = openFileCheckResult.Item2;

                    if (hasOpenFiles && openFiles != null)
                    {
                        string fileList = string.Join(", ", openFiles);
                        System.Windows.MessageBox.Show($"当前无法切换模式，有文件未关闭：{fileList}");
                    }

                    else
                    {
                        string jsonString = File.ReadAllText(jsonPath);
                        JObject jsonObject = JObject.Parse(jsonString);
                        jsonObject["Nodisturb"] = 0;
                        File.WriteAllText(jsonPath, jsonObject.ToString(Formatting.Indented));
                        string exePath = @"unionFileSubstituteSystem.exe";
                        string Shut_arguments = $"--mode u";

                        ProcessStartInfo Shut_startInfo = new ProcessStartInfo();
                        Shut_startInfo.FileName = exePath;
                        Shut_startInfo.Arguments = Shut_arguments;
                        Shut_startInfo.RedirectStandardOutput = true;
                        Shut_startInfo.CreateNoWindow = true;
                        Shut_startInfo.UseShellExecute = false;

                        using (Process process = Process.Start(Shut_startInfo))
                        {
                            using (StreamReader reader = process.StandardOutput)
                            {
                                string result = reader.ReadToEnd();
                                process.WaitForExit();
                            }
                        }

                        var encfsProcesses = Process.GetProcessesByName("encfs");
                        foreach (var a_process in encfsProcesses)
                        {
                            a_process.Kill();
                            a_process.WaitForExit();
                        }

                        string Launch_arguments = $"--mode m";
                        ProcessStartInfo Launch_startInfo = new ProcessStartInfo
                        {
                            FileName = exePath,
                            Arguments = Launch_arguments,
                            RedirectStandardOutput = true,
                            CreateNoWindow = true,
                            UseShellExecute = false
                        };

                        using (Process process = new Process { StartInfo = Launch_startInfo })
                        {
                            process.StartInfo.StandardOutputEncoding = Encoding.UTF8;
                            process.Start();
                            string result = await process.StandardOutput.ReadToEndAsync();
                            await process.WaitForExitAsync();

                            if (result.Trim().StartsWith("Success"))
                            {
                                System.Windows.MessageBox.Show("已成功关闭勿扰模式！");

                                string currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                                string operation = "用户关闭勿扰模式" + line;
                                string cur_result = "成功";
                                LogOperationToFile(currentTime, operation, cur_result);
                            }
                            if (result.Trim().StartsWith("Error"))
                            {
                                System.Windows.MessageBox.Show("关闭勿扰模式失败:" + result);

                                string currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                                string operation = "用户关闭勿扰模式" + line;
                                string cur_result = "失败: " + result;
                                LogOperationToFile(currentTime, operation, cur_result);
                            }

                        }
                        UpdateSystemStatus();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"关闭勿扰模式失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);

                string currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                string operation = "用户关闭勿扰模式" + line;
                string cur_result = "失败: " + ex.Message;
                LogOperationToFile(currentTime, operation, cur_result);
            }
        }

        private async void StartTrappingButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string OpenCountlogFilePath = @"C:\SZTSProgramInstaller\SZTSConfig\sztsfs_openfilecount_log.txt";
                string jsonPath = @"C:\SZTSProgramInstaller\SZTSConfig\forms.json";

                string DrivePath = "C:\\SZTSProgramInstaller\\SZTSConfig\\szts_encdir";
                string DriveLetter = "V:";
                string DrivePassword = "12345678";
                int NoDisturb = 0;
                int TrappingTag = 0;
                if (File.Exists(jsonPath))
                {
                    string jsonStr = File.ReadAllText(jsonPath);
                    dynamic jsonObj = JsonConvert.DeserializeObject(jsonStr);

                    if (jsonObj.driveLetter != null)
                    {
                        DriveLetter = jsonObj.driveLetter;
                    }

                    if (jsonObj.drivePath != null)
                    {
                        DrivePath = jsonObj.drivePath;
                    }

                    if (jsonObj.Nodisturb != null)
                    {
                        NoDisturb = jsonObj.Nodisturb;
                    }

                    if (jsonObj.Trapping != null)
                    {
                        TrappingTag = jsonObj.Trapping;
                    }
                }

                if (TrappingTag == 1)
                {
                    System.Windows.MessageBox.Show("当前已处于诱捕模式！");
                }
                else if (NoDisturb == 1)
                {
                    System.Windows.MessageBox.Show("当前已处于勿扰模式，请取消勿扰模式后再开启诱捕模式！");
                }

                else
                {
                    var openFileCheckResult = await CheckOpenFilesAsync(OpenCountlogFilePath);
                    bool hasOpenFiles = openFileCheckResult.Item1;
                    List<string> openFiles = openFileCheckResult.Item2;

                    if (hasOpenFiles && openFiles != null)
                    {
                        string fileList = string.Join(", ", openFiles);
                        System.Windows.MessageBox.Show($"当前无法切换模式，有文件未关闭：{fileList}");
                    }
                    else
                    {
                        string jsonString = File.ReadAllText(jsonPath);
                        JObject jsonObject = JObject.Parse(jsonString);
                        jsonObject["Trapping"] = 1;
                        File.WriteAllText(jsonPath, jsonObject.ToString(Formatting.Indented));

                        string exePath = @"unionFileSubstituteSystem.exe";
                        string Shut_arguments = $"--mode u";

                        ProcessStartInfo Shut_startInfo = new ProcessStartInfo();
                        Shut_startInfo.FileName = exePath;
                        Shut_startInfo.Arguments = Shut_arguments;
                        Shut_startInfo.RedirectStandardOutput = true;
                        Shut_startInfo.CreateNoWindow = true;
                        Shut_startInfo.UseShellExecute = false;

                        using (Process process = Process.Start(Shut_startInfo))
                        {
                            using (StreamReader reader = process.StandardOutput)
                            {
                                string result = reader.ReadToEnd();
                                process.WaitForExit();
                            }
                        }

                        var encfsProcesses = Process.GetProcessesByName("encfs");
                        foreach (var a_process in encfsProcesses)
                        {
                            a_process.Kill();
                            a_process.WaitForExit();
                        }

                        string Launch_arguments = $"--mode m";
                        ProcessStartInfo Launch_startInfo = new ProcessStartInfo
                        {
                            FileName = exePath,
                            Arguments = Launch_arguments,
                            RedirectStandardOutput = true,
                            CreateNoWindow = true,
                            UseShellExecute = false
                        };

                        using (Process process = new Process { StartInfo = Launch_startInfo })
                        {
                            process.StartInfo.StandardOutputEncoding = Encoding.UTF8;
                            process.Start();
                            string result = await process.StandardOutput.ReadToEndAsync();
                            await process.WaitForExitAsync();

                            if (result.Trim().StartsWith("Success"))
                            {
                                System.Windows.MessageBox.Show("已成功开启诱捕模式！");

                                string currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                                string operation = "用户开启诱捕模式" + line;
                                string cur_result = "成功";
                                LogOperationToFile(currentTime, operation, cur_result);
                            }
                            if (result.Trim().StartsWith("Error"))
                            {
                                System.Windows.MessageBox.Show("开启诱捕模式失败:" + result);

                                string currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                                string operation = "用户开启诱捕模式" + line;
                                string cur_result = "失败: " + result;
                                LogOperationToFile(currentTime, operation, cur_result);
                            }
                        }
                        UpdateSystemStatus();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"开启诱捕模式失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);

                string currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                string operation = "用户开启诱捕模式" + line;
                string cur_result = "失败: " + ex.Message;
                LogOperationToFile(currentTime, operation, cur_result);
            }
        }
        private async void StopTrappingButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string OpenCountlogFilePath = @"C:\SZTSProgramInstaller\SZTSConfig\sztsfs_openfilecount_log.txt";
                string jsonPath = @"C:\SZTSProgramInstaller\SZTSConfig\forms.json";

                string DrivePath = "C:\\SZTSProgramInstaller\\SZTSConfig\\szts_encdir";
                string DriveLetter = "V:";
                string DrivePassword = "12345678";
                int NoDisturb = 0;
                int TrappingTag = 0;
                if (File.Exists(jsonPath))
                {
                    string jsonStr = File.ReadAllText(jsonPath);
                    dynamic jsonObj = JsonConvert.DeserializeObject(jsonStr);

                    if (jsonObj.driveLetter != null)
                    {
                        DriveLetter = jsonObj.driveLetter;
                    }

                    if (jsonObj.drivePath != null)
                    {
                        DrivePath = jsonObj.drivePath;
                    }

                    if (jsonObj.Nodisturb != null)
                    {
                        NoDisturb = jsonObj.Nodisturb;
                    }

                    if (jsonObj.Trapping != null)
                    {
                        TrappingTag = jsonObj.Trapping;
                    }
                }

                if (TrappingTag == 0)
                {
                    System.Windows.MessageBox.Show("当前尚未处于诱捕模式！");
                }
                else
                {
                    var openFileCheckResult = await CheckOpenFilesAsync(OpenCountlogFilePath);
                    bool hasOpenFiles = openFileCheckResult.Item1;
                    List<string> openFiles = openFileCheckResult.Item2;

                    if (hasOpenFiles && openFiles != null)
                    {
                        string fileList = string.Join(", ", openFiles);
                        System.Windows.MessageBox.Show($"当前无法切换模式，有文件未关闭：{fileList}");
                    }

                    else
                    {
                        string jsonString = File.ReadAllText(jsonPath);
                        JObject jsonObject = JObject.Parse(jsonString);
                        jsonObject["Trapping"] = 0;
                        File.WriteAllText(jsonPath, jsonObject.ToString(Formatting.Indented));

                        string exePath = @"unionFileSubstituteSystem.exe";
                        string Shut_arguments = $"--mode u";

                        ProcessStartInfo Shut_startInfo = new ProcessStartInfo();
                        Shut_startInfo.FileName = exePath;
                        Shut_startInfo.Arguments = Shut_arguments;
                        Shut_startInfo.RedirectStandardOutput = true;
                        Shut_startInfo.CreateNoWindow = true;
                        Shut_startInfo.UseShellExecute = false;

                        using (Process process = Process.Start(Shut_startInfo))
                        {
                            using (StreamReader reader = process.StandardOutput)
                            {
                                string result = reader.ReadToEnd();
                                process.WaitForExit();
                            }
                        }

                        var encfsProcesses = Process.GetProcessesByName("encfs");
                        foreach (var a_process in encfsProcesses)
                        {
                            a_process.Kill();
                            a_process.WaitForExit();
                        }

                        string Launch_arguments = $"--mode m";
                        ProcessStartInfo Launch_startInfo = new ProcessStartInfo
                        {
                            FileName = exePath,
                            Arguments = Launch_arguments,
                            RedirectStandardOutput = true,
                            CreateNoWindow = true,
                            UseShellExecute = false
                        };

                        using (Process process = new Process { StartInfo = Launch_startInfo })
                        {
                            process.StartInfo.StandardOutputEncoding = Encoding.UTF8;
                            process.Start();
                            string result = await process.StandardOutput.ReadToEndAsync();
                            await process.WaitForExitAsync();

                            if (result.Trim().StartsWith("Success"))
                            {
                                System.Windows.MessageBox.Show("已成功关闭诱捕模式！");

                                string currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                                string operation = "用户关闭诱捕模式" + line;
                                string cur_result = "成功";
                                LogOperationToFile(currentTime, operation, cur_result);
                            }
                            if (result.Trim().StartsWith("Error"))
                            {
                                System.Windows.MessageBox.Show("关闭诱捕模式失败:" + result);

                                string currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                                string operation = "用户关闭诱捕模式" + line;
                                string cur_result = "失败: " + result;
                                LogOperationToFile(currentTime, operation, cur_result);
                            }
                        }
                        UpdateSystemStatus();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"关闭诱捕模式失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);

                string currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                string operation = "用户关闭诱捕模式" + line;
                string cur_result = "失败: " + ex.Message;
                LogOperationToFile(currentTime, operation, cur_result);
            }
        }
        private async void ChangeUserPasswordBoxButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string currentPassword = ShowPasswordInputDialog("请输入原身份验证密码");
                if (currentPassword == null)
                    return;

                string jsonFilePath = @"C:\SZTSProgramInstaller\SZTSConfig\f_secure.json";
                string storedPasswordBase64 = GetStoredPassword(jsonFilePath);
                string enteredPasswordBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(currentPassword));

                if (enteredPasswordBase64 != storedPasswordBase64)
                {
                    System.Windows.MessageBox.Show("输入的原始密码错误，无法重置用户身份验证密码！");

                    string currentTime0 = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    string operation0 = "用户修改身份验证密码" + line;
                    string cur_result0 = "失败: 输入的原始密码错误";
                    LogOperationToFile(currentTime0, operation0, cur_result0);
                    return;
                }

                var newPasswords = ShowNewPasswordDialog();
                if (newPasswords == null)
                    return;

                string newPassword = newPasswords.Item1;
                string repeatPassword = newPasswords.Item2;

                if (newPassword != repeatPassword)
                {
                    System.Windows.MessageBox.Show("无效输入，两次输入的身份验证密码不同！");
                    return;
                }

                if (!System.Text.RegularExpressions.Regex.IsMatch(newPassword, @"^\d{6}$"))
                {
                    System.Windows.MessageBox.Show("无效输入，用户身份验证密码必须是六位数字！");
                    return;
                }

                string newPasswordBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(newPassword));
                SaveNewPassword(jsonFilePath, newPasswordBase64);

                string exePath = @"unionFileSubstituteSystem.exe";
                string Shut_arguments = $"--mode u";

                ProcessStartInfo Shut_startInfo = new ProcessStartInfo();
                Shut_startInfo.FileName = exePath;
                Shut_startInfo.Arguments = Shut_arguments;
                Shut_startInfo.RedirectStandardOutput = true;
                Shut_startInfo.CreateNoWindow = true;
                Shut_startInfo.UseShellExecute = false;

                using (Process process = Process.Start(Shut_startInfo))
                {
                    using (StreamReader reader = process.StandardOutput)
                    {
                        string result = reader.ReadToEnd();
                        process.WaitForExit();
                    }
                }

                var encfsProcesses = Process.GetProcessesByName("encfs");
                foreach (var a_process in encfsProcesses)
                {
                    a_process.Kill();
                    a_process.WaitForExit();
                }

                string Launch_arguments = $"--mode m";
                ProcessStartInfo Launch_startInfo = new ProcessStartInfo
                {
                    FileName = exePath,
                    Arguments = Launch_arguments,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                    UseShellExecute = false
                };

                using (Process process = new Process { StartInfo = Launch_startInfo })
                {
                    process.StartInfo.StandardOutputEncoding = Encoding.UTF8;
                    process.Start();
                    string result = await process.StandardOutput.ReadToEndAsync();
                    await process.WaitForExitAsync();

                    if (result.Trim().StartsWith("Success"))
                    {
                        System.Windows.MessageBox.Show("修改身份验证密码成功！");

                        string currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                        string operation = "用户修改身份验证密码" + line;
                        string cur_result = "成功";
                        LogOperationToFile(currentTime, operation, cur_result);
                    }
                    if (result.Trim().StartsWith("Error"))
                    {
                        System.Windows.MessageBox.Show("修改身份验证密码失败:" + result);
                        string currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                        string operation = "用户修改身份验证密码" + line;
                        string cur_result = "失败:" + result;
                        LogOperationToFile(currentTime, operation, cur_result);
                    }
                }
                UpdateSystemStatus();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"修改用户身份验证密码失败: {ex.Message}");
                string currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                string operation = "用户修改身份验证密码" + line;
                string cur_result = "失败: " + ex.Message;
                LogOperationToFile(currentTime, operation, cur_result);
            }
        }

        private string ShowPasswordInputDialog(string message)
        {
            var inputDialog = new PasswordInputDialog(message);
            if (inputDialog.ShowDialog() == true)
                return inputDialog.Password;
            return null;
        }

        private Tuple<string, string> ShowNewPasswordDialog()
        {
            var newPasswordDialog = new NewPasswordInputDialog();
            if (newPasswordDialog.ShowDialog() == true)
                return Tuple.Create(newPasswordDialog.NewPassword, newPasswordDialog.RepeatPassword);
            return null;
        }
        private string GetStoredPassword(string filePath)
        {
            try
            {
                string json = File.ReadAllText(filePath);
                var jsonObject = JObject.Parse(json);
                return jsonObject["User"]?.ToString() ?? "";
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"读取密码文件失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return "";
            }
        }

        private void SaveNewPassword(string filePath, string newPasswordBase64)
        {
            try
            {
                string json = File.ReadAllText(filePath);
                var jsonObject = JObject.Parse(json);
                jsonObject["User"] = newPasswordBase64;
                File.WriteAllText(filePath, jsonObject.ToString());
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"保存密码失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ExportFileButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string popPwdPath = @"C:\SZTSProgramInstaller\SZTSProgram\PopPwd2.exe";
                string forms_jsonPath = "C:\\SZTSProgramInstaller\\SZTSConfig\\forms.json";
                string DriveLetter = "V:";

                if (File.Exists(forms_jsonPath))
                {
                    string jsonStr = File.ReadAllText(forms_jsonPath);
                    dynamic jsonObj = JsonConvert.DeserializeObject(jsonStr);

                    if (jsonObj.driveLetter != null)
                    {
                        DriveLetter = jsonObj.driveLetter;
                    }
                }

                if (!Directory.Exists(DriveLetter + "//"))
                {
                    System.Windows.MessageBox.Show("当前" + DriveLetter + "盘未挂载，无法导出文件！");

                    string currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    string operation = "用户导出文件";
                    string cur_result = "失败：文件系统未挂载";
                    LogOperationToFile(currentTime, operation, cur_result);
                }

                Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
                openFileDialog.Title = "选择需要导出的文件";
                openFileDialog.InitialDirectory = DriveLetter;

                bool? result = openFileDialog.ShowDialog();
                if (result == true)
                {
                    string sourceFilePath = openFileDialog.FileName;

                    FolderBrowserDialog folderDialog = new FolderBrowserDialog();
                    folderDialog.Description = "选择导出的目标文件夹";

                    if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        try
                        {
                            string destinationFolderPath = folderDialog.SelectedPath;
                            string fileName = System.IO.Path.GetFileName(sourceFilePath);
                            string destinationFilePath = System.IO.Path.Combine(destinationFolderPath, fileName);

                            ProcessStartInfo startInfo = new ProcessStartInfo
                            {
                                FileName = popPwdPath,
                                UseShellExecute = false,
                                CreateNoWindow = true
                            };

                            using (Process popPwdProcess = Process.Start(startInfo))
                            {
                                await Task.Run(() => popPwdProcess.WaitForExit());
                                int poppwd_result = popPwdProcess.ExitCode;
                                if (poppwd_result == 1)
                                {
                                    System.Windows.MessageBox.Show("身份验证失败，无法导出文件！");
                                    return;
                                }
                            }

                            File.Copy(sourceFilePath, destinationFilePath, overwrite: true);

                            System.Windows.MessageBox.Show("文件已成功导出到目标文件夹！", "导出成功", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        catch (IOException ioEx)
                        {
                            System.Windows.MessageBox.Show($"文件复制失败：{ioEx.Message}");
                            
                            string currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                            string operation = "用户导出文件";
                            string cur_result = "失败：出现文件复制异常 " + ioEx.Message;
                            LogOperationToFile(currentTime, operation, cur_result);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"本体文件导出失败: {ex.Message}");
            }
        }
            public void UpdateSystemStatus()
        {
            try
            {
                string configFilePath = @"C:\SZTSProgramInstaller\SZTSConfig\forms.json";
                string systemMode = "\"数字替身\"常规模式";
                string filesystemStatus = "未挂载";
                string logStatus = "未开启";

                if (File.Exists(configFilePath))
                {
                    var json = File.ReadAllText(configFilePath);
                    JObject config = JObject.Parse(json);

                    int nodisturb = config["Nodisturb"]?.Value<int>() ?? 0;
                    int trapping = config["Trapping"]?.Value<int>() ?? 0;

                    if (nodisturb == 1)
                    {
                        systemMode = "勿扰模式";
                    }
                    else if (trapping == 1)
                    {
                        systemMode = "诱捕模式";
                    }

                    int logCollecting = config["LogStatus"]?.Value<int>() ?? 0;
                    logStatus = logCollecting == 1 ? "已开启" : "未开启";
                }

                string DriveLetter = "V:";
                string jsonPath = @"C:\SZTSProgramInstaller\SZTSConfig\forms.json";
                if (File.Exists(jsonPath))
                {
                    string jsonStr = File.ReadAllText(jsonPath);
                    dynamic jsonObj = JsonConvert.DeserializeObject(jsonStr);
                    if (jsonObj.driveLetter != null)
                    {
                        DriveLetter = jsonObj.driveLetter;
                    }
                }
                if (Directory.Exists(DriveLetter + "//"))
                {
                    filesystemStatus = "已挂载到" + DriveLetter[0] + "盘";
                }

                SystemStatusTextBlock.Text = $"○ 当前系统模式：{systemMode}          ○ 当前文件系统状态：{filesystemStatus}          ○ 当前日志采集状态：{logStatus}          ";
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"更新系统状态失败: {ex.Message}");
            }
        }
    }
}
