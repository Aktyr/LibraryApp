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

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            //Book book = new Book(); 
        }
    }
}