namespace LibraryApp.Controllers;

public class LibraryDataContext
{
    private static readonly Lazy<LibraryDataContext> lazy =
     new(() => new LibraryDataContext());

    public static LibraryDataContext Instance => lazy.Value;

    private LibraryDataContext()
    {
        this.UserDataContext = new UserDataContext();
        this.RoomDataContext = new RoomDataContext();
        this.BookDataContext = new BookDataContext();
        this.RoomBookDataContext = new RoomBookDataContext();
        this.UserRoomBookDataContext = new UserRoomBookDataContext();        
    }

    public BookDataContext BookDataContext { get; set; }
    public RoomDataContext RoomDataContext { get; set; }
    public UserDataContext UserDataContext { get; set; }
    public RoomBookDataContext RoomBookDataContext { get; set; }
    public UserRoomBookDataContext UserRoomBookDataContext { get; set; }

}
