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
    private readonly LibraryDataContext _libraryDataContext;

    public WindowBookCount()
    {
        InitializeComponent();
        _libraryDataContext = LibraryDataContext.Instance;
        this.DataContext = _libraryDataContext.RoomBookDataContext;
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
