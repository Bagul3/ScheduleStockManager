using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;

namespace ScheduleStockManager.Service
{
    class CordnerService
    {
        public List<string> RetrieveStockFromOnline()
        {
            var fileList = GetCSV("https://www.cordners.co.uk/exportcsv/");
            string[] tempStr;
            tempStr = fileList.Split('\t');
            var skus = new List<string>();
            var actualList = new List<string>();


            //If we don't have duplicated values we know that configurable has been deleted
            var shouldAdd = false;

            foreach (var item in tempStr)
            {
                if (!string.IsNullOrEmpty(item))
                {
                    if (item.Contains('\n') && item.Split('\n')[0].Length >= 6)
                    {
                        var sku = item.Split('\n')[0].Substring(0, 6);
                        if (!skus.Contains(sku))
                        {
                            skus.Add(sku);
                            shouldAdd = true;
                        }
                        else if (shouldAdd)
                        {
                            actualList.Add(sku);
                            shouldAdd = false;
                        }
                    }
                }
            }
            var diff = skus.Where(item => !actualList.Any(e => item == e)).ToList();
            return actualList;
        }

        public List<string> RetrieveStockFromOnlineSimples()
        {
            var fileList = GetCSV("https://www.cordners.co.uk/exportcsv/");
            string[] tempStr;
            tempStr = fileList.Split('\t');
            var skus = new List<string>();
            var actualList = new List<string>();


            //If we don't have duplicated values we know that configurable has been deleted
            var shouldAdd = false;

            foreach (var item in tempStr)
            {
                if (!string.IsNullOrEmpty(item))
                {
                    if (item.Contains('\n') && item.Split('\n')[0].Length >= 9)
                    {
                        var sku = item.Split('\n')[0].Substring(0, 9);
                        if (!skus.Contains(sku))
                        {
                            skus.Add(sku);
                            shouldAdd = true;
                        }
                        else if (shouldAdd)
                        {
                            actualList.Add(sku);
                            shouldAdd = false;
                        }
                    }
                }
            }
            var diff = skus.Where(item => !actualList.Any(e => item == e)).ToList();
            return actualList;
        }

        public void SendEmail(bool sucessful, string type)
        {
            new Emailer().SendStockGenerationEmail(sucessful, type);
        }

        private string GetCSV(string url)
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
    }
}
