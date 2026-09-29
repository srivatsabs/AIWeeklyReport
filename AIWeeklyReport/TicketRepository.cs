using Microsoft.Data.SqlClient;

namespace AIWeeklyReport
{
    public class TicketRepository
    {
        private readonly string _connectionString;

        public TicketRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public List<Ticket> GetTickets(DateTime weekStart, DateTime weekEnd, string viewDefinition)
        {
            string sql = viewDefinition;

            var tickets = new List<Ticket>();

            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@WeekStart", weekStart);
            cmd.Parameters.AddWithValue("@WeekEnd", weekEnd);

            conn.Open();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                tickets.Add(new Ticket
                {
                    TicketNo = reader["TicketNo"]?.ToString() ?? "",
                    TicketDate = reader.GetDateTime(reader.GetOrdinal("TicketDate")),
                    LocationID = reader["LocationID"]?.ToString() ?? "",
                    CustomerID = reader["CustomerID"]?.ToString() ?? "",
                    OrderID = reader["OrderID"]?.ToString() ?? "",
                    TaxCodeID = reader["TaxCodeID"]?.ToString() ?? "",
                    PurchaseOrder = reader["PurchaseOrder"]?.ToString() ?? "",
                    Description = reader["Description"]?.ToString() ?? "",
                    DeliveryAddress1 = reader["DeliveryAddress1"]?.ToString() ?? "",
                    Address1 = reader["Address1"]?.ToString() ?? "",
                    City = reader["City"]?.ToString() ?? "",
                    State = reader["State"]?.ToString() ?? "",
                    County = reader["County"]?.ToString() ?? "",
                    Zip = reader["Zip"]?.ToString() ?? "",
                    ProductID = reader["ProductID"]?.ToString() ?? "",
                    Qty = SafeDecimal(reader["Qty"]),
                    Unit = reader["Unit"]?.ToString() ?? "",
                    UnitPrice = SafeDecimal(reader["UnitPrice"]),
                    Price = SafeDecimal(reader["Price"]),
                    FreightRate = SafeDecimal(reader["FreightRate"]),
                    FreightAmount = SafeDecimal(reader["FreightAmount"]),
                    TaxAmount = SafeDecimal(reader["TaxAmount"]),
                    TaxableAmount = SafeDecimal(reader["TaxableAmount"]),
                    VoidStatus = reader["VoidStatus"]?.ToString() ?? "",
                    CustomerDescription = reader["CustomerDescription"]?.ToString() ?? "",
                    LocationDescription = reader["LocationDescription"]?.ToString() ?? "",
                    GroupID = reader["GroupID"]?.ToString() ?? "",
                    SaleType = reader["SaleType"]?.ToString() ?? "",
                    UniqueID = reader["UniqueID"]?.ToString() ?? ""
                });
            }

            return tickets;
        }

        private static decimal SafeDecimal(object value)
        {
            if (value == null || value == DBNull.Value) return 0m;
            return Convert.ToDecimal(value);
        }

        public List<CompanyDetails> GetCompanyDetails()
        {
            const string sql = @"
                SELECT *
                FROM [BRI_Custom].[dbo].[AIWeeklyReport_Configuration]";
                

            var companyDetails = new List<CompanyDetails>();

            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(sql, conn);
            

            conn.Open();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                companyDetails.Add(new CompanyDetails
                {
                    ViewName = reader["ViewName"]?.ToString() ?? "",
                    CompanyName = reader["CompanyName"]?.ToString() ?? "",
                    FileName = reader["FileName"]?.ToString() ?? "",
                    ViewFields = reader["ViewFields"]?.ToString() ?? "",
                    Title = reader["Title"]?.ToString() ?? "",
                    VoidCancelled = reader["VoidCancelled"]?.ToString() ?? "",
        GroupAsphalt = reader["GroupAsphalt"]?.ToString() ?? "",
       GroupRockPlant = reader["GroupRockPlant"]?.ToString() ?? "",
       SaleTypeCustomer = reader["SaleTypeCustomer"]?.ToString() ?? "",
       SaleTypeInventory = reader["SaleTypeInventory"]?.ToString() ?? "",
       SaleTypeJob = reader["SaleTypeJob"]?.ToString() ?? "",
       CustomerId = reader["CustomerId"]?.ToString() ?? "",
       Location = reader["Location"]?.ToString() ?? "",
                    Company = reader["Company"]?.ToString() ?? "",
                    FolderPath = reader["FolderPath"]?.ToString() ?? "",
                });
            }

            return companyDetails;
        }
    }
}
