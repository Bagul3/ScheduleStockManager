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
        private DataSet SeasonalData;
        private static DataSet REMTable;

        public Database()
        {
            SeasonalData = Connection(null, SqlQueries.FetchLatestSeaosn);
            REMTable = Connection(null, SqlQueries.FetchREM);
        }


        public override void InsertIntoDescriptions(string sku)
        {
            var doesSKUExist = Connection(sku, SqlQueries.DoesSKUExist);
            if (doesSKUExist.Tables[0].Rows.Count == 0)
            {
                Console.WriteLine("Adding new SKU to DESC Table: " + sku);
                Connection(sku, SqlQueries.InsertSKU);
            }           
        }

        public override string DoJob(DataRow dr, DataSet dt, List<string> t2TreFs)
        {
            try
            {
                if (dr == null)
                    return "";
                if (doneList.Contains(dr["NewStyle"].ToString()))
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

                                isStock = SantizeStock(actualStock) == "0" ? 0 : 1;

                                inStockFlag = true;
                            }
                            else
                            {
                                isStock = 0;
                            }

                            var colourCollection = GetAllColoursForSku(dr["NewStyle"].ToString().Substring(0,6), t2TreFs);

                            var append = (1000 + i).ToString();
                            groupSkus = dr["NewStyle"].ToString();
                            var groupSkus2 = dr["NewStyle"] + append.Substring(1, 3);

                            var eanRow = dt.Tables[0].Select("T2T_CODE = '" + groupSkus2 + "'").FirstOrDefault();
                            var eanCode = "";
                            if (eanRow != null)
                            {
                                eanCode = eanRow["EAN_CODE"].ToString();
                            }

                            DateTime date = DateTime.Now;
                            if (String.IsNullOrEmpty(dr["LASTDELV"].ToString()))
                            {
                                _logger.LogWrite("Setting default date for: " + dr["NewStyle"].ToString());
                            }
                            else
                            {
                                date = Convert.ToDateTime(dr["LASTDELV"]);
                            }

                            var rem1 = "\"" + GetREMValue(dr["REM"].ToString()) + "\"";
                            var rem2 = "\"" + GetREMValue(dr["REM2"].ToString()) + "\"";

                            var year = UpdateDeliveryDate(dr["USER1"].ToString(), date).ToString("yyyy/MM/dd");

                            var newLine = $"{"\"" + groupSkus2 + "\""},{"\"" + actualStock + "\""},{"\"" + isStock + "\""},{"\"" + year + "\""}" +
                                 $",{"\"" + RemoveLineEndings(eanCode) + "\""},{"\"" + dr["SELL"] + "\""}" +
                                 $",{"\"" + dr["USER1"] + "\""},{rem2},{rem1},{"\"1\""}";
                            // {"\"" + (isStock == 1 ? "2" : "4") + "\""}
                            csv.AppendLine(newLine);

                        }
                        actualStock = "0";
                    }
                }
                doneList.Add(dr["NewStyle"].ToString());


                isStock = inStockFlag ? 1 : 0;
                if (!string.IsNullOrEmpty(dr["NewStyle"].ToString()))
                {


                    DateTime date = DateTime.Now;
                    if (String.IsNullOrEmpty(dr["LASTDELV"].ToString()))
                    {
                        _logger.LogWrite("Setting default date for: " + dr["NewStyle"].ToString());
                    }
                    else
                    {
                        date = Convert.ToDateTime(dr["LASTDELV"]);
                    }

                    var rem1 = "\"" + GetREMValue(dr["REM"].ToString()) + "\"";
                    var rem2 = "\"" + GetREMValue(dr["REM2"].ToString()) + "\"";

                    var year = UpdateDeliveryDate(dr["USER1"].ToString(), date).ToString("yyyy/MM/dd");
                    var newLine2 = $"{"\"" + groupSkus + "\""},{"\"" + SantizeStock(actualStock) + "\""},{"\"" + isStock + "\""},{"\"" + year + "\""},{"\"" + empty + "\""},{"\"" + dr["SELL"] + "\""},{"\"" + dr["USER1"] + "\""},{rem2},{rem1},{"\"" + (isStock == 1 ? "4" : "2") + "\""}";
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

        private List<string> GetAllColoursForSku(string sku, List<string> t2TreFs)
        {
            var colours = t2TreFs.Where(x => x.Contains(sku)).ToList();
            return colours;
        }

        private string SantizeStock(string stock)
        {
            var i = 0;
            var result = int.TryParse(stock, out i);
            if (result)
                return Convert.ToInt32(stock) < 0 ? "0" : stock;
            new LogWriter("ERROR: invalid number " + stock);
            return stock;
        }

        private DateTime UpdateDeliveryDate(string season, DateTime date)
        {
            try
            {
                var delimiter = 10;
                if (SeasonalData != null)
                {
                    for(int i = 0; i < SeasonalData.Tables[0].Rows.Count; i++)
                    {
                        if (season.ToLower() == SeasonalData.Tables[0].Rows[i]["SEASON"].ToString().ToLower())
                        {
                            if (SeasonalData.Tables[0].Rows[i]["TOPPAGE"].ToString() == "true")
                            {
                                return date.AddYears(delimiter - Convert.ToInt32(SeasonalData.Tables[0].Rows[i]["ID"]));
                            }                            
                            else if (SeasonalData.Tables[0].Rows[i]["BOTTOMPAGE"].ToString() == "true")
                            {
                                return date.AddYears(-10);
                            }
                        }
                    }
                }
                
                return date;
            }
            catch(Exception e)
            {
                new LogWriter().LogWrite(e.Message);
                new LogWriter().LogWrite(e.StackTrace);
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

        private static string GetREMValue(string rem)
        {
            if (!string.IsNullOrEmpty(rem))
            {
                var remresult = REMTable.Tables[0].Select("Id = '" + rem + "'").FirstOrDefault();
                if (remresult != null)
                {
                    return remresult["NAME"].ToString();
                }
            }
            return "";
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
