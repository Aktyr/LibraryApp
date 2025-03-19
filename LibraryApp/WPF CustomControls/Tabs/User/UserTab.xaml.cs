using LibraryApp.Controllers;
using LibraryApp.Models;
using LibraryApp.WPF_CustomControls.Show_Issues.User_Books;
using LibraryApp.WPF_CustomControls.Tabs;
using System.Globalization;

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
        CalculateNearestReturnDates();
    }
    public event PropertyChangedEventHandler? PropertyChanged;


    private void CalculateNearestReturnDates()
    {
        foreach (var user in _libraryDataContext.UserDataContext.Users)
        {
            // Находим все записи о выданных книгах для текущего пользователя
            var userBooks = _libraryDataContext.UserRoomBookDataContext.UserRoomBooks
                .Where(x => x.User.Id == user.Id && x.Deadline != null)
                .ToList();

            // Находим ближайшую дату сдачи
            var nearestReturnDate = userBooks
                .OrderBy(x => x.Deadline)
                .FirstOrDefault()?.Deadline;

            var daysLeft = nearestReturnDate - DateTime.Today;            

            if (nearestReturnDate != null)
            {
                user.NearestReturnDate = daysLeft;
            }
            else
            {
                user.NearestReturnDate = null; // если нет долгов
            }
        }
    }
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
            EditObject editObject = new(user);
            editObject.ShowDialog();

            if (editObject.saveChanges == true)
                _libraryDataContext.UserDataContext.SaveChanges();

            dataGrid.Items.Refresh();
        }
    }
    private void DeleteUserButton_Click(object sender, RoutedEventArgs e)
    {
        var user = ((FrameworkElement)e.OriginalSource).DataContext as User;
        var debts = _libraryDataContext.UserRoomBookDataContext.UserRoomBooks.Find(x => x.User.Id == user.Id);

        if (debts != null)
        {
            MessageBox.Show($"Удаление невозможно. У пользователя \n{user.FullName} \nЕщё есть долги");
        }
        else
        {
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
    }
    private void AddUserButton_Click(object sender, RoutedEventArgs e)
    {
        WindowNewUser windowNewUser = new();
        windowNewUser.ShowDialog();
        dataGrid.Items.Refresh();
    }
}