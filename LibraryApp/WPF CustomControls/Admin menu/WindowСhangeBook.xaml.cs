using LibraryApp.Controllers;
using LibraryApp.Models;

namespace LibraryApp.WPF_CustomControls.Admin_menu;

/// <summary>
/// Логика взаимодействия для WindowСhangeBook.xaml
/// </summary>
public partial class WindowСhangeBook : Window, INotifyPropertyChanged
{
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
    private readonly BookDataContext _context;
    public WindowСhangeBook()
    {
        InitializeComponent();
        _context = new BookDataContext();
        DataContext = _context;
    }
    public event PropertyChangedEventHandler? PropertyChanged;
    private void Button_Click(object sender, RoutedEventArgs e)
    {
        _context.SaveChanges();
        Close();
    }
}
