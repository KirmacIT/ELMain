using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Timers;
using System.Net.Mail;
using System.Configuration;
using System.IO;
using System.Net.Mime;

namespace ELMain
{
    public class Mailer
    {
        EventLog moEventLog = new EventLog(typeof(Mailer).FullName);

        private MailMessage GenerateEmailMessage(string vsSendFrom, string vsFirstEmail, string vsSubject, StringBuilder vsBody, bool vbHighPriority)
        {
            MailMessage mail = new MailMessage();
            MailAddress MA_from = new MailAddress(vsSendFrom, Constants.EngineName, Encoding.UTF8);
            mail.From = MA_from;
            mail.IsBodyHtml = true;

            if (vsFirstEmail != string.Empty)
            {
                MailAddress MA_To = new MailAddress(vsFirstEmail, string.Empty, Encoding.UTF8);
                mail.To.Add(MA_To);
            }
            mail.Subject = vsSubject;
            mail.Priority = vbHighPriority ? MailPriority.High : MailPriority.Normal;

            if (vsBody.ToString() != String.Empty)
            {
                mail.Body = vsBody.ToString();
            }


            return mail;
        }

        public void SendEmailWithoutAttachement(string sEmailTo, StringBuilder vsBody, string vsSubject)
        {
            const string METHOD_NAME = "SendEmailWithoutAttachement";
            MailMessage mail;

            try
            {
                moEventLog.WriteLog(METHOD_NAME, "Method Start");

                if (vsBody.ToString() != String.Empty)
                {
                    mail = GenerateEmailMessage(Constants.EmailNoReplyUser, sEmailTo, vsSubject, vsBody, true);

                    SmtpClient smtp = new SmtpClient
                    {
                        Host = Constants.MailServer,
                        Port = Constants.MailPort,
                        Credentials = new System.Net.NetworkCredential(Constants.EmailNoReplyUser, Constants.EmailNoReplyPwd),
                        EnableSsl = true
                    };

                    try
                    {
                        smtp.Send(mail);
                    }
                    catch (Exception oExc)
                    {
                        throw new Exception(oExc.Message);
                    }
                    finally
                    {
                        smtp = null;
                    }
                }
            }
            catch (Exception oExc)
            {
                moEventLog.WriteLog(METHOD_NAME, oExc.Message, true);
            }
            finally
            {
                moEventLog.WriteLog(METHOD_NAME, "Method End");
            }
        }
    }
}
