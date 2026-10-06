namespace AIWeeklyReport
{
    /// <summary>
    /// One row from DW_Reports.dbo.vw_GRI_Daily_Tickets.
    /// </summary>
    public class Ticket
    {
        public string TicketNo { get; set; } = "";
        public DateTime TicketDate { get; set; }
        public string LocationID { get; set; } = "";
        public string CustomerID { get; set; } = "";
        public string OrderID { get; set; } = "";
        public string TaxCodeID { get; set; } = "";
        public string PurchaseOrder { get; set; } = "";
        public string Description { get; set; } = "";
        public string DeliveryAddress1 { get; set; } = "";
        public string Address1 { get; set; } = "";
        public string City { get; set; } = "";
        public string State { get; set; } = "";
        public string County { get; set; } = "";
        public string Zip { get; set; } = "";
        public string ProductID { get; set; } = "";
        public decimal Qty { get; set; }
        public string Unit { get; set; } = "";
        public decimal UnitPrice { get; set; }
        public decimal Price { get; set; }
        public decimal FreightRate { get; set; }
        public decimal FreightAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal TaxableAmount { get; set; }

        /// <summary>Confirmed values: "A" = Active, "C" = Cancelled/Void.</summary>
        public string VoidStatus { get; set; } = "";
        public string CustomerDescription { get; set; } = "";
        public string LocationDescription { get; set; } = "";

        /// <summary>Product category, e.g. "ASPHALT". Drives the Asphalt/Emulsion vs Rock/Aggregate split.</summary>
        public string GroupID { get; set; } = "";

        /// <summary>"Customer" = external sale, "Inventory" = internal transfer, "Job" = GRI-Modesto intercompany.</summary>
        public string SaleType { get; set; } = "";
        public string UniqueID { get; set; } = "";
        public string TicketType { get; set; } = "";
        public string CarrierId { get; set; } = "";
        public string  Credited{ get; set; } = "";
        public string GroupDesc { get; set; } = "";

    }
}
