using Microsoft.Extensions.Configuration;

namespace AIWeeklyReport
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            DateTime weekStart = DateTime.Parse("2026-09-20");
            DateTime weekEnd = DateTime.Parse("2026-09-26");
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
            List<CompanyDetails> companies = new List<CompanyDetails>();
            companies = repository.GetCompanyDetails();
            foreach (var company in companies)
            {
                var currentRaw = repository.GetTickets(weekStart, weekEnd,company.ViewFields);
                var priorRaw = repository.GetTickets(priorWeekStart, priorWeekEnd, company.ViewFields);

                var aggregator = new WeekAggregator(company);
                var current = aggregator.Build(currentRaw, weekStart, weekEnd,company.Location);
                var prior = aggregator.Build(priorRaw, priorWeekStart, priorWeekEnd,company.Location);

                var dashboard = new DashboardBuilder().Build(current, prior);
                // dashboard.LedeParagraphs is already set to the rule-based summary here —
                // it's what ships if the AI call below is skipped or fails.

                var narrativeGenerator = new AiNarrativeGenerator(claudeApiKey);
                dashboard.LedeParagraphs = await narrativeGenerator.GenerateLedeAsync(dashboard, dashboard.LedeParagraphs, company.CompanyName);


                var html = new HtmlReportRenderer().Render(dashboard, company);

                var fileName = $"{company.FileName}_{weekStart:yyyyMMdd}_to_{weekEnd:MMdd}.html";
                File.WriteAllText(fileName, html);

                Console.WriteLine($"Report written to {fileName}");
                if (dashboard.DataQualityFlags.Count > 0)
                {
                    Console.WriteLine($"\n{dashboard.DataQualityFlags.Count} data-quality flag(s) — review before distributing:");
                    foreach (var f in dashboard.DataQualityFlags)
                        Console.WriteLine($"  - {f}");
                }

                // ---- Push the report to SharePoint (app-only auth via Microsoft Graph) ----
                var spTenantId = config["AzureAd:TenantId"] ?? throw new InvalidOperationException("AzureAd:TenantId is missing from appsettings.json");
                var spClientId = config["AzureAd:ClientId"] ?? throw new InvalidOperationException("AzureAd:ClientId is missing from appsettings.json");
                var spClientSecret = config["AzureAd:ClientSecret"] ?? throw new InvalidOperationException("AzureAd:ClientSecret is missing from appsettings.json");
                var spSiteHostname = config["Sharepoint:SiteHostname"] ?? throw new InvalidOperationException("Sharepoint:SiteHostname is missing from appsettings.json");
                var spSitePath = config["Sharepoint:SitePath"] ?? throw new InvalidOperationException("Sharepoint:SitePath is missing from appsettings.json");
                var spBaseFolder = config["Sharepoint:BaseFolder"] ?? throw new InvalidOperationException("Sharepoint:BaseFolder is missing from appsettings.json");
                var spLibraryName = config["Sharepoint:LibraryName"] ?? throw new InvalidOperationException("Sharepoint:LibraryName is missing from appsettings.json");

                // Weekly Reports/{Year}/{Month name} — e.g. "Weekly Reports/2026/August".
                // Uses the week's START date to decide which month a report belongs to; a week
                // that spans a month boundary (e.g. Aug 30–Sep 5) files under the starting month.
                var spFolderPath = string.IsNullOrWhiteSpace(spBaseFolder) ? $"{weekStart:yyyy}/{weekStart:MMMM}" : $"{spBaseFolder.Trim('/')}/{weekStart:yyyy}/{weekStart:MMMM}";

                if (!string.IsNullOrWhiteSpace(spTenantId) && !string.IsNullOrWhiteSpace(spClientId) &&
                    !string.IsNullOrWhiteSpace(spClientSecret) && !string.IsNullOrWhiteSpace(spSiteHostname) &&
                    !string.IsNullOrWhiteSpace(spSitePath))
                {
                    try
                    {
                        var uploader = new SharePointUploader(spTenantId, spClientId, spClientSecret, spSiteHostname, spSitePath, spLibraryName);
                        var webUrl = await uploader.UploadFileAsync(fileName, spFolderPath);
                        Console.WriteLine($"Uploaded to SharePoint: {webUrl}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"SharePoint upload failed (report was still written locally): {ex.Message}");
                    }
                }
                else
                {
                    Console.WriteLine("SharePoint upload skipped — SHAREPOINT_* environment variables not fully set.");
                }
            }
        }
    }
}
