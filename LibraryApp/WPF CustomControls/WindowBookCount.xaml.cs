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
    /// Логика взаимодействия для WindowBookCount.xaml
    /// </summary>
    public partial class WindowBookCount : Window, INotifyPropertyChanged
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
        private readonly RoomBookDataContext _contextRoomBook;
        public WindowBookCount()
        {
            InitializeComponent();
            _contextRoomBook = new RoomBookDataContext();
            this.DataContext = _contextRoomBook;
        }
        public event PropertyChangedEventHandler? PropertyChanged;
        private void AddBookButton_Click(object sender, RoutedEventArgs e)
        {            
            var row = ((FrameworkElement)e.OriginalSource).DataContext as RoomBook;

            if (row != null)
            {
                WindowBookLogic windowBookLogic = new(row);
                windowBookLogic.ShowDialog();
                dataGrid.Items.Refresh();
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            _contextRoomBook.SaveChanges();

            Close();
        }
    }
}
