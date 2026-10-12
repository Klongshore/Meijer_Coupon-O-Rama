namespace CouponORama.Core.Models;

public enum DiscountType
{
    /// <summary>A fixed dollar amount off, e.g. $1.00 off.</summary>
    AmountOff,

    /// <summary>A percentage off, e.g. 20% off.</summary>
    PercentOff,
}

/// <summary>
/// A digital coupon. When <see cref="Sku"/> is set the coupon applies to that item only;
/// otherwise it is a basket-level coupon applied to the whole order.
/// </summary>
public sealed record Coupon
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public string Category { get; init; } = "General";
    public DiscountType DiscountType { get; init; }
    public decimal Value { get; init; }
    public string? Sku { get; init; }
    public int MinQuantity { get; init; } = 1;
    public decimal MinPurchase { get; init; }
    public DateOnly ExpiresOn { get; init; }
    public bool Clipped { get; init; }

    public bool IsBasketLevel => string.IsNullOrWhiteSpace(Sku);

    public bool IsExpired(DateOnly today) => today > ExpiresOn;

    public string DescribeDiscount() => DiscountType switch
    {
        DiscountType.AmountOff => $"${Value:0.00} off",
        DiscountType.PercentOff => $"{Value:0.##}% off",
        _ => Value.ToString("0.00"),
    };
}
