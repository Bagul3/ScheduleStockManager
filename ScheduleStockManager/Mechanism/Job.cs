using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ScheduleStockManager.Models;

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
                //  && DateTime.Now.DayOfWeek != DayOfWeek.Sunday
                if (now > GetStartTime() && now < GetEndTime())
                {
                    var csv = new StringBuilder();
                    this.DoCleanup();
                    var headers = $"{"sku"},{"qty"},{"is_in_stock"},{"LASTDELV"},{"ean"}";
                    csv.AppendLine(headers);
                    Console.WriteLine("Getting SKUs from online file");
                    var t2TreFs = RetrieveStockFromOnline();

                    Console.WriteLine("Cleaning up DESC table");
                    this.Connection(null, SqlQueries.DeleteSKUs);

                    Console.WriteLine("Gathering EAN Codes");
                    var eanDataset = Connection(null, SqlQueries.GetEanCodes);

                    Console.WriteLine("Injecting SKUs");
                    for (var i = 0; i < t2TreFs.Count; i++)
                    {
                        InsertIntoDescriptions(t2TreFs[i]);
                    }

                    Console.WriteLine("Building the stock");
                    var rows = this.Connection(null, SqlQueries.StockQuery);
                    
                    foreach (DataRow reff in rows.Tables[0].Rows)
                    {
                        csv.Append(this.DoJob(reff, eanDataset));
                    }

                    File.AppendAllText(System.Configuration.ConfigurationManager.AppSettings["OutputPath"], csv.ToString());
                    Console.WriteLine(stopwatch.Elapsed);
                    Console.WriteLine("Stock file created on: " + DateTime.Now);
                    stopwatch.Stop();
                }
                Thread.Sleep(this.GetRepetitionIntervalTime());
            }
        }

        public virtual object GetParameters()
        {
            return null;
        }

        public abstract string DoJob(DataRow data, DataSet dt);

        public abstract DataSet Connection(string reff, string query);

        public abstract void InsertIntoDescriptions(string sku);

        public abstract void DoCleanup();

        public abstract bool IsRepeatable();

        public abstract int GetRepetitionIntervalTime();

        public abstract TimeSpan GetStartTime();

        public abstract TimeSpan GetEndTime();

        //private IEnumerable<string> QueryDescriptionRefs()
        //{
        //    var dvEmp = new DataView();
        //    new LogWriter().LogWrite("Getting refs from description file");
        //    try
        //    {
        //        using (var connectionHandler = new OleDbConnection(System.Configuration.ConfigurationManager.AppSettings["ExcelConnectionString"]))
        //        {
        //            connectionHandler.Open();
        //            var adp = new OleDbDataAdapter("SELECT * FROM [Sheet1$B:B]", connectionHandler);

        //            var dsXls = new DataSet();
        //            adp.Fill(dsXls);
        //            dvEmp = new DataView(dsXls.Tables[0]);
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        Console.WriteLine(e);
        //        new LogWriter().LogWrite("Error occured getting refs from description file: " + e);
        //    }

        //    return (from DataRow row in dvEmp.Table.Rows select row.ItemArray[0].ToString()).ToList();
        //}

        public string GetCSV(string url)
        {
            var req = (HttpWebRequest)WebRequest.Create(url);
            var resp = (HttpWebResponse)req.GetResponse();

            var sr = new StreamReader(resp.GetResponseStream());
            var results = sr.ReadToEnd();
            sr.Close();

            return results;
        }

        private List<string> RetrieveStockFromOnline()
        {
            var fileList = GetCSV("https://www.cordners.co.uk/exportcsv/");
            string[] tempStr;
            tempStr = fileList.Split('\t');
            var skus = new List<string>();

            foreach (var item in tempStr)
            {
                if (!string.IsNullOrEmpty(item))
                {
                    if (item.Contains('\n') && item.Split('\n')[0].Length > 6)
                    {
                        var sku = item.Split('\n')[0].Substring(0, 6);
                        if (!skus.Contains(sku))
                        {
                            skus.Add(sku);
                        }
                    }
                }
            }

            return skus;
        }
    }
}
