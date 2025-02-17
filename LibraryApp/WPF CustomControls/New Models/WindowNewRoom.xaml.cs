using LibraryApp.Models;
using LibraryApp.Controllers;
using System.ComponentModel;
using System.Diagnostics.Metrics;
using System.Collections.ObjectModel;
using System.Windows.Controls;

namespace LibraryApp
{
    /// <summary>
    /// Логика взаимодействия для WindowNewRoom.xaml
    /// </summary>
    public partial class WindowNewRoom : Window, INotifyPropertyChanged
    {
        private Room _room = new("Имя комнаты");
        public Room Room
        {
            get => _room;
            set
            {
                _room = value;
                PropertyChanged?.Invoke(this, new(nameof(Room)));
            }
        }
        private Book _book = new("Название", "Автор", 0, "Издатель");
        public Book Book
        {
            get => _book;
            set
            {
                _book = value;
                PropertyChanged?.Invoke(this, new(nameof(Book)));
            }
        }
        private readonly BookDataContext _contextBook;
        private readonly RoomBookDataContext _contextRoomBook;
        private readonly RoomDataContext _contextRoom;

        public WindowNewRoom()
        {
            InitializeComponent();
            _contextBook = new();
            _contextRoomBook = new();
            _contextRoom = new();
            this.DataContext = _contextBook;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void Button_AddRoom(object sender, RoutedEventArgs e)
        {
            ObservableCollection<Room> a;
            
            var selectedItems = dataGrid.SelectedItems;

            if (selectedItems.Count != 0)
            {
                foreach (var item in selectedItems)
                {
                    var book = item as Book;

                    RoomBook RoomBook = new(Room, book);

                    Room.RoomBooks.Add(RoomBook);
                    book.RoomBooks.Add(RoomBook);
                    Room.RoomBooks.Remove(RoomBook);
                    Room.RoomBooks.Add(RoomBook);

                    //_contextBook.Books.Add(book);
                    _contextRoomBook.RoomBooks.Add(RoomBook);
                }
                   //_contextBook.SaveChanges();

                _contextRoom.Rooms.Add(Room);
                _contextRoom.SaveChanges();
                _contextRoomBook.SaveChanges();


                MessageBox.Show("Комната добавлена");

            }
            else MessageBox.Show("Комната добавлена, в ней нет книг");

            Close();

        }
    }
}
