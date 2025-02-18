using LibraryApp.Controllers;
using LibraryApp.Models;
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
using System.Windows.Shapes;

namespace LibraryApp.WPF_CustomControls.Show_Issues.User_Books
{
    /// <summary>
    /// Логика взаимодействия для WindowOpenUserBooks.xaml
    /// </summary>
    public partial class WindowOpenUserBooks : Window, INotifyPropertyChanged
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
        private UserRoomBook _userRoomBook = new(null, null);
        public UserRoomBook UserRoomBook
        {
            get => _userRoomBook;
            set
            {
                _userRoomBook = value;
                PropertyChanged?.Invoke(this, new(nameof(UserRoomBook)));
            }
        }

        private readonly UserDataContext _contextUser;
        private readonly UserRoomBookDataContext _contextUserRoomBook;
        public WindowOpenUserBooks(User user)
        {
            InitializeComponent();
            User = user;
            this.DataContext = User.UserRoomBook;
            
        }
        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
