namespace LibApp.Core.Enums;

/// <summary>
/// Тип действия, фиксируемого в журнале аудита.
/// </summary>
public enum AuditActionType
{
    Create,
    Update,
    Delete,
    Restore,
    Login,
    Logout,
    Borrow,
    Return,
    Register,
    Discard,
    UndoDiscard,
    Anonymize,
    ApproveBorrowRequest,
    RejectBorrowRequest
}