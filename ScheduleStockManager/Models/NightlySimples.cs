using System;

namespace ScheduleStockManager.Models
{
    public class NightlySimples
    {
        public string SKU {get;set;}
        public string StockLevel { get; set; }
        public int IsInStock { get; set; }
        public string Season { get; set; }
        public string EANCode { get; set; }
        public string RRP { get; set; }

        public override string ToString()
        {
            return $"{"\"" + SKU + "\""},{"\"" + StockLevel + "\""},{"\"" + IsInStock + "\""}," +
                                 $"{"\"" + EANCode + "\""},{"\"" + RRP + "\""}," +
                                 $"{"\"" + Season + "\""},{"\"1\""}";
        }
    }
}
