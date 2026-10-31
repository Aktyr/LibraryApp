using LibApp.Core.DTO.Borrowing;
using LibApp.Core.DTO.Entities;

namespace BlazorWeb.Client.Shared.UserBorrows;

/// <summary>
/// Extension-методы для группировки, фильтрации и агрегатов по списку выдач.
/// Одинаковая семантика для MyProfile и UserProfile.
/// </summary>
public static class UserBorrowsQueries
{
    public static List<BorrowedBookDTO> ActiveBorrows(this IEnumerable<BorrowedBookDTO> source)
        => source.Where(b => b.ReturnDate == null)
                 .OrderBy(b => b.Deadline ?? DateTime.MaxValue)
                 .ToList();

    public static List<BorrowedBookDTO> ReturnedBorrows(this IEnumerable<BorrowedBookDTO> source)
        => source.Where(b => b.ReturnDate != null)
                 .OrderByDescending(b => b.ReturnDate)
                 .ToList();

    public static int OverdueCount(this IEnumerable<BorrowedBookDTO> source)
        => source.Count(b => b.ReturnDate == null && b.Penalty > 0);

    public static decimal TotalPenalty(this IEnumerable<BorrowedBookDTO> source)
        => source.Where(b => b.Penalty.HasValue).Sum(b => b.Penalty!.Value);

    public static string FullName(this UserDTO? user)
        => user == null ? "—" : $"{user.LastName} {user.FirstName} {user.MiddleName}".Trim();
}