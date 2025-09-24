using LibraryApp.Models;

namespace LibraryApp.Controllers;

public class FileInteractor<T>
{
    protected virtual string Path => GetDefaultPath();

    private string GetDefaultPath()
    {
        // Автоматически генерируем путь на основе имени типа
        return $"{typeof(T).Name.ToLower()}s.json";
    }

    public FileInteractor()
    {
        // Проверяем существование файла
        if (!File.Exists(Path))
        {
            // Создаем новый файл с пустым массивом
            File.WriteAllText(Path, "[]");
        }

        var json = File.ReadAllText(Path);
        Data = JArray.Parse(json).ToObject<List<T>>() ?? [];
    }
    public List<T> Data { get; init; }
    public void SaveChanges()
    {
        // Используем настройки сериализатора для обработки циклических ссылок
        var serializerSettings = new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            Formatting = Formatting.Indented
        };

        var jsonData = JsonConvert.SerializeObject(Data, Formatting.Indented, serializerSettings);
        File.WriteAllText(Path, jsonData.ToString()); // возможно надо будет убрать .ToString()
    }
}

public static class InteractorFactory
{
    private static readonly Dictionary<Type, object> _instances = new Dictionary<Type, object>();

    public static FileInteractor<T> Create<T>() where T : class
    {
        var type = typeof(T);

        try
        {
            // Проверяем кэш
            if (_instances.TryGetValue(type, out var instance) && instance is FileInteractor<T> cachedInstance)
                return cachedInstance;

            // Создаем универсальный тип
            var interactorType = typeof(FileInteractor<>).MakeGenericType(type);

            // Создаем экземпляр
            var newInstance = Activator.CreateInstance(interactorType)
                ?? throw new InvalidOperationException($"Activator.CreateInstance returned null for {interactorType.Name}");

            // Приводим тип
            if (newInstance is not FileInteractor<T> typedInstance)
                throw new InvalidCastException($"Cannot cast {newInstance.GetType().Name} to FileInteractor<{type.Name}>");

            _instances[type] = typedInstance;
            return typedInstance;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to create FileInteractor for type {type.Name}", ex);
        }
    }
}

/* ИСПОЛЬЗОВАНИЕ

var bookInteractor = InteractorFactory.Create<Book>();
var roomInteractor = InteractorFactory.Create<Room>();
var userInteractor = InteractorFactory.Create<User>();

// Все интеракторы кэшируются - повторные вызовы возвращают тот же объект
var sameBookInteractor = InteractorFactory.Create<Book>();
Console.WriteLine(ReferenceEquals(bookInteractor, sameBookInteractor)); // True
 
 */