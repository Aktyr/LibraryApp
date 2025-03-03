namespace LibraryApp.WPF_CustomControls.Book_Issuing;

public partial class WindowDateSelection : Window
{
    public DateTime Today => DateTime.Now;
    public WindowDateSelection() //доделать каленларь в в выбор даты для выдачи 
    {
        InitializeComponent();
    }

    private void Button_Click(object sender, RoutedEventArgs e)
    {

    }
}
