using LibraryApp.Controllers;
using LibraryApp.Models;
using LibraryApp.WPF_CustomControls.Tabs;
using LibraryApp.WPF_CustomControls.Tabs.Room.Book_adding;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;

namespace LibraryApp.WPF_CustomControls;

public partial class WindowBookCount : Window, INotifyPropertyChanged
{
    #region Коллекции

    public event PropertyChangedEventHandler? PropertyChanged;
    protected virtual void OnPropertyChanged(string propertyName)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private ObservableCollection<RoomBook> _roomBooksList;
    public ObservableCollection<RoomBook> RoomBooksList

    {
        get => _roomBooksList;
        set
        {
            _roomBooksList = value;
            OnPropertyChanged(nameof(RoomBooksList));
        }
    }

    private RoomBook _roomBook = new(null, null);
    public RoomBook RoomBook
    {
        get => _roomBook;
        set
        {
            _roomBook = value;
            OnPropertyChanged(nameof(RoomBook));
        }
    }

    private Room _room = new("Название комнаты");
    public Room Room
    {
        get => _room;
        set
        {
            _room = value;
            OnPropertyChanged(nameof(Room));
        }
    }

    #endregion

    private readonly LibraryDataContext _libraryDataContext;
    public string RoomName => Room.Name;

    public WindowBookCount(Room room)
    {
        InitializeComponent();
        _libraryDataContext = LibraryDataContext.Instance;
        Room = room;

        RoomBooksList = new ObservableCollection<RoomBook>();

        DataContext = this;
        DisplayedInformation();
    }

    private void DisplayedInformation()
    {
        var matchingData = _libraryDataContext.GetAll<RoomBook>()
                                .Where(rb => rb.Room.Id == Room.Id)
                                .ToList();

        // Очищаем и заполняем ObservableCollection
        RoomBooksList.Clear();
        foreach (var item in matchingData)
            RoomBooksList.Add(item);
    }

    #region Кнопки
    private void NewBookButton_Click(object sender, RoutedEventArgs e)
    {
        AddBook addBook = new(Room);
        addBook.ShowDialog();
        DisplayedInformation();
        //dataGrid.Items.Refresh();
    }

    private void AddBookButton_Click(object sender, RoutedEventArgs e)
    {
        var roomBook = dataGrid.SelectedItem as RoomBook;
        if (roomBook != null)
        {
            WindowBookLogic windowBookLogic = new(roomBook);
            windowBookLogic.ShowDialog();
        }
    }
    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        _libraryDataContext.Save<RoomBook>();
        Close();
    }

    private void EditBookButton_Click(object sender, RoutedEventArgs e)
    {
        var book = dataGrid.SelectedItem as RoomBook;
        if (book != null)
        {
            EditObject editObject = new(book);
            editObject.ShowDialog();

            if (editObject.saveChanges == true)
            {
                _libraryDataContext.Save<RoomBook>();
                // Обновляем конкретный элемент в коллекции
                var index = RoomBooksList.IndexOf(book);
                if (index >= 0)
                {
                    RoomBooksList[index] = book;
                }
            }
            dataGrid.SelectedItem = null;
        }
    }

    private void DeleteBookButton_Click(object sender, RoutedEventArgs e)
    {
        var roomBook = dataGrid.SelectedItem as RoomBook;
        if (roomBook != null)
        {
            ConfirmDeletion confirmDeletion = new();
            confirmDeletion.ShowDialog();

            if (confirmDeletion.IsConfirm == true)
            {
                _libraryDataContext.Remove(roomBook);
                _libraryDataContext.Save<RoomBook>();

                // Удаляем из ObservableCollection - UI обновится автоматически
                RoomBooksList.Remove(roomBook);
            }
        }
        dataGrid.SelectedItem = null;
    }
    #endregion
}