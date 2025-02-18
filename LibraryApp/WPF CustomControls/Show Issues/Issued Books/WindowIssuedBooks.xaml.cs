using LibraryApp.Controllers;
using LibraryApp.Models;
using LibraryApp.WPF_CustomControls.Show_Issues.User_Books;
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

namespace LibraryApp.WPF_CustomControls.Show_Issues.Issued_Books
{
    /// <summary>
    /// Логика взаимодействия для WindowIssuedBooks.xaml
    /// </summary>
    public partial class WindowIssuedBooks : Window, INotifyPropertyChanged
    {
        private UserRoomBook _userRoomBook = new(null, null);
        public UserRoomBook UserRoomBook
        {
            get => _userRoomBook;
            set
            {
                _userRoomBook = value;
                PropertyChanged?.Invoke(this, new(nameof(UserRoomBook)));
            }
        }

        private readonly UserRoomBookDataContext _contextUserRoomBook;        
        public WindowIssuedBooks()
        {
            InitializeComponent();            
            _contextUserRoomBook = new();
            DataContext = _contextUserRoomBook;
        }
        public event PropertyChangedEventHandler? PropertyChanged;
        private void Button_Click(object sender, RoutedEventArgs e)
        {

            //var user = ((FrameworkElement)e.OriginalSource).DataContext as User;

            //if (user != null)
            //{
            //    WindowOpenUserBooks windowOpenUserBooks = new(user);
            //    windowOpenUserBooks.Show();
            //}

        }
    }
}
