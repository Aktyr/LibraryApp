using LibraryApp.Models;
using LibraryApp.Controllers;
using System.Collections.ObjectModel;
using System.Windows.Controls;
using System.ComponentModel;

namespace LibraryApp;

/// <summary>
/// Логика взаимодействия для WindowNewUser.xaml
/// </summary>
public partial class WindowNewUser : Window, INotifyPropertyChanged
{
    private User _user = new("Фамилия", "Имя", "Отчество", "Контактная информация");
    public User User
    {
        get => _user; 
        set
        {
            _user = value;
            PropertyChanged?.Invoke(this, new(nameof(User)));   
        } 
    }
    private readonly LibraryDataContext _libraryDataContext;    
    public WindowNewUser()
    {
        InitializeComponent();
        _libraryDataContext = LibraryDataContext.Instance;
        //this.DataContext = _libraryDataContext.UserInteractor; 
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void Button_Click(object sender, RoutedEventArgs e)
    {
        ObservableCollection<User> a;

        _libraryDataContext.Add(User);
        _libraryDataContext.Save<User>();

        MessageBox.Show("Пользователь добавлен");
        Close();
    }
}
