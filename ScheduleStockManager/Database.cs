using System;
using System.Data;
using System.Data.OleDb;
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
        private static DataSet REMTable;

        public Database()
        {
            REMTable = Connection(null, SqlQueries.FetchREM);
        }

        public string GenerateSimples(DataRow dr, DataSet dt)
        {
            try
            {
                var csv = new StringBuilder();
                var actualStock = "0";

                _logger.LogWrite("Working....");

                var isStock = 0;
                var minsize = Convert.ToInt32(dr["MINSIZE"]);
                var maxsize = Convert.ToInt32(dr["MAXSIZE"]);
                for (var i = minsize; i <= maxsize; i++)
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
                            }
                            else
                            {
                                isStock = 0;
                            }

                            var sku = GetSKU(dr, i);
                            var nightly = new NightlySimples()
                            {
                                SKU = sku,
                                StockLevel = actualStock,
                                IsInStock = isStock,
                                SortDate = GetLastDevlieryDate(dr),
                                EANCode = GetEANCode(dt, sku),
                                RRP = dr["SELL"].ToString(),
                                Season = dr["USER1"].ToString(),
                                Rem1 = GetREMValue(dr["REM"].ToString()),
                                Rem2 = GetREMValue(dr["REM2"].ToString())
                            };
                            csv.AppendLine(nightly.ToString());

                        }
                        actualStock = "0";
                    }
                }

                return csv.ToString();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                _logger.LogWrite(e.Message + e.StackTrace);
                throw;
            }
        }

        public string GenerateConfigurables(DataRow dr)
        {
            try
            {
                var csv = new StringBuilder();
                _logger.LogWrite("Working....");
                var nightly = new NightlyConfigurables()
                {
                    SKU = GetConfigurableSKU(dr),
                    SortDate = GetLastDevlieryDate(dr),
                    UDef2 = dr["MasterSubDept"].ToString(),
                    Type = dr["MasterDept"].ToString()
                };
                csv.AppendLine(nightly.ToString());
                return csv.ToString();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                _logger.LogWrite(e.Message + e.StackTrace);
                throw;
            }
        }

        public string GenerateEuro(DataRow dr, decimal conversion_rate)
        {
            var csv = new StringBuilder();
            var minsize = Convert.ToInt32(dr["MINSIZE"]);
            var maxsize = Convert.ToInt32(dr["MAXSIZE"]);
            for (var i = minsize; i <= maxsize; i++)
            {
                var gbp = Convert.ToDecimal(dr["SELL"].ToString());
                var euros = gbp * conversion_rate;
                var decimalPart = euros - Math.Truncate(euros);
                if ((decimalPart * 100) < 50)
                {
                    euros++;
                }
                var euroModel = new EuroModel()
                {
                    RRP = Math.Round(euros).ToString(),
                    SKU = GetSKU(dr, i),
                    SterlingRRP = gbp.ToString()
                };
                csv.AppendLine(euroModel.ToString());
            }
            return csv.ToString();
        }

        private static string GetSKU(DataRow dr, int i)
        {
            var append = (1000 + i).ToString();
            return dr["NewStyle"] + append.Substring(1, 3);
        }

        private static string GetConfigurableSKU(DataRow dr)
        {
            return dr["NewStyle"].ToString();
        }

        private DateTime GetLastDevlieryDate(DataRow dr)
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

            return date;
        }

        private static string GetEANCode(DataSet dt, string groupSkus2)
        {
            var eanRow = dt.Tables[0].Select("T2T_CODE = '" + groupSkus2 + "'").FirstOrDefault();
            var eanCode = "";
            if (eanRow != null)
            {
                eanCode = RemoveLineEndings(eanRow["EAN_CODE"].ToString());
            }

            return eanCode;
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

        public DataSet Connection(string reff, string query)
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

        public DataSet Connection(string reff, string lastmonth, string lastweek, string yesertday, string query)
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
            if (string.IsNullOrEmpty(value))
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
