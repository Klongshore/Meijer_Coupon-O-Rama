using CouponORama.Core.Models;
using CouponORama.Core.Services;

namespace CouponORama.Tests;

public class CouponCalculatorTests
{
    private static readonly DateOnly Today = new(2026, 10, 5);
    private readonly CouponCalculator _calculator = new();

    private static Coupon ItemCoupon(string id, string sku, DiscountType type, decimal value, int minQty = 1) => new()
    {
        Id = id, Title = id, Sku = sku, DiscountType = type, Value = value,
        MinQuantity = minQty, ExpiresOn = Today.AddDays(1), Clipped = true,
    };

    private static Coupon BasketCoupon(string id, DiscountType type, decimal value, decimal minPurchase = 0) => new()
    {
        Id = id, Title = id, DiscountType = type, Value = value,
        MinPurchase = minPurchase, ExpiresOn = Today.AddDays(1), Clipped = true,
    };

    [Fact]
    public void AppliesAmountOffToMatchingItem()
    {
        var result = _calculator.Calculate(
            [new CartItem("MILK", "Milk", 3.49m)],
            [ItemCoupon("C1", "milk", DiscountType.AmountOff, 1m)],
            Today);

        Assert.Equal(3.49m, result.Subtotal);
        Assert.Equal(1m, result.TotalSavings);
        Assert.Equal(2.49m, result.Total);
    }

    [Fact]
    public void PercentOffIsRoundedToCents()
    {
        var result = _calculator.Calculate(
            [new CartItem("BAN", "Bananas", 0.59m, 3)],
            [ItemCoupon("C1", "BAN", DiscountType.PercentOff, 25m)],
            Today);

        Assert.Equal(0.44m, result.TotalSavings); // 1.77 * 25% = 0.4425
    }

    [Fact]
    public void IgnoresUnclippedAndExpiredCoupons()
    {
        var cart = new[] { new CartItem("MILK", "Milk", 3.49m) };
        var unclipped = ItemCoupon("C1", "MILK", DiscountType.AmountOff, 1m) with { Clipped = false };
        var expired = ItemCoupon("C2", "MILK", DiscountType.AmountOff, 1m) with { ExpiresOn = Today.AddDays(-1) };

        var result = _calculator.Calculate(cart, [unclipped, expired], Today);

        Assert.Empty(result.Applied);
    }

    [Fact]
    public void CouponIsValidOnItsExpirationDate()
    {
        var coupon = ItemCoupon("C1", "MILK", DiscountType.AmountOff, 1m) with { ExpiresOn = Today };

        var result = _calculator.Calculate([new CartItem("MILK", "Milk", 3.49m)], [coupon], Today);

        Assert.Single(result.Applied);
    }

    [Fact]
    public void UsesOnlyTheBestCouponPerItem()
    {
        var result = _calculator.Calculate(
            [new CartItem("COFFEE", "Coffee", 10m)],
            [ItemCoupon("SMALL", "COFFEE", DiscountType.AmountOff, 2m), ItemCoupon("BIG", "COFFEE", DiscountType.PercentOff, 30m)],
            Today);

        var applied = Assert.Single(result.Applied);
        Assert.Equal("BIG", applied.Coupon.Id);
        Assert.Equal(3m, applied.Savings);
    }

    [Fact]
    public void RequiresMinimumQuantity()
    {
        var coupon = ItemCoupon("C1", "CEREAL", DiscountType.AmountOff, 3m, minQty: 3);

        Assert.Empty(_calculator.Calculate([new CartItem("CEREAL", "Cereal", 4m, 2)], [coupon], Today).Applied);
        Assert.Single(_calculator.Calculate(
            [new CartItem("CEREAL", "Cereal", 4m, 2), new CartItem("CEREAL", "Cereal", 4m)], [coupon], Today).Applied);
    }

    [Fact]
    public void SavingsNeverExceedItemPrice()
    {
        var result = _calculator.Calculate(
            [new CartItem("GUM", "Gum", 0.99m)],
            [ItemCoupon("C1", "GUM", DiscountType.AmountOff, 5m)],
            Today);

        Assert.Equal(0.99m, result.TotalSavings);
        Assert.Equal(0m, result.Total);
    }

    [Fact]
    public void BasketMinimumIsCheckedAfterItemCoupons()
    {
        var cart = new[] { new CartItem("TV", "TV", 80m) };
        var item = ItemCoupon("TV10", "TV", DiscountType.AmountOff, 10m);
        var basket = BasketCoupon("B10", DiscountType.AmountOff, 10m, minPurchase: 75m);

        var result = _calculator.Calculate(cart, [item, basket], Today);

        var applied = Assert.Single(result.Applied);
        Assert.Equal("TV10", applied.Coupon.Id);
    }

    [Fact]
    public void AppliesBestSingleBasketCoupon()
    {
        var result = _calculator.Calculate(
            [new CartItem("STUFF", "Stuff", 100m)],
            [BasketCoupon("FLAT", DiscountType.AmountOff, 10m, 75m), BasketCoupon("PCT", DiscountType.PercentOff, 5m)],
            Today);

        var applied = Assert.Single(result.Applied);
        Assert.Equal("FLAT", applied.Coupon.Id);
        Assert.Equal(90m, result.Total);
    }
}
