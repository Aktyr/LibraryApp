using LibraryApp.Controllers;
using LibraryApp.Models;
using LibraryApp.WPF_CustomControls.Tabs;
using LibraryApp.WPF_CustomControls.Tabs.Room.Book_adding;
using System.Globalization;
using System.Xml.Serialization;

namespace LibraryApp.WPF_CustomControls;

/// <summary>
/// Логика взаимодействия для RoomTab.xaml
/// </summary>
public partial class RoomTab : UserControl, INotifyPropertyChanged
{
    private readonly LibraryDataContext _libraryDataContext;
    public RoomTab()
    {
        InitializeComponent();
        _libraryDataContext = LibraryDataContext.Instance;
        this.DataContext = _libraryDataContext;
        CalculateNumOfBooks();
    }
    public event PropertyChangedEventHandler? PropertyChanged;

    private void CalculateNumOfBooks()
    {
        foreach (var books in _libraryDataContext.BookList)
        {
            // Проходим по всем комнатам
            foreach (var room in _libraryDataContext.RoomList)
            {
                var roomBooks = _libraryDataContext.GetAll<RoomBook>()
                    .Where(rb => rb.Room.Id == room.Id)
                    .ToList();

                room.SumOfBooks = roomBooks.Sum(rb => rb.BookCount);
            }
        }
    }

    private void AddBookButton_Click(object sender, RoutedEventArgs e)
    {
        var room = dataGrid.SelectedItem as Room;

        if (room != null)
        {
            AddBook addBook = new(room);
            addBook.ShowDialog();
            dataGrid.Items.Refresh();
        }
        dataGrid.SelectedItem = null;
    }
    private void ShowBooksButton_Click(object sender, RoutedEventArgs e)
    {
        var room = dataGrid.SelectedItem as Room;

        if (room != null)
        {
            WindowBookCount windowBookCount = new(room);
            windowBookCount.ShowDialog();
            CalculateNumOfBooks();
            dataGrid.Items.Refresh();
            dataGrid.SelectedItem = null;
        }
    }
    private void EditRoomButton_Click(object sender, RoutedEventArgs e)
    {
        var room = dataGrid.SelectedItem as Room;

        if (room != null)
        {
            EditObject editObject = new(room);
            editObject.ShowDialog();

            if (editObject.saveChanges == true)
                _libraryDataContext.Save<Room>();

            dataGrid.Items.Refresh();
        }
        dataGrid.SelectedItem = null;
    }
    private void DeleteRoomButton_Click(object sender, RoutedEventArgs e) // ?сделать чтобы так же удалялись все RoomBook в текущей Room?
    {
        var room = dataGrid.SelectedItem as Room;

        if (room != null)
        {
            ConfirmDeletion confirmDeletion = new();
            confirmDeletion.ShowDialog();

            if (confirmDeletion.IsConfirm == true)
            {
                _libraryDataContext.Remove(room);
                _libraryDataContext.Save<Room>();
                dataGrid.Items.Refresh();
            }
        }
        dataGrid.SelectedItem = null;
    }
    private void AddRoomButton_Click(object sender, RoutedEventArgs e)
    {
        WindowNewRoom windowNewRoom = new();
        windowNewRoom.ShowDialog();
        dataGrid.Items.Refresh();
        dataGrid.SelectedItem = null;
    }
}
