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

namespace LibraryApp.WPF_CustomControls.Admin_menu
{
    /// <summary>
    /// Логика взаимодействия для WindowСhangeRoom.xaml
    /// </summary>
    public partial class WindowСhangeRoom : Window, INotifyPropertyChanged
    {

        private Room _room = new("Имя комнаты");
        public Room Room
        {
            get => _room;
            set
            {
                _room = value;
                PropertyChanged?.Invoke(this, new(nameof(Room)));
            }
        }
        private readonly RoomDataContext _context;

        public WindowСhangeRoom()
        {
            InitializeComponent();
            _context = new();
            DataContext = _context;
        }
        public event PropertyChangedEventHandler? PropertyChanged;
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            _context.SaveChanges();
            Close();
        }
    }
}
