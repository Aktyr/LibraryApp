namespace BlazorWeb.Data;

// Add profile data for application users by adding properties to the ApplicationUser class
public class ApplicationUser : IdentityUser
{
    // FK → library."User"."Id". Nullable на старте — заполняется при регистрации.
    // Позже сделаем NOT NULL отдельной миграцией, когда появится транзакция между contexts.
    public Guid? LibraryUserId { get; set; }

    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string MiddleName { get; set; } = string.Empty;
    public string ContactInfo { get; set; } = string.Empty;
}