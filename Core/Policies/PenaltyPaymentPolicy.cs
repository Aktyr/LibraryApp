namespace LibApp.Core.Policies;

/// <summary>
/// Правила оплаты штрафа за просрочку. Частичные оплаты не поддерживаются:
/// сумма всегда равна полной сумме штрафа, на одну выдачу — не более одного платежа.
/// </summary>
public static class PenaltyPaymentPolicy
{
    /// <summary>
    /// Проверяет параметры оплаты. Возвращает сообщения для UI; пустой список — оплата допустима.
    /// </summary>
    /// <param name="penalty">Начисленный штраф. null или 0 — платить нечего.</param>
    /// <param name="returnDate">Дата возврата книги. null — книга ещё на руках, штраф может вырасти.</param>
    /// <param name="hasExistingPayment">Признак того, что штраф уже оплачен.</param>
    public static List<string> Validate(
    decimal? penalty,
    DateTime? returnDate,
    bool hasExistingPayment)
    {
        var errors = new List<string>();

        if (!penalty.HasValue || penalty.Value <= 0)
            errors.Add("Нет штрафа для оплаты");
        else if (returnDate == null)
            errors.Add("Штраф можно оплатить только после возврата книги");
        else if (hasExistingPayment)
            errors.Add("Штраф уже оплачен");

        return errors;
    }
}