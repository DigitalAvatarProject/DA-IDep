using System;
using System.Collections.Generic;
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
using System.IO;
using Newtonsoft.Json;
using System.Data.SQLite;
using System.Text.RegularExpressions;
namespace DAIDep.View
{
    public partial class ProtectPwdPage : Page
    {
        private string DriveLetter { get; set; }
        private string DrivePath { get; set; }
        private string TemplatePath { get; set; }
        private int AuthValidTime { get; set; }
        private int PasswordTimeout { get; set; }
        private string LogKeywords { get; set; }
        private int NoDisturb { get; set; }
        private int TrappingTag { get; set; }


        const string validWinPathPattern = @"^[a-zA-Z]:\\[\\\S|*\S]?.*$";
        const string invalidCharsPattern = @"[<>:""/\\|?*]";

        private readonly string jsonPath = "C:\\SZTSProgramInstaller\\SZTSConfig\\forms.json";
        private readonly string pwdjsonPath = "C:\\SZTSProgramInstaller\\SZTSConfig\\f_secure.json";

        private string dbFilePath = @"C:\SZTSProgramInstaller\SZTSProgram\test.db";
        static string currentDate = DateTime.Now.ToString("yyyy-MM-dd");
        private string logFilePath = $"C:\\SZTSProgramInstaller\\SZTSConfig\\sztsfs_gui_logs\\{currentDate}_sztsfs_gui_log.txt";

        private string connectionString;
        private SQLiteConnection connection;
        private string line = "                                       ";

        public static ProtectPwdPage Instance { get; private set; }
        public ProtectPwdPage()
        {
            InitializeComponent();
            Instance = this;

            DriveLetter = string.Empty;
            DrivePath = "C:\\SZTSProgramInstaller\\SZTSConfig\\szts_encdir";
            AuthValidTime = 20;
            PasswordTimeout = 10;
            LogKeywords = "敏感文件资料";
            NoDisturb = 0;
            TrappingTag = 0;
            LoadData();

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
        public void LoadData()
        {
            if (File.Exists(jsonPath))
            {
                try
                {
                    string jsonStr = File.ReadAllText(jsonPath);
                    dynamic jsonObj = JsonConvert.DeserializeObject(jsonStr);

                    DriveLetter = jsonObj.driveLetter != null ? jsonObj.driveLetter.ToString() : "V:";
                    DriveLetterInput.Text = DriveLetter.Length > 0 ? DriveLetter[0].ToString() : string.Empty;

                    DrivePath = jsonObj.drivePath != null ? jsonObj.drivePath.ToString() : "C:\\SZTSProgramInstaller\\SZTSConfig\\szts_encdir";
                    DrivePathInput.Text = DrivePath;

                    AuthValidTime = jsonObj.authValidTime != null ? (int)jsonObj.authValidTime : 20;
                    AuthValidTimeInput.Text = AuthValidTime.ToString();

                    PasswordTimeout = jsonObj.passwordTimeout != null ? (int)jsonObj.passwordTimeout : 10;
                    PasswordTimeoutInput.Text = PasswordTimeout.ToString();

                    LogKeywords = jsonObj.logKeywords != null ? jsonObj.logKeywords.ToString() : "敏感文件资料";
                    LogKeywordInput.Text = LogKeywords;

                    NoDisturb = jsonObj.Nodisturb != null ? (int)jsonObj.Nodisturb : 0;

                    TrappingTag = jsonObj.Trapping != null ? (int)jsonObj.Trapping : 0;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("解析Json配置文件失败: " + ex.Message);
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
                string raw_jsonStr = File.ReadAllText(jsonPath);
                dynamic raw_jsonObj = JsonConvert.DeserializeObject(raw_jsonStr);
                NoDisturb = raw_jsonObj.Nodisturb != null ? (int)raw_jsonObj.Nodisturb : 0;
                TrappingTag = raw_jsonObj.Trapping != null ? (int)raw_jsonObj.Trapping : 0;

                var jsonObj = new
                {
                    driveLetter = DriveLetter,
                    drivePath = DrivePath,
                    authValidTime = AuthValidTime,
                    passwordTimeout = PasswordTimeout,
                    logKeywords = LogKeywords,
                    Nodisturb = NoDisturb,
                    Trapping = TrappingTag
                };
                string jsonStr = JsonConvert.SerializeObject(jsonObj, Formatting.Indented);
                File.WriteAllText(jsonPath, jsonStr);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to save data to JSON: " + ex.Message);
            }
        }
        private void SetDriveLetterButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string input = DriveLetterInput.Text.Trim().ToUpper();
                string newDriveLetter = string.Empty;

                if (input.Length == 1 && input[0] >= 'H' && input[0] <= 'Z')
                {
                    newDriveLetter = input + ":";
                }
                else if (input.Length == 2)
                {
                    if (input[1] == ':')
                    {
                        if (input[0] >= 'H' && input[0] <= 'Z')
                        {
                            newDriveLetter = input;
                        }
                    }
                    else if (input[1] == '：')
                    {
                        if (input[0] >= 'H' && input[0] <= 'Z')
                        {
                            newDriveLetter = input[0] + ":";
                        }
                    }
                }
                if (!string.IsNullOrEmpty(newDriveLetter))
                {
                    DriveLetter = newDriveLetter;
                    SaveData();
                    System.Windows.MessageBox.Show($"目标盘符已设置为 {DriveLetter[0]}，需点击\"更新系统配置○重启文件系统\"后生效！");

                    string currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    string operation = "用户设置目标盘符为 " + DriveLetter[0] + " 盘" + line;
                    string cur_result = "成功";
                    LogOperationToFile(currentTime, operation, cur_result);
                }
                else
                {
                    System.Windows.MessageBox.Show("无效输入，请确保输入一个H到Z之间的有效盘符！");
                    LoadData();
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"设置目标盘符失败: {ex.Message}");
                LoadData();
            }
        }
        private void SetDrivePathButton_Click(object sender, RoutedEventArgs e)
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
                        DrivePathInput.Text = selectedFolderPath;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"浏览并选择加密盘挂载路径失败: {ex.Message}");
            }
        }
        private void ConfirmDrivePathButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string newDrivePath = DrivePathInput.Text.Trim();
                if (!string.IsNullOrEmpty(newDrivePath) && Regex.IsMatch(newDrivePath, validWinPathPattern))
                {
                    if (Directory.Exists(newDrivePath) && Directory.GetFiles(newDrivePath, "*.xml").Length > 0)
                    {
                        DrivePath = newDrivePath;
                        SaveData();
                        System.Windows.MessageBox.Show($"加密盘挂载路径已更新为 {DrivePath}，需点击\"更新系统配置○重启文件系统\"后生效！");

                        string currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                        string operation = "用户设置加密盘挂载路径为 \"" + DrivePath + "\"" + line;
                        string cur_result = "成功";
                        LogOperationToFile(currentTime, operation, cur_result);
                    }
                    else
                    {
                        System.Windows.MessageBox.Show("无效输入，请确保输入一个可挂载的加密文件路径！");
                        LoadData();
                    }
                }
                else
                {
                    System.Windows.MessageBox.Show("无效输入，请确保输入一个可挂载的加密文件路径！");
                    LoadData();
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"设置加密盘挂载路径失败: {ex.Message}");
                LoadData();
            }
        }
        private void SetAuthValidTimeButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (int.TryParse(AuthValidTimeInput.Text.Trim(), out int newAuthValidTime))
                {
                    if (newAuthValidTime <= 0 || newAuthValidTime > 1440)
                    {
                        System.Windows.MessageBox.Show("无效输入，身份验证有效时间（分钟）必须是1到1440之间的整数！");
                        LoadData();
                        return;
                    }
                    AuthValidTime = newAuthValidTime;
                    SaveData();
                    System.Windows.MessageBox.Show($"身份验证有效时间已更新为 {AuthValidTime} 分钟，需点击\"更新系统配置○重启文件系统\"后生效！");

                    string currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    string operation = "用户设置身份验证有效时间为" + AuthValidTime + "分钟" + line;
                    string cur_result = "成功";
                    LogOperationToFile(currentTime, operation, cur_result);
                }
                else
                {
                    System.Windows.MessageBox.Show("无效输入，身份验证有效时间（分钟）必须是1到1440之间的整数！");
                    LoadData();
                    return;
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"设置身份验证有效时间失败: {ex.Message}");
                LoadData();
            }
        }
        private void SetPasswordTimeoutButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (int.TryParse(PasswordTimeoutInput.Text.Trim(), out int newPasswordTimeout))
                {
                    if (newPasswordTimeout <= 4 || newPasswordTimeout > 59)
                    {
                        System.Windows.MessageBox.Show("无效输入，密码输入超时时间（秒）必须是5到59之间的整数！");
                        LoadData();
                    }
                    else
                    {
                        PasswordTimeout = newPasswordTimeout;
                        SaveData();
                        System.Windows.MessageBox.Show($"密码输入超时时间已更新为 {PasswordTimeout} 秒，需点击\"更新系统配置○重启文件系统\"后生效！");

                        string currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                        string operation = "用户设置密码输入超时时间为" + PasswordTimeout + "秒" + line;
                        string cur_result = "成功";
                        LogOperationToFile(currentTime, operation, cur_result);
                    }
                }
                else
                {
                    System.Windows.MessageBox.Show("无效输入，密码输入超时时间（秒）必须是大于0的整数！");
                    LoadData();
                    return;
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"设置密码输入超时时间失败: {ex.Message}");
                LoadData();
            }
        }

        private void SetDrivePwdTextButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!IsDrivePasswordValid(DrivePasswordBox.Password))
                {
                    System.Windows.MessageBox.Show("无效输入，加密盘挂载密码必须是八位字母或数字组合！");
                    LoadData();
                    return;
                }
                else
                {
                    SavePassword(DrivePasswordBox.Password, "Drive");
                    System.Windows.MessageBox.Show($"修改加密盘挂载密码成功，需点击\"更新系统配置○重启文件系统\"后生效！");

                    string currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    string operation = "用户更新加密盘挂载密码" + line;
                    string cur_result = "成功";
                    LogOperationToFile(currentTime, operation, cur_result);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"设置加密盘挂载密码失败: {ex.Message}");
                LoadData();
            }
        }

        private bool IsDrivePasswordValid(string password)
        {
            return password.Length == 8 && password.All(c => char.IsLetterOrDigit(c));
        }

        private bool IsUserPasswordValid(string password)
        {
            return password.Length == 6 && password.All(char.IsDigit);
        }

        private void SavePassword(string password, string key)
        {
            string encodedPassword = Convert.ToBase64String(Encoding.UTF8.GetBytes(password));

            dynamic jsonObj;
            if (File.Exists(pwdjsonPath))
            {
                string jsonStr = File.ReadAllText(pwdjsonPath);
                jsonObj = JsonConvert.DeserializeObject(jsonStr);
            }
            else
            {
                jsonObj = new Newtonsoft.Json.Linq.JObject();
            }

            jsonObj[key] = encodedPassword;
            string output = JsonConvert.SerializeObject(jsonObj, Formatting.Indented);
            File.WriteAllText(pwdjsonPath, output);
        }

        private void SetLogKeywordTextButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string newLogKeyword = LogKeywordInput.Text.Trim();

                if (!string.IsNullOrEmpty(newLogKeyword) && newLogKeyword.Length >= 1 && !Regex.IsMatch(newLogKeyword, invalidCharsPattern))
                {
                    LogKeywords = newLogKeyword;
                    SaveData();
                    System.Windows.MessageBox.Show($"日志采集关键字已设置为: {LogKeywords}，需点击\"更新系统配置○重启文件系统\"后生效！");

                    string currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    string operation = "用户设置日志采集关键字为: " + LogKeywords + line;
                    string cur_result = "成功";
                    LogOperationToFile(currentTime, operation, cur_result);
                }
                else
                {
                    System.Windows.MessageBox.Show("无效输入！");
                    LoadData();
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"设置日志采集关键字失败: {ex.Message}");
                LoadData();
            }
        }
    }
}
