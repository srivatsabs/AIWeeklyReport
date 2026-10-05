using Microsoft.Extensions.Configuration;
using Microsoft.Graph.Models.ExternalConnectors;
using Microsoft.IdentityModel.Logging;
using System.Net;
using System.Net.Mail;
using System.Text;

namespace AIWeeklyReport
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            DateTime weekStart = DateTime.Parse("2026-09-27");
            DateTime weekEnd = DateTime.Parse("2026-10-03");
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
            DateTime today = DateTime.Today;

            int daysSinceSunday = (int)today.DayOfWeek;

            DateTime lastSunday = today.AddDays(-daysSinceSunday - 7);
            DateTime lastSaturday = lastSunday.AddDays(6);

            
            
            // -----------------------------------

            var repository = new TicketRepository(connectionString);
            List<CompanyDetails> companies = new List<CompanyDetails>();
            companies = repository.GetCompanyDetails();
            foreach (var company in companies)
            {
                //if (company.CompanyName.Equals("VSS Emultech"))
                //{
                    var currentRaw = repository.GetTickets(weekStart, weekEnd, company.ViewFields);
                    var priorRaw = repository.GetTickets(priorWeekStart, priorWeekEnd, company.ViewFields);

                    var aggregator = new WeekAggregator(company);
                    var current = aggregator.Build(currentRaw, weekStart, weekEnd, company.Location);
                    var prior = aggregator.Build(priorRaw, priorWeekStart, priorWeekEnd, company.Location);

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
                    var spFolderPath = string.IsNullOrWhiteSpace(spBaseFolder) ? $"{company.FolderPath}/{weekStart:yyyy}/{weekStart:MMMM}" : $"{spBaseFolder.Trim('/')}/{company.FolderPath}/{weekStart:yyyy}/{weekStart:MMMM}";

                    if (!string.IsNullOrWhiteSpace(spTenantId) && !string.IsNullOrWhiteSpace(spClientId) &&
                        !string.IsNullOrWhiteSpace(spClientSecret) && !string.IsNullOrWhiteSpace(spSiteHostname) &&
                        !string.IsNullOrWhiteSpace(spSitePath))
                    {
                        try
                        {
                            var uploader = new SharePointUploader(spTenantId, spClientId, spClientSecret, spSiteHostname, spSitePath, spLibraryName);
                            var webUrl = await uploader.UploadFileAsync(fileName, spFolderPath);
                            Console.WriteLine($"Uploaded to SharePoint: {webUrl}");
                            SendEmail(webUrl, config, company, $"{ weekStart: yyyyMMdd} to { weekEnd: yyyyMMdd}");
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
               // }
            }
        }

        private static void SendEmail(string webUrl, IConfigurationRoot config, CompanyDetails company, string week)
        {
           
            try
            {

                string toAddress = config["Email:ToEmail"]?? throw new InvalidOperationException("Email:ToEmail is missing from appsettings.json");
                string bodyMessage = config["Email:BodyMessage"] ?? throw new InvalidOperationException("Email:BodyMessage is missing from appsettings.json");
                bodyMessage = bodyMessage.Replace("{url}", webUrl);
                bodyMessage = bodyMessage.Replace("{company}", company.Company);
                bodyMessage = bodyMessage.Replace("{week}", week);
                string subject = config["Email:Subject"]?? throw new InvalidOperationException("Email:Subject is missing from appsettings.json");
                subject = subject.Replace("{company}", company.Company);
                subject = subject.Replace("{week}", week);
                //string footerMessage = Configuration.GetSection("AppSettings:ManagerMailFooterMessage").Value.ToString();
                string[] toAddressList = toAddress.Split(';');
                MailMessage newMessage = new MailMessage();
                SmtpClient mailService = new SmtpClient();
                newMessage.From = new MailAddress(config["Email:FromAddress"] ?? throw new InvalidOperationException("Email:FromAddress is missing from appsettings.json"));

                foreach (var to in toAddressList)
                {
                    newMessage.To.Add(to);
                }
                //Add BCC in the emails start
                string bccAddress = config["Email:BCCEmail"] ?? throw new InvalidOperationException("Email:BCCEmail is missing from appsettings.json");

                if (!string.IsNullOrEmpty(bccAddress))
                {
                    string[] bccAddressList = bccAddress.Split(';');
                    foreach (var bcc in bccAddressList)
                    {
                        newMessage.Bcc.Add(bcc);
                    }
                }

                ////Add BCC in the emails end

                newMessage.Subject = subject;
                StringBuilder mailBody = new StringBuilder();
                mailBody.AppendFormat("<p>" + bodyMessage + "</p>");
                //mailBody.AppendFormat("<p>" + footerMessage + "</p>");
                newMessage.Body = mailBody.ToString();
                newMessage.IsBodyHtml = true;
                mailService.Port = Convert.ToInt32(config["Email:Port"] ?? throw new InvalidOperationException("Email:Port is missing from appsettings.json"));
                mailService.EnableSsl = true;
                mailService.DeliveryMethod = SmtpDeliveryMethod.Network;

                mailService.Host = config["Email:SMTPHost"] ?? throw new InvalidOperationException("Email:SMTPHost is missing from appsettings.json");
                mailService.UseDefaultCredentials = false;
                mailService.Credentials = new NetworkCredential(config["Email:EmailAdd"], config["Email:EmailPWD"]);
                mailService.Send(newMessage);
                
                


            }
            catch (SmtpException ex)
            {
                
               
            }
            catch (Exception ex)
            {
                

            }
            
        }

        
    }
}
