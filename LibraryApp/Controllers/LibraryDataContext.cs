using LibraryApp.Models;
using System;
using System.Collections;
using System.Collections.Generic;

namespace LibraryApp.Controllers;

public class LibraryDataContext
{
    private static readonly Lazy<LibraryDataContext> lazy = new(() => new LibraryDataContext());

    public static LibraryDataContext Instance => lazy.Value;

    private LibraryDataContext()
    {
        #region Легаси
        // Старые DataContext'ы
        //this.UserDataContext = new UserDataContext();
        //this.RoomDataContext = new RoomDataContext();
        //this.BookDataContext = new BookDataContext();
        //this.RoomBookDataContext = new RoomBookDataContext();
        //this.UserRoomBookDataContext = new UserRoomBookDataContext();
        #endregion

        // Новые универсальные интеракторы
        this.BookInteractor = InteractorFactory.Create<Book>();
        this.RoomInteractor = InteractorFactory.Create<Room>();
        this.UserInteractor = InteractorFactory.Create<User>();
        this.RoomBookInteractor = InteractorFactory.Create<RoomBook>();
        this.UserRoomBookInteractor = InteractorFactory.Create<UserRoomBook>();
    }
    #region Легаси
    //// Старые DataContext'ы - УСТАРЕЛИ
    //[Obsolete("Используйте BookInteractor вместо BookDataContext")]
    //public BookDataContext BookDataContext { get; set; }

    //[Obsolete("Используйте RoomInteractor вместо RoomDataContext")]
    //public RoomDataContext RoomDataContext { get; set; }

    //[Obsolete("Используйте UserInteractor вместо UserDataContext")]
    //public UserDataContext UserDataContext { get; set; }

    //[Obsolete("Используйте RoomBookInteractor вместо RoomBookDataContext")]
    //public RoomBookDataContext RoomBookDataContext { get; set; }

    //[Obsolete("Используйте UserRoomBookInteractor вместо UserRoomBookDataContext")]
    //public UserRoomBookDataContext UserRoomBookDataContext { get; set; }
    #endregion
    // Новые универсальные интеракторы
    public FileInteractor<Book> BookInteractor { get; set; }
    public FileInteractor<Room> RoomInteractor { get; set; }
    public FileInteractor<User> UserInteractor { get; set; }
    public FileInteractor<RoomBook> RoomBookInteractor { get; set; }
    public FileInteractor<UserRoomBook> UserRoomBookInteractor { get; set; }

    // Упрощенные свойства для доступа к данным (новые)
    public List<Book> BookList => BookInteractor.Data;
    public List<Room> RoomList => RoomInteractor.Data;
    public List<User> UserList => UserInteractor.Data;
    public List<RoomBook> RoomBookList => RoomBookInteractor.Data;
    public List<UserRoomBook> UserRoomBookList => UserRoomBookInteractor.Data;

    // Универсальный словарь для динамического доступа
    private Dictionary<Type, object> Interactors => new()
    {
        [typeof(Book)] = BookInteractor,
        [typeof(Room)] = RoomInteractor,
        [typeof(User)] = UserInteractor,
        [typeof(RoomBook)] = RoomBookInteractor,
        [typeof(UserRoomBook)] = UserRoomBookInteractor
    };
    #region Легаси
    // Старый метод Add - УСТАРЕЛ
    [Obsolete("Используйте новый универсальный метод Add<T>(T entity)")]
    public void Add(object entity)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));

        // Перенаправляем на новый метод через рефлексию
        var method = typeof(LibraryDataContext).GetMethod(nameof(AddGeneric))
                    ?? throw new InvalidOperationException("AddGeneric method not found");
        var genericMethod = method.MakeGenericMethod(entity.GetType());
        genericMethod.Invoke(this, new[] { entity });
    }
    #endregion
    // Новый универсальный метод Add (без dynamic)
    public void AddGeneric<T>(T entity) where T : class
    {
        if (Interactors.TryGetValue(typeof(T), out var interactor))
        {
            // Получаем свойство Data через рефлексию
            var dataProperty = interactor.GetType().GetProperty(nameof(FileInteractor<Book>.Data));
            if (dataProperty == null) return;

            // Получаем значение свойства (список данных)
            var data = dataProperty.GetValue(interactor);
            if (data is IList list)
            {
                list.Add(entity);

                // Вызываем SaveChanges через рефлексию
                var saveMethod = interactor.GetType().GetMethod(nameof(FileInteractor<Book>.SaveChanges));
                saveMethod?.Invoke(interactor, null);
            }
        }
        else
        {
            throw new ArgumentException($"No interactor registered for type {typeof(T).Name}");
        }
    }

    // Упрощенный метод Add (без dynamic - используем явное приведение)
    public void Add<T>(T entity) where T : class
    {
        if (Interactors.TryGetValue(typeof(T), out var interactor))
        {
            // Используем switch для явного приведения типов
            switch (interactor)
            {
                case FileInteractor<Book> bookInteractor when entity is Book book:
                    bookInteractor.Data.Add(book);
                    break;
                case FileInteractor<Room> roomInteractor when entity is Room room:
                    roomInteractor.Data.Add(room);
                    break;
                case FileInteractor<User> userInteractor when entity is User user:
                    userInteractor.Data.Add(user);
                    break;
                case FileInteractor<RoomBook> roomBookInteractor when entity is RoomBook roomBook:
                    roomBookInteractor.Data.Add(roomBook);
                    break;
                case FileInteractor<UserRoomBook> userRoomBookInteractor when entity is UserRoomBook userRoomBook:
                    userRoomBookInteractor.Data.Add(userRoomBook);
                    break;
                default:
                    throw new InvalidOperationException($"Type mismatch for {typeof(T).Name}");
            }
        }
        else
        {
            throw new ArgumentException($"No interactor registered for type {typeof(T).Name}");
        }
    }

    public void Remove<T>(T entity) where T : class
    {
        if (Interactors.TryGetValue(typeof(T), out var interactor))
        {
            switch (interactor)
            {
                case FileInteractor<Book> bookInteractor when entity is Book book:
                    bookInteractor.Data.Remove(book);
                    break;
                case FileInteractor<Room> roomInteractor when entity is Room room:
                    roomInteractor.Data.Remove(room);
                    break;
                case FileInteractor<User> userInteractor when entity is User user:
                    userInteractor.Data.Remove(user);
                    break;
                case FileInteractor<RoomBook> roomBookInteractor when entity is RoomBook roomBook:
                    roomBookInteractor.Data.Remove(roomBook);
                    break;
                case FileInteractor<UserRoomBook> userRoomBookInteractor when entity is UserRoomBook userRoomBook:
                    userRoomBookInteractor.Data.Remove(userRoomBook);
                    break;
                default:
                    throw new InvalidOperationException($"Type mismatch for {typeof(T).Name}");
            }
        }
        else
        {
            throw new ArgumentException($"No interactor registered for type {typeof(T).Name}");
        }
    }

    // Универсальный метод для получения всех данных
    public List<T> GetAll<T>() where T : class
    {
        if (Interactors.TryGetValue(typeof(T), out var interactor))
        {
            return interactor switch
            {
                FileInteractor<Book> bookInteractor when typeof(T) == typeof(Book)
                    => (List<T>)(object)bookInteractor.Data,
                FileInteractor<Room> roomInteractor when typeof(T) == typeof(Room)
                    => (List<T>)(object)roomInteractor.Data,
                FileInteractor<User> userInteractor when typeof(T) == typeof(User)
                    => (List<T>)(object)userInteractor.Data,
                FileInteractor<RoomBook> roomBookInteractor when typeof(T) == typeof(RoomBook)
                    => (List<T>)(object)roomBookInteractor.Data,
                FileInteractor<UserRoomBook> userRoomBookInteractor when typeof(T) == typeof(UserRoomBook)
                    => (List<T>)(object)userRoomBookInteractor.Data,
                _ => throw new InvalidOperationException($"Type mismatch for {typeof(T).Name}")
            };
        }

        throw new ArgumentException($"No interactor registered for type {typeof(T).Name}");
    }

    // Универсальный метод Save для конкретного типа
    public void Save<T>() where T : class
    {
        if (Interactors.TryGetValue(typeof(T), out var interactor))
        {
            switch (interactor)
            {
                case FileInteractor<Book> bookInteractor when typeof(T) == typeof(Book):
                    bookInteractor.SaveChanges();
                    break;
                case FileInteractor<Room> roomInteractor when typeof(T) == typeof(Room):
                    roomInteractor.SaveChanges();
                    break;
                case FileInteractor<User> userInteractor when typeof(T) == typeof(User):
                    userInteractor.SaveChanges();
                    break;
                case FileInteractor<RoomBook> roomBookInteractor when typeof(T) == typeof(RoomBook):
                    roomBookInteractor.SaveChanges();
                    break;
                case FileInteractor<UserRoomBook> userRoomBookInteractor when typeof(T) == typeof(UserRoomBook):
                    userRoomBookInteractor.SaveChanges();
                    break;
                default:
                    throw new InvalidOperationException($"Type mismatch for {typeof(T).Name}");
            }
        }
        else
        {
            throw new ArgumentException($"No interactor registered for type {typeof(T).Name}");
        }
    }

    // Сохранение всех данных
    public void SaveAll()
    {
        BookInteractor.SaveChanges();
        RoomInteractor.SaveChanges();
        UserInteractor.SaveChanges();
        RoomBookInteractor.SaveChanges();
        UserRoomBookInteractor.SaveChanges();
    }
}