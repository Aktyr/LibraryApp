using LibraryApp.Controllers;

namespace LibraryApp.WPF_CustomControls;

/// <summary>
/// Логика взаимодействия для BookTab.xaml
/// </summary>
public partial class BookTab : UserControl, INotifyPropertyChanged
{
    private readonly LibraryDataContext _libraryDataContext;
    public BookTab()
    {
        InitializeComponent();
        _libraryDataContext = LibraryDataContext.Instance;
        this.DataContext = _libraryDataContext.BookDataContext;
    }
    public event PropertyChangedEventHandler? PropertyChanged;
    private void EditBookButton_Click(object sender, RoutedEventArgs e) { }
    private void DeleteBookButton_Click(object sender, RoutedEventArgs e) { }
    private void AddBookButton_Click(object sender, RoutedEventArgs e)
    {
        WindowNewBook windowNewBook = new();
        windowNewBook.ShowDialog();
        dataGrid.Items.Refresh();
    }
}
