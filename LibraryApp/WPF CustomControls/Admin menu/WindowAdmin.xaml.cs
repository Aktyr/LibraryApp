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
    /// Логика взаимодействия для WindowAdmin.xaml
    /// </summary>
    public partial class WindowAdmin : Window
    {
        public WindowAdmin()
        {
            InitializeComponent();
        }

        private void Button_Click_СhangeUser(object sender, RoutedEventArgs e)
        {
            WindowСhangeUser windowСhangeUser = new();
            windowСhangeUser.ShowDialog();
        }

        private void Button_Click_СhangeBook(object sender, RoutedEventArgs e)
        {
            WindowСhangeBook windowСhangeBook = new();
            windowСhangeBook.ShowDialog();
        }

        private void Button_Click_СhangeRoom(object sender, RoutedEventArgs e)
        {
            WindowСhangeRoom windowСhangeRoom = new();
            windowСhangeRoom.ShowDialog(); 
        }
    }
}
