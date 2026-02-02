using LibApp.Application.Controllers;
using LibApp.Application.Events;
using LibApp.Application.Interfaces;
using LibApp.Core.Entities;
using LibApp.Core.Records;
using System.ComponentModel;

namespace LibApp.Application.NewEntity;

internal class NewUserRoomBook : INotifyPropertyChanged
{
    private readonly IRepository _dataContext;
    private List<User>? _cachedUsers;
    private List<RoomBook>? _cachedRoomBooks;
    private DateTime? _dateTime;

    private Id _selectedUserId = new Id(Guid.Empty);
    private Id _selectedRoomBookId = new Id(Guid.Empty);

    public event PropertyChangedEventHandler? PropertyChanged;

    #region Properties
    public Id SelectedUserId
    {
        get => _selectedUserId;
        set
        {
            _selectedUserId = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedUserId)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedUser)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanSave)));
        }
    }
    public Id SelectedRoomBookId
    {
        get => _selectedRoomBookId;
        set
        {
            _selectedRoomBookId = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedRoomBookId)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedRoomBook)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanSave)));
        }
    }
    public DateTime? SelectedDateTime
    {
        get => _dateTime;
        set
        {
            _dateTime = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedDateTime)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanSave)));
        }
    }

    public List<User> AvailableUsers
    {
        get
        {
            _cachedUsers ??= _dataContext.GetAll<User>();
            return _cachedUsers;
        }
    }
    public List<RoomBook> AvailableRoomBooks
    {
        get
        {
            _cachedRoomBooks ??= _dataContext.GetAll<RoomBook>();
            return _cachedRoomBooks;
        }
    }

    public User? SelectedUser => AvailableUsers.FirstOrDefault(r => r.Id == SelectedUserId);
    public RoomBook? SelectedRoomBook => AvailableRoomBooks.FirstOrDefault(rb => rb.Id == SelectedRoomBookId);

    public bool CanSave => SelectedUserId != null &&
                           SelectedRoomBookId != null &&
                           SelectedDateTime != null &&
                           SelectedUserId != new Id(Guid.Empty) &&
                           SelectedRoomBookId != new Id(Guid.Empty);
    #endregion

    public NewUserRoomBook(IRepository dataContext)
    {
        _dataContext = dataContext;
        _dataContext.DataChanged += OnDataChanged;
    }
    private void OnDataChanged(object? sender, DataChangedEventArgs e)
    {
        // Обновляем кэш если изменились Room или Book
        if (e.EntityType == typeof(Room))
        {
            _cachedUsers = null;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AvailableUsers)));
        }

        if (e.EntityType == typeof(Book))
        {
            _cachedRoomBooks = null;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AvailableRoomBooks)));
        }
    }

    public void Save()
    {
        if (!CanSave) return;

        var user = AvailableUsers.First(r => r.Id == SelectedUserId);
        var roomBook = AvailableRoomBooks.First(b => b.Id == SelectedRoomBookId);

        var userRoomBook = new UserRoomBook(user, roomBook, SelectedDateTime);
        _dataContext.Add(roomBook);
        _dataContext.Save<RoomBook>();
        ClearForm();
    }

    private void ClearForm()
    {
        SelectedUserId = new Id(Guid.Empty);
        SelectedRoomBookId = new Id(Guid.Empty);
    }
    public void RefreshData()
    {
        _cachedUsers = null;
        _cachedRoomBooks = null;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AvailableUsers)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AvailableRoomBooks)));
    }
    public void Dispose() => _dataContext.DataChanged -= OnDataChanged;
}
