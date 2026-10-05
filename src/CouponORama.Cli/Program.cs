using System.Globalization;
using CouponORama.Core.Models;
using CouponORama.Core.Services;
using CouponORama.Core.Storage;

CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");

var dataPath = Environment.GetEnvironmentVariable("COUPON_O_RAMA_DATA")
    ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".coupon-o-rama", "coupons.json");
var argList = args.ToList();
var dataIndex = argList.IndexOf("--data");
if (dataIndex >= 0 && dataIndex + 1 < argList.Count)
{
    dataPath = argList[dataIndex + 1];
    argList.RemoveRange(dataIndex, 2);
}

var today = DateOnly.FromDateTime(DateTime.Today);
var wallet = new CouponWallet(JsonStore.LoadCoupons(dataPath));
var command = argList.FirstOrDefault()?.ToLowerInvariant() ?? "help";
var rest = argList.Skip(1).ToArray();

try
{
    switch (command)
    {
        case "list":
            PrintCoupons(rest.Contains("--all") ? wallet.All.OrderBy(c => c.ExpiresOn) : wallet.Available(today));
            break;
        case "search":
            PrintCoupons(wallet.Search(Require(rest, 0, "search term"), today));
            break;
        case "clip":
            Console.WriteLine($"Clipped: {wallet.Clip(Require(rest, 0, "coupon id")).Title}");
            Save();
            break;
        case "unclip":
            Console.WriteLine($"Unclipped: {wallet.Unclip(Require(rest, 0, "coupon id")).Title}");
            Save();
            break;
        case "remove":
            var id = Require(rest, 0, "coupon id");
            Console.WriteLine(wallet.Remove(id) ? $"Removed {id}." : $"No coupon with id '{id}'.");
            Save();
            break;
        case "purge":
            Console.WriteLine($"Removed {wallet.RemoveExpired(today)} expired coupon(s).");
            Save();
            break;
        case "add":
            wallet.Add(ParseCoupon(rest, today));
            Console.WriteLine("Coupon added.");
            Save();
            break;
        case "seed":
            var added = 0;
            foreach (var coupon in SampleData.Coupons(today).Where(c => wallet.Find(c.Id) is null))
            {
                wallet.Add(coupon);
                added++;
            }
            Console.WriteLine($"Added {added} sample coupon(s) to {dataPath}.");
            Save();
            break;
        case "checkout":
            var cart = rest.Length > 0 ? JsonStore.LoadCart(rest[0]) : SampleData.Cart().ToList();
            PrintReceipt(cart, new CouponCalculator().Calculate(cart, wallet.All, today));
            break;
        default:
            PrintHelp();
            break;
    }
    return 0;
}
catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or KeyNotFoundException or FileNotFoundException or FormatException)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    return 1;
}

void Save() => JsonStore.SaveCoupons(dataPath, wallet.All);

static string Require(string[] values, int index, string name) =>
    index < values.Length ? values[index] : throw new ArgumentException($"Missing {name}.");

static Coupon ParseCoupon(string[] values, DateOnly today)
{
    var options = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    for (var i = 0; i + 1 < values.Length; i += 2)
    {
        if (!values[i].StartsWith("--"))
            throw new ArgumentException($"Unexpected argument '{values[i]}'.");
        options[values[i][2..]] = values[i + 1];
    }

    string Get(string key) => options.TryGetValue(key, out var v) ? v : throw new ArgumentException($"Missing --{key}.");
    var inv = CultureInfo.InvariantCulture;
    var discount = Get("discount");
    var isPercent = discount.EndsWith('%');
    return new Coupon
    {
        Id = Get("id"),
        Title = Get("title"),
        Category = options.GetValueOrDefault("category", "General"),
        DiscountType = isPercent ? DiscountType.PercentOff : DiscountType.AmountOff,
        Value = decimal.Parse(discount.Trim('$', '%'), inv),
        Sku = options.GetValueOrDefault("sku"),
        MinQuantity = options.TryGetValue("min-qty", out var q) ? int.Parse(q, inv) : 1,
        MinPurchase = options.TryGetValue("min-purchase", out var m) ? decimal.Parse(m.TrimStart('$'), inv) : 0m,
        ExpiresOn = options.TryGetValue("expires", out var e) ? DateOnly.Parse(e, inv) : today.AddDays(30),
    };
}

static void PrintCoupons(IEnumerable<Coupon> coupons)
{
    var list = coupons.ToList();
    if (list.Count == 0)
    {
        Console.WriteLine("No coupons. Try `seed` to load some samples.");
        return;
    }
    Console.WriteLine($"{"",2} {"ID",-12} {"Discount",-10} {"Expires",-10}  Title");
    foreach (var c in list)
    {
        Console.WriteLine($"{(c.Clipped ? "✂" : " "),2} {c.Id,-12} {c.DescribeDiscount(),-10} {c.ExpiresOn:yyyy-MM-dd}  {c.Title}");
    }
}

static void PrintReceipt(IReadOnlyList<CartItem> cart, CheckoutResult result)
{
    Console.WriteLine("=========== MEIJER COUPON-O-RAMA ===========");
    foreach (var item in cart)
    {
        var label = item.Quantity > 1 ? $"{item.Name} x{item.Quantity}" : item.Name;
        Console.WriteLine($"{label,-34} {item.LineTotal,8:C}");
    }
    Console.WriteLine(new string('-', 44));
    Console.WriteLine($"{"Subtotal",-34} {result.Subtotal,8:C}");
    foreach (var applied in result.Applied)
    {
        Console.WriteLine($"  {applied.Coupon.Title,-32} {-applied.Savings,8:C}");
    }
    Console.WriteLine(new string('-', 44));
    Console.WriteLine($"{"TOTAL",-34} {result.Total,8:C}");
    Console.WriteLine($"You saved {result.TotalSavings:C} with {result.Applied.Count} coupon(s)!");
}

static void PrintHelp() => Console.WriteLine("""
    Meijer Coupon-O-Rama

    Usage: coupon-o-rama [--data <file>] <command> [args]

    Commands:
      list [--all]            Show available coupons (--all includes expired). ✂ = clipped
      search <term>           Find coupons by title, category, or SKU
      clip <id>               Clip a coupon so it applies at checkout
      unclip <id>             Unclip a coupon
      add --id <id> --title <title> --discount <$1.50|20%>
          [--sku <sku>] [--category <name>] [--min-qty <n>]
          [--min-purchase <amount>] [--expires <yyyy-mm-dd>]
      remove <id>             Delete a coupon
      purge                   Delete all expired coupons
      seed                    Load sample coupons
      checkout [cart.json]    Apply clipped coupons to a cart (sample cart if omitted)

    Coupons are stored in ~/.coupon-o-rama/coupons.json unless --data or
    COUPON_O_RAMA_DATA is set.
    """);
