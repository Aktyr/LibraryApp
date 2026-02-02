using LibApp.Application.Interfaces;
using LibApp.Core.Entities;
using System.ComponentModel;

namespace LibApp.Application.NewEntity;

internal class NewUser : INotifyPropertyChanged
{
    private readonly IRepository _dataContext;

    public event PropertyChangedEventHandler? PropertyChanged;

    private string _lastName = "";
    private string _firstName = "";
    private string _middleName = "";
    private string _contactInfo = "";

    #region Properties
    public string LastName
    {
        get => _lastName;
        set
        {
            _lastName = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LastName)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanSave)));
        }
    }
    public string FirstName
    {
        get => _firstName;
        set
        {
            _lastName = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FirstName)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanSave)));
        }
    }
    public string MiddleName
    {
        get => _middleName;
        set
        {
            _lastName = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MiddleName)));
        }
    }
    public string ContactInfo
    {
        get => _contactInfo;
        set
        {
            _lastName = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ContactInfo)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanSave)));
        }
    }
    public bool CanSave => !string.IsNullOrWhiteSpace(LastName) &&
                           !string.IsNullOrWhiteSpace(FirstName) &&
                           !string.IsNullOrWhiteSpace(ContactInfo);
    #endregion


    public NewUser(IRepository dataContext)
    {
        _dataContext = dataContext;
    }

    public void Save()
    {
        User user = new(LastName, FirstName, MiddleName, ContactInfo);
        _dataContext.Add(user);
        _dataContext.Save<User>();
        ClearForm();
    }

    private void ClearForm()
    {
        LastName = "";
        FirstName = "";
        MiddleName = "";
        ContactInfo = "";
    }
}
