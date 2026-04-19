namespace LibApp.Core.Entities;

public class User : IEntity
{
    #region User Data
    public Id Id { get; set; } = null!;
    public string LastName { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string MiddleName { get; set; } = string.Empty; // Не обязательное поле
    public string ContactInfo { get; set; } = string.Empty;

    // Аутентификация 
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Reader;
    #endregion

    // todo добавить подтверждение почты IsEmailConfirmed 
    // todo добавить проверку сложности пароля 
    // todo тайм-аут после N неудачных вводов пароля? 

    public TimeSpan? NearestReturnTimeSpan
    {
        get
        {
            var nearestDeadline = RoomBooks
                .Where(x => x.Deadline.HasValue)
                .Select(x => x.Deadline)
                .OrderBy(x => x)
                .FirstOrDefault();

            return nearestDeadline.HasValue
                ? nearestDeadline.Value - DateTime.Now
                : null;
        }
    }
    [InverseProperty(nameof(UserRoomBook.User))] public virtual ICollection<UserRoomBook> RoomBooks { get; set; } = [];
    public override string ToString() => $"{LastName} {FirstName} {MiddleName}, {ContactInfo}";
    public string FullName => $"{LastName} {FirstName} {MiddleName}";
}