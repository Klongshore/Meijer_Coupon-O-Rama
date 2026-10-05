namespace CouponORama.Core.Models;

public sealed record AppliedCoupon(Coupon Coupon, decimal Savings);

public sealed record CheckoutResult(decimal Subtotal, IReadOnlyList<AppliedCoupon> Applied)
{
    public decimal TotalSavings => Applied.Sum(a => a.Savings);
    public decimal Total => Subtotal - TotalSavings;
}
