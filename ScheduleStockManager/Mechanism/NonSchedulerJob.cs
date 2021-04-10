using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using ScheduleStockManager.Models;
using StockCSV;

namespace ScheduleStockManager.Mechanism
{
    public class NonSchedulerJob
    {
        public void ExecuteJob()
        {
            var stopwatch = new Stopwatch();
            var database = new Database();

            stopwatch.Start();
            if (true)
            {
                var SEASON = "S17";
                var csv = new StringBuilder();
                database.DoCleanup();
                var headers = $"{"sku"},{"qty"},{"is_in_stock"},{"sort_date"},{"ean"},{"price"},{"season"},{"rem1"},{"rem2"},{"visibility"}";
                csv.AppendLine(headers);
                Console.WriteLine("Getting SKUs from online file");
                var t2TreFs = RetrieveStockFromOnline();

                Console.WriteLine("Gathering EAN Codes");
                var eanDataset = database.Connection(null, SqlQueries.GetEanCodes);

                Console.WriteLine("Building the stock");
                var data = database.Connection(null, SqlQueries.StockQuery);                
                
                foreach (string sku in t2TreFs)
                {
                    var rows = data.Tables[0].Select($"REF = {sku} AND USER1 = '{SEASON}'");
                    foreach (DataRow reff in rows)
                    {
                        csv.Append(database.DoJob(reff, eanDataset, t2TreFs));
                    }
                }

                File.AppendAllText($"{System.Configuration.ConfigurationManager.AppSettings["OutputPath"]}-{SEASON}.csv" , csv.ToString());                
                Console.WriteLine(stopwatch.Elapsed);
                Console.WriteLine("Stock file created on: " + DateTime.Now);
                stopwatch.Stop();
            }
        }

        public string GetCSV(string url)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            var req = (HttpWebRequest)WebRequest.Create(url);
            using (WebResponse response = req.GetResponse())
            {
                Stream responseStream = response.GetResponseStream();
                var sr = new StreamReader(response.GetResponseStream());
                var results = sr.ReadToEnd();
                sr.Close();
                return results;
            }         
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
