using LibApp.Application.Controllers;
using LibApp.Application.Events;
using LibApp.Application.Interfaces;
using LibApp.Core.Entities;
using LibApp.Core.Records;
using System.ComponentModel;

namespace LibApp.Application.NewEntity;

internal class NewRoomBook : INotifyPropertyChanged
{
    private readonly IRepository _dataContext;
    private List<Room>? _cachedRooms; 
    private List<Book>? _cachedBooks; 

    private Id _selectedRoomId = new Id(Guid.Empty);
    private Id _selectedBookId = new Id(Guid.Empty);

    public event PropertyChangedEventHandler? PropertyChanged;

    #region Properties
    public Id SelectedRoomId
    {
        get => _selectedRoomId;
        set
        {
            _selectedRoomId = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedRoomId)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedRoom)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanSave)));
        }
    }
    public Id SelectedBookId
    {
        get => _selectedBookId;
        set
        {
            _selectedBookId = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedBookId)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedBook)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanSave)));
        }
    }

    public List<Room> AvailableRooms
    {
        get
        {
            _cachedRooms ??= _dataContext.GetAll<Room>();
            return _cachedRooms;
        }
    }
    public List<Book> AvailableBooks
    {
        get
        {
            _cachedBooks ??= _dataContext.GetAll<Book>();
            return _cachedBooks;
        }
    }

    public Room? SelectedRoom => AvailableRooms.FirstOrDefault(r => r.Id == SelectedRoomId);
    public Book? SelectedBook => AvailableBooks.FirstOrDefault(b => b.Id == SelectedBookId);

    public bool CanSave => SelectedRoomId != null &&
                           SelectedBookId != null &&
                           SelectedRoomId != new Id(Guid.Empty) &&
                           SelectedBookId != new Id(Guid.Empty);
    #endregion

    public NewRoomBook(IRepository dataContext)
    {
        _dataContext = dataContext;
        _dataContext.DataChanged += OnDataChanged;
    }
    public void Save()
    {
        if (!CanSave) return;

        var room = AvailableRooms.First(r => r.Id == SelectedRoomId);
        var book = AvailableBooks.First(b => b.Id == SelectedBookId);

        var roomBook = new RoomBook(room, book);
        _dataContext.Add(roomBook);
        _dataContext.Save<RoomBook>();
        ClearForm();
    }
    private void ClearForm()
    {
        SelectedRoomId = new Id(Guid.Empty);
        SelectedBookId = new Id(Guid.Empty);
    }

    public void RefreshData()
    {
        _cachedRooms = null;
        _cachedBooks = null;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AvailableRooms)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AvailableBooks)));
    }
    private void OnDataChanged(object? sender, DataChangedEventArgs e)
    {
        // Обновляем кэш если изменились Room или Book
        if (e.EntityType == typeof(Room))
        {
            _cachedRooms = null;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AvailableRooms)));
        }

        if (e.EntityType == typeof(Book))
        {
            _cachedBooks = null;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AvailableBooks)));
        }
    }
    public void Dispose() => _dataContext.DataChanged -= OnDataChanged;
}
