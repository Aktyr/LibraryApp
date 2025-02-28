using LibraryApp.Controllers;
using LibraryApp.Models;
using LibraryApp.WPF_CustomControls.Show_Issues.User_Books;

namespace LibraryApp.WPF_CustomControls.Show_Issues;

/// <summary>
/// Логика взаимодействия для WindowUserBooks.xaml
/// </summary>
public partial class WindowUserBooks : Window, INotifyPropertyChanged
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
    //private UserRoomBook _userRoomBook = new(null, null);
    //public UserRoomBook UserRoomBook
    //{
    //    get => _userRoomBook;
    //    set
    //    {
    //        _userRoomBook = value;
    //        PropertyChanged?.Invoke(this, new(nameof(UserRoomBook)));
    //    }
    //}

    private readonly UserDataContext _contextUser;
    //private readonly UserRoomBookDataContext _contextUserRoomBook;
    public WindowUserBooks()
    {
        InitializeComponent();
        _contextUser = new();
        //_contextUserRoomBook = new();
        DataContext = _contextUser;
    }
    public event PropertyChangedEventHandler? PropertyChanged;

    private void Button_Click(object sender, RoutedEventArgs e)
    {

        var user = ((FrameworkElement)e.OriginalSource).DataContext as User;

        if (user != null)
        {
            WindowOpenUserBooks windowOpenUserBooks = new(user); // передаём пользователя чтобы взглянуть на выданные книги
            windowOpenUserBooks.Show();                
        }
        
    }
}
