namespace Web.Helpers;

public static class TransactionHelpers
{
    public static string FormatCardMask(string? cardNumber)
    {
        if (string.IsNullOrWhiteSpace(cardNumber)) return "**** **** **** ****";
        var last4 = cardNumber.Length >= 4 ? cardNumber.Substring(cardNumber.Length - 4) : cardNumber;
        return $"**** **** **** {last4}";
    }

    public static string ResolveTransactionBeneficiary(string? beneficiary)
    {
        if (string.IsNullOrWhiteSpace(beneficiary)) return "-";
        if (beneficiary.Length == 16 && beneficiary.All(char.IsDigit))
            return beneficiary.Substring(12);
        return beneficiary;
    }

    public static string ResolveTransactionOrigin(string? origin)
    {
        if (string.IsNullOrWhiteSpace(origin)) return "-";
        if (origin.Length == 16 && origin.All(char.IsDigit))
            return origin.Substring(12);
        return origin;
    }
}
