using LibraryApp.Controllers;
using LibraryApp.Models;

namespace LibraryApp.WPF_CustomControls;

/// <summary>
/// Логика взаимодействия для WindowBookIssuing.xaml
/// </summary>
public partial class WindowBookIssuing : Window, INotifyPropertyChanged
{
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

    private  UserDataContext _contextUser;
    private readonly RoomBookDataContext _contextRoomBook;
    private readonly UserRoomBookDataContext _contextUserRoomBook;

    public WindowBookIssuing(User user, UserDataContext contextUser)
    {
        InitializeComponent();
        _contextUser = contextUser;
        _contextRoomBook = new();
        _contextUserRoomBook = new();
        this.DataContext = _contextRoomBook;
        User = user;
    }
    public event PropertyChangedEventHandler? PropertyChanged;

    private void Button_Click(object sender, RoutedEventArgs e)
    {
        var selectedItems = dataGrid.SelectedItems;

        if (selectedItems.Count > 0)
        {                
            List<string> ErrorMessage = new();

            foreach (var item in selectedItems)
            {
                var roomBook = item as RoomBook;
                if (roomBook.BookCount > 0)
                {

                    UserRoomBook UserRoomBook = new(User, roomBook);                        
                    User.UserRoomBook.Add(UserRoomBook); 
                    User.IssedBooks = User.UserRoomBook.Count; 
                    _contextUserRoomBook.UserRoomBooks.Add(UserRoomBook);
                    roomBook.BookCount -= 1;
                }
                else ErrorMessage.Add(roomBook.Book.Name);

            }

            if (ErrorMessage.Count > 0)
            {
                string message = $"Выдача отменена\nСледующие книги не были выданы, так как отсутствуют на складе:\n\n{string.Join("\n", ErrorMessage)}";
                MessageBox.Show(message);
            }
            else
            {
                dataGrid.Items.Refresh();
                _contextUserRoomBook.SaveChanges();
                _contextRoomBook.SaveChanges();
                _contextUser.SaveChanges();
                MessageBox.Show("Все книги выданны успешно");
                Close();
            }

        }
        else MessageBox.Show("Выберите хотя бы одну книгу");
    }
}
