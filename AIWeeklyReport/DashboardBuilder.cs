using DocumentFormat.OpenXml.ExtendedProperties;
using System.Globalization;

namespace AIWeeklyReport
{
    public class DashboardBuilder
    {
        private const int TopCustomerCount = 12;
        private const int TopMoverCount = 12;
        private const int TopProductCount = 12;

        public DashboardData Build(WeekAggregate current, WeekAggregate prior, string companyName)
        {
            var data = new DashboardData { Current = current, Prior = prior };

            
           
            if (companyName.Equals("GRI"))
            {
                data.TilesAsphalt = new List<TileRow>
            {
                Tile("Sales (ticket price)", current.TotalSalesByAsphalt, prior.TotalTicketsByAsphalt, MoneyCompact),
                Tile("Tons shipped", current.TotalTonsByAsphalt, prior.TotalTonsByAsphalt, n => Math.Round(n).ToString("N0", CultureInfo.InvariantCulture)),
                Tile("Tickets", current.TotalTicketsByAsphalt, prior.TotalTicketsByAsphalt, n => Math.Round(n).ToString("N0", CultureInfo.InvariantCulture)),
                Tile("Sales per ton", current.SalesPerTonByAsphalt, prior.SalesPerTonByAsphalt, n => "$" + n.ToString("N2", CultureInfo.InvariantCulture))
            };
                data.TilesRockPlant = new List<TileRow>
            {
                Tile("Sales (ticket price)", current.TotalSalesByRockplant, prior.TotalSalesByRockplant, MoneyCompact),
                Tile("Tons shipped", current.TotalTonsByRockplant, prior.TotalTonsByRockplant, n => Math.Round(n).ToString("N0", CultureInfo.InvariantCulture)),
                Tile("Tickets", current.TotalTicketsByRockplant, prior.TotalTicketsByRockplant, n => Math.Round(n).ToString("N0", CultureInfo.InvariantCulture)),
                Tile("Sales per ton", current.SalesPerTonByRockplant, prior.SalesPerTonByRockplant, n => "$" + n.ToString("N2", CultureInfo.InvariantCulture))
            };
            }
            else
            {
                data.Tiles = new List<TileRow>
            {
                Tile("Sales (ticket price)", current.TotalSales, prior.TotalSales, MoneyCompact),
                Tile("Tons shipped", current.TotalTons, prior.TotalTons, n => Math.Round(n).ToString("N0", CultureInfo.InvariantCulture)),
                Tile("Tickets", current.TotalTickets, prior.TotalTickets, n => Math.Round(n).ToString("N0", CultureInfo.InvariantCulture)),
                Tile("Sales per ton", current.SalesPerTon, prior.SalesPerTon, n => "$" + n.ToString("N2", CultureInfo.InvariantCulture))
            };
            }
                // --- Days: match current[i] to prior[i] by position (both run Sun..Sat) ---
                for (int i = 0; i < current.Days.Count; i++)
                {
                    var c = current.Days[i];
                    var p = i < prior.Days.Count ? prior.Days[i] : new DayAgg();
                    data.Days.Add(new DayRow
                    {
                        Label = c.Date.ToString("ddd MMM d", CultureInfo.InvariantCulture),
                        Tickets = c.Tickets,
                        Tons = c.Tons,
                        Sales = c.Sales,
                        PriorSales = p.Sales,
                        ChangePercent = PercentChange(c.Sales, p.Sales)
                    });
                }

            // --- Plants: match by name, prior defaults to 0 if plant didn't appear last week ---
            var priorPlants = prior.Plants.ToDictionary(p => p.Plant, p => p.TotalSales);
            data.Plants = current.Plants
                .Select(c =>
                {
                    var priorTotal = priorPlants.TryGetValue(c.Plant, out var pv) ? pv : 0m;
                    return new PlantRow
                    {
                        Plant = c.Plant,
                        AsphaltSales = c.AsphaltSales,
                        RockSales = c.RockSales,
                        OtherSales = c.OtherSales,
                        Tons = c.Tons,
                        Tickets = c.Tickets,
                        PriorTotal = priorTotal,
                        ChangePercent = PercentChange(c.TotalSales, priorTotal)
                    };
                })
                .OrderByDescending(p => p.TotalSales)
                .ToList();

            // --- Biggest movers: union of customers seen in either week, matched by CustomerId ---
            var currentByCustomer = current.Customers.ToDictionary(c => c.CustomerId, c => c);
            var priorByCustomer = prior.Customers.ToDictionary(c => c.CustomerId, c => c);
            var allCustomerIds = currentByCustomer.Keys.Union(priorByCustomer.Keys);

            var allMovers = allCustomerIds.Select(id =>
            {
                var name = currentByCustomer.TryGetValue(id, out var cc) ? cc.CustomerName
                         : priorByCustomer[id].CustomerName;
                var curSales = currentByCustomer.TryGetValue(id, out var cc2) ? cc2.Sales : 0m;
                var priorSales = priorByCustomer.TryGetValue(id, out var pc) ? pc.Sales : 0m;
                return new MoverRow { CustomerName = name, CurrentSales = curSales, PriorSales = priorSales };
            }).ToList();

            var risers = allMovers.Where(m => m.Change > 0).OrderByDescending(m => m.Change).Take(TopMoverCount / 2);
            var fallers = allMovers.Where(m => m.Change < 0).OrderBy(m => m.Change).Take(TopMoverCount / 2);
            data.Movers = risers.Concat(fallers).OrderByDescending(m => m.Change).ToList();

            data.LedeParagraphs = BuildLede(data);

            return data;
        }

        private static TileRow Tile(string label, decimal current, decimal prior, Func<decimal, string> format)
        {
            return new TileRow
            {
                Label = label,
                DisplayValue = format(current),
                PriorDisplayValue = format(prior),
                ChangePercent = PercentChange(current, prior)
            };
        }

        private static decimal? PercentChange(decimal current, decimal prior)
        {
            if (prior == 0) return null;
            return Math.Round((current - prior) / prior * 100, 1);
        }

        private static string MoneyCompact(decimal v)
        {
            if (Math.Abs(v) >= 1_000_000m) return "$" + (v / 1_000_000m).ToString("N2", CultureInfo.InvariantCulture) + "M";
            if (Math.Abs(v) >= 1_000m) return "$" + (v / 1_000m).ToString("N0", CultureInfo.InvariantCulture) + "K";
            return "$" + v.ToString("N0", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Rule-based summary built only from figures actually computed above — no fabricated
        /// claims about specific jobs or causes the data can't support on its own. Used as the
        /// default lede, and as the fallback if the AI narrative generator is unavailable or fails.
        /// </summary>
        public static List<string> BuildLede(DashboardData data)
        {
            var paras = new List<string>();
            var salesChange = PercentChange(data.Current.TotalSales, data.Prior.TotalSales);
            var tonsChange = PercentChange(data.Current.TotalTons, data.Prior.TotalTons);

            if (salesChange.HasValue && tonsChange.HasValue)
            {
                var direction = salesChange >= 0 ? "rose" : "fell";
                var tonDirection = tonsChange >= 0 ? "rose" : "fell";
                paras.Add($"Sales {direction} {Math.Abs(salesChange.Value):0.0}% while tons shipped {tonDirection} " +
                          $"{Math.Abs(tonsChange.Value):0.0}% versus the prior week.");
            }

            var topPlant = data.Plants.OrderByDescending(p => Math.Abs(p.ChangePercent ?? 0)).FirstOrDefault();
            if (topPlant != null && topPlant.ChangePercent.HasValue)
            {
                var dir = topPlant.ChangePercent >= 0 ? "gained the most" : "gave back the most";
                paras.Add($"{topPlant.Plant} {dir}, moving from {MoneyCompact(topPlant.PriorTotal)} to " +
                          $"{MoneyCompact(topPlant.TotalSales)} ({(topPlant.ChangePercent >= 0 ? "+" : "")}{topPlant.ChangePercent:0.0}%).");
            }

            var topMover = data.Movers.OrderByDescending(m => Math.Abs(m.Change)).FirstOrDefault();
            if (topMover != null)
            {
                var dir = topMover.Change >= 0 ? "the largest increase" : "the largest decrease";
                paras.Add($"{topMover.CustomerName} had {dir} of the week, {(topMover.Change >= 0 ? "up" : "down")} " +
                          $"{MoneyCompact(Math.Abs(topMover.Change))} to {MoneyCompact(topMover.CurrentSales)}.");
            }

            if (data.Current.CancelledCount > 0)
            {
                paras.Add($"{data.Current.CancelledCount} tickets were cancelled this week, totaling " +
                          $"{MoneyCompact(data.Current.CancelledSales)}; see Cancelled tickets below.");
            }

            return paras;
        }
    }
}
