using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;

namespace ELMain
{
    public static class Constants
    {
        public static readonly string NewLine = Environment.NewLine;
        public static string CrLf => $"{Cr}{Lf}";

        public const char Cr = '\r';
        public const char Lf = '\n';
        public const char FormFeed = '\f';
        public const char Null = '\0';
        public const char BackSpace = '\b';

        public const char Tab = '\t';
        public const char VertTab = '\v';

        public const string STATUS_PREPARED = "Prepared";
        public const string STATUS_WAITING = "Waiting";
        public const string STATUS_ERROR = "Error";
        public const string STATUS_SUCCESS = "Success";
        public const string STATUS_IGNORE = "Ignore";

        public static bool LogEvents
        {
            get
            {
                return Convert.ToBoolean(ConfigurationManager.AppSettings["LogEvents"]);
            }
        }


        public static String LogDir
        {
            get
            {
                return Convert.ToString(ConfigurationManager.AppSettings["LogDir"]);
            }
        }

        public static String AutodeleteDays
        {
            get
            {
                return Convert.ToString(ConfigurationManager.AppSettings["AutodeleteDays"]);
            }
        }


        public static Int32? ConvertToNullableInteger(string vsInput)
        {

            if (vsInput == String.Empty)
            {
                return null;
            }
            else
            {
                if (vsInput.ToUpper() == "NULL")
                {
                    return null;
                }
                else
                {
                    return Convert.ToInt32(vsInput);
                }
            }
        }
 
 

        public static string EmailNoReplyPwd
        {
            get
            {
                return ConfigurationManager.AppSettings["EmailNoReplyPwd"].ToString();
            }
        }

        public static string EmailNoReplyUser
        {
            get
            {
                return ConfigurationManager.AppSettings["EmailNoReplyUser"].ToString();
            }
        }

        public static string MailServer
        {
            get
            {
                return ConfigurationManager.AppSettings["MailServer"].ToString();
            }
        }

        //MailPort
        public static int MailPort
        {
            get
            {
                return Convert.ToInt32(ConfigurationManager.AppSettings["MailPort"].ToString());
            }
        }

        public static string EngineName
        {
            get
            {
                return ConfigurationManager.AppSettings["EngineName"].ToString();
            }
        }

    }
}
