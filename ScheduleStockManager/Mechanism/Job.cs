using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using ScheduleStockManager.Models;
using ScheduleStockManager.Service;
using StockCSV;
using HtmlAgilityPack;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using OpenQA.Selenium;

namespace ScheduleStockManager.Mechanism
{
    public class Job
    {
        private CordnerService _cordnersService;        
        public Job()
        {
            _cordnersService = new CordnerService();            
        }

        public void ExecuteJob()
        {
            var successful = true;
            var type = "";
            Database database = new Database();
            try
            {
                if (DateTime.Now.DayOfWeek == DayOfWeek.Sunday)
                    Environment.Exit(0);

                var generateSimple = System.Configuration.ConfigurationManager.AppSettings["GenerateSimple"];
                Console.WriteLine("Getting SKUs from online file");
                var skuFromOnline = _cordnersService.RetrieveStockFromOnline();
                Console.WriteLine("Loading Cornders Stock into memory");
                var allcordnersStock = database.Connection(null, SqlQueries.StockQueryALL);
                Console.WriteLine("Cleanup existing files");
                DoCleanup();

                if(System.Configuration.ConfigurationManager.AppSettings["GenerateEuro"].ToUpper() == "TRUE")
                {
                    Console.WriteLine("Building euro");
                    type = "EURO";
                    BuildEuro(skuFromOnline, allcordnersStock, database);
                }
                else if (generateSimple.ToUpper() == "TRUE")
                {
                    Console.WriteLine("Building simple stock");
                    type = "SIMPLE";
                    BuildSimples(skuFromOnline, allcordnersStock, database);
                }
                else
                {
                    Console.WriteLine("Building configurable stock");
                    type = "CONFIGURABLES";
                    BuildParent(skuFromOnline, allcordnersStock, database);
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
                _cordnersService.SendEmail(successful, type);                
            }
        }

        private void BuildEuro(List<string> skuFromOnline, DataSet allcordnersStock, Database database)
        {
            var headers = $"{"\"" + "sku" + "\""},{"\"" + "euro" + "\""},{"\"" + "gbp" + "\""}";

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var time = GetTimestamp(DateTime.Now);
            var output = System.Configuration.ConfigurationManager.AppSettings["OutputPath"];

            Console.WriteLine("Getting exchange rate");
            var exchange_rate = GetExchangeRate();
            new LogWriter().LogWrite("EXCHANGE RATE: " + exchange_rate);

            File.AppendAllText($"{output}/stock-euro-{time}.csv", headers.ToString() + Environment.NewLine);

            foreach (string sku in skuFromOnline)
            {
                var rows = allcordnersStock.Tables[0].Select($"REF = {sku}");
                foreach (DataRow reff in rows)
                {
                    var result = database.GenerateEuro(reff, exchange_rate);
                    if (result != "")
                        File.AppendAllText($"{output}/stock-euro-{time}.csv", result.ToString());
                }
                if (new FileInfo($"{output}/stock-euro-{time}.csv").Length > 1887436)
                {
                    time = GetTimestamp(DateTime.Now);
                    File.AppendAllText($"{output}/stock-euro-{time}.csv", headers.ToString() + Environment.NewLine);
                    Console.WriteLine(stopwatch.Elapsed);
                    Console.WriteLine("Stock file created on: " + DateTime.Now);
                    stopwatch.Stop();
                    stopwatch.Start();
                }
            }
        }

        private decimal GetExchangeRate()
        {
            var chromeOptions = new ChromeOptions();
            chromeOptions.AddArguments("headless");
            var browser = new ChromeDriver(Environment.CurrentDirectory, chromeOptions);
            try
            {
                string url = "https://ulsterbanktravelmoney.com/travel-money/buy-euros";
                browser.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(60);
                browser.Navigate().GoToUrl(url);
                new WebDriverWait(browser, TimeSpan.FromSeconds(60)).Until(ExpectedConditions.ElementExists(By.XPath("//*[text() = 'Exchange rate']/following-sibling::span")));

                var doc = new HtmlDocument();
                doc.LoadHtml(browser.PageSource);
                var exchange_rate_text = doc.DocumentNode.SelectSingleNode("//*[text() = 'Exchange rate']/following-sibling::span");
                var exhange_rate = Convert.ToDecimal(exchange_rate_text.InnerText.Split('€')[1]);
                
                var decimalPart = exhange_rate - Math.Truncate(exhange_rate);

                if (decimalPart.ToString().Length > 5)
                {
                    if ((Convert.ToInt32(decimalPart.ToString().Substring(4, 2)) < 50))
                    {
                        exhange_rate += 0.01m;
                    }
                }
                
                return Math.Round(exhange_rate, 2);
            }
            catch(Exception ex)
            {
                new LogWriter("Unable to get exchange rate!");
                throw ex;
            }
            finally
            {
                browser.Close();
                browser.Quit();
            }

        }

        private void BuildParent(List<string> skuFromOnline, DataSet allcordnersStock, Database database)
        {
            var headers = $"{"\"" + "sku" + "\""},{"\"" + "sort_date" + "\""},{"\"" + "udef2" + "\""},{"\"" + "type" + "\""}";
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var time = GetTimestamp(DateTime.Now);
            var output = System.Configuration.ConfigurationManager.AppSettings["OutputPath"];

            File.AppendAllText($"{output}/stock-configurables-{time}.csv", headers.ToString() + Environment.NewLine);
            foreach (string sku in skuFromOnline)
            {
                var rows = allcordnersStock.Tables[0].Select($"REF = {sku}");
                foreach (DataRow reff in rows)
                {
                    var result = database.GenerateConfigurables(reff);
                    if (result != "")
                        File.AppendAllText($"{output}/stock-configurables-{time}.csv", result.ToString());
                }
            }
        }

        private void BuildSimples(List<string> skuFromOnline, DataSet allcordnersStock, Database database)
        {
            var headers = $"{"\"" + "sku" + "\""},{"\"" + "qty" + "\""},{"\"" + "is_in_stock" + "\""},{"\"" + "sort_date" + "\""},{"\"" + "ean" + "\""},{"\"" + "price" + "\""},{"\"" + "season" + "\""},{"\"" + "rem1" + "\""},{"\"" + "rem2" + "\""},{"\"" + "visibility" + "\""}";            

            Console.WriteLine("Gathering EAN Codes");
            var eanDataset = database.Connection(null, SqlQueries.GetEanCodes);                        

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var time = GetTimestamp(DateTime.Now);
            var output = System.Configuration.ConfigurationManager.AppSettings["OutputPath"];            

            File.AppendAllText($"{output}/stock-{time}.csv", headers.ToString() + Environment.NewLine);
            foreach (string sku in skuFromOnline)
            {
                var rows = allcordnersStock.Tables[0].Select($"REF = {sku}");
                foreach (DataRow reff in rows)
                {
                    var result = database.GenerateSimples(reff, eanDataset);
                    if (result != "")
                        File.AppendAllText($"{output}/stock-{time}.csv", result.ToString());
                }
                if (new FileInfo($"{output}/stock-{time}.csv").Length > 1887436)
                {
                    time = GetTimestamp(DateTime.Now);
                    File.AppendAllText($"{output}/stock-{time}.csv", headers.ToString() + Environment.NewLine);
                    Console.WriteLine(stopwatch.Elapsed);
                    Console.WriteLine("Stock file created on: " + DateTime.Now);
                    stopwatch.Stop();
                    stopwatch.Start();
                }
            }
        }

        private void DoCleanup()
        {
            Console.WriteLine($"The Clean Job thread started successfully.");
            new LogWriter("The Clean Job thread started successfully");
            Console.WriteLine("Clean up: removing exisiting stock.csv");

            foreach (string file in Directory.EnumerateFiles(System.Configuration.ConfigurationManager.AppSettings["OutputPath"], "*.csv"))
            {
                File.Delete(file);
            }
        }

        private static string GetTimestamp(DateTime value)
        {
            return value.ToString("yyyyMMddHHmmssffff");
        }
        
    }
}
