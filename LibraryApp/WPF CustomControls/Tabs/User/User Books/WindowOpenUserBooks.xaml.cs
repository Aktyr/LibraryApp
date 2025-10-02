using System.Collections.ObjectModel;
using LibraryApp.Controllers;
using LibraryApp.Models;


namespace LibraryApp.WPF_CustomControls.Show_Issues.User_Books
{
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

        private ObservableCollection<UserRoomBook> _userRoomBook;
        public ObservableCollection<UserRoomBook> UserRoomBook
        {
            get => _userRoomBook;
            set
            {
                _userRoomBook = value;
                PropertyChanged?.Invoke(this, new(nameof(UserRoomBook)));
            }
        }

        private readonly LibraryDataContext _libraryDataContext;
        public WindowOpenUserBooks(User user)
        {
            InitializeComponent();
            _libraryDataContext = LibraryDataContext.Instance;
            UserRoomBook = new ObservableCollection<UserRoomBook>();
            User = user;
            DisplayedInformation();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void DisplayedInformation()
        {
            var matchingData = _libraryDataContext.GetAll<UserRoomBook>()
                                    .Where(us => us.User.Id == User.Id)
                                    .ToList();

            UserRoomBook.Clear();
            foreach (var item in matchingData)
            {
                UserRoomBook.Add(item);
            }

            dataGrid.ItemsSource = UserRoomBook; //
        }

        private void WriteOffBookButton_Click(object sender, RoutedEventArgs e)
        {
            if (((FrameworkElement)e.OriginalSource).DataContext is not UserRoomBook userRoomBook)
                return;

            // Проверяем, что у UserRoomBook есть ссылка на RoomBook
            if (userRoomBook.RoomBook == null)
            {
                MessageBox.Show("Ошибка: отсутствует информация о связи с комнатой и книгой", 
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Получаем книгу и комнату из RoomBook
            var book = userRoomBook.RoomBook.Book;
            var room = userRoomBook.RoomBook.Room;

            if (book == null || room == null)
            {
                MessageBox.Show("Ошибка: не найдена информация о книге или комнате",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Находим запись RoomBook для этой комнаты и книги
            var roomBook = _libraryDataContext.GetAll<RoomBook>()
                .FirstOrDefault(rb =>
                    rb.Room.Id == room.Id &&
                    rb.Book.Id == book.Id);

            if (roomBook == null)
            {
                MessageBox.Show("Ошибка: книга не найдена в указанной комнате",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            roomBook.BookCount += 1;

            _libraryDataContext.Remove(userRoomBook);
            _libraryDataContext.SaveAll();
            DisplayedInformation();

            // проверку на случайное списывание

            MessageBox.Show("Книга успешно списана у пользователя");
        }
    }
}