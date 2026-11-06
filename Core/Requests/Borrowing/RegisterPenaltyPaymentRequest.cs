namespace LibApp.Core.Requests.Borrowing;

/// <summary>Оплата штрафа кассиром. Сумма не передаётся — всегда равна штрафу по выдаче.</summary>
public class RegisterPenaltyPaymentRequest : IAddOrUpdateRequest
{
    public Guid UserRoomBookId { get; set; }

    /// <summary>Примечание кассира (номер квитанции и т.п.). Необязательно.</summary>
    public string? Notes { get; set; }
}