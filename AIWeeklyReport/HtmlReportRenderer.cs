using System.Globalization;
using System.Text;

namespace AIWeeklyReport
{
    public class HtmlReportRenderer
    {
        private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;

        public string Render(DashboardData data)
        {
            var sb = new StringBuilder();
            sb.Append(HeadAndCss(data));
            sb.Append("<div class=\"wrap\">\n");
            sb.Append(Masthead(data));
            sb.Append(Tiles(data));
            sb.Append(Lede(data));
            sb.Append(SalesByDaySection(data));
            sb.Append(SalesByPlantSection(data));
            sb.Append(SaleTypeAndProductGroupSection(data));
            sb.Append(TopProductsSection(data));
            sb.Append(TopCustomersAndMoversSection(data));
            sb.Append(JobOrdersSection(data));
            sb.Append(CancelledTicketsSection(data));
            sb.Append(Footer(data));
            sb.Append("</div>\n");
            sb.Append(Script(data));
            return sb.ToString();
        }

        // ---------- head / css ----------
        private static string HeadAndCss(DashboardData d)
        {
            return $@"<title>GRI Weekly Sales</title>
<link rel=""stylesheet"" href=""https://fonts.googleapis.com/css2?family=IBM+Plex+Sans+Condensed:wght@500;600;700&family=IBM+Plex+Sans:wght@400;500;600&family=IBM+Plex+Mono:wght@400;500&display=swap"">
<style>
:root{{
  color-scheme:light;
  --ground:#f4f3ee; --surface:#fbfaf7; --surface2:#efede6;
  --ink:#16150f; --ink2:#55534a; --muted:#87857b;
  --line:#ddd9cf; --rule:#c9c5b8; --border:rgba(22,21,15,.12);
  --asphalt:#2a78d6; --rock:#eb6834; --fees:#8f8c82; --prior:#b9b6ab;
  --up:#006300; --down:#d03b3b; --flag:#8a4b00; --flagbg:#fff3df;
  --tipbg:#fbfaf7;
}}
@media (prefers-color-scheme: dark){{
  :root:not([data-theme=""light""]){{
    color-scheme:dark;
    --ground:#131311; --surface:#1d1d1a; --surface2:#26261f;
    --ink:#f5f4ef; --ink2:#c3c1b5; --muted:#8c8a80;
    --line:#33322c; --rule:#45443c; --border:rgba(245,244,239,.12);
    --asphalt:#3987e5; --rock:#d95926; --fees:#8c8a80; --prior:#4d4c44;
    --up:#0ca30c; --down:#e66767; --flag:#f0b25c; --flagbg:#2b2416;
    --tipbg:#26261f;
  }}
}}
:root[data-theme=""dark""]{{
  color-scheme:dark;
  --ground:#131311; --surface:#1d1d1a; --surface2:#26261f;
  --ink:#f5f4ef; --ink2:#c3c1b5; --muted:#8c8a80;
  --line:#33322c; --rule:#45443c; --border:rgba(245,244,239,.12);
  --asphalt:#3987e5; --rock:#d95926; --fees:#8c8a80; --prior:#4d4c44;
  --up:#0ca30c; --down:#e66767; --flag:#f0b25c; --flagbg:#2b2416;
  --tipbg:#26261f;
}}
*{{box-sizing:border-box}}
body{{margin:0;background:var(--ground);color:var(--ink);
  font:14.5px/1.5 ""IBM Plex Sans"",system-ui,-apple-system,""Segoe UI"",sans-serif;
  padding:36px 20px 72px}}
.wrap{{max-width:1120px;margin:0 auto}}
h1,h2,h3{{font-family:""IBM Plex Sans Condensed"",""Arial Narrow"",system-ui,sans-serif;text-wrap:balance;margin:0}}
h1{{font-size:34px;font-weight:700;letter-spacing:-.01em;line-height:1.05}}
h2{{font-size:20px;font-weight:600;letter-spacing:-.005em}}
.mono{{font-family:""IBM Plex Mono"",ui-monospace,Consolas,monospace}}
.eyebrow{{font-size:11.5px;letter-spacing:.09em;text-transform:uppercase;color:var(--muted);font-weight:500}}
.num{{font-variant-numeric:tabular-nums}}
.up{{color:var(--up)}} .down{{color:var(--down)}}

.mast{{display:flex;flex-wrap:wrap;justify-content:space-between;align-items:flex-end;gap:16px 32px;
  padding-bottom:18px;border-bottom:2px solid var(--ink);margin-bottom:22px}}
.mast .period{{font-family:""IBM Plex Sans Condensed"",system-ui,sans-serif;font-size:19px;font-weight:500;color:var(--ink2);margin-top:6px}}
.mast .meta{{text-align:right;color:var(--muted);font-size:12.5px;line-height:1.55}}
.mast .meta b{{color:var(--ink2);font-weight:500}}

.tiles{{display:grid;grid-template-columns:repeat(4,1fr);gap:0;border-top:1px solid var(--rule);border-bottom:1px solid var(--rule);margin-bottom:28px}}
.tile{{padding:16px 18px 16px 0;border-right:1px solid var(--line);margin-right:18px}}
.tile:last-child{{border-right:0;margin-right:0}}
.tile .lab{{font-size:12.5px;color:var(--ink2);margin-bottom:6px}}
.tile .val{{font-family:""IBM Plex Sans Condensed"",system-ui,sans-serif;font-size:36px;font-weight:600;line-height:1;letter-spacing:-.01em}}
.tile .val small{{font-size:17px;font-weight:500;color:var(--muted);margin-left:3px}}
.tile .dl{{font-size:12.5px;margin-top:8px;color:var(--muted)}}
.tile .dl b{{font-weight:600}}
@media (max-width:760px){{.tiles{{grid-template-columns:1fr 1fr}}.tile:nth-child(2){{border-right:0;margin-right:0}}}}

.lede{{max-width:68ch;font-size:15.5px;line-height:1.6;color:var(--ink);margin:0 0 30px}}
.lede p{{margin:0 0 10px}}

section{{margin-bottom:34px}}
.sechead{{display:flex;align-items:baseline;justify-content:space-between;gap:16px;flex-wrap:wrap;margin-bottom:4px}}
.cap{{color:var(--muted);font-size:12.5px;margin:0 0 14px;max-width:80ch}}
.grid2{{display:grid;grid-template-columns:1fr 1fr;gap:28px}}
@media (max-width:860px){{.grid2{{grid-template-columns:1fr}}}}

.chart{{position:relative}}
.legend{{display:flex;gap:18px;flex-wrap:wrap;font-size:12.5px;color:var(--ink2);margin:0 0 10px}}
.legend span{{display:inline-flex;align-items:center;gap:6px}}
.sw{{width:11px;height:11px;border-radius:3px;display:inline-block;flex:none}}
.sw.tick{{width:2px;height:13px;border-radius:0;background:var(--ink)}}
svg{{display:block;width:100%;height:auto;overflow:visible}}
svg text{{font-family:""IBM Plex Sans"",system-ui,sans-serif;fill:var(--ink2)}}
.gridline{{stroke:var(--line);stroke-width:1}}
.axis{{stroke:var(--rule);stroke-width:1}}
.tip{{position:absolute;pointer-events:none;opacity:0;transition:opacity .08s;background:var(--tipbg);border:1px solid var(--border);
  border-radius:6px;padding:7px 10px;font-size:12.5px;line-height:1.45;color:var(--ink);box-shadow:0 4px 14px rgba(0,0,0,.12);white-space:nowrap;z-index:2}}
.tip b{{font-weight:600}}
.tip .k{{color:var(--muted)}}
details{{margin-top:8px}}
summary{{cursor:pointer;font-size:12.5px;color:var(--muted);user-select:none}}
summary:focus-visible{{outline:2px solid var(--asphalt);outline-offset:2px}}

.tblwrap{{overflow-x:auto}}
table{{border-collapse:collapse;width:100%;font-size:13.5px}}
th,td{{padding:7px 10px;text-align:left;vertical-align:top;border-bottom:1px solid var(--line)}}
th{{font-size:11.5px;letter-spacing:.06em;text-transform:uppercase;color:var(--muted);font-weight:500;border-bottom:1px solid var(--rule);white-space:nowrap}}
td.r,th.r{{text-align:right}}
td.r{{font-variant-numeric:tabular-nums}}
tr.total td{{font-weight:600;border-top:1px solid var(--rule);border-bottom:0}}
td .sub{{display:block;color:var(--muted);font-size:12px}}
td.code{{font-family:""IBM Plex Mono"",ui-monospace,monospace;font-size:12.5px;color:var(--ink2);white-space:nowrap}}
.bar{{display:inline-block;height:9px;border-radius:2px;vertical-align:middle;margin-right:6px}}
.chip{{display:inline-block;font-size:11px;padding:1px 7px;border-radius:999px;background:var(--surface2);color:var(--ink2);font-weight:500;letter-spacing:.02em;white-space:nowrap}}
.chip.a{{background:color-mix(in srgb,var(--asphalt) 16%,transparent);color:var(--asphalt)}}
.chip.r{{background:color-mix(in srgb,var(--rock) 18%,transparent);color:var(--rock)}}

.note{{background:var(--flagbg);border-left:3px solid var(--flag);padding:10px 14px;border-radius:0 6px 6px 0;font-size:13.5px;color:var(--ink);margin:0 0 14px;max-width:80ch}}
.note b{{color:var(--flag)}}
.foot{{border-top:1px solid var(--rule);padding-top:14px;color:var(--muted);font-size:12.5px;line-height:1.6;max-width:90ch}}
.foot code{{font-family:""IBM Plex Mono"",ui-monospace,monospace;font-size:12px;background:var(--surface2);padding:1px 5px;border-radius:3px;color:var(--ink2)}}
@media (prefers-reduced-motion:reduce){{.tip{{transition:none}}}}
@media print{{body{{padding:0;background:#fff}}.tip{{display:none}}}}
</style>
";
        }

        // ---------- sections ----------
        private static string Masthead(DashboardData d)
        {
            var c = d.Current;
            return $@"<header class=""mast"">
  <div>
    <div class=""eyebrow"">George Reed Inc. &middot; Weekly Sales Report</div>
    <h1>GRI Weekly Sales</h1>
    <div class=""period"">Week of {c.WeekStart:dddd, MMMM d} &ndash; {c.WeekEnd:dddd, MMMM d, yyyy}</div>
  </div>
  <div class=""meta"">
    <div>Source <b>DW_Reports.dbo.vw_GRI_Daily_Tickets</b></div>
    <div>Pulled <b>{DateTime.Now:MMM d, yyyy}</b> &middot; compared with <b>{d.Prior.WeekStart:MMM d} &ndash; {d.Prior.WeekEnd:MMM d}</b></div>
    <div>Active tickets only &middot; {c.CancelledCount} cancelled ticket{(c.CancelledCount == 1 ? "" : "s")} excluded</div>
  </div>
</header>
";
        }

        private static string Tiles(DashboardData d)
        {
            var sb = new StringBuilder("<div class=\"tiles\">\n");
            foreach (var t in d.Tiles)
            {
                sb.Append($@"  <div class=""tile"">
    <div class=""lab"">{t.Label}</div>
    <div class=""val num"">{t.DisplayValue}</div>
    <div class=""dl"">{ChangeBadge(t.ChangePercent)} vs {t.PriorDisplayValue} prior week</div>
  </div>
");
            }
            sb.Append("</div>\n");
            return sb.ToString();
        }

        private static string Lede(DashboardData d)
        {
            var sb = new StringBuilder("<div class=\"lede\">\n");
            foreach (var p in d.LedeParagraphs)
                sb.Append($"  <p>{p}</p>\n");
            sb.Append("</div>\n");
            return sb.ToString();
        }

        private static string SalesByDaySection(DashboardData d)
        {
            var sb = new StringBuilder();
            sb.Append(@"<section>
  <div class=""sechead""><h2>Sales by day</h2></div>
  <p class=""cap"">Ticket price per day, this week beside the prior week.</p>
  <div class=""legend""><span><i class=""sw"" style=""background:var(--asphalt)""></i>This week</span><span><i class=""sw"" style=""background:var(--prior)""></i>Prior week</span></div>
  <div class=""chart"" id=""daily""></div>
  <details><summary>Show daily table</summary>
    <div class=""tblwrap""><table>
      <tr><th>Day</th><th class=""r"">Tickets</th><th class=""r"">Tons</th><th class=""r"">Sales</th><th class=""r"">Prior wk sales</th><th class=""r"">Change</th></tr>
");
            foreach (var day in d.Days)
            {
                sb.Append($@"      <tr><td>{day.Label}</td><td class=""r"">{day.Tickets:N0}</td><td class=""r"">{day.Tons:N0}</td><td class=""r"">{Money(day.Sales)}</td><td class=""r"">{Money(day.PriorSales)}</td><td class=""r"">{ChangeText(day.ChangePercent)}</td></tr>
");
            }
            var weekChange = d.Prior.TotalSales == 0 ? (decimal?)null : Math.Round((d.Current.TotalSales - d.Prior.TotalSales) / d.Prior.TotalSales * 100, 1);
            sb.Append($@"      <tr class=""total""><td>Week</td><td class=""r"">{d.Current.TotalTickets:N0}</td><td class=""r"">{d.Current.TotalTons:N0}</td><td class=""r"">{Money(d.Current.TotalSales)}</td><td class=""r"">{Money(d.Prior.TotalSales)}</td><td class=""r"">{ChangeText(weekChange)}</td></tr>
    </table></div>
  </details>
</section>
");
            return sb.ToString();
        }

        private static string SalesByPlantSection(DashboardData d)
        {
            var sb = new StringBuilder();
            sb.Append(@"<section>
  <div class=""sechead""><h2>Sales by plant</h2></div>
  <p class=""cap"">This week's sales split by product group, with the prior week's total marked for comparison.</p>
  <div class=""legend""><span><i class=""sw"" style=""background:var(--asphalt)""></i>Asphalt</span><span><i class=""sw"" style=""background:var(--rock)""></i>Rock plant</span><span><i class=""sw"" style=""background:var(--fees)""></i>Unclassified</span><span><i class=""sw tick""></i>Prior week total</span></div>
  <div class=""chart"" id=""plants""></div>
  <div class=""tblwrap"" style=""margin-top:14px""><table>
    <tr><th>Plant</th><th class=""r"">Sales</th><th class=""r"">Prior week</th><th class=""r"">Change</th><th class=""r"">Tons</th><th class=""r"">Tickets</th><th class=""r"">$ / ton</th></tr>
");
            foreach (var p in d.Plants)
            {
                var sub = $"Asphalt {Money(p.AsphaltSales)}, rock {Money(p.RockSales)}" + (p.OtherSales > 0 ? $", other {Money(p.OtherSales)}" : "");
                sb.Append($@"    <tr><td>{Html(p.Plant)} <span class=""sub"">{sub}</span></td><td class=""r"">{Money(p.TotalSales)}</td><td class=""r"">{Money(p.PriorTotal)}</td><td class=""r"">{ChangeText(p.ChangePercent)}</td><td class=""r"">{p.Tons:N0}</td><td class=""r"">{p.Tickets:N0}</td><td class=""r"">${p.DollarsPerTon:N2}</td></tr>
");
            }
            var weekChange = d.Prior.TotalSales == 0 ? (decimal?)null : Math.Round((d.Current.TotalSales - d.Prior.TotalSales) / d.Prior.TotalSales * 100, 1);
            sb.Append($@"    <tr class=""total""><td>All plants</td><td class=""r"">{Money(d.Current.TotalSales)}</td><td class=""r"">{Money(d.Prior.TotalSales)}</td><td class=""r"">{ChangeText(weekChange)}</td><td class=""r"">{d.Current.TotalTons:N0}</td><td class=""r"">{d.Current.TotalTickets:N0}</td><td class=""r"">${d.Current.SalesPerTon:N2}</td></tr>
  </table></div>
</section>
");
            return sb.ToString();
        }

        private static string SaleTypeAndProductGroupSection(DashboardData d)
        {
            var c = d.Current;
            var sb = new StringBuilder();
            sb.Append(@"<section class=""grid2"">
  <div>
    <div class=""sechead""><h2>Sale type</h2></div>
    <p class=""cap"">Customer sales carry tax; job and inventory tickets do not. Inventory is material moved to another GRI plant.</p>
    <div class=""tblwrap""><table>
      <tr><th>Type</th><th class=""r"">Tickets</th><th class=""r"">Tons</th><th class=""r"">Sales</th><th class=""r"">Share</th></tr>
");
            foreach (var st in c.SaleTypes)
            {
                var share = c.TotalSales == 0 ? 0 : Math.Round(st.Sales / c.TotalSales * 100, 1);
                sb.Append($@"      <tr><td>{Html(st.Type)}</td><td class=""r"">{st.Tickets:N0}</td><td class=""r"">{st.Tons:N0}</td><td class=""r"">{Money(st.Sales)}</td><td class=""r"">{share:0.0}%</td></tr>
");
            }
            sb.Append($@"      <tr class=""total""><td>Total</td><td class=""r"">{c.TotalTickets:N0}</td><td class=""r"">{c.TotalTons:N0}</td><td class=""r"">{Money(c.TotalSales)}</td><td class=""r"">100%</td></tr>
    </table></div>
    <p class=""cap"" style=""margin-top:10px"">Sales tax collected on customer tickets: {Money(c.CustomerTaxCollected)}.</p>
  </div>
  <div>
    <div class=""sechead""><h2>Product group</h2></div>
    <p class=""cap"">Sales and tonnage by product group.</p>
    <div class=""tblwrap""><table>
      <tr><th>Group</th><th class=""r"">Tons</th><th class=""r"">Sales</th><th class=""r"">$ / ton</th></tr>
");
            foreach (var pg in c.ProductGroups)
            {
                var chip = pg.Label == "Asphalt" ? "<span class=\"chip a\">Asphalt</span>"
                         : pg.Label == "Rock plant" ? "<span class=\"chip r\">Rock plant</span>"
                         : Html(pg.Label);
                var perTon = pg.Tons == 0 ? "&mdash;" : ("$" + (pg.Sales / pg.Tons).ToString("N2", Culture));
                sb.Append($@"      <tr><td>{chip}</td><td class=""r"">{pg.Tons:N0}</td><td class=""r"">{Money(pg.Sales)}</td><td class=""r"">{perTon}</td></tr>
");
            }
            sb.Append(@"    </table></div>
  </div>
</section>
");
            return sb.ToString();
        }

        private static string TopProductsSection(DashboardData d)
        {
            var products = d.Current.Products.Take(12).ToList();
            var maxSales = products.Count > 0 ? products.Max(p => p.Sales) : 1m;
            var sb = new StringBuilder(@"<section>
  <div class=""sechead""><h2>Top products</h2></div>
  <p class=""cap"">Ranked by sales. Bars show sales relative to the top product.</p>
  <div class=""tblwrap""><table>
    <tr><th>Product</th><th>Code</th><th class=""r"">Lines</th><th class=""r"">Tons</th><th class=""r"">Avg $/ton</th><th class=""r"">Sales</th></tr>
");
            foreach (var p in products)
            {
                var avgPerTon = p.Tons == 0 ? 0 : p.Sales / p.Tons;
                var barColor = p.GroupId == "ASPHALT" ? "var(--asphalt)" : p.GroupId == "ROCKPLANT" ? "var(--rock)" : "var(--fees)";
                var barWidth = maxSales == 0 ? 0 : Math.Max(2, Math.Round(p.Sales / maxSales * 110));
                sb.Append($@"    <tr><td>{Html(p.Description)}</td><td class=""code"">{Html(p.ProductId)}</td><td class=""r"">{p.Lines:N0}</td><td class=""r"">{p.Tons:N0}</td><td class=""r"">${avgPerTon:N2}</td><td class=""r""><i class=""bar"" style=""background:{barColor};width:{barWidth}px""></i>{Money(p.Sales)}</td></tr>
");
            }
            sb.Append(@"  </table></div>
</section>
");
            return sb.ToString();
        }

        private static string TopCustomersAndMoversSection(DashboardData d)
        {
            var sb = new StringBuilder(@"<section class=""grid2"">
  <div>
    <div class=""sechead""><h2>Top customers</h2></div>
    <p class=""cap"">By sales this week, across all sale types.</p>
    <div class=""tblwrap""><table>
      <tr><th>Customer</th><th class=""r"">Tickets</th><th class=""r"">Tons</th><th class=""r"">Sales</th></tr>
");
            foreach (var c in d.Current.Customers.Take(12))
            {
                var sub = string.IsNullOrEmpty(c.Tag) ? "" : $" <span class=\"sub\">{Html(c.Tag)}</span>";
                sb.Append($@"      <tr><td>{Html(c.CustomerName)}{sub}</td><td class=""r"">{c.Tickets:N0}</td><td class=""r"">{c.Tons:N0}</td><td class=""r"">{Money(c.Sales)}</td></tr>
");
            }
            sb.Append(@"    </table></div>
  </div>
  <div>
    <div class=""sechead""><h2>Biggest movers</h2></div>
    <p class=""cap"">Largest week-over-week changes in customer sales.</p>
    <div class=""tblwrap""><table>
      <tr><th>Customer</th><th class=""r"">Prior week</th><th class=""r"">This week</th><th class=""r"">Change</th></tr>
");
            foreach (var m in d.Movers)
            {
                var cls = m.Change >= 0 ? "up" : "down";
                var sign = m.Change >= 0 ? "+" : "&minus;";
                sb.Append($@"      <tr><td>{Html(m.CustomerName)}</td><td class=""r"">{Money(m.PriorSales)}</td><td class=""r"">{Money(m.CurrentSales)}</td><td class=""r {cls}"">{sign}{Money(Math.Abs(m.Change))}</td></tr>
");
            }
            sb.Append(@"    </table></div>
  </div>
</section>
");
            return sb.ToString();
        }

        private static string JobOrdersSection(DashboardData d)
        {
            var sb = new StringBuilder(@"<section>
  <div class=""sechead""><h2>Job orders</h2></div>
  <p class=""cap"">Material ticketed to GRI job numbers this week, ranked by sales.</p>
  <div class=""tblwrap""><table>
    <tr><th>Job</th><th>Description</th><th>Plant</th><th class=""r"">Tickets</th><th class=""r"">Tons</th><th class=""r"">Sales</th></tr>
");
            foreach (var j in d.Current.JobOrders)
            {
                sb.Append($@"    <tr><td class=""code"">{Html(j.JobCode)}</td><td>{Html(j.Description)}</td><td>{Html(j.Plant)}</td><td class=""r"">{j.Tickets:N0}</td><td class=""r"">{j.Tons:N0}</td><td class=""r"">{Money(j.Sales)}</td></tr>
");
            }
            sb.Append(@"  </table></div>
</section>
");
            return sb.ToString();
        }

        private static string CancelledTicketsSection(DashboardData d)
        {
            var c = d.Current;
            var sb = new StringBuilder(@"<section>
  <div class=""sechead""><h2>Cancelled tickets</h2></div>
  <p class=""cap"">Tickets with void status C in the period. These are excluded from every figure above.</p>
");
            if (c.CancelledCount == 0)
            {
                sb.Append("  <div class=\"note\"><b>No cancelled tickets this week.</b></div>\n</section>\n");
                return sb.ToString();
            }
            sb.Append($@"  <div class=""note""><b>{c.CancelledCount} tickets, {Money(c.CancelledSales)}, {c.CancelledTons:N0} tons.</b></div>
  <div class=""tblwrap""><table>
    <tr><th>Ticket</th><th>Date</th><th>Plant</th><th>Customer</th><th>Material</th><th class=""r"">Tons</th><th class=""r"">Price</th></tr>
");
            foreach (var t in c.CancelledTickets)
            {
                sb.Append($@"    <tr><td class=""code"">{Html(t.TicketNo)}</td><td>{t.Date:MMM d}</td><td>{Html(t.Plant)}</td><td>{Html(t.Customer)}</td><td>{Html(t.Material)}</td><td class=""r"">{t.Tons:N2}</td><td class=""r"">{Money(t.Price)}</td></tr>
");
            }
            sb.Append("  </table></div>\n</section>\n");
            return sb.ToString();
        }

        private static string Footer(DashboardData d)
        {
            var c = d.Current;
            var flagsNote = d.DataQualityFlags.Count > 0
                ? $" {d.DataQualityFlags.Count} data-quality flag(s) were raised during generation and should be reviewed before distribution."
                : "";
            return $@"<div class=""foot"">
  <p>Built from the query <code>SELECT * FROM DW_Reports.[dbo].[vw_GRI_Daily_Tickets] WHERE TicketDate &gt;= '{c.WeekStart:MM/dd/yyyy}' AND TicketDate &lt;= '{c.WeekEnd:MM/dd/yyyy}'</code>.
  Sales is the sum of the Price column (material and fees, before tax). Tons is the sum of Qty (all rows use unit 'Ton').
  Prior-week figures use the same view for {d.Prior.WeekStart:MMM d} &ndash; {d.Prior.WeekEnd:MMM d}.{flagsNote}</p>
</div>
";
        }

        // ---------- script ----------
        private static string Script(DashboardData d)
        {
            var dayLabels = string.Join(",", d.Days.Select(x => "'" + x.Label.Replace("'", "") + "'"));
            var tw = string.Join(",", d.Days.Select(x => x.Sales.ToString("0.00", Culture)));
            var pw = string.Join(",", d.Days.Select(x => x.PriorSales.ToString("0.00", Culture)));
            var tix = string.Join(",", d.Days.Select(x => x.Tickets));

            var maxDay = Math.Max(1m, d.Days.Count > 0 ? d.Days.Max(x => Math.Max(x.Sales, x.PriorSales)) : 1m);
            var dayAxisMax = RoundUpToStep(maxDay, out var dayStep);

            var plantRows = string.Join(",\n      ", d.Plants.Select(p =>
                $"{{n:'{Html(p.Plant).Replace("'", "")}', a:{p.AsphaltSales.ToString("0.00", Culture)}, r:{p.RockSales.ToString("0.00", Culture)}, o:{p.OtherSales.ToString("0.00", Culture)}, p:{p.PriorTotal.ToString("0.00", Culture)}}}"));

            var maxPlant = Math.Max(1m, d.Plants.Count > 0 ? d.Plants.Max(p => Math.Max(p.TotalSales, p.PriorTotal)) : 1m);
            var plantAxisMax = RoundUpToStep(maxPlant, out var plantStep);

            return $@"<script>
(function(){{
  var fmt$ = function(v){{ return '$' + Math.round(v).toLocaleString('en-US'); }};
  var fmtK = function(v){{ return v >= 1e6 ? '$' + (v/1e6).toFixed(1) + 'M' : '$' + Math.round(v/1e3) + 'K'; }};
  var NS = 'http://www.w3.org/2000/svg';
  function el(n, a, txt){{ var e = document.createElementNS(NS, n); for (var k in a) e.setAttribute(k, a[k]); if (txt != null) e.textContent = txt; return e; }}
  function tip(host){{ var t = document.createElement('div'); t.className = 'tip'; host.appendChild(t); return t; }}
  function showTip(t, host, x, y, html){{
    t.innerHTML = html; t.style.opacity = 1;
    var r = host.getBoundingClientRect(), w = t.offsetWidth;
    var left = x - w/2; if (left < 0) left = 0; if (left + w > r.width) left = r.width - w;
    t.style.left = left + 'px'; t.style.top = (y - t.offsetHeight - 10) + 'px';
  }}

  (function(){{
    var days = [{dayLabels}];
    var tw = [{tw}];
    var pw = [{pw}];
    var tix = [{tix}];
    var W = 1080, H = 300, padL = 56, padR = 12, padT = 22, padB = 34;
    var plotW = W - padL - padR, plotH = H - padT - padB;
    var max = {dayAxisMax.ToString("0", Culture)}, step = {dayStep.ToString("0", Culture)};
    var svg = el('svg', {{viewBox: '0 0 ' + W + ' ' + H, role: 'img', 'aria-label': 'Sales by day, this week versus prior week'}});
    var y = function(v){{ return padT + plotH - (v/max)*plotH; }};
    for (var g = 0; g <= max; g += step){{
      svg.appendChild(el('line', {{x1: padL, x2: W - padR, y1: y(g), y2: y(g), 'class': g === 0 ? 'axis' : 'gridline'}}));
      svg.appendChild(el('text', {{x: padL - 8, y: y(g) + 4, 'text-anchor': 'end', 'font-size': 11.5}}, g === 0 ? '0' : '$' + (g/1000) + 'K'));
    }}
    var host = document.getElementById('daily'); host.appendChild(svg); var t = tip(host);
    var slot = plotW / days.length, bw = Math.min(44, slot * 0.32), gap = 3;
    days.forEach(function(d, i){{
      var cx = padL + slot * i + slot/2;
      var xP = cx - bw - gap/2, xT = cx + gap/2;
      var pr = el('rect', {{x: xP, y: y(pw[i]), width: bw, height: Math.max(0, y(0) - y(pw[i])), fill: 'var(--prior)', rx: 3}});
      var tr = el('rect', {{x: xT, y: y(tw[i]), width: bw, height: Math.max(0, y(0) - y(tw[i])), fill: 'var(--asphalt)', rx: 3}});
      svg.appendChild(pr); svg.appendChild(tr);
      if (tw[i] > 0) svg.appendChild(el('text', {{x: xT + bw/2, y: y(tw[i]) - 6, 'text-anchor': 'middle', 'font-size': 11.5, 'font-weight': 600, fill: 'var(--ink)'}}, fmtK(tw[i])));
      else svg.appendChild(el('text', {{x: xT + bw/2, y: y(0) - 6, 'text-anchor': 'middle', 'font-size': 11, fill: 'var(--muted)'}}, 'none'));
      svg.appendChild(el('text', {{x: cx, y: H - 12, 'text-anchor': 'middle', 'font-size': 12}}, d));
      var hit = el('rect', {{x: padL + slot * i, y: padT, width: slot, height: plotH, fill: 'transparent'}});
      hit.addEventListener('mousemove', function(ev){{
        var r = host.getBoundingClientRect();
        var idx = Math.floor((ev.clientX - r.left - padL) / slot);
        if (idx < 0) idx = 0; if (idx >= days.length) idx = days.length - 1;
        var chg = pw[idx] > 0 ? Math.round((tw[idx]-pw[idx])/pw[idx]*100) : null;
        showTip(t, host, ev.clientX - r.left, ev.clientY - r.top,
          '<b>' + days[idx] + '</b><br><span class=""k"">This week</span> ' + fmt$(tw[idx]) + ' &middot; ' + tix[idx].toLocaleString() + ' tickets<br><span class=""k"">Prior week</span> ' + fmt$(pw[idx]) + (chg === null ? '' : '<br><span class=""k"">Change</span> ' + (chg >= 0 ? '+' : '') + chg + '%'));
      }});
      hit.addEventListener('mouseleave', function(){{ t.style.opacity = 0; }});
      svg.appendChild(hit);
    }});
  }})();

  (function(){{
    var rows = [
      {plantRows}
    ];
    var W = 1080, labW = 150, padR = 70, rowH = 40, padT = 8, padB = 30;
    var H = padT + rows.length * rowH + padB, plotW = W - labW - padR;
    var max = {plantAxisMax.ToString("0", Culture)}, step = {plantStep.ToString("0", Culture)};
    var x = function(v){{ return labW + (v/max)*plotW; }};
    var svg = el('svg', {{viewBox: '0 0 ' + W + ' ' + H, role: 'img', 'aria-label': 'Sales by plant and product group'}});
    for (var g = 0; g <= max; g += step){{
      svg.appendChild(el('line', {{x1: x(g), x2: x(g), y1: padT, y2: padT + rows.length*rowH, 'class': g === 0 ? 'axis' : 'gridline'}}));
      svg.appendChild(el('text', {{x: x(g), y: H - 8, 'text-anchor': 'middle', 'font-size': 11.5}}, g === 0 ? '0' : '$' + (g/1000000).toFixed(1) + 'M'));
    }}
    var host = document.getElementById('plants'); host.appendChild(svg); var t = tip(host);
    rows.forEach(function(rw, i){{
      var yy = padT + i*rowH + 8, bh = rowH - 16, cur = 0;
      svg.appendChild(el('text', {{x: labW - 12, y: yy + bh/2 + 4, 'text-anchor': 'end', 'font-size': 13, fill: 'var(--ink)'}}, rw.n));
      [['a','var(--asphalt)'],['r','var(--rock)'],['o','var(--fees)']].forEach(function(s){{
        var v = rw[s[0]]; if (v <= 0) return;
        var x0 = x(cur), x1 = x(cur + v);
        svg.appendChild(el('rect', {{x: x0 + (cur > 0 ? 1 : 0), y: yy, width: Math.max(0.5, x1 - x0 - (cur > 0 ? 1 : 0)), height: bh, fill: s[1], rx: 2}}));
        cur += v;
      }});
      var tot = rw.a + rw.r + rw.o;
      svg.appendChild(el('text', {{x: x(tot) + 8, y: yy + bh/2 + 4, 'font-size': 12, 'font-weight': 600, fill: 'var(--ink)'}}, fmtK(tot)));
      if (rw.p > 0) svg.appendChild(el('line', {{x1: x(rw.p), x2: x(rw.p), y1: yy - 4, y2: yy + bh + 4, stroke: 'var(--ink)', 'stroke-width': 2}}));
      var hit = el('rect', {{x: labW, y: padT + i*rowH, width: plotW + padR, height: rowH, fill: 'transparent'}});
      hit.addEventListener('mousemove', function(ev){{
        var rr = host.getBoundingClientRect();
        var chg = rw.p > 0 ? Math.round((tot - rw.p)/rw.p*100) : null;
        showTip(t, host, ev.clientX - rr.left, ev.clientY - rr.top,
          '<b>' + rw.n + '</b><br><span class=""k"">Asphalt</span> ' + fmt$(rw.a) + '<br><span class=""k"">Rock plant</span> ' + fmt$(rw.r) + (rw.o > 0 ? '<br><span class=""k"">Other</span> ' + fmt$(rw.o) : '') + '<br><span class=""k"">Total</span> ' + fmt$(tot) + (chg === null ? '' : ' (' + (chg >= 0 ? '+' : '') + chg + '% vs prior ' + fmt$(rw.p) + ')'));
      }});
      hit.addEventListener('mouseleave', function(){{ t.style.opacity = 0; }});
      svg.appendChild(hit);
    }});
  }})();
}})();
</script>
";
        }

        // ---------- helpers ----------
        private static decimal RoundUpToStep(decimal max, out decimal step)
        {
            // Pick a "nice" axis max/step similar to the hand-authored template (e.g. 800K/200K, 1.4M/200K).
            var target = max * 1.15m;
            decimal[] steps = { 10, 25, 50, 100, 250, 500, 1000, 2500, 5000, 10000, 25000, 50000, 100000, 200000, 250000, 500000, 1000000 };
            step = steps.Last();
            foreach (var s in steps)
            {
                if (target / s <= 6) { step = s; break; }
            }
            var axisMax = Math.Ceiling(target / step) * step;
            return axisMax <= 0 ? step * 4 : axisMax;
        }

        private static string Money(decimal v) => "$" + Math.Round(v).ToString("N0", Culture);

        private static string ChangeBadge(decimal? pct)
        {
            if (!pct.HasValue) return "<b>&mdash;</b>";
            var cls = pct >= 0 ? "up" : "down";
            var arrow = pct >= 0 ? "&#9650;" : "&#9660;";
            return $"<b class=\"{cls}\">{arrow} {Math.Abs(pct.Value):0.0}%</b>";
        }

        private static string ChangeText(decimal? pct)
        {
            if (!pct.HasValue) return "&mdash;";
            var cls = pct >= 0 ? "up" : "down";
            var sign = pct >= 0 ? "+" : "&minus;";
            return $"<span class=\"r {cls}\">{sign}{Math.Abs(pct.Value):0.0}%</span>";
        }

        private static string Html(string? s) => System.Net.WebUtility.HtmlEncode(s ?? "");
    }
}
