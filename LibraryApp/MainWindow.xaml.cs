global using System.Text;
global using System.Windows;
global using System.Windows.Controls;
global using System.Windows.Data;
global using System.Windows.Documents;
global using System.Windows.Input;
global using System.Windows.Media;
global using System.Windows.Media.Imaging;
global using System.Windows.Navigation;
global using System.Windows.Shapes;
global using Newtonsoft.Json.Linq;
global using Newtonsoft.Json;
global using System.Collections.Generic;
global using System.IO;
global using System.Threading.Tasks;
global using System.Xml.Linq;
global using System.Linq;

using LibraryApp.Controllers;
using LibraryApp.Models;

namespace LibraryApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Button_Click_NewUser(object sender, RoutedEventArgs e)
        {
            WindowNewUser windowNewUser = new();
            windowNewUser.ShowDialog();
        }

        private void Button_Click_NewBook(object sender, RoutedEventArgs e)
        {
            WindowNewBook windowNewBook = new();
            windowNewBook.ShowDialog();
        }

        private void Button_Click_NewRoom(object sender, RoutedEventArgs e)
        {
            WindowNewRoom windowNewRoom = new();
            windowNewRoom.ShowDialog();
        }
    }
}