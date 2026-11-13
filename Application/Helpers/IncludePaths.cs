namespace LibApp.Application.Helpers;

public static class IncludePaths
{
    public static class Book
    {
        public const string RoomBook = nameof(Core.Entities.Book.RoomBook);
        public const string RoomBookRoom = nameof(Core.Entities.Book.RoomBook) + "." + nameof(Core.Entities.RoomBook.Room);
        public const string RoomBookBook = nameof(Core.Entities.Book.RoomBook) + "." + nameof(Core.Entities.RoomBook.Book);
        public const string Images = nameof(Core.Entities.Book.Images);
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
    public static class User
    {
        public const string RoomBooks = nameof(Core.Entities.User.RoomBooks);
    }
    public static class UserRoomBook
    {
        public const string RoomBookBook = nameof(Core.Entities.UserRoomBook.RoomBook) + "." + nameof(Core.Entities.RoomBook.Book);
        public const string RoomBookRoom = nameof(Core.Entities.UserRoomBook.RoomBook) + "." + nameof(Core.Entities.RoomBook.Room);
        public const string PenaltyPayments = nameof(Core.Entities.UserRoomBook.PenaltyPayments);
        public const string User = nameof(Core.Entities.UserRoomBook.User);
    }
    public static class BorrowRequest
    {
        public const string User = nameof(Core.Entities.BorrowRequest.User);
        public const string RoomBookBook = nameof(Core.Entities.BorrowRequest.RoomBook) + "." + nameof(Core.Entities.RoomBook.Book);
        public const string RoomBookRoom = nameof(Core.Entities.BorrowRequest.RoomBook) + "." + nameof(Core.Entities.RoomBook.Room);
        public const string ProcessedByUser = nameof(Core.Entities.BorrowRequest.ProcessedByUser);
    }
}