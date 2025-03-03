using LibraryApp.Controllers;
using LibraryApp.Models;

namespace LibraryApp.WPF_CustomControls;

/// <summary>
/// Логика взаимодействия для WindowBookCount.xaml
/// </summary>
public partial class WindowBookCount : Window, INotifyPropertyChanged
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

    private Room _room = new("Название комнаты");
    public Room Room
    {
        get => _room;
        set
        {
            _room = value;
            PropertyChanged?.Invoke(this, new(nameof(RoomBook)));
        }
    }
    private readonly LibraryDataContext _libraryDataContext;
    public string RoomName => Room.Name;
    public WindowBookCount(Room room)
    {
        InitializeComponent();
        _libraryDataContext = LibraryDataContext.Instance;
        Room = room;
        this.DataContext = _libraryDataContext.RoomBookDataContext; // нужно обратиться только к книгам в текущей комнате
    }
    public event PropertyChangedEventHandler? PropertyChanged;
    private void AddBookButton_Click(object sender, RoutedEventArgs e)
    {            
        var roomBook = ((FrameworkElement)e.OriginalSource).DataContext as RoomBook;

        if (roomBook != null)
        {
            WindowBookLogic windowBookLogic = new(roomBook);
            windowBookLogic.ShowDialog();
            dataGrid.Items.Refresh();
        }
    }

    private void Button_Click(object sender, RoutedEventArgs e)
    {        
        _libraryDataContext.RoomBookDataContext.SaveChanges();

        Close();
    }
}
