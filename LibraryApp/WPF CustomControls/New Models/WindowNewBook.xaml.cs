using LibraryApp.Controllers;
using LibraryApp.Models;
using System.Collections.ObjectModel;
using System.Media;


namespace LibraryApp;

/// <summary>
/// Логика взаимодействия для WindowNewBook.xaml
/// </summary>
public partial class WindowNewBook : Window, INotifyPropertyChanged
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

    private readonly LibraryDataContext _libraryDataContext;
    public WindowNewBook()
    {
        InitializeComponent();
        _libraryDataContext = LibraryDataContext.Instance; 
       // this.DataContext = _libraryDataContext.BookInteractor; // А зачем это здеть вообще?
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void Button_Click(object sender, RoutedEventArgs e)
    {
        ObservableCollection<Book> a;
        if (Book.Year > 0)
        {
            _libraryDataContext.Add(Book);
            _libraryDataContext.Save<Book>();

            MessageBox.Show("Книга добавлена");
            Close();
        }
        else
        {
            SystemSounds.Beep.Play();
            MessageBox.Show("Неверный формат года\nВведите целое положительное число");
        }
    }
}
