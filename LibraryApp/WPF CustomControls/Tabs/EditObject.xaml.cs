namespace LibraryApp.WPF_CustomControls.Tabs;

/// <summary>
/// Логика взаимодействия для EditObject.xaml
/// </summary>
public partial class EditObject : Window
{
    public bool saveChanges;
    public EditObject(object editObject)
    {
        InitializeComponent();
        SetData(editObject);
        saveChanges = false;
    }
    private void SetData(object data)
    {
        if (data != null)
        {
            var collection = new List<object> { data };
            dataGrid.ItemsSource = collection;
        }
    }


    private void Button_Click(object sender, RoutedEventArgs e)
    {
        saveChanges = true; // изменения сохраняются и без кнопки....
        Close();
    }
}
