namespace LibApp.Core.Policies;

public static class AuditActionStyle
{
    public static (string Background, string Foreground) Get(AuditActionType type) => type switch
    {
        AuditActionType.Create => ("#e6f7eb", "#0a7f2e"),
        AuditActionType.Update => ("#e6f0fb", "#003a6b"),
        AuditActionType.Delete => ("#fde7e9", "#a30000"),
        AuditActionType.Restore => ("#e6f7eb", "#0a7f2e"),
        AuditActionType.Borrow => ("#fff3d9", "#8a4b00"),
        AuditActionType.Return => ("#e6f7eb", "#0a7f2e"),
        AuditActionType.Register => ("#e6f0fb", "#003a6b"),
        AuditActionType.Discard => ("#fde7e9", "#a30000"),
        AuditActionType.UndoDiscard => ("#fff3d9", "#8a4b00"),
        AuditActionType.Anonymize => ("#3d0000", "#fff"),
        AuditActionType.ApproveBorrowRequest => ("#e6f7eb", "#0a7f2e"),
        AuditActionType.RejectBorrowRequest => ("#fde7e9", "#a30000"),
        AuditActionType.PayPenalty => ("#e6f7eb", "#0a7f2e"),
        AuditActionType.LoginFailed => ("#fde7e9", "#a30000"),
        _ => ("#f3f3f3", "#555")
    };
}