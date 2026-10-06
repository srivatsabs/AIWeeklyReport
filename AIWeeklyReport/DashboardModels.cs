namespace AIWeeklyReport
{
    public class DayRow
    {
        public string Label { get; set; } = "";
        public int Tickets { get; set; }
        public decimal Tons { get; set; }
        public decimal Sales { get; set; }
        public decimal PriorSales { get; set; }
        public decimal? ChangePercent { get; set; }
    }

    public class PlantRow
    {
        public string Plant { get; set; } = "";
        public decimal AsphaltSales { get; set; }
        public decimal RockSales { get; set; }
        public decimal OtherSales { get; set; }
        public decimal TotalSales => AsphaltSales + RockSales + OtherSales;
        public decimal PriorTotal { get; set; }
        public decimal? ChangePercent { get; set; }
        public decimal Tons { get; set; }
        public int Tickets { get; set; }
        public decimal DollarsPerTon => Tons == 0 ? 0 : TotalSales / Tons;
    }

    public class MoverRow
    {
        public string CustomerName { get; set; } = "";
        public decimal PriorSales { get; set; }
        public decimal CurrentSales { get; set; }
        public decimal Change => CurrentSales - PriorSales;
    }

    public class TileRow
    {
        public string Label { get; set; } = "";
        public string DisplayValue { get; set; } = "";
        public decimal? ChangePercent { get; set; }
        public string PriorDisplayValue { get; set; } = "";
    }

    public class DashboardData
    {
        public WeekAggregate Current { get; set; } = null!;
        public WeekAggregate Prior { get; set; } = null!;

        public List<TileRow> Tiles { get; set; } = new();
        public List<TileRow> TilesAsphalt { get; set; } = new();
        public List<TileRow> TilesRockPlant { get; set; } = new();
        public List<DayRow> Days { get; set; } = new();
        public List<PlantRow> Plants { get; set; } = new();
        public List<MoverRow> Movers { get; set; } = new();
        public List<string> LedeParagraphs { get; set; } = new();

        public List<string> DataQualityFlags => Current.DataQualityFlags.Concat(Prior.DataQualityFlags).Distinct().ToList();
    }
}
