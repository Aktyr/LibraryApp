using System.ComponentModel;
using LibApp.Application.Interfaces;
using LibApp.Core.Entities;

namespace LibApp.Application.NewEntity;


internal class NewRoom : INotifyPropertyChanged
{
    private readonly IRepository _dataContext;

    public event PropertyChangedEventHandler? PropertyChanged;

    private string _name = "";

    public string Name
    {
        get => _name;
        set
        {
            _name = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanSave)));
        }
    }
    public bool CanSave => !string.IsNullOrWhiteSpace(Name);

    public NewRoom(IRepository dataContext)
    {
        _dataContext = dataContext;
    }
    public void Save()
    {
        Room room = new(Name);
        _dataContext.Add(room);
        _dataContext.Save<Room>();
        ClearForm();
    }

    private void ClearForm()
    {
        Name = "";
    }
}
