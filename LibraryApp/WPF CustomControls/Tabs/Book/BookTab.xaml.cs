using LibraryApp.Controllers;
using LibraryApp.Models;
using LibraryApp.WPF_CustomControls.Tabs;

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
        this.DataContext = _libraryDataContext;
    }
    public event PropertyChangedEventHandler? PropertyChanged;
    private void EditBookButton_Click(object sender, RoutedEventArgs e)
    {
        var book = ((FrameworkElement)e.OriginalSource).DataContext as Book;

        if (book != null)
        {
            EditObject editObject = new(book);
            editObject.ShowDialog();

            if (editObject.saveChanges == true)
                _libraryDataContext.Save<Book>();

            dataGrid.Items.Refresh();
        }
    }
    private void DeleteBookButton_Click(object sender, RoutedEventArgs e)
    {
        var book = ((FrameworkElement)e.OriginalSource).DataContext as Book;

        if (book != null)
        {
            ConfirmDeletion confirmDeletion = new();
            confirmDeletion.ShowDialog();

            if (confirmDeletion.IsConfirm == true)
            {
                //_libraryDataContext.BookDataContext.Books.Remove(book);
                //_libraryDataContext.BookDataContext.SaveChanges();

                _libraryDataContext.Remove(book); // проверить работу
                _libraryDataContext.Save<Book>();

                dataGrid.Items.Refresh();
            }
        }
    }
    private void AddBookButton_Click(object sender, RoutedEventArgs e)
    {
        WindowNewBook windowNewBook = new();
        windowNewBook.ShowDialog();
        dataGrid.Items.Refresh();
    }
}
