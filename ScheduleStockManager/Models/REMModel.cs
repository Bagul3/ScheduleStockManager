using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScheduleStockManager.Models
{
    public class REMModel
    {
        public string Property { get; set; }
        public string Name { get; set; }
        public string T2T_Id { get; set; }
        public string REM { get; set; }

        public REMModel()
        {

        }

        public REMModel(string name, string id, string rem_property, string rem)
        {
            this.Name = name;
            this.T2T_Id = id;
            this.Property = rem_property;
            this.REM = rem;
        }
    }
}
