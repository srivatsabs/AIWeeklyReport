using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace AIWeeklyReport
{
    /// <summary>
    /// Generates the narrative summary paragraphs via the internal AI gateway
    /// (OpenAI-compatible chat/completions schema). The model is only ever given the
    /// already-computed figures (never raw ticket rows) and is instructed not to
    /// introduce any number, name, or cause that isn't in that data. If the call fails
    /// for any reason, the caller's rule-based fallback is used instead so the report
    /// always ships.
    /// </summary>
    public class AiNarrativeGenerator
    {
        private const string ApiUrl = "https://rfc-ai-gateway.reed.net/v1/chat/completions";

        private const string Model = "RFC-AI-V2";

        private readonly string _apiKey;
        private readonly HttpClient _http;

        public AiNarrativeGenerator(string apiKey, HttpClient? httpClient = null)
        {
            _apiKey = apiKey;
            _http = httpClient ?? new HttpClient();
        }

        /// <summary>Quick connectivity/auth check against the gateway before relying on it for a real run.</summary>
        public async Task<bool> IsApiKeyValidAsync(CancellationToken ct = default)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, ApiUrl);
            request.Headers.Add("Authorization", "Bearer " + _apiKey);
            try
            {
                using var response = await _http.SendAsync(request, ct);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<List<string>> GenerateLedeAsync(DashboardData data, List<string> fallback, string companyName,CancellationToken ct = default)
        {
            try
            {
                var prompt = BuildPrompt(data, companyName);

                var requestBody = new
                {
                    model = Model,
                    max_tokens = 700,
                    messages = new[]
                    {
                        new { role = "user", content = prompt }
                    }
                };

                var json = JsonSerializer.Serialize(requestBody);

                using var request = new HttpRequestMessage(HttpMethod.Post, ApiUrl);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                using var response = await _http.SendAsync(request, ct);
                var responseBody = await response.Content.ReadAsStringAsync(ct);

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"AI gateway returned {(int)response.StatusCode}: {responseBody}");
                    return fallback;
                }

                var paragraphs = ExtractParagraphs(responseBody);
                return paragraphs.Count > 0 ? paragraphs : fallback;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"AI narrative generation failed, using rule-based summary instead: {ex.Message}");
                return fallback;
            }
        }

        /// <summary>
        /// OpenAI-compatible chat/completions response shape:
        /// { "choices": [ { "message": { "role": "assistant", "content": "..." } } ] }
        /// </summary>
        private static List<string> ExtractParagraphs(string responseBody)
        {
            using var doc = JsonDocument.Parse(responseBody);

            if (!doc.RootElement.TryGetProperty("choices", out var choices) || choices.GetArrayLength() == 0)
                return new List<string>();

            var text = choices[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? "";

            return text
                .Split(new[] { "\r\n\r\n", "\n\n" }, StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .Where(p => p.Length > 0)
                .ToList();
        }

        private static string BuildPrompt(DashboardData d, string companyName)
        {
            var summary = new
            {
                week = new { start = d.Current.WeekStart.ToString("yyyy-MM-dd"), end = d.Current.WeekEnd.ToString("yyyy-MM-dd") },
                totals = new
                {
                    sales = d.Current.TotalSales,
                    priorWeekSales = d.Prior.TotalSales,
                    tons = d.Current.TotalTons,
                    priorWeekTons = d.Prior.TotalTons,
                    tickets = d.Current.TotalTickets,
                    priorWeekTickets = d.Prior.TotalTickets
                },
                plants = d.Plants.Select(p => new
                {
                    p.Plant,
                    sales = p.TotalSales,
                    priorWeekSales = p.PriorTotal,
                    changePercent = p.ChangePercent
                }),
                productGroups = d.Current.ProductGroups.Select(g => new { g.Label, g.Sales, g.Tons }),
                biggestMovers = d.Movers.Take(6).Select(m => new
                {
                    m.CustomerName,
                    priorWeekSales = m.PriorSales,
                    sales = m.CurrentSales,
                    change = m.Change
                }),
                cancelledTickets = new { count = d.Current.CancelledCount, sales = d.Current.CancelledSales }
            };

            var dataJson = JsonSerializer.Serialize(summary, new JsonSerializerOptions { WriteIndented = false });

            return $@"You are writing the short narrative summary at the top of {companyName}'s internal weekly sales report.

Write 2-4 short paragraphs (like a market recap) highlighting the most notable patterns in the data below: the overall sales/tonnage trend versus prior week, which plant(s) moved the most, and which customer(s) drove the biggest swings.

STRICT RULES:
- Use ONLY the numbers and names in the JSON below. Do not invent a job name, project detail, customer reason, or any number not present in the data.
- If you don't know WHY something changed (e.g. a specific job or contract), don't guess — just state what changed.
- Plain prose only. No headers, no bullet points, no markdown formatting.
- Separate paragraphs with a single blank line.
- Keep it tight: this is an executive summary, not a full report.

DATA:
{dataJson}";
        }
    }
}
