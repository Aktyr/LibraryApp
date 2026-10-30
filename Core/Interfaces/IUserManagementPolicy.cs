namespace LibApp.Core.Interfaces;

/// <summary>
/// Единые правила «кто кого может создавать / редактировать / удалять».
/// Используется в WebAPI (гибридные эндпоинты), BFF-прокси и UI —
/// чтобы авторизация не разъезжалась между слоями.
/// </summary>
public interface IUserManagementPolicy
{
    bool CanCreate(UserRole actor, UserRole targetRole);
    bool CanEdit(UserRole actor, UserRole targetRole);
    bool CanDelete(UserRole actor);
}