using TMPro;
using UnityEngine;

/// <summary>경매 입찰 금액 TMP_InputField — 직접 입력 + step 버튼 공통 처리.</summary>
public static class AuctionBidInputUtility
{
    public static void Configure(TMP_InputField input)
    {
        if (input == null)
            return;

        input.contentType = TMP_InputField.ContentType.IntegerNumber;
        input.lineType = TMP_InputField.LineType.SingleLine;
        GameUIFont.ApplyInputField(input);
    }

    public static void SetAmount(TMP_InputField input, int amount)
    {
        if (input == null)
            return;

        input.SetTextWithoutNotify(FormatAmount(amount));
    }

    public static void ClearInput(TMP_InputField input)
    {
        if (input == null)
            return;

        input.SetTextWithoutNotify(string.Empty);
    }

    public static string FormatAmount(int amount) => amount.ToString("N0");

    public static bool TryParseAmount(string text, out int amount)
    {
        amount = 0;
        if (string.IsNullOrWhiteSpace(text))
            return false;

        var cleaned = text.Replace(",", string.Empty).Replace(" ", string.Empty).Trim();
        if (cleaned.EndsWith("G", System.StringComparison.OrdinalIgnoreCase))
            cleaned = cleaned[..^1];

        return int.TryParse(cleaned, out amount) && amount >= 0;
    }

    public static bool HasUserInput(TMP_InputField input) =>
        input != null && !string.IsNullOrWhiteSpace(input.text);

    /// <summary>입력값이 있으면 파싱, 없으면 fallback(내부 제안가) 사용.</summary>
    public static int ResolveAmount(TMP_InputField input, int fallback, int minAmount, int step)
    {
        var amount = fallback;
        if (input != null && TryParseAmount(input.text, out var parsed))
            amount = parsed;

        amount = Mathf.Max(amount, minAmount);
        if (step > 1)
            amount = AlignToStep(amount, minAmount, step);

        return amount;
    }

    private static int AlignToStep(int value, int min, int step)
    {
        if (value <= min)
            return min;

        var offset = value - min;
        var steps = Mathf.RoundToInt(offset / (float)step);
        return min + (steps * step);
    }
}
