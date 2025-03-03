using LibraryApp.Controllers;
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

namespace LibraryApp.WPF_CustomControls.Tabs
{
    /// <summary>
    /// Логика взаимодействия для EditObject.xaml
    /// </summary>
    public partial class EditObject : Window
    {
        public bool Confirm;
        public EditObject(object editObject)
        {
            InitializeComponent();
            Confirm = false;
            this.DataContext = editObject;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
