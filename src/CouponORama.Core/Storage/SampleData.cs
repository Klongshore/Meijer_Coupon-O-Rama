using CouponORama.Core.Models;

namespace CouponORama.Core.Storage;

public static class SampleData
{
    public static IEnumerable<Coupon> Coupons(DateOnly today) =>
    [
        new() { Id = "MILK1", Title = "$1 off Meijer 2% Milk, 1 gal", Category = "Dairy", DiscountType = DiscountType.AmountOff, Value = 1.00m, Sku = "MILK-2PCT-GAL", ExpiresOn = today.AddDays(14) },
        new() { Id = "CEREAL3", Title = "$3 off 3 boxes of cereal", Category = "Breakfast", DiscountType = DiscountType.AmountOff, Value = 3.00m, Sku = "CEREAL-OATS", MinQuantity = 3, ExpiresOn = today.AddDays(7) },
        new() { Id = "BANANA25", Title = "25% off bananas", Category = "Produce", DiscountType = DiscountType.PercentOff, Value = 25m, Sku = "BANANAS-LB", ExpiresOn = today.AddDays(3) },
        new() { Id = "COFFEE2", Title = "$2 off ground coffee", Category = "Pantry", DiscountType = DiscountType.AmountOff, Value = 2.00m, Sku = "COFFEE-12OZ", ExpiresOn = today.AddDays(30) },
        new() { Id = "BASKET10", Title = "$10 off a $75+ order", Category = "Store", DiscountType = DiscountType.AmountOff, Value = 10.00m, MinPurchase = 75m, ExpiresOn = today.AddDays(10) },
        new() { Id = "BASKET5PCT", Title = "5% off your order", Category = "Store", DiscountType = DiscountType.PercentOff, Value = 5m, ExpiresOn = today.AddDays(5) },
    ];

    public static IEnumerable<CartItem> Cart() =>
    [
        new("MILK-2PCT-GAL", "Meijer 2% Milk, 1 gal", 3.49m),
        new("CEREAL-OATS", "Toasted Oats Cereal", 3.99m, 3),
        new("BANANAS-LB", "Bananas (lb)", 0.59m, 3),
        new("COFFEE-12OZ", "Ground Coffee 12 oz", 8.99m),
        new("CHICKEN-BRST", "Boneless Chicken Breast", 12.49m, 4),
        new("PAPER-TOWEL", "Paper Towels 6pk", 9.99m),
    ];
}
