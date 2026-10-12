using System.Text.Json;
using System.Text.Json.Serialization;
using CouponORama.Core.Models;

namespace CouponORama.Core.Storage;

/// <summary>Reads and writes coupons and carts as JSON files.</summary>
public static class JsonStore
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public static List<Coupon> LoadCoupons(string path) => Load<List<Coupon>>(path) ?? [];

    public static void SaveCoupons(string path, IEnumerable<Coupon> coupons) =>
        Save(path, coupons.OrderBy(c => c.Id, StringComparer.OrdinalIgnoreCase).ToList());

    public static List<CartItem> LoadCart(string path) =>
        Load<List<CartItem>>(path) ?? throw new FileNotFoundException($"Cart file not found: {path}", path);

    private static T? Load<T>(string path) where T : class
    {
        if (!File.Exists(path))
            return null;
        using var stream = File.OpenRead(path);
        return JsonSerializer.Deserialize<T>(stream, Options);
    }

    private static void Save<T>(string path, T value)
    {
        var dir = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
        var temp = path + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(value, Options));
        File.Move(temp, path, overwrite: true);
    }
}
