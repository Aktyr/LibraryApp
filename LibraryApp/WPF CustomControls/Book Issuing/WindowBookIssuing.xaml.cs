using LibraryApp.Controllers;
using LibraryApp.Models;
using System.Collections.ObjectModel;
using System.Security.RightsManagement;

namespace LibraryApp.WPF_CustomControls
{
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

        private readonly UserDataContext _contextUser;
        private readonly RoomBookDataContext _contextRoomBook;
        private readonly UserRoomBookDataContext _contextUserRoomBook;

        public WindowBookIssuing(User user)
        {
            InitializeComponent();
            _contextRoomBook = new();
            _contextUserRoomBook = new();
            _contextUser = new();
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
                        //User.UserRoomBook.Add(UserRoomBook); // в пользователя действительно добавляются данные 
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
                    //_contextUser.SaveChanges(); // оно почему-то не сохраняет изменения, но буквально то же самое прокатывает с книгами
                    _contextRoomBook.SaveChanges();
                    _contextUserRoomBook.SaveChanges();
                    MessageBox.Show("Все книги выданны успешно");
                    Close();
                }

            }
            else MessageBox.Show("Выберите хотя бы одну книгу");
        }
    }
}
