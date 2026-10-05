namespace CouponORama.Core.Models;

public sealed record CartItem(string Sku, string Name, decimal UnitPrice, int Quantity = 1)
{
    public decimal LineTotal => UnitPrice * Quantity;
}
