namespace AIWeeklyReport
{
    public class DayAgg
    {
        public DateTime Date { get; set; }
        public int Tickets { get; set; }
        public decimal Tons { get; set; }
        public decimal Sales { get; set; }
    }

    public class PlantAgg
    {
        public string Plant { get; set; } = "";
        public decimal AsphaltSales { get; set; }
        public decimal RockSales { get; set; }
        public decimal OtherSales { get; set; }
        public decimal TotalSales => AsphaltSales + RockSales + OtherSales;
        public decimal Tons { get; set; }
        public int Tickets { get; set; }
    }

    public class SaleTypeAgg
    {
        public string Type { get; set; } = "";
        public int Tickets { get; set; }
        public decimal Tons { get; set; }
        public decimal Sales { get; set; }
    }

    public class ProductGroupAgg
    {
        public string Label { get; set; } = "";
        public decimal Tons { get; set; }
        public decimal Sales { get; set; }
    }

    public class ProductAgg
    {
        public string ProductId { get; set; } = "";
        public string Description { get; set; } = "";
        public string GroupId { get; set; } = "";
        public int Lines { get; set; }
        public decimal Tons { get; set; }
        public decimal Sales { get; set; }
    }

    public class CustomerAgg
    {
        public string CustomerId { get; set; } = "";
        public string CustomerName { get; set; } = "";
        public int Tickets { get; set; }
        public decimal Tons { get; set; }
        public decimal Sales { get; set; }

        /// <summary>"Internal jobs", "Inventory transfer", or "" when the customer has mixed/ordinary sale types.</summary>
        public string Tag { get; set; } = "";
    }

    public class JobOrderAgg
    {
        public string JobCode { get; set; } = "";
        public string Description { get; set; } = "";
        public string Plant { get; set; } = "";
        public int Tickets { get; set; }
        public decimal Tons { get; set; }
        public decimal Sales { get; set; }
    }

    public class CancelledTicketRow
    {
        public string TicketNo { get; set; } = "";
        public DateTime Date { get; set; }
        public string Plant { get; set; } = "";
        public string Customer { get; set; } = "";
        public string Material { get; set; } = "";
        public decimal Tons { get; set; }
        public decimal Price { get; set; }
    }
    public class CorrectedTicketRow
    {
        public string TicketNo { get; set; } = "";
        public DateTime Date { get; set; }
        public string Plant { get; set; } = "";
        public string Customer { get; set; } = "";
        public string Material { get; set; } = "";
        public decimal Tons { get; set; }
        public decimal Price { get; set; }
    }
    public class TicketByCarrierIdRow
    {
        public int Tickets { get; set; } 
        public DateTime Date { get; set; }
        public string Plant { get; set; } = "";
        public string CarrierId { get; set; } = "";
        
        public decimal Sales { get; set; }
        public decimal Tons { get; set; }
        public decimal Price { get; set; }
    }
    public class TicketByCreditedRow
    {
        public int Tickets { get; set; }
        public DateTime Date { get; set; }
        public string Plant { get; set; } = "";
        public string Credited { get; set; } = "";

        public decimal Sales { get; set; }
        public decimal Tons { get; set; }
        public decimal Price { get; set; }
    }
    public class TicketByTypeRow
    {
        public string TicketType { get; set; } = "";
        public int TicketByTypeCount { get; set; } 
       
        
    }

    /// <summary>
    /// Everything computed from one week's raw ticket rows. VoidStatus='C' rows are
    /// kept in RawCancelled for the Cancelled Tickets section but excluded from every
    /// other figure below.
    /// </summary>
    public class WeekAggregate
    {
        public DateTime WeekStart { get; set; }
        public DateTime WeekEnd { get; set; }

        public int TotalTickets { get; set; }
        public decimal TotalTons { get; set; }
        public decimal TotalSales { get; set; }
        public decimal SalesPerTon => TotalTons == 0 ? 0 : TotalSales / TotalTons;

        public int CancelledCount { get; set; }
        public decimal CancelledSales { get; set; }
        public decimal CancelledTons { get; set; }
        public List<CancelledTicketRow> CancelledTickets { get; set; } = new();
        public int CorrectedCount { get; set; }
        public decimal CorrectedSales { get; set; }
        public decimal CorrectedTons { get; set; }
        public List<CorrectedTicketRow> CorrectedTickets { get; set; } = new();

        public int TicketByTypeCount { get; set; }
        public decimal TicketByTypeSales { get; set; }
        public decimal TicketByTypeTons { get; set; }
        public List<TicketByTypeRow> TicketByTypeTickets { get; set; } = new();

        public int TicketByCarrierIdCount { get; set; }
        public decimal TicketByCarrierIdSales { get; set; }
        public decimal TicketByCarrierIdTons { get; set; }
        public List<TicketByCarrierIdRow> TicketByCarrierIdTickets { get; set; } = new();

        public int TicketByCreditedCount { get; set; }
        public decimal TicketByCreditedSales { get; set; }
        public decimal TicketByCreditedTons { get; set; }
        public List<TicketByCreditedRow> TicketByCreditedTickets { get; set; } = new();
        public decimal CustomerTaxCollected { get; set; }

        public List<DayAgg> Days { get; set; } = new();
        public List<PlantAgg> Plants { get; set; } = new();
        public List<SaleTypeAgg> SaleTypes { get; set; } = new();
        public List<ProductGroupAgg> ProductGroups { get; set; } = new();
        public List<ProductAgg> Products { get; set; } = new();
        public List<CustomerAgg> Customers { get; set; } = new();
        public List<JobOrderAgg> JobOrders { get; set; } = new();

        public List<string> DataQualityFlags { get; set; } = new();
    }
}
