using LibraryApp.Models;
using LibraryApp.Controllers;
using System.Collections.ObjectModel;
using System.Media;

namespace LibraryApp;

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

    private readonly LibraryDataContext _libraryDataContext;

    public WindowNewRoom()
    {
        InitializeComponent();
        _libraryDataContext = LibraryDataContext.Instance;

        this.DataContext = _libraryDataContext.BookDataContext;
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
                Book = book;

                RoomBook RoomBook = new(Room, book);

                _libraryDataContext.RoomBookDataContext.RoomBooks.Add(RoomBook);
                //Room.RoomBooks.Add(RoomBook);
            }
            _libraryDataContext.RoomDataContext.Rooms.Add(Room);

            //_libraryDataContext.BookDataContext.SaveChanges(); 
            //_libraryDataContext.RoomDataContext.SaveChanges();
            _libraryDataContext.RoomBookDataContext.SaveChanges();

            MessageBox.Show("Комната добавлена");
            Close();

        }
        else
        {
            SystemSounds.Beep.Play();
            MessageBox.Show("Выберите хотябы одну книгу в комнату");
        }

    }
}
