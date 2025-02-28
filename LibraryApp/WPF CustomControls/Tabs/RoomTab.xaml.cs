using LibraryApp.Controllers;

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
        this.DataContext = _libraryDataContext.RoomDataContext;
    }
    public event PropertyChangedEventHandler? PropertyChanged;

    private void AddBookButton_Click(object sender, RoutedEventArgs e) { }
    private void ShowBooksButton_Click(object sender, RoutedEventArgs e) { }
    private void EditUserButton_Click(object sender, RoutedEventArgs e) { }
    private void AddRoomButton_Click(object sender, RoutedEventArgs e)
    {
        WindowNewRoom windowNewRoom = new();
        windowNewRoom.ShowDialog();
        dataGrid.Items.Refresh();
    }
}
