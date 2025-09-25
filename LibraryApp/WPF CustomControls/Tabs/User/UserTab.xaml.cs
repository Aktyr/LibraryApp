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
        this.DataContext = _libraryDataContext;
        CalculateNearestReturnDates();
    }
    public event PropertyChangedEventHandler? PropertyChanged;

    // сделать что-то с отображением данных только в днях
    private void CalculateNearestReturnDates()
    {
        foreach (var user in _libraryDataContext.UserList)
        {
            // Находим все записи о выданных книгах для текущего пользователя
            var userBooks = _libraryDataContext.UserRoomBookList
                .Where(x => x.User.Id == user.Id && x.Deadline != null)
                .ToList();

            // Находим ближайшую дату сдачи
            var nearestReturnDate = userBooks
                .OrderBy(x => x.Deadline)
                .FirstOrDefault()?.Deadline;

            if (nearestReturnDate != null)
            {
                user.NearestReturnDate = nearestReturnDate - DateTime.Today;
            }
            else
            {
                user.NearestReturnDate = null; // если нет долгов
            }
        }


    }
    private void AddBookButton_Click(object sender, RoutedEventArgs e)
    {
        var user = dataGrid.SelectedItem as User;
            
        if (user != null)
        {
            WindowBookIssuing windowBookIssuing = new(user);
            windowBookIssuing.ShowDialog();
            CalculateNearestReturnDates();
            dataGrid.Items.Refresh();
            dataGrid.SelectedItem = null;
        }
    }
    private void ShowBooksButton_Click(object sender, RoutedEventArgs e)
    {
        var user = dataGrid.SelectedItem as User;

        if (user != null)
        {
            WindowOpenUserBooks windowOpenUserBooks = new(user); // передаём пользователя чтобы взглянуть на выданные книги
            windowOpenUserBooks.ShowDialog();
            CalculateNearestReturnDates();
            dataGrid.Items.Refresh(); 
            dataGrid.SelectedItem = null;
        }
    }
    private void EditUserButton_Click(object sender, RoutedEventArgs e)
    {
        var user = dataGrid.SelectedItem as User;

        if (user != null)
        {
            EditObject editObject = new(user);
            editObject.ShowDialog();

            if (editObject.saveChanges == true)
                _libraryDataContext.Save<User>();

            dataGrid.Items.Refresh();
            dataGrid.SelectedItem = null;
        }
    }
    private void DeleteUserButton_Click(object sender, RoutedEventArgs e)
    {
        var user = dataGrid.SelectedItem as User;
        var debts = _libraryDataContext.UserRoomBookList.Find(x => x.User.Id == user.Id);

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

                if (confirmDeletion.IsConfirm == true)
                {
                    _libraryDataContext.Remove(user);
                    _libraryDataContext.Save<User>();
                    dataGrid.Items.Refresh();                    
                }
            }
        }
        dataGrid.SelectedItem = null;
    }
    private void AddUserButton_Click(object sender, RoutedEventArgs e)
    {
        WindowNewUser windowNewUser = new();
        windowNewUser.ShowDialog();
        dataGrid.Items.Refresh();
        dataGrid.SelectedItem = null;
    }
}
