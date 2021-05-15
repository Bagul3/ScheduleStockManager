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
            var successful = true;
            try
            {
                var database = new Database();
                var date = DateTime.Now.Date;                
                if (true)
                {
                    var seasonsdata = database.Connection(null, SqlQueries.FetchSeasons);
                    var csv = new StringBuilder();
                    var csvBody = new StringBuilder();                    
                    var headers = $"{"\"" + "sku" + "\""},{"\"" + "qty" + "\""},{"\"" + "is_in_stock" + "\""},{"\"" + "sort_date" + "\""},{"\"" + "ean" + "\""},{"\"" + "price" + "\""},{"\"" + "season" + "\""},{"\"" + "rem1" + "\""},{"\"" + "rem2" + "\""},{"\"" + "visibility" + "\""}";
                    Console.WriteLine("Getting SKUs from online file");
                    var skuFromOnline = RetrieveStockFromOnline();

                    Console.WriteLine("Gathering EAN Codes");
                    var eanDataset = database.Connection(null, SqlQueries.GetEanCodes);

                    Console.WriteLine("Building the stock");
                    var allcordnersStock = database.Connection(null, SqlQueries.StockQueryALL);

                    //List<string> seasons = seasonsdata.Tables[0].Select().Select(x => x["User1"].ToString()).Distinct().ToList();                    
                    database.DoCleanup("");
                    if (true)
                    {
                        var stopwatch = new Stopwatch();
                        stopwatch.Start();
                        var time = GetTimestamp(DateTime.Now);
                        File.AppendAllText($"{System.Configuration.ConfigurationManager.AppSettings["OutputPath"]}/stock-{time}.csv", headers.ToString() + Environment.NewLine);
                        foreach (string sku in skuFromOnline)
                        {
                            var rows = allcordnersStock.Tables[0].Select($"REF = {sku}");
                            foreach (DataRow reff in rows)
                            {
                                var result = database.DoJob(reff, eanDataset, skuFromOnline);
                                if (result != "")
                                    File.AppendAllText($"{System.Configuration.ConfigurationManager.AppSettings["OutputPath"]}/stock-{time}.csv", result.ToString() + Environment.NewLine);
                            }
                            if (new FileInfo($"{System.Configuration.ConfigurationManager.AppSettings["OutputPath"]}/stock-{time}.csv").Length > 1887436)
                            {
                                time = GetTimestamp(DateTime.Now);
                                File.AppendAllText($"{System.Configuration.ConfigurationManager.AppSettings["OutputPath"]}/stock-{time}.csv", headers.ToString() + Environment.NewLine);
                            }
                        }
                        //UpdateNightly(date, season);                        
                        Console.WriteLine(stopwatch.Elapsed);
                        Console.WriteLine("Stock file created on: " + DateTime.Now);
                        stopwatch.Stop();
                    }
                }
            }
            catch(Exception ex)
            {
                successful = false;
                new LogWriter().LogWrite(ex.Message);
                new LogWriter().LogWrite(ex.StackTrace);
            }
            finally
            {
                SendEmail(successful);
            }
        }

        public static String GetTimestamp(DateTime value)
        {
            return value.ToString("yyyyMMddHHmmssffff");
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

        private void SendEmail(bool sucessful)
        {
          new Emailer().SendStockGenerationEmail(sucessful);
        }

        private bool HasGeneratedForToday(DateTime date, string season)
        {
            CordnersEntities cordners = new CordnersEntities();
            var result = cordners.Nightly.FirstOrDefault(x => x.Date == date && x.Season == season);
            return result != null;
        }

        private void UpdateNightly(DateTime date, string season)
        {
            var nightly = new Nightly()
            {
                Date = date,
                Season = season
            };

            CordnersEntities cordners = new CordnersEntities();
            cordners.Nightly.Add(nightly);
            cordners.SaveChanges();
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
