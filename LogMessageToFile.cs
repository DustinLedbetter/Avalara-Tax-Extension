/***********************************************************************************************************************************
*                                                 GOD First                                                                        *
* Author: Dustin Ledbetter                                                                                                         *
* Release Date: 11-19-2018                                                                                                         *
* Last Edited: 12-7-2018                                                                                                           *
* Version: 1.0                                                                                                                     *
* Purpose: Called when a logging event has been setup in other methods for actions occurring while the code is running             *
************************************************************************************************************************************/


using System;
using System.IO;


namespace AvalaraTaxExtension
{
    class LogMessageToFile
    {


        #region  |--This Method is used to write all of our logs to a txt file--|

        public void LogMessagesToFile(string StoreFrontName, string logPart1, string logPart2, string msg)
        {
            // Get the Date and time stamps as desired
            string currentLogDate = DateTime.Now.ToString("MMddyyyy");
            string currentLogTimeInsertMain = DateTime.Now.ToString("HH:mm:ss tt");

            // Get the storefront's name from storefront to send logs to correct folder
            //string sfName = StoreFrontName.GetValue(FieldType.SystemProperty, SystemProperty.STOREFRONT_NAME, null);

            // Setup Message to display in .txt file 
            msg = string.Format("Time: {0:G}:  Message: {1}{2}", currentLogTimeInsertMain, msg, Environment.NewLine);

            // Add message to the file 
            File.AppendAllText(logPart1 + StoreFrontName + logPart2 + currentLogDate + ".txt", msg);
        }

        #endregion


        #region |--This is a template for adding the log messages into a method you are overriding for debugging--|

        /*
        // An example of how to check if in ebug mode and log messages to the store and to the .txt file as needed
        if ((string)ModuleData[ _YourExetensionAbbreviation_DEBUGGING_MODE] == "true")
        {
            // Log messages to the storefront "Logs" page
            LogMessage($"Our message to the store);                                    // Log Our message to the storefront "Logs" page
            LogMessage($"Our message to the store with variable: " + msg);             // Log Our message with variable to the storefront "Logs" page

            // Log messages to the .txt file
            LogMessageToFile($"Our message to the .txt logs);                          // Log Our message to the storefront "Logs" page
            LogMessageTofile($"Our message to the .txt logs with variable: " + msg);   // Log Our message with variable to the storefront "Logs" page
        }
        */

        #endregion


        // End of the class: LogMessageToFile
    }

    // End of file
}
