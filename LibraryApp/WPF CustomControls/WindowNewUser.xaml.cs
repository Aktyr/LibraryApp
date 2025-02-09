using LibraryApp.Models;
using LibraryApp.Controllers;
using System.Collections.ObjectModel;
using System.Windows.Controls;
using System.ComponentModel;

namespace LibraryApp
{
    /// <summary>
    /// Логика взаимодействия для WindowNewUser.xaml
    /// </summary>
    public partial class WindowNewUser : Window, INotifyPropertyChanged
    {
        private User _user = new("Фамилия", "Имя", "Отчество", "Контактная информация");
        public User User
        {
            get => _user; 
            set
            {
                _user = value;
                PropertyChanged?.Invoke(this, new(nameof(User)));   
            } 
        }

        private readonly UserDataContext _context;
        public WindowNewUser()
        {
            InitializeComponent();
            _context = new UserDataContext();
            this.DataContext = _context;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            ObservableCollection<User> a;

            var context = new UserDataContext();
            context.Users.Add(User);
            context.SaveChanges();
            MessageBox.Show("Пользователь добавлен");
            Close();
        }
    }
}
