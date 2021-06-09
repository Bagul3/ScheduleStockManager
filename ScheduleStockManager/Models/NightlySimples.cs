using System;

namespace ScheduleStockManager.Models
{
    public class NightlySimples
    {
        public string SKU {get;set;}
        public string StockLevel { get; set; }
        public int IsInStock { get; set; }
        public DateTime SortDate { get; set; }
        public string Season { get; set; }
        public string EANCode { get; set; }
        public string RRP { get; set; }
        public string Rem1 { get; set; }
        public string Rem2 { get; set; }

        public override string ToString()
        {
            return $"{"\"" + SKU + "\""},{"\"" + StockLevel + "\""},{"\"" + IsInStock + "\""},{"\"" + SortDate.ToString("yyyy/MM/dd") + "\""}" +
                                 $",{"\"" + EANCode + "\""},{"\"" + RRP + "\""}" +
                                 $",{"\"" + Season + "\""},{Rem1},{Rem2},{"\"1\""}";
        }
    }
}
