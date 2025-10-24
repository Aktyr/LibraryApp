namespace LibraryApp.Models;

public class RoomBook(Room room, Book book) : INotifyPropertyChanged
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Room Room { get; set; } = room;

    [JsonIgnore] private int _bookCount;
    public int BookCount
    {
        get => _bookCount;
        set
        {
            if (_bookCount != value)
            {
                _bookCount = value;
                OnPropertyChanged(nameof(BookCount));
            }
        }
    }

    public Book Book { get; set; } = book;

    public event PropertyChangedEventHandler PropertyChanged;
    protected virtual void OnPropertyChanged(string propertyName) 
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}