namespace LibApp.Core.Entities;

public class User : IEntity
{
    public User()
    {
        Id = new Id(Guid.NewGuid());
    }

    public Id Id { get; set; } = null!;
    public string LastName { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string MiddleName { get; set; } = string.Empty;
    public string ContactInfo { get; set; } = string.Empty;
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


    public ICollection<UserRoomBook> RoomBooks { get; set; } = [];
    public override string ToString() => $"{LastName} {FirstName} {MiddleName}, {ContactInfo}";
    public string FullName => $"{LastName} {FirstName} {MiddleName}";
}