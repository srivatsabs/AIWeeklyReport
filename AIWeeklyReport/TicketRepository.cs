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

        public List<Ticket> GetTickets(DateTime weekStart, DateTime weekEnd)
        {
            const string sql = @"
                SELECT TicketNo, TicketDate, LocationID, CustomerID, OrderID, TaxCodeID,
                       PurchaseOrder, Description, DeliveryAddress1, Address1, City, State,
                       County, Zip, ProductID, Qty, Unit, UnitPrice, Price, FreightRate,
                       FreightAmount, TaxAmount, TaxableAmount, VoidStatus, CustomerDescription,
                       LocationDescription, GroupID, SaleType, UniqueID
                FROM DW_Reports.dbo.vw_GRI_Daily_Tickets
                WHERE TicketDate >= @WeekStart AND TicketDate <= @WeekEnd";

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
    }
}
