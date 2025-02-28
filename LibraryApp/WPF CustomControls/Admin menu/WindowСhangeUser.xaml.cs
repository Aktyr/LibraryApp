using LibraryApp.Controllers;
using LibraryApp.Models;

namespace LibraryApp.WPF_CustomControls.Admin_menu;

/// <summary>
/// Логика взаимодействия для WindowСhangeUser.xaml
/// </summary>
public partial class WindowСhangeUser : Window, INotifyPropertyChanged
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

    private readonly UserDataContext _context;
    public WindowСhangeUser()
    {
        InitializeComponent();
        _context = new UserDataContext();
        this.DataContext = _context;
    }
    public event PropertyChangedEventHandler? PropertyChanged;

    private void Button_Click(object sender, RoutedEventArgs e)
    {
        _context.SaveChanges();
        Close();
    }
}
