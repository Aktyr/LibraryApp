using LibraryApp.Controllers;
using LibraryApp.Models;

namespace LibraryApp.WPF_CustomControls
{
    /// <summary>
    /// Логика взаимодействия для WindowBookIssuing.xaml
    /// </summary>
    public partial class WindowBookIssuing : Window
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
        private RoomBook _roomBook = new(null, null);
        public RoomBook RoomBook
        {
            get => _roomBook;
            set
            {
                _roomBook = value;
                PropertyChanged?.Invoke(this, new(nameof(RoomBook)));
            }
        }

        private readonly BookDataContext _contextBook;
        private readonly UserDataContext _contextUser;
        public WindowBookIssuing()
        {
            InitializeComponent();
            _contextBook = new BookDataContext();
            _contextUser = new UserDataContext();
            //dataGrid_RoomBooks.ItemsSource = _contextBook.Books;
            //dataGrid_Users.ItemsSource = _contextUser.Users;
            //this.DataContext = _contextUser;
            //this.DataContext = _contextBook;
        }
        public event PropertyChangedEventHandler? PropertyChanged;





    }
}
