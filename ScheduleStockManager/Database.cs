using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ScheduleStockManager;
using ScheduleStockManager.Mechanism;
using ScheduleStockManager.Models;

namespace StockCSV
{
    public class Database : Job
    {
        private readonly LogWriter _logger = new LogWriter();

        public void CreateDbfFile(List<string> t2TreFs)
        {
            using (var connection = new OleDbConnection(System.Configuration.ConfigurationManager.AppSettings["AccessConnectionString"]))
            {
                connection.Open();
                var command = connection.CreateCommand();

                command.CommandText = "create table Descriptions(T2TREF int)";
                command.ExecuteNonQuery();
                connection.Close();
            }

            using (var connection = new OleDbConnection())
            {
                connection.Open();
                var command = connection.CreateCommand();
                foreach (var t2Tref in t2TreFs)
                {
                    var sql = "Insert INTO DESCRIPT (T2TREF) VALUES ({0});";
                    sql = string.Format(sql, String.Format("{0:00000}", t2Tref));
                    command.CommandText = sql;
                    command.ExecuteNonQuery();
                }
            }
        }

        public override string DoJob(DataSet data)
        {
            try
            {
                if (data.Tables.Count == 0)
                    return "";
                var csv = new StringBuilder();
                var actualStock = "0";
                var inStockFlag = false;
                var groupSkus = "";

                foreach (DataRow dr in data.Tables[0].Rows)
                {
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
                                    if (String.IsNullOrEmpty(dr["LY" + i].ToString()))
                                    {
                                        actualStock = dr["QTY" + i].ToString();
                                    }
                                    else
                                    {
                                        actualStock =
                                            (Convert.ToInt32(dr["QTY" + i]) - Convert.ToInt32(dr["LY" + i]))
                                            .ToString();
                                    }

                                    isStock = 1;
                                    inStockFlag = true;
                                }
                                else
                                {
                                    isStock = 0;
                                }
                                var append = (1000 + i).ToString();
                                groupSkus = dr["NewStyle"].ToString();
                                var groupSkus2 = dr["NewStyle"] + append.Substring(1, 3);
                                var newLine = $"{"\"" + groupSkus2 + "\""},{"\"" + actualStock + "\""},{"\"" + isStock + "\""}";
                                csv.AppendLine(newLine);
                            }
                            actualStock = "0";
                        }
                    }

                    isStock = inStockFlag ? 1 : 0;
                    if (!string.IsNullOrEmpty(dr["NewStyle"].ToString()))
                    {
                        var newLine2 = $"{"\"" + groupSkus + "\""},{"\"" + actualStock + "\""},{"\"" + isStock + "\""}";
                        csv.AppendLine(newLine2);
                    }
                    inStockFlag = false;
                    if (data.Tables[0].Rows.Count > 1)
                    {
                        break;
                    }
                }
                Console.WriteLine("Job Finished");
                _logger.LogWrite("Finished");
                return csv.ToString();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                _logger.LogWrite(e.Message + e.StackTrace);
                throw;
            }
        }

        public override void DoCleanup()
        {
            Console.WriteLine("Clean up: removing exisiting stock.csv");
            if (File.Exists(System.Configuration.ConfigurationManager.AppSettings["OutputPath"]))
            {
                File.Delete(System.Configuration.ConfigurationManager.AppSettings["OutputPath"]);
            }
        }

        public override DataSet Connection(string reff)
        {
            using (var connectionHandler = new OleDbConnection(System.Configuration.ConfigurationManager.AppSettings["AccessConnectionString"]))
            {
                var dataset = new DataSet();
                for (var attempts = 0; attempts < 5; attempts++)
                {
                    try
                    {
                        var data = new DataSet();
                        connectionHandler.OpenAsync();
                        var myAccessCommand = new OleDbCommand(SqlQueries.StockQuery, connectionHandler);
                        myAccessCommand.Parameters.AddWithValue("?", reff);
                        var myDataAdapter = new OleDbDataAdapter(myAccessCommand);
                        myDataAdapter.Fill(data);
                        break;

                    }
                    catch { }
                    Thread.Sleep(50); // Possibly a good idea to pause here, explanation below
                }
                return dataset;
            }
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
    }
}
