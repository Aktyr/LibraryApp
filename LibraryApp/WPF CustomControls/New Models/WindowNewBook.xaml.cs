using LibraryApp.Controllers;
using LibraryApp.Models;
using LibraryApp.WPF_CustomControls;
using System.Collections.ObjectModel;
using System.Media;


namespace LibraryApp;

/// <summary>
/// Логика взаимодействия для WindowNewBook.xaml
/// </summary>
public partial class WindowNewBook : Window, INotifyPropertyChanged
{
    private Book _book = new("", "", 0, "");
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
       // this.DataContext = _libraryDataContext.BookInteractor; // А зачем это здесь вообще?
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void Button_Click(object sender, RoutedEventArgs e)
    {
        // Сбросить все подсветки перед проверкой
        FieldHighlighter.ResetAllFields(NameTextBox, AuthorTextBox, YearTextBox, PublisherTextBox);

        var errors = new List<string>();
        var hasError = false;

        // Проверка обязательных полей
        if (string.IsNullOrWhiteSpace(Book.Name))
        {
            FieldHighlighter.HighlightField(NameTextBox);
            errors.Add("Название");
            hasError = true;
        }

        if (string.IsNullOrWhiteSpace(Book.Autor))
        {
            FieldHighlighter.HighlightField(AuthorTextBox);
            errors.Add("Автор");
            hasError = true;
        }

        if (string.IsNullOrWhiteSpace(Book.Publisher))
        {
            FieldHighlighter.HighlightField(PublisherTextBox);
            errors.Add("Издатель");
            hasError = true;
        }

        if (Book.Year <= 0 || Book.Year > 9999)
        {
            FieldHighlighter.HighlightField(YearTextBox);
            errors.Add("Год (Должен быть положительным числом)");
            hasError = true;
        }

        if (Book.Year > 9999)
        {
            FieldHighlighter.HighlightField(YearTextBox);
            errors.Add("Год (Не может превышать 9999)");
            hasError = true;
        }

        if (hasError)
        {
            SystemSounds.Beep.Play();
            MessageBox.Show($"Заполните корректно все поля:\n{string.Join("\n", errors)}",
                          "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        ObservableCollection<Book> a;
        _libraryDataContext.Add(Book);
        _libraryDataContext.Save<Book>();

        MessageBox.Show("Книга добавлена");
        Close();
    }

}
