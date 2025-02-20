using LibraryApp.Controllers;
using LibraryApp.Models;
using System.Collections.ObjectModel;
using System.Windows.Controls;

namespace LibraryApp.WPF_CustomControls
{
    /// <summary>
    /// Логика взаимодействия для WindowUserСhoice.xaml
    /// </summary>
    public partial class WindowUserСhoice : Window, INotifyPropertyChanged
    {
        private User _user = new("Фамилия", "Имя", "Отчество", "Контактная информация");
        public User User
        {
            get => _user;
            set
            {
                _user = value;
                PropertyChanged?.Invoke(this, new(nameof(User)));
            }
        }

        public readonly UserDataContext _context;

        public WindowUserСhoice()
        {
            InitializeComponent();
            _context = new UserDataContext();
            this.DataContext = _context;
        }
        public event PropertyChangedEventHandler? PropertyChanged;

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var selectedItems = dataGrid.SelectedItems;

            if (selectedItems.Count == 1)
            {
                var user = (User)selectedItems[0];

                WindowBookIssuing windowBookIssuing = new(user, _context);
                windowBookIssuing.ShowDialog();                
                Close();
            }
            else if (selectedItems.Count >= 1) MessageBox.Show("Выберите только одного пользователя");
            else MessageBox.Show("Выберите пользователя");
        }
    }
}
