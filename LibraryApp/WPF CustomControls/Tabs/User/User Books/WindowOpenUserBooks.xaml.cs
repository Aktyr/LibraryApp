using LibraryApp.Controllers;
using LibraryApp.Models;


namespace LibraryApp.WPF_CustomControls.Show_Issues.User_Books;

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
    private readonly LibraryDataContext _libraryDataContext;
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

    
    public WindowOpenUserBooks(User user)
    {
        InitializeComponent();
        _libraryDataContext = LibraryDataContext.Instance;
        User = user;
        DisplayedInformation();
    }
    public event PropertyChangedEventHandler? PropertyChanged;
    private void DisplayedInformation()
    {
        List<UserRoomBook> Data = [];
        var matchingData = _libraryDataContext.UserRoomBookDataContext.UserRoomBooks
                                .Where(us =>
                                       us.User.Id == User.Id)
                                .ToList();

        Data.AddRange(matchingData);
        
        dataGrid.ItemsSource = Data;
    }
}
