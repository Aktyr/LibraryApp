using LibraryApp.Controllers;
using LibraryApp.Models;

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
   
    }
    private void ShowBooksButton_Click(object sender, RoutedEventArgs e) { }
    private void EditUserButton_Click(object sender, RoutedEventArgs e) { }
    private void DeleteUserButton_Click(object sender, RoutedEventArgs e) { }
    private void AddUserButton_Click(object sender, RoutedEventArgs e)
    {
        WindowNewUser windowNewUser = new();
        windowNewUser.ShowDialog();
        dataGrid.Items.Refresh();
    }
}
