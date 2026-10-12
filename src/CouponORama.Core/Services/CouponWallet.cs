using CouponORama.Core.Models;

namespace CouponORama.Core.Services;

/// <summary>An in-memory collection of coupons keyed by id (case-insensitive).</summary>
public sealed class CouponWallet
{
    private readonly Dictionary<string, Coupon> _coupons = new(StringComparer.OrdinalIgnoreCase);

    public CouponWallet(IEnumerable<Coupon>? coupons = null)
    {
        foreach (var coupon in coupons ?? [])
        {
            Add(coupon);
        }
    }

    public IReadOnlyCollection<Coupon> All => _coupons.Values;

    public void Add(Coupon coupon)
    {
        if (string.IsNullOrWhiteSpace(coupon.Id))
            throw new ArgumentException("Coupon id is required.", nameof(coupon));
        if (coupon.Value <= 0)
            throw new ArgumentException("Coupon value must be positive.", nameof(coupon));
        if (coupon.DiscountType == DiscountType.PercentOff && coupon.Value > 100)
            throw new ArgumentException("Percent-off coupons cannot exceed 100%.", nameof(coupon));
        if (!_coupons.TryAdd(coupon.Id, coupon))
            throw new InvalidOperationException($"A coupon with id '{coupon.Id}' already exists.");
    }

    public Coupon? Find(string id) => _coupons.GetValueOrDefault(id);

    public bool Remove(string id) => _coupons.Remove(id);

    public Coupon Clip(string id) => SetClipped(id, true);

    public Coupon Unclip(string id) => SetClipped(id, false);

    public int RemoveExpired(DateOnly today)
    {
        var expired = _coupons.Values.Where(c => c.IsExpired(today)).Select(c => c.Id).ToList();
        expired.ForEach(id => _coupons.Remove(id));
        return expired.Count;
    }

    public IEnumerable<Coupon> Available(DateOnly today) =>
        _coupons.Values.Where(c => !c.IsExpired(today)).OrderBy(c => c.ExpiresOn).ThenBy(c => c.Title);

    public IEnumerable<Coupon> Search(string term, DateOnly today) =>
        Available(today).Where(c =>
            c.Title.Contains(term, StringComparison.OrdinalIgnoreCase)
            || c.Category.Contains(term, StringComparison.OrdinalIgnoreCase)
            || (c.Sku?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false));

    private Coupon SetClipped(string id, bool clipped)
    {
        var coupon = Find(id) ?? throw new KeyNotFoundException($"No coupon with id '{id}'.");
        var updated = coupon with { Clipped = clipped };
        _coupons[coupon.Id] = updated;
        return updated;
    }
}
