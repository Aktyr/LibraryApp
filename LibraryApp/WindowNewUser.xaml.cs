using LibraryApp.Models;
using LibraryApp.Controllers;
using System.Collections.ObjectModel;
using System.Windows.Controls;

namespace LibraryApp
{
    /// <summary>
    /// Логика взаимодействия для WindowNewUser.xaml
    /// </summary>
    public partial class WindowNewUser : Window
    {
        public WindowNewUser() => InitializeComponent();

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string lastName = textBox_LastName.Text;
            string firstName = textBox_FirstName.Text;
            string middleName = textBox_MiddleName.Text;
            string contactInfo = textBox_ContactInfo.Text;

            User user = new User(lastName, firstName, middleName, contactInfo);
            var context = new UserDataContext();
            context.Users.Add(user);
            context.SaveChanges();

            MessageBox.Show("Пользователь добавлен");
            Close();
        }
    }
}
