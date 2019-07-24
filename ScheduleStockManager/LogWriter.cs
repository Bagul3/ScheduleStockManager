using System;
using System.IO;
using System.Reflection;

namespace ScheduleStockManager
{
    public class LogWriter
    {
        private string m_exePath = string.Empty;

        public LogWriter(string logMessage)
        {
            LogWrite(logMessage);
        }

        public LogWriter()
        {

        }

        public void LogWrite(string logMessage)
        {
            m_exePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            try
            {
                DeleteOldFiles();
                using (var w = File.AppendText(m_exePath + "\\" + "log.txt"))
                {
                    Log(logMessage, w);
                }
            }
            catch (Exception ex)
            {
            }
        }

        private void Log(string logMessage, TextWriter txtWriter)
        {
            try
            {
                txtWriter.Write("\r\nLog Entry : ");
                txtWriter.WriteLine("{0} {1}", DateTime.Now.ToLongTimeString(),
                    DateTime.Now.ToLongDateString());
                txtWriter.WriteLine("  :");
                txtWriter.WriteLine("  :{0}", logMessage);
                txtWriter.WriteLine("-------------------------------");
            }
            catch (Exception ex)
            {
            }
        }

        private void DeleteOldFiles()
        {
            FileInfo fi = new FileInfo(m_exePath + "\\" + "log.txt");
            if (DateTime.Now.Day == 3 || DateTime.Now.Day == 14)
                fi.Delete();
        }
    }
}