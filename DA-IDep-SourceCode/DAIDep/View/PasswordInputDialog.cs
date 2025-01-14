using System.Windows;

namespace DAIDep.View
{
    public partial class PasswordInputDialog : Window
    {
        public string Password { get; private set; }
        public string Message { get; set; }

        public PasswordInputDialog(string message)
        {
            InitializeComponent();
            Message = message;
            DataContext = this;
        }

        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            Password = PasswordBox.Password;
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
