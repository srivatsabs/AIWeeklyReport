using Microsoft.Extensions.Configuration;
namespace AIWeeklyReport
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            // ---- Configure these for each run ----
            //string connectionString = "Server=10.10.3.101\\VISTA;Initial Catalog=BRI_Custom;User ID=SQLReports;Password=%hPsq72G;MultipleActiveResultSets=true;Encrypt=False;TrustServerCertificate=True;";
            DateTime weekStart = DateTime.Parse("2026-08-02");
            DateTime weekEnd = DateTime.Parse("2026-08-08");
            // ----------------------------------------
            var config = new ConfigurationBuilder()
                            .SetBasePath(AppContext.BaseDirectory)
                            .AddJsonFile("appsettings.json", optional: false)
                            .Build();
            var connectionString = config["Database:ConnectionString"]
                                        ?? throw new InvalidOperationException("Database:ConnectionString is missing from appsettings.json");
            var claudeApiKey = config["Claude:ApiKey"] ?? throw new InvalidOperationException("Claude:ApiKey is missing from appsettings.json");
            var priorWeekStart = weekStart.AddDays(-7);
            var priorWeekEnd = weekEnd.AddDays(-7);

            var repository = new TicketRepository(connectionString);
            var currentRaw = repository.GetTickets(weekStart, weekEnd);
            var priorRaw = repository.GetTickets(priorWeekStart, priorWeekEnd);

            var aggregator = new WeekAggregator();
            var current = aggregator.Build(currentRaw, weekStart, weekEnd);
            var prior = aggregator.Build(priorRaw, priorWeekStart, priorWeekEnd);

            var dashboard = new DashboardBuilder().Build(current, prior);
            // dashboard.LedeParagraphs is already set to the rule-based summary here —
            // it's what ships if the AI call below is skipped or fails.

            // ---- AI-written narrative (falls back to the rule-based summary above) ----
            //var apiKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");

            var narrativeGenerator = new AiNarrativeGenerator(claudeApiKey);
            dashboard.LedeParagraphs = await narrativeGenerator.GenerateLedeAsync(dashboard, dashboard.LedeParagraphs);


            var html = new HtmlReportRenderer().Render(dashboard);

            var fileName = $"GRI_Weekly_Sales_{weekStart:yyyyMMdd}_to_{weekEnd:MMdd}.html";
            File.WriteAllText(fileName, html);

            Console.WriteLine($"Report written to {fileName}");
            if (dashboard.DataQualityFlags.Count > 0)
            {
                Console.WriteLine($"\n{dashboard.DataQualityFlags.Count} data-quality flag(s) — review before distributing:");
                foreach (var f in dashboard.DataQualityFlags)
                    Console.WriteLine($"  - {f}");
            }
        }
    }
}
