namespace AIWeeklyReport
{
    public class WeekAggregator
    {
        //private const string VoidCancelled = "C";
        //private const string GroupAsphalt = "ASPHALT";
        //private const string GroupRockPlant = "ROCKPLANT";
        //private const string SaleTypeCustomer = "Customer";
        //private const string SaleTypeInventory = "Inventory";
        //private const string SaleTypeJob = "Job";
        //private const string CustomerId = "A00011";
        private readonly string VoidCancelled;
        private readonly string GroupAsphalt;
        private readonly string GroupRockPlant;
        private readonly string SaleTypeCustomer;
        private readonly string SaleTypeInventory;
        private readonly string SaleTypeJob;
        private readonly string CustomerId;
        private readonly string Corrected;
        public WeekAggregator(CompanyDetails companyDetails)
       
        {
            VoidCancelled = companyDetails.VoidCancelled;
            GroupAsphalt = companyDetails.GroupAsphalt;
            GroupRockPlant = companyDetails.GroupRockPlant;
            SaleTypeCustomer = companyDetails.SaleTypeCustomer;
            SaleTypeInventory = companyDetails.SaleTypeInventory;
            SaleTypeJob = companyDetails.SaleTypeJob;
            CustomerId = companyDetails.CustomerId;
            Corrected= companyDetails.Corrected;
        }
        /// <summary>
        /// Environmental fee line item. Fees are real revenue (kept in every Sales figure)
        /// but aren't a quantity of material (excluded from every Tons figure). Applied via
        /// this one predicate everywhere below so Sales and Tons stay consistent across every
        /// section of the report — previously this was only applied to the headline and daily
        /// totals, which made those numbers not reconcile with the plant/customer/product tables.
        /// </summary>
        private const string FeeProductId = "ENVIFEE";
        private static bool IsFeeLine(Ticket t) => t.ProductID == FeeProductId;

        public WeekAggregate Build(List<Ticket> rawTickets, DateTime weekStart, DateTime weekEnd, string Location)
        {
            var agg = new WeekAggregate { WeekStart = weekStart.Date, WeekEnd = weekEnd.Date };

            foreach (var t in rawTickets)
            {
                if (t.VoidStatus != "A" && t.VoidStatus != VoidCancelled && t.VoidStatus != Corrected)
                    agg.DataQualityFlags.Add($"Ticket {t.TicketNo} has unrecognized VoidStatus='{t.VoidStatus}'.");

                if (t.SaleType == SaleTypeJob && t.CustomerID != CustomerId)
                    agg.DataQualityFlags.Add($"Ticket {t.TicketNo}: SaleType='Job' but CustomerID='{t.CustomerID}' ({t.CustomerDescription}), not {Location}.");
            }

            var cancelled = rawTickets.Where(t => t.VoidStatus == VoidCancelled).ToList();
            var active = rawTickets.Where(t => t.VoidStatus != VoidCancelled).ToList();

            var corrected = rawTickets.Where(t => t.VoidStatus == Corrected).ToList();
            // --- Cancelled tickets ---
            agg.CancelledCount = cancelled.Select(t => t.TicketNo).Distinct().Count();
            agg.CancelledSales = cancelled.Sum(t => t.Price);
            agg.CancelledTons = cancelled.Sum(t => t.Qty);
            agg.CancelledTickets = cancelled
                .Select(t => new CancelledTicketRow
                {
                    TicketNo = t.TicketNo,
                    Date = t.TicketDate.Date,
                    Plant = t.LocationDescription,
                    Customer = t.CustomerDescription,
                    Material = t.Description,
                    Tons = t.Qty,
                    Price = t.Price
                })
                .OrderByDescending(c => c.Price)
                .ToList();

            // --- Corrected tickets ---
            agg.CorrectedCount = corrected.Select(t => t.TicketNo).Distinct().Count();
            agg.CorrectedSales = corrected.Sum(t => t.Price);
            agg.CorrectedTons = corrected.Sum(t => t.Qty);
            agg.CorrectedTickets = corrected
                .Select(t => new CorrectedTicketRow
                {
                    TicketNo = t.TicketNo,
                    Date = t.TicketDate.Date,
                    Plant = t.LocationDescription,
                    Customer = t.CustomerDescription,
                    Material = t.Description,
                    Tons = t.Qty,
                    Price = t.Price
                })
                .OrderByDescending(c => c.Price)
                .ToList();

            // --- Ticket by Type tickets ---
          
            var ticketByType = active
     .GroupBy(t => t.TicketType)
     .ToList();
            Console.WriteLine($"rawTickets: {rawTickets.Count}");
            Console.WriteLine($"groups: {ticketByType.Count}");

            agg.TicketByTypeTickets = ticketByType
                .Select(g => new TicketByTypeRow
                {
                    TicketType = g.Key == "M" ? "Manual Tickets":"Scale Tickets",

                    TicketByTypeCount = g
                        .Select(t => t.TicketNo)
                        .Distinct()
                        .Count(),
                    Tons = g.Where(t => !IsFeeLine(t)).Sum(t => t.Qty),
                    Sales = g.Sum(t => t.Price),
                })
                .ToList();

            

            // --- Ticket by CarriedId  ---
            var TicketByCarrierId = active
     .GroupBy(t => t.CarrierId)
     .ToList();
            agg.TicketByCarrierIdTickets = TicketByCarrierId
    .Select(g => new TicketByCarrierIdRow
    {
        
             CarrierId = g.Key,
           
             Tickets = g.Select(t => t.TicketNo).Distinct().Count(),
             Tons = g.Where(t => !IsFeeLine(t)).Sum(t => t.Qty),
             Sales = g.Sum(t => t.Price),
             
        
    })
    .ToList();


            // --- Ticket by Credited  ---
            var TicketByCredited = active.Where(x => x.Credited == "C")
     .GroupBy(t => t.Credited)
     .ToList();

            agg.TicketByCreditedTickets = TicketByCredited
     .Select(g => new TicketByCreditedRow
     {

         Credited = g.Key == "N"? "Not credited":g.Key 
         ,

         Tickets = g.Select(t => t.TicketNo).Distinct().Count(),
         Tons = g.Where(t => !IsFeeLine(t)).Sum(t => t.Qty),
         Sales = g.Sum(t => t.Price),


     })
     .ToList();

            // --- Headline totals (active rows, all sale types) ---
            // Sales include fee revenue; Tons exclude fee lines (a fee isn't a quantity of material).
            agg.TotalTickets = active.Select(t => t.TicketNo).Distinct().Count();
            agg.TotalTons = active.Where(t => !IsFeeLine(t)).Sum(t => t.Qty);
            agg.TotalSales = active.Sum(t => t.Price);


            //only for GRI 
            agg.TotalTicketsByAsphalt = active.Where(t => t.GroupID == GroupAsphalt).Select(t => t.TicketNo).Distinct().Count();
            agg.TotalTonsByAsphalt = active.Where(t => t.GroupID == GroupAsphalt).Where(t => !IsFeeLine(t)).Sum(t => t.Qty);
            agg.TotalSalesByAsphalt = active.Where(t => t.GroupID == GroupAsphalt).Sum(t => t.Price);
            agg.TotalTicketsByRockplant = active.Where(t => t.GroupID == GroupRockPlant).Select(t => t.TicketNo).Distinct().Count();
            agg.TotalTonsByRockplant = active.Where(t => t.GroupID == GroupRockPlant).Where(t => !IsFeeLine(t)).Sum(t => t.Qty);
            agg.TotalSalesByRockplant = active.Where(t => t.GroupID == GroupRockPlant).Sum(t => t.Price);

            agg.CustomerTaxCollected = active.Where(t => t.SaleType == SaleTypeCustomer).Sum(t => t.TaxAmount);

            // --- Sales by day (Sun..Sat from weekStart) ---
            for (var d = weekStart.Date; d <= weekEnd.Date; d = d.AddDays(1))
            {
                var dayRows = active.Where(t => t.TicketDate.Date == d).ToList();
                agg.Days.Add(new DayAgg
                {
                    Date = d,
                    Tickets = dayRows.Select(t => t.TicketNo).Distinct().Count(),
                    Tons = dayRows.Where(t => !IsFeeLine(t)).Sum(t => t.Qty),
                    Sales = dayRows.Sum(t => t.Price)
                });
            }

            // --- Sales by plant, split by product group ---
            agg.Plants = active
                .GroupBy(t => t.LocationDescription)
                .Select(g => new PlantAgg
                {
                    Plant = g.Key,
                    AsphaltSales = g.Where(t => t.GroupID == GroupAsphalt).Sum(t => t.Price),
                    RockSales = g.Where(t => t.GroupID == GroupRockPlant).Sum(t => t.Price),
                    OtherSales = g.Where(t => t.GroupID != GroupAsphalt && t.GroupID != GroupRockPlant).Sum(t => t.Price),
                    Tons = g.Where(t => !IsFeeLine(t)).Sum(t => t.Qty),
                    Tickets = g.Select(t => t.TicketNo).Distinct().Count()
                })
                .OrderByDescending(p => p.TotalSales)
                .ToList();

            // --- Sale type breakdown ---
            agg.SaleTypes = active
                .GroupBy(t => t.SaleType)
                .Select(g => new SaleTypeAgg
                {
                    Type = g.Key,
                    Tickets = g.Select(t => t.TicketNo).Distinct().Count(),
                    Tons = g.Where(t => !IsFeeLine(t)).Sum(t => t.Qty),
                    Sales = g.Sum(t => t.Price)
                })
                .OrderByDescending(s => s.Sales)
                .ToList();

            // --- Product group breakdown ---
            //agg.ProductGroups = active
            //    .GroupBy(t => IsFeeLine(t) ? "Fees"
            //                : t.GroupID == GroupAsphalt ? "Asphalt"
            //                : t.GroupID == GroupRockPlant ? "Rock plant"    : "Unclassified material");

            agg.ProductGroups = active
    .GroupBy(t =>
        IsFeeLine(t) ? "Fees" :
        t.GroupID == GroupAsphalt ? "Asphalt" :
        t.GroupID == GroupRockPlant ? "Rock plant" :
        t.GroupDesc)
                .Select(g => new ProductGroupAgg
                {
                    Label = g.Key,
                    Tons = g.Where(t => !IsFeeLine(t)).Sum(t => t.Qty),
                    Sales = g.Sum(t => t.Price)
                })
                .OrderByDescending(p => p.Sales)
                .ToList();

            // --- Top products ---
            agg.Products = active
                .GroupBy(t => new { t.ProductID, t.Description, t.GroupID,t.GroupDesc })
                .Select(g => new ProductAgg
                {
                    ProductId = g.Key.ProductID,
                    Description = g.Key.Description,
                    GroupId = g.Key.GroupID,
                    Lines = g.Count(),
                    Tons = g.Sum(t => t.Qty),
                    Sales = g.Sum(t => t.Price),
                    GroupDesc= g.Key.GroupDesc
                })
                .OrderByDescending(p => p.Sales)
                .ToList();

            // --- Customers (all sale types, tagged when a customer is exclusively Job or Inventory) ---
            agg.Customers = active
                .GroupBy(t => new { t.CustomerID, t.CustomerDescription })
                .Select(g =>
                {
                    var types = g.Select(t => t.SaleType).Distinct().ToList();
                    var tag = types.Count == 1 && types[0] == SaleTypeJob ? "Internal jobs"
                            : types.Count == 1 && types[0] == SaleTypeInventory ? "Inventory transfer"
                            : "";
                    return new CustomerAgg
                    {
                        CustomerId = g.Key.CustomerID,
                        CustomerName = g.Key.CustomerDescription,
                        Tickets = g.Select(t => t.TicketNo).Distinct().Count(),
                        Tons = g.Where(t => !IsFeeLine(t)).Sum(t => t.Qty),
                        Sales = g.Sum(t => t.Price),
                        Tag = tag
                    };
                })
                .OrderByDescending(c => c.Sales)
                .ToList();

            // --- Job orders (SaleType='Job' only, grouped by job code + project + plant) ---
            agg.JobOrders = active
                .Where(t => t.SaleType == SaleTypeJob)
                .GroupBy(t => new { t.PurchaseOrder, t.DeliveryAddress1, t.LocationDescription })
                .Select(g => new JobOrderAgg
                {
                    JobCode = g.Key.PurchaseOrder,
                    Description = g.Key.DeliveryAddress1,
                    Plant = g.Key.LocationDescription,
                    Tickets = g.Select(t => t.TicketNo).Distinct().Count(),
                    Tons = g.Where(t => !IsFeeLine(t)).Sum(t => t.Qty),
                    Sales = g.Sum(t => t.Price)
                })
                .OrderByDescending(j => j.Sales)
                .ToList();

            return agg;
        }
    }
}
