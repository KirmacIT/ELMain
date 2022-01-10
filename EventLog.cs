using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.IO;


namespace ELMain
{

    public class EventLog
    {
        private const string MODULE_NAME = "EventLog";

        private string msComponentName;
        private string msModuleName;
        private bool mbLogEvents = false;
        private string msLogDir;
        private string msLogFilePath;
        private string msErrLogFilePath;
        private string msDate = "";
        private int miAutodeleteDays;
        private bool mbLogDirectoryChecked = false;
        private bool mbLogFileChecked = false;
        private bool mbErrLogFileChecked = false;
        private StreamWriter moStreamWriter;

        private static System.Threading.Mutex moMutex;

        public EventLog(string vsComponentModule) : base()
        {
            int iPos;

            //iPos = Strings.InStr(vsComponentModule, ".");
            iPos = vsComponentModule.IndexOf(".");
            if (iPos > 0)
            {
                msComponentName = vsComponentModule.Substring(0, iPos);
                msModuleName = vsComponentModule.Substring(iPos + 1);
                //msModuleName = Strings.Mid(vsComponentModule, iPos + 1);
            }
            else
            {
                msComponentName = vsComponentModule;
                msModuleName = "<Unspecified>";
            }

            // initialize module level valiables (log directory, log file, etc.)
            InitializeLogSettings();
        }

        public EventLog(string vsComponent, string vsModule) : base()
        {
            msComponentName = vsComponent;
            msModuleName = vsModule;

            // initialize module level valiables (log directory, log file, etc.)
            InitializeLogSettings();
        }

        public bool LogEvents
        {
            get
            {
                return mbLogEvents;
            }
            set
            {
                mbLogEvents = value;
            }
        }

        public string LogDir
        {
            get
            {
                return msLogDir;
            }
            set
            {
                msLogDir = value;
            }
        }

        //public string LogonUser
        //{
        //    set
        //    {
        //        msUserName = value;
        //    }
        //}

        private void InitializeLogSettings()
        {
            string sValue;
 

            try
            {
                 // read "LogEvents" flag (On/Off) 
                mbLogEvents = Constants.LogEvents;

                // read "LogDir" path  
                sValue = Constants.LogDir;

                if (sValue.Length > 0)
                {
                    msLogDir = sValue.Trim();
                    // fix the path with "\"
                    if (!msLogDir.EndsWith(@"\"))
                        msLogDir = msLogDir + @"\";
                }
                else
                {
                    // set default (temporary) Logs directory, we have to log errors anyway!
                    msLogDir = Directory.GetParent(System.Reflection.Assembly.GetExecutingAssembly().Location).ToString();
                    msLogDir = msLogDir + @"\Logs\";
                }

                // read "AutodeleteDays"  
                sValue = Constants.AutodeleteDays;

                if (sValue.Length > 0)
                {
                    int iDelay;

                    Int32.TryParse(sValue, out iDelay);

                    if (iDelay != 0)
                        miAutodeleteDays = iDelay;
                    else
                        miAutodeleteDays = 5;// default
                }
                else
                {
                    miAutodeleteDays = 5; // default
                }

                // build a log file path
                msDate = DateTime.Now.ToString("yyMMdd"); 
                msLogFilePath = msLogDir + msComponentName + "_" + msDate + ".log";
                msErrLogFilePath = msLogDir + "Errors_" + msDate + ".log";

                // initialize Mutex object for cross-process communication
                // Note: this object is shared
                if (moMutex == null)
                    moMutex = new System.Threading.Mutex();
            }
            catch (Exception ex)
            {
                string s = ex.Message;
            }
        }

        private void CheckLogFileExistance()
        {
            // This method does the job only once(!),
            // and after that the proper flags are set.
            const string METHOD_NAME = "CheckLogFileExistance";

            try
            {
                // make sure the Logs dir exists
                if (!mbLogDirectoryChecked)
                {
                    if (!Directory.Exists(msLogDir))
                        // create the Log directory
                        Directory.CreateDirectory(msLogDir);
                    mbLogDirectoryChecked = true;
                }

                // if the log file doesn't exist - cleanup old files (if any!)
                if (!mbLogFileChecked)
                {
                    if (!File.Exists(msLogFilePath))
                        // autoclean the old (< 5 Days) log files for the component
                        CleanupOldFiles(msComponentName + "_*.log", miAutodeleteDays);
                    mbLogFileChecked = true;
                }
            }
            catch (Exception ex)
            {
                WriteLocalErrLog(METHOD_NAME, "ERROR: " + ex.Message);
                throw ex;
            }
        }

        private void CheckErrLogFileExistance()
        {
            // This method does the job only once(!),
            // and proper flags are set.
            const string METHOD_NAME = "CheckErrLogFileExistance";

            try
            {
                // make sure the Logs dir exists
                if (!mbLogDirectoryChecked)
                {
                    if (!Directory.Exists(msLogDir))
                        // create the Log directory
                        Directory.CreateDirectory(msLogDir);
                    mbLogDirectoryChecked = true;
                }

                // if the error log file doesn't exist - cleanup old files (if any!)
                if (!mbErrLogFileChecked)
                {
                    if (!File.Exists(msErrLogFilePath))
                        // autoclean the old (< 5 Days) error log files
                        CleanupOldFiles("Errors_*.log", miAutodeleteDays);
                    mbErrLogFileChecked = true;
                }
            }
            catch (Exception ex)
            {
                WriteLocalErrLog(METHOD_NAME, "ERROR: " + ex.Message);
                throw ex;
            }
        }

        private void CleanupOldFiles(string vsPattern, double vdNumDays)
        {
            // Autoclean the old (> 5 Days) error log files
            const string METHOD_NAME = "CleanupOldFiles";

            try
            {
                DirectoryInfo oDI = new DirectoryInfo(msLogDir);
                //FileInfo oFI;
                FileInfo[] oFIArray = oDI.GetFiles(vsPattern);
                DateTime dOldDate = DateTime.Now.AddDays(-vdNumDays);


                // delete the old files
                foreach (FileInfo oFI in oFIArray)
                {
                    if (oFI.CreationTime < dOldDate)
                        oFI.Delete();
                }
            }
            catch (Exception ex)
            {
                WriteLocalErrLog(METHOD_NAME, "ERROR: " + ex.Message);
                throw ex;
            }
        }

        public void WriteLog(string vsMsg, bool vbIsError = false)
        {
            // Write a log message (vsMsg) into a log file
            const string METHOD_NAME = "WriteLog2";
            string sMsg;
            string sErrMsg = String.Empty;

            try
            {
                if (mbLogEvents)
                {
           

                    CheckLogFileExistance();

                    // date can be changed, so change the file name
                    CheckForDateChange();

                    // build a message 
                    sMsg = DateTime.Now.ToLongTimeString() + Constants.Tab;

                    if (vbIsError)
                    {
                        // build normal and error messages
                        sErrMsg = sMsg;
                        sMsg = sMsg + msModuleName + Constants.Tab + "ERROR: " + vsMsg;
                        sErrMsg = sErrMsg + msComponentName + "." + msModuleName + Constants.Tab
                                  + "ERROR: " + vsMsg;
                    }
                    else
                        sMsg = sMsg + msModuleName + Constants.Tab + vsMsg;

                    // write to a log file
                    WriteLogLine(msLogFilePath, sMsg);

                    if (vbIsError)
                    {
                        // write also to the error log file
                        CheckErrLogFileExistance();

                        // write to an err log file
                        WriteLogLine(msErrLogFilePath, sErrMsg);
                    }
                }
                else if (vbIsError)
                {
                    // write to the error log file regardless of the mbLogEvents value
                    CheckErrLogFileExistance();

                    // date can be changed, so change the file name
                    CheckForDateChange();

                    // build an error message to log
                    sErrMsg = DateTime.Now.ToLongTimeString() + Constants.Tab
                              + msComponentName + "." + msModuleName + Constants.Tab
                              + "ERROR: " + vsMsg;

                    // write to an err log file
                    WriteLogLine(msErrLogFilePath, sErrMsg);
                }
            }
            catch (Exception ex)
            {
                WriteLocalErrLog(METHOD_NAME, "ERROR: " + ex.Message);
                return;
            }
        }

        public void WriteLog(string vsMethod, string vsMsg, bool vbIsError = false)
        {
            // Write a log message (vsMethod + vsMsg) into a log file
            const string METHOD_NAME = "WriteLog3";
            string sMsg;
            string sErrMsg = String.Empty;

            try
            {
                if (mbLogEvents)
                {


                    CheckLogFileExistance();

                    // date can be changed, so change the file name
                    CheckForDateChange();

                    // build a message
                    sMsg = DateTime.Now.ToLongTimeString() + Constants.Tab;
                    if (vbIsError)
                    {
                        // build normal and error messages
                        sErrMsg = sMsg;
                        sMsg = sMsg + msModuleName + Constants.Tab
                               + vsMethod + Constants.Tab
                               + "ERROR: " + vsMsg;
                        sErrMsg = sErrMsg + msComponentName + "." + msModuleName + Constants.Tab
                                  + vsMethod + Constants.Tab
                                  + "ERROR: " + vsMsg;
                    }
                    else
                        sMsg = sMsg + msModuleName + Constants.Tab
                               + vsMethod + Constants.Tab
                               + vsMsg;

                    // write to a log file
                    WriteLogLine(msLogFilePath, sMsg);

                    if (vbIsError)
                    {
                        // write also to the error log file
                        CheckErrLogFileExistance();

                        // write to an err log file
                        WriteLogLine(msErrLogFilePath, sErrMsg);
                    }
                }
                else if (vbIsError)
                {
                    // write to the error log file regardless of the mbLogEvents value
                    CheckErrLogFileExistance();

                    // date can be changed, so change the file name
                    CheckForDateChange();

                       // build an error message to log
                    sErrMsg = DateTime.Now.ToLongTimeString() + Constants.Tab
                              + msComponentName + "." + msModuleName + Constants.Tab
                              + vsMethod + Constants.Tab
                              + "ERROR: " + vsMsg;

                    // write to an err log file
                    WriteLogLine(msErrLogFilePath, sErrMsg);
                }
            }
            catch (Exception ex)
            {
                WriteLocalErrLog(METHOD_NAME, "ERROR: " + ex.Message);
                return;
            }
        }

        private void WriteLogLine(string vsLogFilePath, string vsMessage)
        {
            const string METHOD_NAME = "WriteLogLine";

            // get mutex
            try
            {
                moMutex.WaitOne();
            }
            catch (ThreadInterruptedException ex)
            {
                // exit the function
                WriteLocalErrLog(METHOD_NAME, "ERROR(1): " + ex.Message);
                return;
            }

            // write to log file
            try
            {
                moStreamWriter = File.AppendText(vsLogFilePath);
                moStreamWriter.WriteLine(vsMessage);
                moStreamWriter.Flush();
            }
            catch (Exception ex)
            {
                WriteLocalErrLog(METHOD_NAME, "ERROR(2): " + ex.GetType().ToString() + ", " + ex.Message);
                return;
            }
            finally
            {
                if (moStreamWriter != null)
                    moStreamWriter.Close();
                moMutex.ReleaseMutex();
            }
        }

        private void CheckForDateChange()
        {
            // Update log file name if Date has been changed
            string sDate = "";

            try
            {
                sDate = DateTime.Now.ToString("yyMMdd");
                if (sDate != msDate)
                {
                    msDate = sDate;
                    msLogFilePath = msLogDir + msComponentName + "_" + msDate + ".log";
                    msErrLogFilePath = msLogDir + "Errors_" + msDate + ".log";
                }
            }
            catch
            {
            }
        }

        private void WriteLocalErrLog(string vsMethodName, string vsMsg)
        {
            // Method writes a log message into an error log file
            const string THIS_COMPONENT_NAME = "ELMain.EventLog";

            string sMsg;
            StreamWriter oStreamWriter;
            // Dim sFileName As String

            try
            {
                sMsg = DateTime.Now.ToLongTimeString()  + Constants.Tab
                      + THIS_COMPONENT_NAME + Constants.Tab
                      + vsMethodName + Constants.Tab + vsMsg;

                // write to log file
                oStreamWriter = File.AppendText(msErrLogFilePath);
                oStreamWriter.WriteLine(sMsg);
                oStreamWriter.Flush();
                oStreamWriter.Close();
            }
            catch
            {
            }
        }


    }

}
