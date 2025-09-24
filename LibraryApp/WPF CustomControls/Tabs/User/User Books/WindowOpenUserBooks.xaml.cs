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
            var userRoomBook = ((FrameworkElement)e.OriginalSource).DataContext as UserRoomBook;

            if (userRoomBook != null)
            {
                var foundId = _libraryDataContext.GetAll<RoomBook>().Find
                    (x => x.Id == userRoomBook.RoomBook.Id);
                _libraryDataContext.Remove(userRoomBook);

                foundId.BookCount += 1;

                _libraryDataContext.Save<UserRoomBook>();
                _libraryDataContext.Save<RoomBook>();

                _libraryDataContext.Remove(userRoomBook);

                MessageBox.Show("Книга списана");                
            }
        }
    }
}