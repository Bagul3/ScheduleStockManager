using System;
using System.Net;
using System.Net.Mail;
using System.Text;

namespace ScheduleStockManager
{
    class Emailer
    {
        public void SendStockGenerationEmail(bool success)
        {
            try
            {
                if (System.Configuration.ConfigurationManager.AppSettings["EmailGeneration"] =="True")
                {
                    SmtpClient mySmtpClient = CreateSmtpClient();
                    MailMessage email = success ? BuildSuccessEmail() : BuildErrorEmail();
                    mySmtpClient.Send(email);
                }                
            }

            catch (SmtpException ex)
            {
                new LogWriter().LogWrite("ERROR: Unable to Send email");
                new LogWriter().LogWrite(ex.Message);
                new LogWriter().LogWrite(ex.StackTrace);
            }
            catch (Exception ex)
            {
                new LogWriter().LogWrite(ex.Message);
                new LogWriter().LogWrite(ex.StackTrace);
            }
        }

        private MailMessage BuildSuccessEmail()
        {
            MailMessage myMail = new MailMessage();
            myMail.To.Add(new MailAddress("vandershannon@gmail.com", "Conor"));
            myMail.To.Add(new MailAddress("david@cordners.co.uk", "David"));
            myMail.From = new MailAddress("nightly@job.com", "Nightly Job");

            myMail.Subject = "Nightly Job: Sucessfully Generated";
            myMail.SubjectEncoding = Encoding.UTF8;

            myMail.Body = $"Nightly Job sucessfully generated stock files on {DateTime.Now}";
            myMail.BodyEncoding = Encoding.UTF8;

            myMail.IsBodyHtml = true;
            return myMail;
        }

        private MailMessage BuildErrorEmail()
        {
            MailMessage myMail = new MailMessage();
            myMail.To.Add(new MailAddress("vandershannon@gmail.com", "Conor"));
            myMail.To.Add(new MailAddress("david@cordners.co.uk", "David"));
            myMail.From = new MailAddress("nightly@job.com", "Nightly Job");

            myMail.Subject = "Nightly Job: Error Generated";
            myMail.SubjectEncoding = Encoding.UTF8;

            myMail.Body = $"Nightly Job was unsucessfully generated stock files on {DateTime.Now}";
            myMail.BodyEncoding = Encoding.UTF8;

            myMail.IsBodyHtml = true;
            return myMail;
        }

        private SmtpClient CreateSmtpClient()
        {
            return new SmtpClient
            {
                Host = "smtp-relay.sendinblue.com",
                Port = 587,
                EnableSsl = true,
                UseDefaultCredentials = false,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Credentials = new NetworkCredential(
                                    userName: "cshannon@rapid7.com",
                                    password: "Fy4G0gwEmR7Ox1Ua"
                )
            };
        }
    }
}
