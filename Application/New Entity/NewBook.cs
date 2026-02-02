using LibApp.Application.Interfaces;
using LibApp.Core.Entities;
using System.ComponentModel;

namespace LibApp.Application.NewEntity;

internal class NewBook : INotifyPropertyChanged
{
    private readonly IRepository _dataContext;

    public event PropertyChangedEventHandler? PropertyChanged;

    private string _title = "";
    private string _author = "";
    private int _year;
    private string _publisher = "";

    #region Properties
    public string Title
    {
        get => _title;
        set
        {
            _title = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Title)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanSave)));
        }
    }

    public string Author
    {
        get => _author;
        set
        {
            _author = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Author)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanSave)));
        }
    }

    public int Year
    {
        get => _year;
        set
        {
            _year = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Year)));
        }
    }

    public string Publisher
    {
        get => _publisher;
        set
        {
            _publisher = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Publisher)));
        }
    }
    public bool CanSave => !string.IsNullOrWhiteSpace(Title) &&
                           !string.IsNullOrWhiteSpace(Author);
    #endregion

    public NewBook(IRepository dataContext)
    {
        _dataContext = dataContext;
    }

    public void Save()
    {
        Book book = new(Title, Author, Year, Publisher);
        _dataContext.Add(book);
        _dataContext.Save<Book>();
        ClearForm();
    }

    private void ClearForm()
    {
        Title = "";
        Author = "";
        Year = 0;
        Publisher = "";
    }
}
