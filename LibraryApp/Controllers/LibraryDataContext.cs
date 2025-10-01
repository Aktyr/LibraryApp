using LibraryApp.Models;
using System;
using System.Collections;
using System.Collections.Generic;

namespace LibraryApp.Controllers;


public class LibraryDataContext : IDataContext
{
    private static readonly Lazy<LibraryDataContext> _instance = new(() => new LibraryDataContext());
    public static LibraryDataContext Instance => _instance.Value;

    private readonly Dictionary<Type, object> _interactors;
    private LibraryDataContext()
    {
        _interactors = new Dictionary<Type, object>();
        RegisterSupportedTypes();
    }

    private void RegisterSupportedTypes()
    {
        Register<Book>();
        Register<Room>();
        Register<User>();
        Register<RoomBook>();
        Register<UserRoomBook>();
    }

    // Упрощённые свойства доступа
    public List<Book> BookList => GetAll<Book>();
    public List<Room> RoomList => GetAll<Room>();
    public List<User> UserList => GetAll<User>();
    public List<RoomBook> RoomBookList => GetAll<RoomBook>();
    public List<UserRoomBook> UserRoomBookList => GetAll<UserRoomBook>();
       



    private void Register<T>() where T : class => _interactors[typeof(T)] = InteractorFactory.Create<T>();
    public List<T> GetAll<T>() where T : class => GetInteractor<T>().Data; // Сделать приватным?
    public void Add<T>(T entity) where T : class => GetInteractor<T>().Data.Add(entity);
    public bool Remove<T>(T entity) where T : class => GetInteractor<T>().Data.Remove(entity);
    public void Save<T>() where T : class => GetInteractor<T>().SaveChanges();

    public void SaveAll()
    {
        foreach (var interactor in _interactors.Values.OfType<dynamic>())
            interactor.SaveChanges();
    }

    private FileInteractor<T> GetInteractor<T>() where T : class
    {
        return _interactors.TryGetValue(typeof(T), out var interactor)
            ? interactor as FileInteractor<T> ?? throw new InvalidCastException($"Invalid interactor for {typeof(T).Name}")
            : throw new ArgumentException($"No interactor registered for {typeof(T).Name}");
    }
}
public interface IDataContext
{
    void Add<T>(T entity) where T : class;
    bool Remove<T>(T entity) where T : class;
    List<T> GetAll<T>() where T : class;
    void Save<T>() where T : class;
    void SaveAll();
}
