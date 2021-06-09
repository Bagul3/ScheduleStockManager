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
