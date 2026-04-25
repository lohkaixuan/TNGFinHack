using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ApiApp.AI
{
    // 1) Domain ---------------------------------------------------------------
    public enum Category
    {
        FoodGroceries,
        Utilities,
        Shopping,
        HousingExpense,
        Transportation,
        Healthcare,
        Entertainments,
        WorkLearning,
        Travel,
        Charity,
        Subscription,
        Other
    }
    public record TxInput(
        string? merchant,
        string? description,
        string? mcc,
        decimal amount,
        string currency = "MYR",
        string? country = "MY"
    );
    public record TxOutput(Category category, double confidence, string? rationale = null);
    public interface ICategorizer
    {
        Task<TxOutput> CategorizeAsync(TxInput tx, CancellationToken ct = default);
    }
    public static class CategoryParser
    {
        private static readonly Dictionary<string, Category> Map = new(StringComparer.OrdinalIgnoreCase)
        {
            ["food groceries"] = Category.FoodGroceries, ["food & groceries"] = Category.FoodGroceries,
            ["food and groceries"] = Category.FoodGroceries, ["groceries"] = Category.FoodGroceries,
            ["grocery"] = Category.FoodGroceries, ["fb"] = Category.FoodGroceries, ["f&b"] = Category.FoodGroceries,
            ["food & beverage"] = Category.FoodGroceries, ["food and beverage"] = Category.FoodGroceries,
            ["food"] = Category.FoodGroceries,
            ["utilities"] = Category.Utilities, ["utility"] = Category.Utilities, ["bills"] = Category.Utilities,
            ["housing expense"] = Category.HousingExpense, ["housing"] = Category.HousingExpense,
            ["rent"] = Category.HousingExpense, ["mortgage"] = Category.HousingExpense,
            ["transportation"] = Category.Transportation, ["transport"] = Category.Transportation,
            ["shopping"] = Category.Shopping,
            ["healthcare"] = Category.Healthcare, ["health"] = Category.Healthcare,
            ["entertainments"] = Category.Entertainments, ["entertainment"] = Category.Entertainments,
            ["work learning"] = Category.WorkLearning, ["work & learning"] = Category.WorkLearning,
            ["work and learning"] = Category.WorkLearning, ["learning"] = Category.WorkLearning,
            ["education"] = Category.WorkLearning, ["course"] = Category.WorkLearning,
            ["travel"] = Category.Travel,
            ["charity"] = Category.Charity, ["donation"] = Category.Charity, ["donations"] = Category.Charity,
            ["subscription"] = Category.Subscription, ["subscriptions"] = Category.Subscription,
            ["other"] = Category.Other
        };

        public static bool TryParse(string? csvValue, out Category cat)
        {
            cat = Category.Other;
            if (string.IsNullOrWhiteSpace(csvValue)) return false;

            var key = csvValue.Trim().ToLowerInvariant()
                              .Replace("_", " ").Replace("-", " ")
                              .Replace("  ", " ");
            key = key.Replace(" and ", " & ");
            if (Map.TryGetValue(key, out cat))
                return true;

            var enumKey = key.Replace(" ", "").Replace("&", "");
            if (Enum.TryParse(enumKey, true, out cat))
                return true;

            cat = Category.Other;
            return true;
        }

        public static Category FromCsv(string? csvValue, Category fallback = Category.Other)
            => TryParse(csvValue, out var cat) ? cat : fallback;
    }

    public sealed class RulesCategorizer : ICategorizer
    {
        private static readonly (Regex re, Category cat)[] Map = new[]
        {
            (new Regex("mcd|kfc|starbucks|tealive|kopitiam|mamak|foodpanda|grab ?food|jaya|aeon|tesco|lotus|mydin|giant|mart|grocer", RegexOptions.IgnoreCase), Category.FoodGroceries),
            (new Regex("tng|touch ?n ?go|maxis|celcom|digi|tm|tenaga|tnb|water|electric|utility|bill", RegexOptions.IgnoreCase), Category.Utilities),
            (new Regex("lazada|shopee|uniqlo|mr ?diy", RegexOptions.IgnoreCase), Category.Shopping),
            (new Regex("rent|mortgage|housing|maintenance|condo", RegexOptions.IgnoreCase), Category.HousingExpense),
            (new Regex("petronas|shell|bhp|grab(?!.*food)|rapidkl|mrt|lrt|bus|taxi|parking|toll", RegexOptions.IgnoreCase), Category.Transportation),
            (new Regex("watsons|guardian|clinic|hospital|pharmacy|medical|doctor|dental", RegexOptions.IgnoreCase), Category.Healthcare),
            (new Regex("netflix|spotify|cinema|movie|game|karaoke|entertain", RegexOptions.IgnoreCase), Category.Entertainments),
            (new Regex("course|class|book|tuition|udemy|coursera|workshop|stationery", RegexOptions.IgnoreCase), Category.WorkLearning),
            (new Regex("hotel|flight|airasia|malaysia airlines|travel|booking", RegexOptions.IgnoreCase), Category.Travel),
            (new Regex("charity|donation|zakat|fitrah", RegexOptions.IgnoreCase), Category.Charity),
            (new Regex("subscription|monthly plan|renewal|membership", RegexOptions.IgnoreCase), Category.Subscription),
        };

        public Task<TxOutput> CategorizeAsync(TxInput tx, CancellationToken ct = default)
        {
            var hay = $"{tx.merchant} {tx.description}".ToLowerInvariant();
            foreach (var (re, cat) in Map)
                if (re.IsMatch(hay)) return Task.FromResult(new TxOutput(cat, 0.85));

            if (!string.IsNullOrWhiteSpace(tx.mcc) && tx.mcc.StartsWith("58")) // restaurant MCC
                return Task.FromResult(new TxOutput(Category.FoodGroceries, 0.7));

            return Task.FromResult(new TxOutput(Category.Other, 0.3));
        }
    }

    public sealed class ZeroShotCategorizer : ICategorizer
    {
        private readonly HttpClient _http;
        private readonly RulesCategorizer _fallback;
        private static readonly string[] Labels = Enum.GetNames(typeof(Category));
        public ZeroShotCategorizer(HttpClient http, RulesCategorizer fallback)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _fallback = fallback ?? throw new ArgumentNullException(nameof(fallback));
        }

        public async Task<TxOutput> CategorizeAsync(TxInput tx, CancellationToken ct = default)
        {
            var text = $"{tx.merchant} {tx.description}".Trim();
            if (string.IsNullOrWhiteSpace(text))
                return await _fallback.CategorizeAsync(tx, ct);
            try
            {
                var req = new { inputs = text, parameters = new { candidate_labels = Labels } };

                using var r = await _http.PostAsJsonAsync(
                    "https://api-inference.huggingface.co/models/facebook/bart-large-mnli", req, ct);
                r.EnsureSuccessStatusCode();

                using var doc  = await JsonDocument.ParseAsync(await r.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
                var labels     = doc.RootElement.GetProperty("labels").EnumerateArray().Select(e => e.GetString()!).ToArray();
                var scores     = doc.RootElement.GetProperty("scores").EnumerateArray().Select(e => e.GetDouble()).ToArray();
                if (labels.Length == 0 || scores.Length == 0)
                    return await _fallback.CategorizeAsync(tx, ct);

                var idx        = Array.IndexOf(scores, scores.Max());
                var normalized = labels[idx].Replace("&", "").Replace(" ", "");
                var ok         = Enum.TryParse<Category>(normalized, true, out var parsed);
                var cat        = ok ? parsed : Category.Other;

                return new TxOutput(cat, scores[idx], $"ZSC: {text}");
            }
            catch
            {
                return await _fallback.CategorizeAsync(tx, ct);
            }
        }
    }
}
