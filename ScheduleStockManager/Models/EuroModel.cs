using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScheduleStockManager.Models
{
    public class EuroModel
    {
        public string SKU { get; set; }
        public string RRP { get; set; }
        public string SterlingRRP { get; set; }

        public override string ToString()
        {
            return $"{"\"" + SKU + "\""},{"\"" + RRP + "\""},{"\"" + SterlingRRP + "\""}";
        }
    }
}
