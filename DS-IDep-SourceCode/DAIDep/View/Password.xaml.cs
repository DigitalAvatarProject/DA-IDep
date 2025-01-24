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

namespace DAIDep.View
{
    /// <summary>
    /// PasswordPage.xaml 的交互逻辑
    /// </summary>
    public partial class Password : Page
    {
        public Password()
        {
            isAdmin = false;
            InitializeComponent();
        }
        public static bool isAdmin { get; private set; } = false;

        public static bool GetAdmin()
        {
            return isAdmin;
        }
        public void setAdmin(bool admin)
        {
            isAdmin = admin;
        }
        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            PasswordBox passwordBox = (PasswordBox)sender;
            if (passwordBox.Password.Length == passwordBox.MaxLength)
            {
                // Move focus to the next control
                TraversalRequest request = new TraversalRequest(FocusNavigationDirection.Next);
                passwordBox.MoveFocus(request);
            }
        }
        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            string password = passwordBox1.Password + passwordBox2.Password + passwordBox3.Password + passwordBox4.Password + passwordBox5.Password + passwordBox6.Password;
            string filePath = "./file.txt"; // 替换为你的文件路径
            string correctPassword = "000000";
            if (File.Exists(filePath))
            {

                correctPassword = File.ReadAllText(filePath);
            }
            else
            {
                System.Windows.MessageBox.Show($"Password file not found in {System.IO.Path.GetFullPath(filePath)}");
            }
            if (password == correctPassword)
            {
                isAdmin = true;
                System.Windows.MessageBox.Show("Admin mode enabled.");
            }
            else
            {
                isAdmin = false;
            }
            NavigationService.GetNavigationService(this).Navigate(new ConfigPage());
        }
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            isAdmin = false;
            NavigationService.GoBack();
        }
    }

}
