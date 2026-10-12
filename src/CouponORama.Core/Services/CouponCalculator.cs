using CouponORama.Core.Models;

namespace CouponORama.Core.Services;

/// <summary>
/// Applies clipped, unexpired coupons to a cart.
/// Rules:
///  - At most one item coupon per SKU; the one with the biggest savings wins.
///  - Then at most one basket coupon, evaluated against the subtotal after item coupons.
///  - Savings never exceed the amount they are discounting.
/// </summary>
public sealed class CouponCalculator
{
    public CheckoutResult Calculate(IEnumerable<CartItem> cart, IEnumerable<Coupon> coupons, DateOnly today)
    {
        var lines = cart
            .GroupBy(i => i.Sku, StringComparer.OrdinalIgnoreCase)
            .Select(g => (Sku: g.Key, Quantity: g.Sum(i => i.Quantity), Total: g.Sum(i => i.LineTotal)))
            .ToList();
        var eligible = coupons.Where(c => c.Clipped && !c.IsExpired(today)).ToList();
        var subtotal = lines.Sum(l => l.Total);
        var applied = new List<AppliedCoupon>();

        foreach (var line in lines)
        {
            var best = eligible
                .Where(c => !c.IsBasketLevel
                    && string.Equals(c.Sku, line.Sku, StringComparison.OrdinalIgnoreCase)
                    && line.Quantity >= c.MinQuantity
                    && line.Total >= c.MinPurchase)
                .Select(c => new AppliedCoupon(c, Savings(c, line.Total)))
                .MaxBy(a => a.Savings);
            if (best is { Savings: > 0 })
            {
                applied.Add(best);
            }
        }

        var afterItemCoupons = subtotal - applied.Sum(a => a.Savings);
        var bestBasket = eligible
            .Where(c => c.IsBasketLevel && afterItemCoupons >= c.MinPurchase)
            .Select(c => new AppliedCoupon(c, Savings(c, afterItemCoupons)))
            .MaxBy(a => a.Savings);
        if (bestBasket is { Savings: > 0 })
        {
            applied.Add(bestBasket);
        }

        return new CheckoutResult(subtotal, applied);
    }

    private static decimal Savings(Coupon coupon, decimal amount)
    {
        var raw = coupon.DiscountType switch
        {
            DiscountType.AmountOff => coupon.Value,
            DiscountType.PercentOff => Math.Round(amount * coupon.Value / 100m, 2, MidpointRounding.AwayFromZero),
            _ => 0m,
        };
        return Math.Clamp(raw, 0m, amount);
    }
}
