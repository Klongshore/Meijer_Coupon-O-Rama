# Meijer Coupon-O-Rama

A C# (.NET 10) command-line app for managing digital grocery coupons: clip them, search them,
and see how much you save at checkout.

## Projects

| Path | Description |
| --- | --- |
| `src/CouponORama.Core` | Coupon/cart models, the savings calculator, wallet, and JSON storage |
| `src/CouponORama.Cli` | The `coupon-o-rama` console app |
| `tests/CouponORama.Tests` | xUnit tests |

## Getting started

```bash
dotnet build
dotnet test

# Load sample coupons, clip a few, and check out the sample cart
dotnet run --project src/CouponORama.Cli -- seed
dotnet run --project src/CouponORama.Cli -- clip MILK1
dotnet run --project src/CouponORama.Cli -- clip BASKET10
dotnet run --project src/CouponORama.Cli -- list
dotnet run --project src/CouponORama.Cli -- checkout
dotnet run --project src/CouponORama.Cli -- checkout samples/cart.json
```

Run with no arguments (or `help`) for the full command list. Coupons are saved to
`~/.coupon-o-rama/coupons.json`; override with `--data <file>` or the `COUPON_O_RAMA_DATA`
environment variable.

## Coupon rules

- Only **clipped**, **unexpired** coupons apply (a coupon is valid through its expiration date).
- **Item coupons** (with a SKU) apply to matching cart lines; at most one per SKU, and the
  biggest saving wins. `MinQuantity` and `MinPurchase` must be met.
- **Basket coupons** (no SKU) apply once to the order, after item coupons; the best one wins
  and its `MinPurchase` is checked against the post-item-coupon subtotal.
- Discounts are either a dollar amount (`$1.50`) or a percentage (`20%`, rounded to the cent),
  and never exceed the amount being discounted.

## Cart file format

```json
[
  { "sku": "MILK-2PCT-GAL", "name": "Meijer 2% Milk, 1 gal", "unitPrice": 3.49, "quantity": 2 }
]
```
