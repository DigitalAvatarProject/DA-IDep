using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using Newtonsoft.Json;
using System.IO;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Threading;
using System.Data.SQLite;
using Path = System.IO.Path;

namespace DAIDep.View.Configs
{
    public partial class FileFoldsPage : Page
    {
        public ObservableCollection<DocumentItem> Documents { get; set; }
        public ObservableCollection<DocumentItem> ReplaceDocuments { get; set; }
		private string TemplatePath { get; set; }
        const string validWinPathPattern = @"^[a-zA-Z]:\\[\\\S|*\S]?.*$";
        private string SourcePath = "";
        private string SourceName = "";

        private readonly string jsonPath = "C:\\SZTSProgramInstaller\\SZTSConfig\\docs.json";
        private readonly string forms_jsonPath = "C:\\SZTSProgramInstaller\\SZTSConfig\\forms.json";

        private string dbFilePath = @"C:\SZTSProgramInstaller\SZTSProgram\test.db";
        static string currentDate = DateTime.Now.ToString("yyyy-MM-dd");
        private string logFilePath = $"C:\\SZTSProgramInstaller\\SZTSConfig\\sztsfs_gui_logs\\{currentDate}_sztsfs_gui_log.txt";

        private string connectionString;
        private SQLiteConnection connection;
        private string line = "                                       ";

        private static readonly Regex validPattern = new Regex(
            @"^(.*/)?raw-[^\\/:*?""<>|]+(\.(txt|pdf|xls|xlsx|doc|docx|csv|ppt|pptx)|/)$",
            RegexOptions.IgnoreCase);

        public class DocumentItem
        {
            public string Name { get; set; }
            public string TypeDescription { get; set; }
        }
        public FileFoldsPage()
        {
            InitializeComponent();
            Documents = new ObservableCollection<DocumentItem>();
            TemplatePath = "W:\\敏感文件资料\\temp\\替身文件模版";
            ReplaceDocuments = new ObservableCollection<DocumentItem>();
            LoadData();

            connectionString = $"Data Source={dbFilePath};Version=3;";

            this.DataContext = this;
        }
        private void InitializeDatabase()
        {
            try
            {
                connection = new SQLiteConnection(connectionString);
                connection.Open();

                string createTableQuery = @"
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
                if (!File.Exists(logFilePath))
                {
                    File.Create(logFilePath).Dispose();
                }
                using (StreamWriter sw = File.AppendText(logFilePath))
                {
                    sw.WriteLine(logMessage);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"用户操作行为写入日志文件失败: {ex.Message}");
            }
        }
        private void LoadData()
        {
            if (File.Exists(jsonPath))
            {
                try
                {
                    string jsonStr = File.ReadAllText(jsonPath);
                    dynamic jsonObj = JsonConvert.DeserializeObject(jsonStr);

                    foreach (var doc in jsonObj.documents)
                    {
                        var item = new DocumentItem();
                        item.Name = doc.Name.ToString();
                        item.TypeDescription = Path.HasExtension(item.Name) ? Path.GetExtension(item.Name) : "folder";
                        Documents.Add(item);
                    }

                    TemplatePath = jsonObj.templatePath != null ? jsonObj.templatePath.ToString() : "/敏感文件资料/temp/替身文件模版/";
                    TemplatePathInput.Text = TemplatePath;

                    foreach (var doc in jsonObj.replace_documents)
                    {
                        var item = new DocumentItem();
                        item.Name = doc.Name.ToString();
                        item.TypeDescription = Path.HasExtension(item.Name) ? Path.GetExtension(item.Name) : "folder";
                        ReplaceDocuments.Add(item);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("配置文件加载解析失败: " + ex.Message);
                }
            }
            else
            {
                Console.WriteLine("配置文件不存在，创建新文件.");
                SaveData();
            }
        }

        private void SaveData()
        {
            try
            {
                var jsonObj = new
                {
                    documents = Documents,
                    templatePath = TemplatePath,
                    replace_documents = ReplaceDocuments
                };
                string jsonStr = JsonConvert.SerializeObject(jsonObj, Formatting.Indented);
                File.WriteAllText(jsonPath, jsonStr);
            }
            catch (Exception ex)
            {
                Console.WriteLine("配置文件加载解析失败: " + ex.Message);
            }
        }
        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();

                openFileDialog.Filter = "所有文件 (*.*)|*.*";
                openFileDialog.Title = "选择文件";

                bool? result = openFileDialog.ShowDialog();

                if (result == true)
                {
                    string selectedFileName = openFileDialog.FileName;
                    string fileNameOnly = System.IO.Path.GetFileName(selectedFileName);
                    InputTextBox.Text = fileNameOnly;

                    SourcePath = selectedFileName;
                    SourceName = fileNameOnly;
                }
            }

            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"浏览并选择本体文件失败: {ex.Message}");
            }
        }
        public static bool IsNotValidFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return true;

            char[] invalidChars = Path.GetInvalidFileNameChars();

            if (fileName.Any(c => invalidChars.Contains(c)))
                return true;

            string extension = Path.GetExtension(fileName);
            if (string.IsNullOrEmpty(extension))
                return true;

            string nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
            if (string.IsNullOrWhiteSpace(nameWithoutExtension))
                return true;
            return false;
        }
        private async void AddButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
				string exePath = @"C:\SZTSProgramInstaller\SZTSProgram\unionFileSubstituteSystem.exe";
                string DrivePath = "C:\\SZTSProgramInstaller\\SZTSConfig\\szts_encdir";
                string DriveLetter = "V:";
                string DrivePassword = "12345678";
                if (File.Exists(forms_jsonPath))
                {
                    string jsonStr = File.ReadAllText(forms_jsonPath);
                    dynamic jsonObj = JsonConvert.DeserializeObject(jsonStr);

                    if (jsonObj.driveLetter != null)
                    {
                        DriveLetter = jsonObj.driveLetter;
                    }
                }

                string Launch_arguments = $"--mode m";
                string Shut_arguments = $"--mode u";

                string newDocumentName = InputTextBox.Text.Trim();

                if (SourcePath == "" || SourceName == "")
                {
                    System.Windows.MessageBox.Show("请通过左侧浏览按钮选择要防护的本体文件所在路径！");
                }

                else if (IsNotValidFileName(newDocumentName))
                {
                    System.Windows.MessageBox.Show("不是合法的本体文件名称！");
                }

                else
                {
                    var newDocument = new DocumentItem
                    {
                        Name = newDocumentName,
                        TypeDescription = Path.GetExtension(newDocumentName)
                    };

                    if (Documents.Any(doc => doc.Name == newDocument.Name))
                    {
                        System.Windows.MessageBox.Show("该文件在本体文件列表中已存在！");
                    }

                    else if (ReplaceDocuments.Any(doc => doc.Name == newDocument.Name))
                    {
                        System.Windows.MessageBox.Show("本体文件不可以与替身模版文件重名！");
                    }

                    else
                    {
                        Documents.Add(newDocument);
                        SaveData();
                        InputTextBox.Clear();

                        string TargetPath = DriveLetter + "//" + SourceName;
                        if (File.Exists(TargetPath))
                        {
                            ProcessStartInfo Shut_startInfo = new ProcessStartInfo();
                            Shut_startInfo.FileName = exePath;
                            Shut_startInfo.Arguments = Shut_arguments;
                            Shut_startInfo.RedirectStandardOutput = true;
                            Shut_startInfo.CreateNoWindow = true;
                            Shut_startInfo.UseShellExecute = false;

                            using (Process shut_process = Process.Start(Shut_startInfo))
                            {
                                using (StreamReader reader = shut_process.StandardOutput)
                                {
                                    string result = reader.ReadToEnd();
                                    shut_process.WaitForExit();
                                }
                            }

                            var encfsProcesses = Process.GetProcessesByName("encfs");
                            foreach (var a_process in encfsProcesses)
                            {
                                a_process.Kill();
                                a_process.WaitForExit();
                            }

                            ProcessStartInfo Launch_startInfo = new ProcessStartInfo
                            {
                                FileName = exePath,
                                Arguments = Launch_arguments,
                                RedirectStandardOutput = true,
                                CreateNoWindow = true,
                                UseShellExecute = false
                            };

                            using (Process launch_process = new Process { StartInfo = Launch_startInfo })
                            {
                                launch_process.Start();
                                string result = await launch_process.StandardOutput.ReadToEndAsync();
                                await launch_process.WaitForExitAsync();

                                if (result.Trim().StartsWith("Success"))
                                {
                                    System.Windows.MessageBox.Show("已添加文件\"" + SourceName + "\"到本体文件列表，文件已存在于" + DriveLetter + "盘根目录！");

                                    string currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                                    string operation = "用户添加文件\"" + SourceName + "\"到本体文件列表，文件已存在于" + DriveLetter + "盘根目录" + line;
                                    string cur_result = "成功";
                                    LogOperationToFile(currentTime, operation, cur_result);
                                }
                                if (result.Trim().StartsWith("Error"))
                                {
                                    System.Windows.MessageBox.Show("已添加文件" + SourceName + "到本体文件列表，但重启文件系统并更新系统配置失败！");

                                    string currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                                    string operation = "用户添加文件\"" + SourceName + "\"到本体文件列表，文件已存在于" + DriveLetter + "盘根目录" + line;
                                    string cur_result = "添加文件到本体文件列表成功，重启文件系统并更新系统配置失败";
                                    LogOperationToFile(currentTime, operation, cur_result);
                                }
                            }
                        }
                        else
                        {
                            if (!Directory.Exists(DriveLetter + "//"))
                            {
                                System.Windows.MessageBox.Show("已添加文件\"" + SourceName + "\"到本体文件列表，当前" + DriveLetter + "盘未挂载，请挂载后手动拷贝文件到" + DriveLetter + "盘！");

                                string currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                                string operation = "用户添加文件\"" + SourceName + "\"到本体文件列表，" + DriveLetter + "盘未挂载，需挂载后手动拷贝文件到" + DriveLetter + "盘" + line;
                                string cur_result = "成功";

                                LogOperationToFile(currentTime, operation, cur_result);
                            }

                            else
                            {
                                if (SourcePath.StartsWith(DriveLetter, StringComparison.OrdinalIgnoreCase))
                                {
                                    ProcessStartInfo Shut_startInfo = new ProcessStartInfo();
                                    Shut_startInfo.FileName = exePath;
                                    Shut_startInfo.Arguments = Shut_arguments;
                                    Shut_startInfo.RedirectStandardOutput = true;
                                    Shut_startInfo.CreateNoWindow = true;
                                    Shut_startInfo.UseShellExecute = false;

                                    using (Process shut_process = Process.Start(Shut_startInfo))
                                    {
                                        using (StreamReader reader = shut_process.StandardOutput)
                                        {
                                            string result = reader.ReadToEnd();
                                            shut_process.WaitForExit();
                                        }
                                    }

                                    var encfsProcesses = Process.GetProcessesByName("encfs");
                                    foreach (var a_process in encfsProcesses)
                                    {
                                        a_process.Kill();
                                        a_process.WaitForExit();
                                    }

                                    ProcessStartInfo Launch_startInfo = new ProcessStartInfo
                                    {
                                        FileName = exePath,
                                        Arguments = Launch_arguments,
                                        RedirectStandardOutput = true,
                                        CreateNoWindow = true,
                                        UseShellExecute = false
                                    };

                                    using (Process launch_process = new Process { StartInfo = Launch_startInfo })
                                    {
                                        launch_process.Start();
                                        string result = await launch_process.StandardOutput.ReadToEndAsync();
                                        await launch_process.WaitForExitAsync();

                                        if (result.Trim().StartsWith("Success"))
                                        {
                                            System.Windows.MessageBox.Show("已添加文件\"" + SourceName + "\"到本体文件列表，该文件位于" + SourcePath + "目录！");

                                            string currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                                            string operation = "用户添加文件\"" + SourceName + "\"到本体文件列表，该文件位于" + SourcePath + "目录" + line;
                                            string cur_result = "成功";
                                            LogOperationToFile(currentTime, operation, cur_result);
                                        }
                                        if (result.Trim().StartsWith("Error"))
                                        {
                                            System.Windows.MessageBox.Show("已添加文件" + SourceName + "到本体文件列表，但重启文件系统并更新系统配置失败！");

                                            string currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                                            string operation = "用户添加文件\"" + SourceName + "\"到本体文件列表，该文件位于" + SourcePath + "目录" + line;
                                            string cur_result = "添加文件到本体文件列表成功，重启文件系统并更新系统配置失败";
                                            LogOperationToFile(currentTime, operation, cur_result);
                                        }
                                    }
                                }
                                else
                                {
                                    using (FileStream sourceStream = new FileStream(SourcePath, FileMode.Open, FileAccess.Read))
                                    using (FileStream targetStream = new FileStream(TargetPath, FileMode.Create, FileAccess.Write))
                                    {
                                        sourceStream.CopyTo(targetStream);
                                    }

                                    ProcessStartInfo Shut_startInfo = new ProcessStartInfo();
                                    Shut_startInfo.FileName = exePath;
                                    Shut_startInfo.Arguments = Shut_arguments;
                                    Shut_startInfo.RedirectStandardOutput = true;
                                    Shut_startInfo.CreateNoWindow = true;
                                    Shut_startInfo.UseShellExecute = false;

                                    using (Process shut_process = Process.Start(Shut_startInfo))
                                    {
                                        using (StreamReader reader = shut_process.StandardOutput)
                                        {
                                            string result = reader.ReadToEnd();
                                            shut_process.WaitForExit();
                                        }
                                    }

                                    var encfsProcesses = Process.GetProcessesByName("encfs");
                                    foreach (var a_process in encfsProcesses)
                                    {
                                        a_process.Kill();
                                        a_process.WaitForExit();
                                    }

                                    ProcessStartInfo Launch_startInfo = new ProcessStartInfo
                                    {
                                        FileName = exePath,
                                        Arguments = Launch_arguments,
                                        RedirectStandardOutput = true,
                                        CreateNoWindow = true,
                                        UseShellExecute = false
                                    };

                                    using (Process launch_process = new Process { StartInfo = Launch_startInfo })
                                    {
                                        launch_process.Start();
                                        string result = await launch_process.StandardOutput.ReadToEndAsync();
                                        await launch_process.WaitForExitAsync();

                                        if (result.Trim().StartsWith("Success"))
                                        {
                                            System.Windows.MessageBox.Show("已添加文件\"" + SourceName + "\"到本体文件列表，并复制到" + DriveLetter + "盘根目录！");

                                            string currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                                            string operation = "用户添加文件\"" + SourceName + "\"到本体文件列表，并复制到" + DriveLetter + "盘根目录" + line;
                                            string cur_result = "成功";
                                            LogOperationToFile(currentTime, operation, cur_result);
                                        }
                                        if (result.Trim().StartsWith("Error"))
                                        {
                                            System.Windows.MessageBox.Show("已添加文件" + SourceName + "到本体文件列表，但复制到" + DriveLetter + "盘根目录失败！");

                                            string currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                                            string operation = "用户添加文件\"" + SourceName + "\"到本体文件列表，并复制到" + DriveLetter + "盘根目录" + line;
                                            string cur_result = "添加文件成功，复制文件到\" + DriveLetter + \"盘根目录失败";
                                            LogOperationToFile(currentTime, operation, cur_result);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"添加本体文件失败: {ex.Message}");
            }
        }
        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
				string exePath = @"C:\SZTSProgramInstaller\SZTSProgram\unionFileSubstituteSystem.exe";
                string Launch_arguments = $"--mode m";
                string Shut_arguments = $"--mode u";

                System.Windows.Controls.Button deleteButton = sender as System.Windows.Controls.Button;
                DocumentItem documentToDelete = deleteButton?.DataContext as DocumentItem;

                if (documentToDelete != null && Documents.Any(doc => doc.Name == documentToDelete.Name))
                {
                    Documents.Remove(documentToDelete);
                    SaveData();

                    ProcessStartInfo Shut_startInfo = new ProcessStartInfo();
                    Shut_startInfo.FileName = exePath;
                    Shut_startInfo.Arguments = Shut_arguments;
                    Shut_startInfo.RedirectStandardOutput = true;
                    Shut_startInfo.CreateNoWindow = true;
                    Shut_startInfo.UseShellExecute = false;

                    using (Process shut_process = Process.Start(Shut_startInfo))
                    {
                        using (StreamReader reader = shut_process.StandardOutput)
                        {
                            string result = reader.ReadToEnd();
                            shut_process.WaitForExit();
                        }
                    }

                    var encfsProcesses = Process.GetProcessesByName("encfs");
                    foreach (var a_process in encfsProcesses)
                    {
                        a_process.Kill();
                        a_process.WaitForExit();
                    }

                    ProcessStartInfo Launch_startInfo = new ProcessStartInfo
                    {
                        FileName = exePath,
                        Arguments = Launch_arguments,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true,
                        UseShellExecute = false
                    };

                    using (Process launch_process = new Process { StartInfo = Launch_startInfo })
                    {
                        launch_process.Start();
                        string result = await launch_process.StandardOutput.ReadToEndAsync();
                        await launch_process.WaitForExitAsync();

                        if (result.Trim().StartsWith("Success"))
                        {
                            System.Windows.MessageBox.Show("已从本体文件列表中删除\"" + documentToDelete.Name + "\"！");
                            string currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                            string operation = "用户从本体文件列表中删除\"" + documentToDelete.Name + "\"，并更新系统配置-重启文件系统";
                            string cur_result = "成功";
                            LogOperationToFile(currentTime, operation, cur_result);
                        }
                        if (result.Trim().StartsWith("Error"))
                        {
                            System.Windows.MessageBox.Show("已从本体文件列表中删除\"" + documentToDelete.Name + "\"，但更新系统配置并重启文件系统失败！");
                            string currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                            string operation = "用户从本体文件列表中删除\"" + documentToDelete.Name + "\"，并更新系统配置-重启文件系统";
                            string cur_result = "失败";
                            LogOperationToFile(currentTime, operation, cur_result);
                        }
                    }
                }
            }

            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"删除本体文件失败: {ex.Message}");
            }
        }
        private void ReplaceBrowseButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();

                openFileDialog.Filter = "所有文件 (*.*)|*.*";
                openFileDialog.Title = "选择文件";

                bool? result = openFileDialog.ShowDialog();

                if (result == true)
                {
                    string selectedFileName = openFileDialog.FileName;
                    string fileNameOnly = System.IO.Path.GetFileName(selectedFileName);
                    ReplaceInputTextBox.Text = fileNameOnly;
                }
            }

            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"浏览并选择替身模版文件失败: {ex.Message}");
            }
        }
        private void ReplaceAddButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string newDocumentName = ReplaceInputTextBox.Text.Trim();

                if (string.IsNullOrEmpty(newDocumentName) || string.IsNullOrWhiteSpace(newDocumentName))
                {
                    System.Windows.MessageBox.Show("替身模版文件名称不能为空！");
                }

                else if (IsNotValidFileName(newDocumentName))
                {
                    System.Windows.MessageBox.Show("不是合法的替身模版文件名称！");
                }

                else
                {
                    if (true)
                    {
                        var newDocument = new DocumentItem
                        {
                            Name = newDocumentName,
                            TypeDescription = Path.HasExtension(newDocumentName) ? Path.GetExtension(newDocumentName) : "folder"
                        };

                        if (ReplaceDocuments.Any(doc => doc.Name == newDocument.Name))
                        {
                            System.Windows.MessageBox.Show("该文件在替身文件列表中已存在！");
                        }

                        else if (ReplaceDocuments.Any(doc => doc.TypeDescription == newDocument.TypeDescription))
                        {
                            System.Windows.MessageBox.Show(newDocument.TypeDescription + "类型的替身文件模版在替身列表中已存在，如需修改请先删除列表中的" + newDocument.TypeDescription + "格式替身模版文件！");
                        }

                        else
                        {
                            ReplaceDocuments.Add(newDocument);
                            SaveData();
                            ReplaceInputTextBox.Clear();
                            System.Windows.MessageBox.Show("已添加文件\"" + newDocument.Name + "\"到替身模版列表，需点击\"更新系统配置○重启文件系统\"后生效！");

                            string currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                            string operation = "用户添加文件\"" + newDocument.Name + "\"到替身模版列表";
                            string cur_result = "成功";
                            LogOperationToFile(currentTime, operation, cur_result);
                        }
                    }
                }
            }

            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"添加替身模版文件失败: {ex.Message}");
            }
        }
        private void ReplaceDeleteButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Windows.Controls.Button deleteButton = sender as System.Windows.Controls.Button;
                DocumentItem documentToDelete = deleteButton?.DataContext as DocumentItem;

                if (documentToDelete != null && ReplaceDocuments.Any(doc => doc.Name == documentToDelete.Name))
                {
                    ReplaceDocuments.Remove(documentToDelete);
                    SaveData();

                    System.Windows.MessageBox.Show("已从替身模版列表中删除\"" + documentToDelete.Name + "\"，需点击\"更新系统配置○重启文件系统\"后生效！");
                    string currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    string operation = "用户从替身模版列表中删除\"" + documentToDelete.Name + "\"";
                    string cur_result = "成功";
                    LogOperationToFile(currentTime, operation, cur_result);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"删除替身模版文件失败: {ex.Message}");
            }
        }

        private void SetTemplatePathBrowseringButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (var folderBrowserDialog = new FolderBrowserDialog())
                {
                    folderBrowserDialog.Description = "选择一个文件夹";
                    folderBrowserDialog.ShowNewFolderButton = true;

                    DialogResult result = folderBrowserDialog.ShowDialog();

                    if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(folderBrowserDialog.SelectedPath))
                    {
                        string selectedFolderPath = folderBrowserDialog.SelectedPath;
                        TemplatePathInput.Text = selectedFolderPath;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"浏览并选择替身文件模板路径失败: {ex.Message}");
            }
        }
        private void SetTemplatePathButton_Click(object sender, RoutedEventArgs e)
        {
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
            try
            {
                string newTemplatePath = TemplatePathInput.Text.Trim();
                if (!newTemplatePath.StartsWith(DriveLetter, StringComparison.OrdinalIgnoreCase))
                {
                    System.Windows.MessageBox.Show($"用户只能设置 {DriveLetter} 盘中目录作为替身文件模板路径！");
                    return;
                }

                if (!Directory.Exists(DriveLetter + "//"))
                {
                    System.Windows.MessageBox.Show($"请先挂载文件系统到{DriveLetter}盘后再设置替身文件模板路径！");
                    return;
                }

                if (!string.IsNullOrEmpty(newTemplatePath) && Regex.IsMatch(newTemplatePath, validWinPathPattern) && Directory.Exists(newTemplatePath))
                {
                    TemplatePath = newTemplatePath;
                    SaveData();
                    string sourceDirectory = @"C:\SZTSProgramInstaller\SZTSConfig\目录树模版\敏感文件资料\temp\替身文件模版";
                    try
                    {
                        string[] files = Directory.GetFiles(sourceDirectory);
                        foreach (string file in files)
                        {
                            string fileName = Path.GetFileName(file);
                            string destFile = Path.Combine(newTemplatePath, fileName);
                            File.Copy(file, destFile, true);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Windows.MessageBox.Show($"复制替身文件模版到新路径失败: {ex.Message}");
                    }

                    System.Windows.MessageBox.Show($"替身文件模版路径已更新为 {TemplatePath}，需点击\"更新系统配置○重启文件系统\"后生效！");
                    
                    string currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    string operation = "用户设置替身文件模版路径为\"" + TemplatePath + "\"" + line;
                    string cur_result = "成功";
                    LogOperationToFile(currentTime, operation, cur_result);
                }
                else
                {
                    if (!Directory.Exists(newTemplatePath))
                    {
                        System.Windows.MessageBox.Show("无效输入，该文件路径不存在！");
                    }
                    else
                    {
                        System.Windows.MessageBox.Show("无效输入，请确保输入一个合法文件路径！");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"设置替身文件模版路径失败: {ex.Message}");
            }
        }
        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
        private void ReplaceListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}
