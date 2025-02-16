using LibraryApp.Controllers;
using LibraryApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace LibraryApp.WPF_CustomControls
{
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
        //private readonly RoomBookDataContext _contextRoomBook;
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
            }
            Close();
        }


    }
}
