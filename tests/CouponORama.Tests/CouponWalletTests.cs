using CouponORama.Core.Models;
using CouponORama.Core.Services;
using CouponORama.Core.Storage;

namespace CouponORama.Tests;

public class CouponWalletTests
{
    private static readonly DateOnly Today = new(2026, 10, 5);

    private static Coupon Make(string id, int daysUntilExpiry = 5) => new()
    {
        Id = id, Title = $"Coupon {id}", Category = "Dairy", DiscountType = DiscountType.AmountOff,
        Value = 1m, ExpiresOn = Today.AddDays(daysUntilExpiry),
    };

    [Fact]
    public void ClipAndUnclipToggleState()
    {
        var wallet = new CouponWallet([Make("A")]);

        Assert.True(wallet.Clip("a").Clipped);
        Assert.True(wallet.Find("A")!.Clipped);
        Assert.False(wallet.Unclip("A").Clipped);
    }

    [Fact]
    public void ClipUnknownCouponThrows()
    {
        Assert.Throws<KeyNotFoundException>(() => new CouponWallet().Clip("nope"));
    }

    [Fact]
    public void RejectsDuplicateIdsAndInvalidValues()
    {
        var wallet = new CouponWallet([Make("A")]);

        Assert.Throws<InvalidOperationException>(() => wallet.Add(Make("a")));
        Assert.Throws<ArgumentException>(() => wallet.Add(Make("B") with { Value = 0 }));
        Assert.Throws<ArgumentException>(() => wallet.Add(Make("C") with { DiscountType = DiscountType.PercentOff, Value = 150 }));
    }

    [Fact]
    public void AvailableExcludesExpiredAndSortsByExpiry()
    {
        var wallet = new CouponWallet([Make("LATE", 10), Make("OLD", -1), Make("SOON", 1)]);

        Assert.Equal(["SOON", "LATE"], wallet.Available(Today).Select(c => c.Id));
        Assert.Equal(1, wallet.RemoveExpired(Today));
        Assert.Equal(2, wallet.All.Count);
    }

    [Fact]
    public void SearchMatchesTitleCategoryAndSku()
    {
        var wallet = new CouponWallet([Make("A") with { Sku = "MILK-GAL" }, Make("B") with { Category = "Produce" }]);

        Assert.Single(wallet.Search("milk", Today));
        Assert.Single(wallet.Search("produce", Today));
        Assert.Equal(2, wallet.Search("coupon", Today).Count());
    }

    [Fact]
    public void JsonStoreRoundTripsCoupons()
    {
        var path = Path.Combine(Path.GetTempPath(), $"coupons-{Guid.NewGuid():N}.json");
        try
        {
            var original = SampleData.Coupons(Today).ToList();
            JsonStore.SaveCoupons(path, original);

            var loaded = JsonStore.LoadCoupons(path);

            Assert.Equal(original.OrderBy(c => c.Id), loaded.OrderBy(c => c.Id));
            Assert.Contains("\"PercentOff\"", File.ReadAllText(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void LoadingMissingCouponFileReturnsEmpty()
    {
        Assert.Empty(JsonStore.LoadCoupons(Path.Combine(Path.GetTempPath(), $"missing-{Guid.NewGuid():N}.json")));
    }
}
