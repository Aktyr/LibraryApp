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
global using System.ComponentModel;

using LibraryApp.Controllers;
using LibraryApp.Models;
using LibraryApp.WPF_CustomControls;
using LibraryApp.WPF_CustomControls.Admin_menu;
using LibraryApp.WPF_CustomControls.Show_Issues;

namespace LibraryApp;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

}