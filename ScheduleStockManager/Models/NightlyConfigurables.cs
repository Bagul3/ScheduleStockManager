using System;

namespace ScheduleStockManager.Models
{
    class NightlyConfigurables
    {
        public string SKU { get; set; }
        public DateTime SortDate { get; set; }

        public string UDef2 { get; set; }

        public string Type { get; set; }

        public string REM1 { get; set; }
        public string REM2 { get; set; }
        public override string ToString()
        {
            return $"{"\"" + SKU + "\""},{"\"" + SortDate.ToString("yyyy/MM/dd") + "\""},{"\"" + UDef2 + "\""},{"\"" + Type + "\""},{"\"" + REM1 + "\""},{"\"" + REM2 + "\""}";
        }
    }
}
