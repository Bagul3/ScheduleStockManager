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
                if (true)
                {
                    var csv = new StringBuilder();
                    this.DoCleanup();
                    var headers = $"{"sku"},{"qty"},{"is_in_stock"},{"sort_date"},{"ean"},{"price"},{"REM"},{"REM2"},{"season"}";
                    csv.AppendLine(headers);
                    Console.WriteLine("Getting SKUs from online file");
                    var t2TreFs = RetrieveStockFromOnline();

                    //Console.WriteLine("Creating new DESC database");
                    //CreateDbfFile();

                    //Console.WriteLine("Cleaning up DESC table");
                    //this.Connection(null, SqlQueries.DeleteSKUs);

                    Console.WriteLine("Gathering EAN Codes");
                    var eanDataset = Connection(null, SqlQueries.GetEanCodes);

                    Console.WriteLine("Injecting SKUs");
                    for (var i = 0; i < t2TreFs.Count; i++)
                    {
                        InsertIntoDescriptions(t2TreFs[i]);
                    }

                    Console.WriteLine("Building the stock");
                    var rows = this.Connection(null, SqlQueries.StockQuery);
                    
                    if (System.Configuration.ConfigurationManager.AppSettings["Split_Stock_File"].ToUpper() == "TRUE")
                    {
                        var i = 0;
                        int card = 0;
                        foreach (DataRow reff in rows.Tables[0].Rows)
                        {
                            Random rnd = new Random();
                            card = rnd.Next(520);
                            csv.Append(this.DoJob(reff, eanDataset, t2TreFs));
                            i++;
                            if (i == Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["Split_Delimiter"]))
                            {
                                File.AppendAllText(System.Configuration.ConfigurationManager.AppSettings["Split_OutputPath"] + card + ".csv", csv.ToString());
                                csv = new StringBuilder();
                                csv.AppendLine(headers);
                                i = 0;
                            }
                        }

                        File.AppendAllText(System.Configuration.ConfigurationManager.AppSettings["Split_OutputPath"] + card + ".csv", csv.ToString());
                        csv = new StringBuilder();
                    }
                    else
                    {
                        foreach (DataRow reff in rows.Tables[0].Rows)
                        {
                            csv.Append(this.DoJob(reff, eanDataset, t2TreFs));
                        }

                        File.AppendAllText(System.Configuration.ConfigurationManager.AppSettings["OutputPath"], csv.ToString());
                    }
                    
                    Console.WriteLine(stopwatch.Elapsed);
                    Console.WriteLine("Stock file created on: " + DateTime.Now);
                    stopwatch.Stop();
                    rows = null;
                    csv = null;
                    t2TreFs = null;
                }
                Thread.Sleep(this.GetRepetitionIntervalTime());
            }
        }

        public virtual object GetParameters()
        {
            return null;
        }

        public abstract string DoJob(DataRow data, DataSet dt, List<string> t2Refs);

        public abstract DataSet Connection(string reff, string query);

        public abstract DataSet Connection(string reff, string lastmonth, string lastweek, string yesertday, string query);

        public abstract void InsertIntoDescriptions(string sku);

        public abstract void DoCleanup();

        public abstract bool IsRepeatable();

        public abstract int GetRepetitionIntervalTime();

        public abstract TimeSpan GetStartTime();

        public abstract TimeSpan GetEndTime();

        public string GetCSV(string url)
        {
            var req = (HttpWebRequest)WebRequest.Create(url);
            var resp = (HttpWebResponse)req.GetResponse();

            var sr = new StreamReader(resp.GetResponseStream());
            var results = sr.ReadToEnd();
            sr.Close();

            return results;
        }

        public void DeleteExistingDd()
        {
            Console.WriteLine("Creating new DESC database");
            if (File.Exists(System.Configuration.ConfigurationManager.AppSettings["AccessConnectionString"] + "DESC.dbf"))
            {
                File.Delete(System.Configuration.ConfigurationManager.AppSettings["AccessConnectionString"] + "DESC.dbf");
            }
        }

        public void CreateDbfFile()
        {
            try
            {
                using (var connection = new OleDbConnection(System.Configuration.ConfigurationManager.AppSettings["AccessConnectionString"]))
                {
                    connection.Open();
                    var command = connection.CreateCommand();

                    command.CommandText = "create table DESC(INDIVIDUAL (ID int primary key, SKU char(100))";
                    command.ExecuteNonQuery();
                    connection.Close();
                }
            }
            catch(Exception e)
            {
                Console.WriteLine("Stack trace: " + e.StackTrace);
                Console.WriteLine("Message: " + e.Message);
                Console.Read();
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
