﻿using LibraryApp.Controllers;
using LibraryApp.Models;
using System.Media;

namespace LibraryApp.WPF_CustomControls;

/// <summary>
/// Логика взаимодействия для WindowBookIssuing.xaml
/// </summary>
public partial class WindowBookIssuing : Window, INotifyPropertyChanged
{
    private DateTime? _selectedDate;
    public DateTime? SelectedDate
    {
        get => _selectedDate;
        set
        {
            _selectedDate = value;
            PropertyChanged?.Invoke(this, new(nameof(SelectedDate)));
        }
    }

    private RoomBook _roomBook = new(null, null);
    public RoomBook RoomBook
    {
        get => _roomBook;
        set
        {
            _roomBook = value;
            PropertyChanged?.Invoke(this, new(nameof(RoomBook)));
        }
    }

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

    public WindowBookIssuing(User user)
    {
        InitializeComponent();
        _libraryDataContext = LibraryDataContext.Instance;
        this.DataContext = _libraryDataContext.RoomBookDataContext;
        User = user;        
        CalendarSettings();
    }
    public event PropertyChangedEventHandler? PropertyChanged;

    private void CalendarSettings()
    {
        SelectedDate = null;
        DateTime startDate = DateTime.Now;
        DateTime endDate = startDate.AddMonths(1);

        //Calendar_WPF.DisplayDateStart = startDate;
        //Calendar_WPF.DisplayDateEnd = endDate;

        // Заблокируем все даты до начала диапазона
        Calendar_WPF.BlackoutDates.Add(new CalendarDateRange(DateTime.MinValue, startDate));

        // Заблокируем все даты после конца диапазона
        Calendar_WPF.BlackoutDates.Add(new CalendarDateRange(endDate, DateTime.MaxValue));
    }


    private void Button_Click(object sender, RoutedEventArgs e)
    {
        var selectedItems = dataGrid.SelectedItems;

        SelectedDate = Calendar_WPF.SelectedDate;

        if (selectedItems.Count > 0)
        {
            if (SelectedDate != null)
            {
                List<string> ErrorMessage = new();

                foreach (var item in selectedItems)
                {
                    var roomBook = item as RoomBook;
                    if (roomBook.BookCount > 0)
                    {
                        UserRoomBook UserRoomBook = new(User, roomBook, SelectedDate);

                        _libraryDataContext.UserRoomBookDataContext.UserRoomBooks.Add(UserRoomBook);
                        roomBook.BookCount -= 1;
                    }
                    else ErrorMessage.Add(roomBook.Book.Name);

                }

                if (ErrorMessage.Count > 0)
                {
                    string message = $"Выдача отменена\nСледующие книги не были выданы, так как отсутствуют на складе:\n\n{string.Join("\n", ErrorMessage)}";
                    SystemSounds.Beep.Play();
                    MessageBox.Show(message);
                }
                else
                {
                    dataGrid.Items.Refresh();

                    _libraryDataContext.UserRoomBookDataContext.SaveChanges();
                    _libraryDataContext.RoomBookDataContext.SaveChanges();
                    _libraryDataContext.UserDataContext.SaveChanges();

                    MessageBox.Show("Все книги выданы успешно");
                    Close();
                }
            }
            else
            {
                SystemSounds.Beep.Play();
                MessageBox.Show("Выберите дату");
            }
        }
        else
        {
            SystemSounds.Beep.Play();
            MessageBox.Show("Выберите хотя бы одну книгу");
        }
    }
}
