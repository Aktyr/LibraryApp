namespace LibApp.Application.Helpers;

public static class IncludePaths
{
    public static class Book
    {
        public const string RoomBook = nameof(Core.Entities.Book.RoomBook);
        public const string RoomBookRoom = nameof(Core.Entities.Book.RoomBook) + "." + nameof(Core.Entities.RoomBook.Room);
    }

    public static class Room
    {
        public const string RoomBooks = nameof(Core.Entities.Room.RoomBooks);
        public const string RoomBooksBook = nameof(Core.Entities.Room.RoomBooks) + "." + nameof(Core.Entities.RoomBook.Book);
    }
    public static class RoomBook
    {
        public const string Book = nameof(Core.Entities.RoomBook.Book);
        public const string Room = nameof(Core.Entities.RoomBook.Room);
    }
}