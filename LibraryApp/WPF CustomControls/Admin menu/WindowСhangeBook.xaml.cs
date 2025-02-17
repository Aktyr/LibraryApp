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
    /// Логика взаимодействия для WindowСhangeBook.xaml
    /// </summary>
    public partial class WindowСhangeBook : Window, INotifyPropertyChanged
    {
        private Book _book = new("Название", "Автор", 0, "Издатель");
        public Book Book
        {
            get => _book;
            set
            {
                _book = value;
                PropertyChanged?.Invoke(this, new(nameof(Book)));
            }
        }
        private readonly BookDataContext _context;
        public WindowСhangeBook()
        {
            InitializeComponent();
            _context = new BookDataContext();
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
