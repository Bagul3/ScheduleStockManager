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
using System.Net;
using System.IO.Compression;

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
                Console.WriteLine("Loading Cornders Stock into memory");
                var allcordnersStock = database.Connection(null, SqlQueries.StockQueryALL);
                Console.WriteLine("Cleanup existing files");
                DoCleanup();

                if(System.Configuration.ConfigurationManager.AppSettings["GenerateEuro"].ToUpper() == "TRUE")
                {
                    Console.WriteLine("Updating USD price");
                    BuildEuro(database);
                }
                else if (System.Configuration.ConfigurationManager.AppSettings["GenerateUSD"].ToUpper() == "TRUE")
                {
                    Console.WriteLine("Updating USD price");
                    type = "USD";
                    BuildUSD(database);
                }
                else if (System.Configuration.ConfigurationManager.AppSettings["GenerateAUD"].ToUpper() == "TRUE")
                {
                    Console.WriteLine("Updating AUD price");
                    type = "AUD";
                    BuildAUD(database);
                }
                else if (generateSimple.ToUpper() == "TRUE")
                {
                    Console.WriteLine("Getting SKUs from online file");
                    var skuFromOnline = _cordnersService.RetrieveStockFromOnlineSimples();
                    Console.WriteLine("Building simple stock");
                    type = "SIMPLE";
                    BuildSimples(skuFromOnline, allcordnersStock, database);
                }
                else
                {
                    Console.WriteLine("Getting SKUs from online file");
                    var skuFromOnline = _cordnersService.RetrieveStockFromOnline();
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

        private void BuildEuro(Database database)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            Console.WriteLine("Getting exchange rate");
            var exchange_rate = GetExchangeRate();
            new LogWriter().LogWrite("EXCHANGE RATE: " + exchange_rate);
            database.Connection(null, SqlQueries.DeleteEuro);
            database.Connection(exchange_rate.ToString(), SqlQueries.InsertEuro);
        }

        private void BuildUSD(Database database)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            Console.WriteLine("Getting exchange rate");
            var exchange_rate = GetExchangeRate(true);
            new LogWriter().LogWrite("EXCHANGE RATE: " + exchange_rate);
            database.Connection(null, SqlQueries.DeleteUSD);
            database.Connection(exchange_rate.ToString(), SqlQueries.InsertUSD);
        }

        private void BuildAUD(Database database)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            Console.WriteLine("Getting exchange rate");
            var exchange_rate = GetExchangeRate(false, true);
            new LogWriter().LogWrite("EXCHANGE RATE: " + exchange_rate);
            database.Connection(null, SqlQueries.DeleteAUD);
            database.Connection(exchange_rate.ToString(), SqlQueries.InsertAUD);
        }

        private decimal FetchExchangeRate(Database database)
        {
            var ds = database.Connection(null, SqlQueries.FetchEUROPrice);
            return Convert.ToDecimal(ds.Tables[0].Rows[0]["PRICE"].ToString());
        }

        private decimal FetchExchangeRateUSD(Database database)
        {
            var ds = database.Connection(null, SqlQueries.FetchUSDPrice);
            return Convert.ToDecimal(ds.Tables[0].Rows[0]["PRICE"].ToString());
        }

        private decimal FetchExchangeRateAUD(Database database)
        {
            var ds = database.Connection(null, SqlQueries.FetchAUDPrice);
            return Convert.ToDecimal(ds.Tables[0].Rows[0]["PRICE"].ToString());
        }

        private const string ChromeDriverDownloadPage = "https://googlechromelabs.github.io/chrome-for-testing/#stable";

        private string FetchChromeDriverDownloadUrl()
        {
            string downloadUrl = null;

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

            using (WebClient client = new WebClient())
            {
                string pageHtml = client.DownloadString(ChromeDriverDownloadPage);
                var doc = new HtmlDocument();
                doc.LoadHtml(pageHtml);

                // Find the URL for the stable chromedriver_win32 zip using the provided XPath
                var node = doc.DocumentNode.SelectSingleNode("//*[@id='stable']/p/code[1]");
                if (node != null)
                {
                    downloadUrl = "https://storage.googleapis.com/chrome-for-testing-public/" + node.InnerText.Trim() + "/win32/chromedriver-win32.zip";
                }
            }

            if (string.IsNullOrEmpty(downloadUrl))
            {
                new LogWriter("Unable to find the stable chromedriver_win32 download URL.");
                throw new Exception("Unable to find the stable chromedriver_win32 download URL.");
            }

            return downloadUrl;
        }

        private void DownloadAndExtractChromeDriver(string downloadUrl)
        {
            string zipPath = Path.Combine(Directory.GetCurrentDirectory(), "chromedriver.zip");
            string extractPath = Directory.GetCurrentDirectory();
            string driverFilePath = Path.Combine(extractPath + "\\chromedriver-win32", "chromedriver.exe");

            using (WebClient client = new WebClient())
            {
                Console.WriteLine("Downloading ChromeDriver from: " + downloadUrl);
                new LogWriter("Downloading ChromeDriver from: " + downloadUrl);
                client.DownloadFile(downloadUrl, zipPath);
            }

            // Delete existing files before extracting new ones
            if (File.Exists(driverFilePath))
            {
                Console.WriteLine("Deleting existing chromedriver.exe");
                new LogWriter("Deleting existing chromedriver.exe");
                File.Delete(driverFilePath);
            }

            string licenseFilePath = Path.Combine(extractPath + "\\chromedriver-win32", "LICENSE.chromedriver");
            if (File.Exists(licenseFilePath))
            {
                Console.WriteLine("Deleting existing LICENSE.chromedriver");
                new LogWriter("Deleting existing LICENSE.chromedriver");
                File.Delete(licenseFilePath);
            }

            new LogWriter("Extracting ChromeDriver to: " + extractPath + "\\chromedriver-win32");
            Console.WriteLine("Extracting ChromeDriver to: " + extractPath + "\\chromedriver-win32");
            ZipFile.ExtractToDirectory(zipPath, extractPath);
            File.Delete(zipPath);

            // Verify the ChromeDriver was extracted correctly
            if (File.Exists(driverFilePath))
            {
                Console.WriteLine("chromedriver.exe successfully extracted to: " + driverFilePath);
                new LogWriter("chromedriver.exe successfully extracted to: " + driverFilePath);
            }
            else
            {
                new LogWriter("chromedriver.exe was not found after extraction.");
                throw new Exception("chromedriver.exe was not found after extraction.");
            }
        }

        public decimal GetExchangeRate(bool isUSD = false, bool isAUD = false)
        {
            ChromeDriver browser = null;
            string tempFolderPath = null;
            try
            {
                // Fetch the download URL for the latest stable chromedriver_win32
                string downloadUrl = FetchChromeDriverDownloadUrl();
                Console.WriteLine("ChromeDriver download URL: " + downloadUrl);

                // Download and extract the ChromeDriver
                tempFolderPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                Directory.CreateDirectory(tempFolderPath);

                string zipFilePath = Path.Combine(tempFolderPath, "chromedriver.zip");
                string extractFolderPath = Path.Combine(tempFolderPath, "chromedriver-win32");

                using (WebClient client = new WebClient())
                {
                    client.DownloadFile(downloadUrl, zipFilePath);
                }

                System.IO.Compression.ZipFile.ExtractToDirectory(zipFilePath, tempFolderPath);

                string driverFilePath = Path.Combine(extractFolderPath, "chromedriver.exe");

                var chromeOptions = new ChromeOptions();
                chromeOptions.AddArguments("headless");
                chromeOptions.AddArguments("--disable-gpu");
                chromeOptions.AddArguments("--no-sandbox");
                chromeOptions.AddArguments("--disable-dev-shm-usage");

                // Use ChromeDriverService to specify the path to chromedriver.exe
                var chromeDriverService = ChromeDriverService.CreateDefaultService(extractFolderPath, "chromedriver.exe");
                chromeDriverService.HideCommandPromptWindow = true;

                browser = new ChromeDriver(chromeDriverService, chromeOptions);

                string url = "https://www.xe.com/currencyconverter/convert/?Amount=1&From=GBP&To=EUR";
                if (isUSD)
                    url = "https://www.xe.com/currencyconverter/convert/?Amount=1&From=GBP&To=USD";
                else if (isAUD)
                    url = "https://www.xe.com/currencyconverter/convert/?Amount=1&From=GBP&To=AUD";

                browser.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(60);
                browser.Navigate().GoToUrl(url);

                // Wait for the element containing the exchange rate to be present
                new WebDriverWait(browser, TimeSpan.FromSeconds(60)).Until(ExpectedConditions.ElementExists(By.XPath("//p[contains(text(), 'British Pound =')]/following-sibling::p[1]")));

                var doc = new HtmlDocument();
                doc.LoadHtml(browser.PageSource);

                // Extract the exchange rate text using the correct XPath
                var exchange_rate_node = doc.DocumentNode.SelectSingleNode("//p[contains(text(), 'British Pound =')]/following-sibling::p[1]");
                if (exchange_rate_node == null)
                {
                    throw new Exception("Exchange rate element not found.");
                }

                string exchange_rate_text = exchange_rate_node.InnerText.Split(' ')[0];
                if (string.IsNullOrEmpty(exchange_rate_text))
                {
                    throw new Exception("Exchange rate text is empty.");
                }

                decimal exchange_rate = Convert.ToDecimal(exchange_rate_text);

                return Math.Round(exchange_rate, 2);
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine("Unable to get exchange rate!");
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
                throw;
            }
            finally
            {
                // Ensure the browser is closed properly
                if (browser != null)
                {
                    browser.Close();
                    browser.Quit();
                }

                // Clean up the temporary folder
                if (tempFolderPath != null && Directory.Exists(tempFolderPath))
                {
                    Directory.Delete(tempFolderPath, true);
                }
            }
        }


        private void BuildParent(List<string> skuFromOnline, DataSet allcordnersStock, Database database)
        {
            var headers = $"{"\"" + "sku" + "\""},{"\"" + "sort_date" + "\""},{"\"" + "udef2" + "\""},{"\"" + "type" + "\""},{"\"" + "REM1" + "\""},{"\"" + "REM2" + "\""}";
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
            var headers = $"{"\"" + "sku" + "\""},{"\"" + "qty" + "\""},{"\"" + "is_in_stock" + "\""},{"\"" + "ean" + "\""},{"\"" + "price" + "\""},{"\"" + "season" + "\""},{"\"" + "visibility" + "\""},{"\"" + "euro" + "\""},{"\"" + "usd" + "\""},{"\"" + "aud" + "\""}";            

            Console.WriteLine("Gathering EAN Codes");
            var eanDataset = database.Connection(null, SqlQueries.GetEanCodes);                        

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var time = GetTimestamp(DateTime.Now);
            Console.WriteLine("Getting exchange rate");
            var exchange_rate = FetchExchangeRate(database);
            var exchange_rate_usd = FetchExchangeRateUSD(database);
            var exchange_rate_aud = FetchExchangeRateAUD(database);
            var output = System.Configuration.ConfigurationManager.AppSettings["OutputPath"];            

            File.AppendAllText($"{output}/stock-{time}.csv", headers.ToString() + Environment.NewLine);
            foreach (string sku in skuFromOnline)
            {
                var rows = allcordnersStock.Tables[0].Select($"NEWSTYLE = {sku}");
                foreach (DataRow reff in rows)
                {
                    var result = database.GenerateSimples(reff, eanDataset, exchange_rate, exchange_rate_usd, exchange_rate_aud);
                    if (result != "")
                        File.AppendAllText($"{output}/stock-{time}.csv", result.ToString());
                }
                if (new FileInfo($"{output}/stock-{time}.csv").Length > 1572864)
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
