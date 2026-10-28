namespace LibApp.ApplicationTests;

/// <summary>
/// Заглушка ICurrentUserService для юнит-тестов.
/// Возвращает предсказуемые значения, чтобы команды,
/// читающие ApprovedById / ApprovedBy, работали без HttpContext.
/// </summary>
public class FakeCurrentUserService : ICurrentUserService
{
    public Id? LibraryUserId { get; set; } = new Id(Guid.NewGuid());
    public string? FullName { get; set; } = "Тестовый Библиотекарь";
    public string? Email { get; set; } = "test.librarian@test.local";
    public UserRole? Role { get; set; } = UserRole.Librarian;
}