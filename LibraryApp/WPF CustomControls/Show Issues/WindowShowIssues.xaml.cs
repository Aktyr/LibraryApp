using LibraryApp.WPF_CustomControls.Show_Issues.Issued_Books;

namespace LibraryApp.WPF_CustomControls.Show_Issues;

/// <summary>
/// Логика взаимодействия для WindowShowIssues.xaml
/// </summary>
public partial class WindowShowIssues : Window
{
    public WindowShowIssues()
    {
        InitializeComponent();
    }

    private void Button_Click_IssedBooks(object sender, RoutedEventArgs e)
    {
        WindowIssuedBooks windowIssuedBooks = new();
        windowIssuedBooks.Show();
    }

    private void Button_Click_Users(object sender, RoutedEventArgs e)
    {
        WindowUserBooks windowUserBooks = new();
        windowUserBooks.Show();
    }
}
