using LibraryApp.Models;
using System.Media;

namespace LibraryApp.WPF_CustomControls;

/// <summary>
/// Логика взаимодействия для WindowBookLogic.xaml
/// </summary>
public partial class WindowBookLogic : Window, INotifyPropertyChanged
{
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
    public WindowBookLogic(RoomBook roomBook)
    {
        InitializeComponent();
        RoomBook = roomBook;
    }
    public event PropertyChangedEventHandler? PropertyChanged;

    private void Button_AddBook(object sender, RoutedEventArgs e)
    {
        var countBookString = TextBox_Input.Text;
        int сountBook;
        if (int.TryParse(countBookString, out сountBook))
        {
            RoomBook.BookCount += сountBook;
            Close();
        }
        else
        {
            SystemSounds.Beep.Play();
            MessageBox.Show("Введите целое число",
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }


}
