using LibraryApp.Controllers;
using LibraryApp.Models;

namespace LibraryApp.WPF_CustomControls.Admin_menu;

/// <summary>
/// Логика взаимодействия для WindowСhangeRoom.xaml
/// </summary>
public partial class WindowСhangeRoom : Window, INotifyPropertyChanged
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
    private readonly RoomDataContext _context;

    public WindowСhangeRoom()
    {
        InitializeComponent();
        _context = new();
        DataContext = _context;
    }
    public event PropertyChangedEventHandler? PropertyChanged;
    private void Button_Click(object sender, RoutedEventArgs e)
    {
        _context.SaveChanges();
        Close();
    }
}
