using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.IO;
using System.Linq;
using System.Text;
using ScheduleStockManager;
using ScheduleStockManager.Mechanism;
using ScheduleStockManager.Models;

namespace StockCSV
{
    public class Database : Job
    {
        private readonly LogWriter _logger = new LogWriter();
        private List<string> doneList = new List<string>();

        public override void InsertIntoDescriptions(string sku)
        {
            var doesSKUExist = Connection(sku, SqlQueries.DoesSKUExist);
            if (doesSKUExist.Tables[0].Rows.Count == 0)
            {
                Console.WriteLine("Adding new SKU to DESC Table: " + sku);
                Connection(sku, SqlQueries.InsertSKU);
            }                
        }

        public override string DoJob(DataRow dr, DataSet ean, List<REMModel> rem1, List<REMModel> rem2)
        {
            try
            {
                if (dr == null)
                    return "";
                var csv = new StringBuilder();
                var actualStock = "0";
                var inStockFlag = false;
                var groupSkus = "";
                var empty = "";

                _logger.LogWrite("Working....");

                var isStock = 0;
                for (var i = 1; i < 14; i++)
                {
                    if (!string.IsNullOrEmpty(dr["QTY" + i].ToString()))
                    {
                        if (dr["QTY" + i].ToString() != "")
                        {
                            if (Convert.ToInt32(dr["QTY" + i]) > 0)
                            {
                                if (dr["LY" + i].ToString() == "0" ||
                                    string.IsNullOrEmpty(dr["LY" + i].ToString()))
                                {
                                    actualStock = dr["QTY" + i].ToString();
                                }
                                else
                                {
                                    actualStock =
                                        (Convert.ToInt32(dr["QTY" + i]) - Convert.ToInt32(dr["LY" + i]))
                                        .ToString();
                                }

                                isStock = actualStock == "0" ? 0 : 1;

                                inStockFlag = true;
                            }
                            else
                            {
                                isStock = 0;
                            }
                            var append = (1000 + i).ToString();
                            groupSkus = dr["NewStyle"].ToString();
                            var groupSkus2 = dr["NewStyle"] + append.Substring(1, 3);

                            var eanRow = ean.Tables[0].Select("T2T_CODE = '" + groupSkus2 + "'").FirstOrDefault();
                            var eanCode = "";
                            if (eanRow != null)
                            {
                                eanCode = eanRow["EAN_CODE"].ToString();
                            }

                            var year = IncreaseYearIfCurrentSeason(dr["USER1"].ToString(), Convert.ToDateTime(dr["LASTDELV"])).ToString("yyyy/MM/dd");
                            var rem1Code = rem1.FirstOrDefault(x => x.T2T_Id == dr["REM"].ToString())?.Name;
                            rem1Code = rem1Code ?? "";
                            var rem2Code = rem1.FirstOrDefault(x => x.T2T_Id == dr["REM2"].ToString())?.Name;

                            var newLine = $"{"\"" + groupSkus2 + "\""},{"\"" + actualStock + "\""},{"\"" + isStock + "\""},{"\"" + year + "\""},{"\"" + RemoveLineEndings(eanCode) + "\""},{"\"" + dr["SELL"] + "\""}," +
                                $"{"\"" + rem1Code + "\""},{"\"" + rem2Code + "\""},{"\"" + dr["USER1"] + "\""}";
                            csv.AppendLine(newLine);

                        }
                        actualStock = "0";
                    }
                }
                doneList.Add(dr["NewStyle"].ToString());


                isStock = inStockFlag ? 1 : 0;
                if (!string.IsNullOrEmpty(dr["NewStyle"].ToString()))
                {
                    var year = IncreaseYearIfCurrentSeason(dr["USER1"].ToString(), Convert.ToDateTime(dr["LASTDELV"])).ToString("yyyy/MM/dd");
                    var rem1Code = rem1.FirstOrDefault(x => x.T2T_Id == dr["REM"].ToString())?.Name;
                    rem1Code = rem1Code ?? "";
                    var rem2Code = rem1.FirstOrDefault(x => x.T2T_Id == dr["REM2"].ToString())?.Name;

                    var newLine2 = $"{"\"" + groupSkus + "\""},{"\"" + actualStock + "\""},{"\"" + isStock + "\""},{"\"" + year + "\""}," +
                        $"{"\"" + empty + "\""},{"\"" + dr["SELL"] + "\""},{"\"" + rem1Code + "\""},{"\"" + rem2Code + "\""},{"\"" + dr["USER1"] + "\""}";
                    csv.AppendLine(newLine2);
                }

                return csv.ToString();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                _logger.LogWrite(e.Message + e.StackTrace);
                throw;
            }
            finally
            {

            }
        }

        private DateTime IncreaseYearIfCurrentSeason(string season, DateTime date)
        {
            //if (season.Length != 3 || !season.ToLower().Contains('s') || !season.ToLower().Contains('w'))
            //    return date;

            //var year = DateTime.Now.Year.ToString().Split(new string[] { "20" }, StringSplitOptions.None)[1];
            //var seasonYear = season.ToLower().Split('s')[1];
            //if (year == seasonYear)
            //    return date.AddYears(1);
            if(season.ToLower() == System.Configuration.ConfigurationManager.AppSettings["Season"].ToLower())
            {
                return date.AddYears(1);
            }
            return date;
        }

        public override void DoCleanup()
        {
            Console.WriteLine($"The Clean Job thread started successfully.");
            new LogWriter("The Clean Job thread started successfully");
            Console.WriteLine("Clean up: removing exisiting stock.csv");
            if (File.Exists(System.Configuration.ConfigurationManager.AppSettings["OutputPath"]))
            {
                File.Delete(System.Configuration.ConfigurationManager.AppSettings["OutputPath"]);
            }
        }

        public override DataSet Connection(string reff, string query)
        {
            var dataset = new DataSet();
            using (var connectionHandler = new OleDbConnection(System.Configuration.ConfigurationManager.AppSettings["AccessConnectionString"]))
            {
                connectionHandler.Close();
                connectionHandler.Open();
                var myAccessCommand = new OleDbCommand(query, connectionHandler);
                if (reff != null)
                {
                    myAccessCommand.Parameters.AddWithValue("?", reff);
                }

                try
                {
                    var myDataAdapter = new OleDbDataAdapter(myAccessCommand);
                    myDataAdapter.Fill(dataset);
                }
                catch(Exception e)
                {
                    Console.WriteLine(e.Message);
                }
                finally
                {
                    connectionHandler.Close();
                }
            }
            return dataset;
        }

        public override DataSet Connection(string reff, string lastmonth, string lastweek, string yesertday, string query)
        {
            var dataset = new DataSet();
            using (var connectionHandler = new OleDbConnection(System.Configuration.ConfigurationManager.AppSettings["AccessConnectionString"]))
            {
                connectionHandler.OpenAsync();
                var myAccessCommand = new OleDbCommand(query, connectionHandler);
                if (reff != null)
                {
                    myAccessCommand.Parameters.AddWithValue("?", lastmonth);
                    myAccessCommand.Parameters.AddWithValue("?", lastweek);
                    myAccessCommand.Parameters.AddWithValue("?", yesertday);
                    myAccessCommand.Parameters.AddWithValue("?", reff);
                }

                var myDataAdapter = new OleDbDataAdapter(myAccessCommand);
                myDataAdapter.Fill(dataset);
            }
            return dataset;
        }


        public override bool IsRepeatable()
        {
            return true;
        }

        public override int GetRepetitionIntervalTime()
        {
            return 5000;
        }

        public override TimeSpan GetStartTime()
        {
            return TimeSpan.Parse(System.Configuration.ConfigurationManager.AppSettings["StartTime"]);
        }

        public override TimeSpan GetEndTime()
        {
            return TimeSpan.Parse(System.Configuration.ConfigurationManager.AppSettings["EndTime"]);
        }

        public static string RemoveLineEndings(string value)
        {
            if (String.IsNullOrEmpty(value))
            {
                return value;
            }

            string lineSeparator = ((char) 0x2028).ToString();
            string paragraphSeparator = ((char) 0x2029).ToString();

            return value.Replace("\r\n", string.Empty)
                .Replace("\n", string.Empty)
                .Replace("\r", string.Empty)
                .Replace(lineSeparator, string.Empty)
                .Replace(paragraphSeparator, string.Empty);
        }

        
    }
}
