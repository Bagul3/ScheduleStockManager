using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

//    Parallel.ForEach(t2TreFs, (reff) =>
//{
//lock (reff)
//{
//csv.Append(this.DoJob(reff));
//}

//});

namespace ScheduleStockManager.Mechanism
{
    public abstract class Job
    {
        public void ExecuteJob()
        {
            if (!this.IsRepeatable()) return;
            while (true)
            {
                var now = DateTime.Now.TimeOfDay;
                var stopwatch = new Stopwatch();
                stopwatch.Start();
                if (now > GetStartTime() && now < GetEndTime() && DateTime.Now.DayOfWeek != DayOfWeek.Sunday)
                {
                    var csv = new StringBuilder();
                    this.DoCleanup();
                    var headers = $"{"sku"},{"qty"},{"is_in_stock"}";
                    csv.AppendLine(headers);
                    var t2TreFs = QueryDescriptionRefs();

                    foreach (var reff in t2TreFs)
                    {
                        Console.WriteLine("Generating stock for: " + reff);
                        var dataset = this.Connection(reff);
                        csv.Append(this.DoJob(dataset));
                    }
                        
                    File.AppendAllText(System.Configuration.ConfigurationManager.AppSettings["OutputPath"], csv.ToString());
                    Console.WriteLine(stopwatch.Elapsed);
                    stopwatch.Stop();
                }
                Thread.Sleep(this.GetRepetitionIntervalTime());
            }
        }

        public virtual object GetParameters()
        {
            return null;
        }

        public abstract string DoJob(DataSet data);

        public abstract DataSet Connection(string reff);

        public abstract void DoCleanup();

        public abstract bool IsRepeatable();

        public abstract int GetRepetitionIntervalTime();

        public abstract TimeSpan GetStartTime();

        public abstract TimeSpan GetEndTime();

        private IEnumerable<string> QueryDescriptionRefs()
        {
            var dvEmp = new DataView();
            new LogWriter().LogWrite("Getting refs from description file");
            try
            {
                using (var connectionHandler = new OleDbConnection(System.Configuration.ConfigurationManager.AppSettings["ExcelConnectionString"]))
                {
                    connectionHandler.Open();
                    var adp = new OleDbDataAdapter("SELECT * FROM [Sheet1$B:B]", connectionHandler);

                    var dsXls = new DataSet();
                    adp.Fill(dsXls);
                    dvEmp = new DataView(dsXls.Tables[0]);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                new LogWriter().LogWrite("Error occured getting refs from description file: " + e);
            }

            return (from DataRow row in dvEmp.Table.Rows select row.ItemArray[0].ToString()).ToList();
        }
    }
}
