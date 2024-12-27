using LibraryApp.Controllers;
using LibraryApp.Models;


namespace LibraryApp
{
    /// <summary>
    /// Логика взаимодействия для WindowNewBook.xaml
    /// </summary>
    public partial class WindowNewBook : Window
    {
        public WindowNewBook() => InitializeComponent();

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string name = textBox_Name.Text;
            string autor = textBox_Autor.Text;
            string yearString = textBox_Year.Text;
            string publisher = textBox_Publisher.Text;

            if (!int.TryParse(yearString, out int year))
            {                
                MessageBox.Show("Неверный формат года. Пожалуйста, введите целое число.");
                return;
            }

            Book book = new(name, autor, year, publisher);
            var context = new BookDataContext();
            context.Books.Add(book);
            context.SaveChanges();

            MessageBox.Show("Книга добавлена");

            Close();
        }
    }
}
