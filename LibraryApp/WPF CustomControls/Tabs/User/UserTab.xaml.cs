using LibraryApp.Controllers;
using LibraryApp.Models;
using LibraryApp.WPF_CustomControls.Show_Issues.User_Books;

namespace LibraryApp.WPF_CustomControls;

/// <summary>
/// Логика взаимодействия для UserTab.xaml
/// </summary>
public partial class UserTab : UserControl, INotifyPropertyChanged
{
    private readonly LibraryDataContext _libraryDataContext;
    public UserTab()
    {
        InitializeComponent();
        _libraryDataContext = LibraryDataContext.Instance;
        this.DataContext = _libraryDataContext.UserDataContext;
    }
    public event PropertyChangedEventHandler? PropertyChanged;

    private void AddBookButton_Click(object sender, RoutedEventArgs e)
    {
        var user = ((FrameworkElement)e.OriginalSource).DataContext as User;

        if (user != null)
        {
            WindowBookIssuing windowBookIssuing = new(user);
            windowBookIssuing.ShowDialog();
            dataGrid.Items.Refresh();
        }
    }
    private void ShowBooksButton_Click(object sender, RoutedEventArgs e)
    {
        var user = ((FrameworkElement)e.OriginalSource).DataContext as User;

        if (user != null)
        {
            WindowOpenUserBooks windowOpenUserBooks = new(user); // передаём пользователя чтобы взглянуть на выданные книги
            windowOpenUserBooks.ShowDialog();
            dataGrid.Items.Refresh();
        }
    }
    private void EditUserButton_Click(object sender, RoutedEventArgs e)
    {
        var user = ((FrameworkElement)e.OriginalSource).DataContext as User;

        if (user != null)
        {
            dataGrid.Items.Refresh();
        }
    }
    private void DeleteUserButton_Click(object sender, RoutedEventArgs e) // сделать проверку на наличие выданных книг чтобы удалять пользователей без долгов
    {
        var user = ((FrameworkElement)e.OriginalSource).DataContext as User;

        if (user != null)
        {
            ConfirmDeletion confirmDeletion = new();
            confirmDeletion.ShowDialog();

            if (confirmDeletion.Confirm == true)
            {
                _libraryDataContext.UserDataContext.Users.Remove(user);
                _libraryDataContext.UserDataContext.SaveChanges();
                dataGrid.Items.Refresh();
            }
        }
    }
    private void AddUserButton_Click(object sender, RoutedEventArgs e)
    {
        WindowNewUser windowNewUser = new();
        windowNewUser.ShowDialog();
        dataGrid.Items.Refresh();
    }
}
