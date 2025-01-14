using System.Windows;

namespace DAIDep.View
{
    public partial class NewPasswordInputDialog : Window
    {
        public string NewPassword => NewPasswordBox.Password;
        public string RepeatPassword => RepeatPasswordBox.Password;

        public NewPasswordInputDialog()
        {
            InitializeComponent();
        }

        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
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
