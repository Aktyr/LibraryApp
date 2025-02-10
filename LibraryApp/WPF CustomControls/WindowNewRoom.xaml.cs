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
        private readonly BookDataContext _context;

        public WindowNewRoom()
        {
            InitializeComponent();
            _context = new BookDataContext();
            this.DataContext = _context;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void Button_AddRoom(object sender, RoutedEventArgs e)
        {
            ObservableCollection<Room> a;

            RoomDataContext context = new();

            RoomBookDataContext contextRoomBook = new();
            RoomDataContext contextRoom = new();
            BookDataContext contextBook = new();

            context.Rooms.Add(Room);
            context.SaveChanges();
            var selectedItems = dataGrid.SelectedItems;

            if (selectedItems.Count != 0)
            {                
                foreach (var item in selectedItems)
                {
                    //ProcessItem(item);
                    var book = item as Book;

                    RoomBook RoomBook = new(Room, book);

                    Room.Books.Add(book);
                    book.Rooms.Add(Room);
                    Room.Books.Remove(book);
                    Room.Books.Add(book);

                    //contextBook.Books.Add(book);
                    contextRoomBook.RoomBooks.Add(RoomBook);
                }

                contextRoom.Rooms.Add(Room);

                contextRoomBook.SaveChanges();
                contextRoom.SaveChanges();
                contextBook.SaveChanges();

                MessageBox.Show("Комната добавлена");

            }
            else MessageBox.Show("Комната добавлена, в ней нет книг");

        }

        private void ProcessItem(object item)
        {
            var book = item as Book;

            RoomBookDataContext contextRoomBook = new();
            RoomDataContext contextRoom = new();
            BookDataContext contextBook = new();

            RoomBook RoomBook = new(Room, book);

            Room.Books.Add(book);
            book.Rooms.Add(Room);
            Room.Books.Remove(book);
            Room.Books.Add(book);


            contextRoomBook.RoomBooks.Add(RoomBook);
            contextRoom.Rooms.Add(Room);
            contextBook.Books.Add(book);

            contextRoomBook.SaveChanges();
            contextRoom.SaveChanges();
            contextBook.SaveChanges();


        }
    }
}
