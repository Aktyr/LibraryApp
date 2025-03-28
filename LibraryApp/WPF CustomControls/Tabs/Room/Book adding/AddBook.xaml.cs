﻿using LibraryApp.Models;
using LibraryApp.Controllers;
using System.Media;

namespace LibraryApp.WPF_CustomControls.Tabs.Room.Book_adding;

/// <summary>
/// Логика взаимодействия для AddBook.xaml
/// </summary>
public partial class AddBook : Window, INotifyPropertyChanged
{
    private Models.Room _room = new("Имя комнаты");
    public Models.Room Room
    {
        get => _room;
        set
        {
            _room = value;
            PropertyChanged?.Invoke(this, new(nameof(Room)));
        }
    }
    private readonly LibraryDataContext _libraryDataContext;
    public AddBook(Models.Room room)
    {
        InitializeComponent();
        _libraryDataContext = LibraryDataContext.Instance;
        this.DataContext = _libraryDataContext.BookDataContext;
        Room = room;
    }
    public event PropertyChangedEventHandler? PropertyChanged;

    private void Button_Click(object sender, RoutedEventArgs e)
    {
        var selectedItems = dataGrid.SelectedItems;

        if (selectedItems.Count > 0)
        {
            List<string> ErrorMessage = new();
            foreach (var item in selectedItems)
            {
                var book = item as Book;
                RoomBook roomBook = new(Room, book);

                //проверка существует ли уже такая комната с книгой
                if (!_libraryDataContext.RoomBookDataContext.RoomBooks.Any
                    (rb =>
                     rb.Room.Id == roomBook.Room.Id &&
                     rb.Book.Id == roomBook.Book.Id))
                {
                    _libraryDataContext.RoomBookDataContext.RoomBooks.Add(roomBook);
                }
                else ErrorMessage.Add(roomBook.Book.Name);
            }
            _libraryDataContext.RoomBookDataContext.SaveChanges();
            dataGrid.Items.Refresh();

            if (ErrorMessage.Count > 0)
            {
                string message = $"Книги добавлены успешно\nНо следующие дубликаты проигнорированы:\n\n{string.Join("\n", ErrorMessage)}";
                SystemSounds.Beep.Play();
                MessageBox.Show(message);
            }
            else
            {
                MessageBox.Show("Все книги выданы успешно");
            }
                Close();

        }
        else
        {
            SystemSounds.Beep.Play();
            MessageBox.Show("Выберите хотя бы одну книгу в комнату");
        }
    }
}

