using LibraryApp.Controllers;
using LibraryApp.Models;
using System.Collections.Specialized;
using System.ComponentModel;


namespace LibraryApp
{
    /// <summary>
    /// Логика взаимодействия для WindowNewBook.xaml
    /// </summary>
    public partial class WindowNewBook : Window, INotifyPropertyChanged
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
        public WindowNewBook() => InitializeComponent();

        public event PropertyChangedEventHandler? PropertyChanged;

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string yearString = textBox_Year.Text;

            if (!int.TryParse(yearString, out int year))
            {                
                MessageBox.Show("Неверный формат года. Пожалуйста, введите целое число.");
                return;
            }
           
            var context = new BookDataContext();
            context.Books.Add(Book);
            context.SaveChanges();
            MessageBox.Show("Книга добавлена");
            Close();
        }
    }
}
