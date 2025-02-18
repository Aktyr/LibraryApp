using LibraryApp.WPF_CustomControls.Show_Issues.Issued_Books;
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

namespace LibraryApp.WPF_CustomControls.Show_Issues
{
    /// <summary>
    /// Логика взаимодействия для WindowShowIssues.xaml
    /// </summary>
    public partial class WindowShowIssues : Window
    {
        public WindowShowIssues()
        {
            InitializeComponent();
        }

        private void Button_Click_IssedBooks(object sender, RoutedEventArgs e)
        {
            WindowIssuedBooks windowIssuedBooks = new();
            windowIssuedBooks.Show();
        }

        private void Button_Click_Users(object sender, RoutedEventArgs e)
        {
            WindowUserBooks windowUserBooks = new();
            windowUserBooks.Show();
        }
    }
}
