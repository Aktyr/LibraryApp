using LibraryApp.Controllers;
using LibraryApp.Models;
using LibraryApp.WPF_CustomControls;
using System.ComponentModel;
using System.Windows;

namespace LibraryApp;

public partial class WindowNewUser : Window, INotifyPropertyChanged
{
    private User _user = new("","","","");
    public User User
    {
        get => _user;
        set
        {
            _user = value;
            PropertyChanged?.Invoke(this, new(nameof(User)));
        }
    }

    public WindowNewUser()
    {
        InitializeComponent();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void Button_Click(object sender, RoutedEventArgs e)
    {
        // Сбросить все подсветки перед проверкой
        FieldHighlighter.ResetAllFields(LastNameTextBox, FirstNameTextBox, ContactInfoTextBox);

        var errors = new List<string>();
        var hasError = false;

        // Проверка полей и подсветка ошибок
        if (string.IsNullOrWhiteSpace(User.LastName))
        {
            FieldHighlighter.HighlightField(LastNameTextBox);
            errors.Add("Фамилия");
            hasError = true;
        }

        if (string.IsNullOrWhiteSpace(User.FirstName))
        {
            FieldHighlighter.HighlightField(FirstNameTextBox);
            errors.Add("Имя");
            hasError = true;
        }

        if (string.IsNullOrWhiteSpace(User.ContactInfo))
        {
            FieldHighlighter.HighlightField(ContactInfoTextBox);
            errors.Add("Контактная информация");
            hasError = true;
        }

        if (hasError)
        {
            MessageBox.Show($"Заполните обязательные поля: {string.Join(", ", errors)}",
                          "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Если ошибок нет - сохраняем
        var context = LibraryDataContext.Instance;
        context.Add(User);
        context.Save<User>();

        MessageBox.Show("Пользователь добавлен");
        Close();
    }

}