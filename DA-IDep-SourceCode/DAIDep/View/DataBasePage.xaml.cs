using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Data;
using System.Data.SQLite;
using System.Windows.Media.Imaging;
using System.Diagnostics;
using System.Windows.Navigation;
using System.Reflection.Metadata;

namespace DAIDep.View
{
    public partial class DataBasePage : Page
    {
        private string dbConnectionString;
        private string localDbPath = "C:\\SZTSProgramInstaller\\SZTSProgram\\test.db";
        private string defaultDbPath = "C:\\SZTSProgramInstaller\\SZTSConfig\\szts_log.db";

        private string GUIlogFilePath;
        private string TriggerlogFilePath;
        private string line = "                                                                                                                ";

        private int allCount = 5000;
        private int limitCount = 501;

        private void InitializeTriggerLogFilePath()
        {
            string currentDate = DateTime.Now.ToString("yyyy-MM-dd");
            string logFilePath = $"C:\\SZTSProgramInstaller\\SZTSConfig\\sztsfs_trigger_replacement_logs\\{currentDate}_sztsfs_trigger_replacement_log.txt";

            if (!System.IO.File.Exists(logFilePath))
            {
                File.Create(logFilePath).Dispose();
            }
            TriggerlogFilePath = logFilePath;
        }
        private void InitializeGUILogFilePath()
        {
            string currentDate = DateTime.Now.ToString("yyyy-MM-dd");
            string logFilePath = $"C:\\SZTSProgramInstaller\\SZTSConfig\\sztsfs_gui_logs\\{currentDate}_sztsfs_gui_log.txt";

            if (!System.IO.File.Exists(logFilePath))
            {
                File.Create(logFilePath).Dispose();
            }
            GUIlogFilePath = logFilePath;
        }

        private void SetDatabaseConnectionString()
        {
            if (System.IO.File.Exists(localDbPath))
            {
                dbConnectionString = $"Data Source={localDbPath};Version=3;Cache=Shared;";
            }
            else
            {
                dbConnectionString = $"Data Source={defaultDbPath};Version=3;Cache=Shared;";
            }
        }

        public DataBasePage()
        {
            InitializeComponent();
            SetDatabaseConnectionString();
            FilesystemButton_Click(null, null);
        }
        private void LoadData(string tableName)
        {
            try
            {
                DataGrid.Visibility = Visibility.Visible;
                DisplayImage.Visibility = Visibility.Collapsed;

                using (SQLiteConnection connection = new SQLiteConnection(dbConnectionString))
                {
                    connection.Open();
                    string query = $"SELECT * FROM {tableName} ORDER BY \"Time of Day\" DESC LIMIT " + allCount;
                    
                    SQLiteDataAdapter dataAdapter = new SQLiteDataAdapter(query, connection);
                    DataTable dataTable = new DataTable();
                    dataAdapter.Fill(dataTable);

                    string[] columnNames;
                    switch (tableName)
                    {
                        case "filesystem":
                            columnNames = new string[] { "Process Name", "PID", "Path", "Operation" };
                            break;
                        case "network":
                            columnNames = new string[] { "Process Name", "PID", "Operation", "Path", "Detail", "Command line" };
                            break;
                        case "process":
                            columnNames = new string[] { "Process Name", "PID", "Parent PID", "Operation", "Command Line", "User", "Path" };
                            break;
                        case "malicious":
                            columnNames = new string[] { "Process Name", "PID", "Malicious File Path", "Operation", "Command Line" };
                            break;
                        default:
                            columnNames = new string[0];
                            break;
                    }

                    ProcessDataTable(dataTable, tableName, limitCount, columnNames);

                    DataGrid.Columns.Clear();
                    DataGrid.ItemsSource = null;
                    DataGrid.AutoGenerateColumns = false;

                    switch (tableName)
                    {
                        case "filesystem":
                            AddColumns(dataTable, new string[] { "RowNum", "Time of Day", "Process Name", "PID", "Path", "Operation" },
                                        new string[] { "序号", "时间", "进程名称", "进程号", "文件路径", "操作行为" });
                            break;
                        case "network":
                            AddColumns(dataTable, new string[] { "RowNum", "Time of Day", "Process Name", "PID", "Operation", "Path", "Detail", "Command line" },
                                        new string[] { "序号", "时间", "进程名称", "进程号", "网络行为", "四元组", "详细信息", "进程命令信息" });
                            break;
                        case "process":
                            AddColumns(dataTable, new string[] { "RowNum", "Time of Day", "Process Name", "PID", "Parent PID", "Operation", "Command Line", "User", "Path" },
                                        new string[] { "序号", "时间", "进程名称", "进程号", "父进程号", "操作行为", "进程命令信息", "用户账号", "路径" });
                            break;
                        case "malicious":
                            AddColumns(dataTable, new string[] { "RowNum", "Time of Day", "Process Name", "PID", "Malicious File Path", "Operation", "Command Line" },
                                        new string[] { "序号", "时间", "进程名称", "进程号", "可疑文件路径", "操作行为", "进程命令信息" });
                            break;
                            //case "operations":
                            //    AddColumns(dataTable, new string[] { "RowNum", "Time of Day", "Operation", "Result"},
                            //               new string[] { "序号", "时间", "用户操作", "操作结果" });
                            break;
                    }

                    DataGrid.ItemsSource = dataTable.DefaultView;
                }
            }
            catch (SQLiteException ex) when (ex.Message.Contains("database is locked"))
            {
                System.Windows.MessageBox.Show($"日志数据正在入库，请等待一分钟左右再进行查看！");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"加载日志数据失败: {ex.Message}");
            }
        }
        private void ProcessDataTable(DataTable dataTable, string tableName, int maxRows, string[] columnNames)
        {
            if (!dataTable.Columns.Contains("RowNum"))
            {
                dataTable.Columns.Add("RowNum", typeof(int));
            }

            DataColumn formattedTimeColumn = new DataColumn("Formatted Time of Day");
            if (tableName == "triggers" || tableName == "operations")
            {
                dataTable.Columns.Add(formattedTimeColumn);
            }

            DataRow previousRow = null;
            List<DataRow> rowsToDelete = new List<DataRow>();

            for (int i = dataTable.Rows.Count - 1; i >= 0; i--)
            {
                DataRow currentRow = dataTable.Rows[i];
                if (tableName == "filesystem")
                {
                    string operation = currentRow["Operation"].ToString();
                    if (operation != "CreateFile" && operation != "ReadFile" && operation != "WriteFile")
                    {
                        rowsToDelete.Add(currentRow);
                        continue;
                    }
                }

                if (tableName == "malicious")
                {
                    string malicious_file_path= currentRow["Malicious File Path"].ToString();
                    if (malicious_file_path.StartsWith("\\Device\\Volume{"))
                    {
                        rowsToDelete.Add(currentRow);
                        continue;
                    }
                }

                if (tableName == "filesystem")
                {
                    string processName = currentRow["Process Name"].ToString();
                    if (processName == "unionFileSubstituteSystem.exe" || processName == "encfs.exe")
                    {
                        rowsToDelete.Add(currentRow);
                        continue;
                    }
                }

                DateTime timeOfDay = DateTime.Parse(currentRow["Time of Day"].ToString());
                if (tableName == "triggers" || tableName == "operations")
                {
                    string formattedTime = timeOfDay.ToString("yyyy-MM-dd h:mm:ss tt", System.Globalization.CultureInfo.InvariantCulture);
                    formattedTime = formattedTime.Replace("AM", "上午").Replace("PM", "下午");
                    currentRow["Formatted Time of Day"] = formattedTime;
                }
                else
                {
                    DateTime adjustedTime = timeOfDay.AddMinutes(-6);
                    currentRow["Time of Day"] = adjustedTime.ToString("h:mm:ss tt");
                }

                if (previousRow != null)
                {
                    bool isSame = true;
                    foreach (var columnName in columnNames)
                    {
                        if (!currentRow[columnName].Equals(previousRow[columnName]))
                        {
                            isSame = false;
                            break;
                        }
                    }

                    if (isSame)
                    {
                        TimeSpan currentTime = DateTime.Parse(currentRow["Time of Day"].ToString()).TimeOfDay;
                        TimeSpan previousTime = DateTime.Parse(previousRow["Time of Day"].ToString()).TimeOfDay;
                        double timeDifference = (currentTime - previousTime).Duration().TotalSeconds;

                        if (timeDifference < 1)
                        {
                            rowsToDelete.Add(currentRow);
                            continue;
                        }
                    }
                }
                previousRow = currentRow;
            }

            foreach (DataRow row in rowsToDelete)
            {
                dataTable.Rows.Remove(row);
            }
            rowsToDelete.Clear();

            if (tableName == "triggers" || tableName == "operations")
            {
                dataTable.Columns.Remove("Time of Day");
                formattedTimeColumn.ColumnName = "Time of Day";

                DataTable sortedTable = dataTable.DefaultView.ToTable();

                int rowNum = 1;
                foreach (DataRow row in sortedTable.Rows)
                {
                    row["RowNum"] = rowNum++;
                    if (rowNum > maxRows)
                    {
                        rowsToDelete.Add(row);
                    }
                }

                foreach (DataRow row in rowsToDelete)
                {
                    sortedTable.Rows.Remove(row);
                }

                sortedTable.AcceptChanges();
                dataTable.Clear();
                dataTable.Merge(sortedTable);
            }
            else
            {
                int rowNum = 1;
                foreach (DataRow row in dataTable.Rows)
                {
                    row["RowNum"] = rowNum++;
                    if (rowNum > maxRows)
                    {
                        rowsToDelete.Add(row);
                    }
                }

                foreach (DataRow row in rowsToDelete)
                {
                    dataTable.Rows.Remove(row);
                }
                dataTable.AcceptChanges();
            }
        }
        private void AddColumns(DataTable dataTable, string[] columnNames, string[] columnHeaders)
        {
            for (int i = 0; i < columnNames.Length; i++)
            {
                if (dataTable.Columns.Contains(columnNames[i]))
                {
                    DataGridTextColumn column = new DataGridTextColumn
                    {
                        Header = columnHeaders[i],
                        Binding = new System.Windows.Data.Binding($"[{columnNames[i]}]"),
                        CanUserSort = false
                    };
                    DataGrid.Columns.Add(column);
                }
            }
        }

        private void FilesystemButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                LoadData("filesystem");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"读取文件操作行为日志失败: {ex.Message}");
            }
        }

        private void NetworkButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                LoadData("network");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"读取进程网络行为日志失败: {ex.Message}");
            }
        }
        private void ProcessButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                LoadData("process");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"读取进程详细信息日志失败: {ex.Message}");
            }
        }

        private void MalwareButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                LoadData("malicious");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"读取可疑文件信息日志失败: {ex.Message}");
            }
        }

        private void TriggerButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                InitializeTriggerLogFilePath();
                if (TriggerlogFilePath != null)
                {
                    LoadTriggerLogData(TriggerlogFilePath);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"读取触发替身事件日志失败: {ex.Message}");
            }
        }
        private void OperationButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                InitializeGUILogFilePath();
                if (GUIlogFilePath != null)
                {
                    LoadGUILogData(GUIlogFilePath);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"读取用户操作行为日志失败: {ex.Message}");
            }
        }
        private void LoadTriggerLogData(string logFilePath)
        {
            try
            {
                DataGrid.ItemsSource = null;
                DataGrid.Columns.Clear();

                DataGrid.Visibility = Visibility.Visible;
                DisplayImage.Visibility = Visibility.Collapsed;

                string tempLogFilePath = $"{logFilePath}_tmp";
                System.IO.File.Copy(logFilePath, tempLogFilePath, true);

                DataTable dataTable = new DataTable();
                dataTable.Columns.Add("RowNum", typeof(int));
                dataTable.Columns.Add("Time of Day", typeof(DateTime));
                dataTable.Columns.Add("Process Name", typeof(string));
                dataTable.Columns.Add("PID", typeof(string));
                dataTable.Columns.Add("Original File Path", typeof(string));
                dataTable.Columns.Add("Replacement File Path", typeof(string));

                var rows = new List<DataRow>();
                int rowCount = 0;

                var allLines = System.IO.File.ReadAllLines(tempLogFilePath);
                var lastLines = allLines.Reverse().Take(allCount);

                foreach (var line in lastLines)
                {
                    var values = line.Split(',');
                    if (values.Length == 5)
                    {
                        if (DateTime.TryParse(values[0], out DateTime timeOfDay))
                        {
                            if (values[1] == "<unknown>")
                            {
                                continue;
                            }

                            var newRow = dataTable.NewRow();
                            newRow["Time of Day"] = timeOfDay;
                            newRow["Process Name"] = values[1];
                            newRow["PID"] = values[2];
                            newRow["Original File Path"] = values[3];
                            newRow["Replacement File Path"] = values[4];
                            rows.Add(newRow);

                            rowCount++;
                            if (rowCount >= allCount)
                            {
                                break;
                            }
                        }
                    }
                }

                foreach (var row in rows)
                {
                    dataTable.Rows.Add(row);
                }

                ProcessDataTable(dataTable, "triggers", limitCount, new string[] { "Process Name", "PID", "Original File Path", "Replacement File Path" });

                DataGrid.Columns.Clear();
                DataGrid.ItemsSource = null;
                DataGrid.AutoGenerateColumns = false;

                AddColumns(dataTable, new string[] { "RowNum", "Time of Day", "Process Name", "PID", "Original File Path", "Replacement File Path" },
                           new string[] { "序号", "时间", "进程名称", "进程号", "原始文件路径", "替换文件路径" + line });

                DataGrid.ItemsSource = dataTable.DefaultView;

                System.IO.File.Delete(tempLogFilePath);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"加载触发替身事件日志数据失败: {ex.Message}");
            }
        }

        private void LoadGUILogData(string logFilePath)
        {
            try
            {
                DataGrid.ItemsSource = null;
                DataGrid.Columns.Clear();
                DataGrid.AutoGenerateColumns = false;

                if (!File.Exists(logFilePath))
                {
                    DataTable emptyTable = new DataTable();
                    emptyTable.Columns.Add("RowNum", typeof(string));
                    emptyTable.Columns.Add("Time of Day", typeof(string));
                    emptyTable.Columns.Add("Operation", typeof(string));
                    emptyTable.Columns.Add("Result", typeof(string));

                    AddColumns(emptyTable, new[] { "RowNum", "Time of Day", "Operation", "Result" },
                               new[] { "序号", "时间", "用户操作", "操作结果" + line});

                    DataGrid.ItemsSource = emptyTable.DefaultView;
                    return;
                }

                DataTable dataTable = new DataTable();
                dataTable.Columns.Add("RowNum", typeof(int));
                dataTable.Columns.Add("Time of Day", typeof(DateTime));
                dataTable.Columns.Add("Operation", typeof(string));
                dataTable.Columns.Add("Result", typeof(string));

                int rowNum = 1;
                foreach (var line in File.ReadAllLines(logFilePath))
                {
                    var values = line.Split(',');
                    if (values.Length == 3)
                    {
                        DataRow row = dataTable.NewRow();
                        row["RowNum"] = rowNum++;
                        row["Time of Day"] = DateTime.Parse(values[0]);
                        row["Operation"] = values[1];
                        row["Result"] = values[2];
                        dataTable.Rows.Add(row);
                    }
                }

                AddColumns(dataTable, new[] { "RowNum", "Time of Day", "Operation", "Result" },
                           new[] { "序号", "时间", "用户操作", "操作结果" + line});

                DataGrid.ItemsSource = dataTable.DefaultView;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"加载日志数据失败：{ex.Message}");
            }
        }
        
        private void CreateEmptyTemplate()
        {
            DataTable emptyTable = new DataTable();
            emptyTable.Columns.Add("RowNum", typeof(string));
            emptyTable.Columns.Add("Time of Day", typeof(string));
            emptyTable.Columns.Add("Operation", typeof(string));
            emptyTable.Columns.Add("Result", typeof(string));

            AddColumns(emptyTable, new string[] { "RowNum", "Time of Day", "Operation", "Result" },
                       new string[] { "序号", "时间", "用户操作", "操作结果" + line});

            DataGrid.ItemsSource = emptyTable.DefaultView;
        }

        private void ImageButton_Click(object sender, RoutedEventArgs e)
        {
            DataGrid.Visibility = Visibility.Collapsed;
            DisplayImage.Visibility = Visibility.Visible;

            string defaultPath = @"C:\\SZTSProgramInstaller\\SZTSConfig\\pgraph.png";
            string todayDate = DateTime.Today.ToString("yyyy-MM-dd");
            string directoryPath = @"C:\\SZTSProgramInstaller\\SZTSProgram\\database_backups\\";
            string imageName = $"{todayDate}-pgraph.png";
            string imagePath = Path.Combine(directoryPath, imageName);

            if (!System.IO.File.Exists(imagePath))
            {
                imagePath = defaultPath;
            }
            try
            {
                DisplayImage.Source = new BitmapImage(new Uri(imagePath, UriKind.Absolute));
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"从此路径加载图片失败: {imagePath}\nException: {ex.Message}");
            }
        }

        private void OpenFolderButton_Click(object sender, RoutedEventArgs e)
        {   
            try
            {
                string defaultPath = @"C:\SZTSProgramInstaller\SZTSConfig";
                string folderPath = @"C:\SZTSProgramInstaller\SZTSProgram\malicious_file";

                if (!System.IO.Directory.Exists(folderPath))
                {
                    folderPath = defaultPath;
                }

                Process.Start("explorer.exe", folderPath);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"打开文件夹路径失败: {ex.Message}");
            }
        }

        private void ReturnButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GetNavigationService(this).Navigate(new ConfigPage());
        }
    }
}
