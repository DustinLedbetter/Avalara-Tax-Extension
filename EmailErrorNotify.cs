/***********************************************************************************************************************************
*                                                 GOD First                                                                        *
* Author: Dustin Ledbetter                                                                                                         *
* Release Date: 11-19-2018                                                                                                         *
* Last Edited: 1-22-2019                                                                                                           *
* Version: 1.0                                                                                                                     *
* Purpose: Used to send email out to developer responsible for the site when an error occurs                                       *
************************************************************************************************************************************/


using System.Net;
using System.Net.Mail;


namespace AvalaraTaxExtension
{
    class EmailErrorNotify
    {

        // This flag is used to test which part of the try catch is being called and stored in the logs of the main storefront 
        public static string checkFlag;

        // This method is called to send an email when an exception error occurs in the main extension
        public static void CreateMessage(string esubject, string ebody)
        {

            // Create a message and set up the recipients.
            MailMessage message = new MailMessage
            (
               "nulled for security",
               "nulled for security",
               esubject,
               ebody
            );

            // Set it so that our html newline will register when the email is created
            message.IsBodyHtml = true;

            #region |--This section is used to create the client without using the webconfig file on the storefronts--|

            // Setup and prepare to send the email
            SmtpClient client = new SmtpClient();
            try
            {
                //client.Host = "internalmail.aflac.com";
                client.Host = "nulled for security";
                //client.EnableSsl = true;
                NetworkCredential NetworkCred = new NetworkCredential();
                NetworkCred.UserName = nulled for security;
                NetworkCred.Password = nulled for security;
                client.UseDefaultCredentials = nulled for security;
                client.Credentials = nulled for security;
                client.Port = nulled for security;
                client.Send(message);
                // Set flag to Yes so we can know the try succeeded
                checkFlag = "Yes";
            }
            catch
            {
                // Our email send failed; clean up the pieces
                message.Dispose();
                client = null;
                // Set flag to No so we can know the try Failed
                checkFlag = "No";
            }

            #endregion


            #region |--This is a template for sending an email in a catch block to alert of failures within an extension--|

            // This section calls the class EmailErrorNotify and sends information from here to it to be emailed to the devlopers in charge of the site
            // I have used it from acatch block in my other sites. 


            /*
                // Log issue with storefront and to file regardless of whether in debug mode or not
                LogMessage("Error in overriding CalculateTax2 Method");                                // This logs that there was an error in the DB process
                LogMessageToFile("Error in overriding CalculateTax2 Method");                          // This logs that there was an error in the DB process

                // Get the storefront's name from storefront and Date and time stamps as desired
                string sfName = SF.GetValue(FieldType.SystemProperty, SystemProperty.STOREFRONT_NAME, null);
                string currentLogDate = DateTime.Now.ToString("MMddyyyy");
                string currentLogTimeInsertMain = DateTime.Now.ToString("HH:mm:ss tt");

                //Setup our date and time for error
                string ErrorDate = string.Format("Date: {0}  Time: {1:G} <br>", currentLogDate, currentLogTimeInsertMain);

                // Setup our email body and message
                string subjectstring = "Storefront: \"" + sfName + "\" had an ERROR occur in overriding the CalculateTax2 Method";
                string bodystring = "Storefront: \"" + sfName + "\" had an ERROR occur in overriding the CalculateTax2 Method <br>" +
                                    ErrorDate +
                                    "Extension: Avalara Tax Extension <br>" +
                                    "ERROR occured with Order ID: " + ErrorOrderID;

                // Call method to send our error as an email to developers maintaining sites
                EmailErrorNotify.CreateMessage(subjectstring, bodystring);

                // Log issue with storefront and to file regardless of whether in debug mode or not
                LogMessage($"Error in overriding CalculateTax2 send email method called");             // This logs that Error in DB connection send email method called
                LogMessageToFile($"Error in overriding CalculateTax2 send email method called");       // This logs that Error in DB connection send email method called

                LogMessage($"Email sent successfully: {EmailErrorNotify.checkFlag}");                  // This logs Email sent successfully flag response 
                LogMessageToFile($"Email sent successfully: {EmailErrorNotify.checkFlag}");            // This logs Email sent successfully flag response 
            */

            #endregion


            #region |--This section was used with testing to see if I could create the client using the webconfig file on the storefronts--|

            /*
             * // This was used to send an email from outside of the extension.
             * 
             
               //Send the message.
                SmtpClient client = new SmtpClient();  // creating object of smptpclient  
                client.Send(message);

               // This section below goes into the webconfig file of the storefront 
               < system.net >
                   < mailSettings >
                       < smtp from = "from@email.com" >
                           < network host = "nulled for security" port = "nulled for security" userName = "nulled for security" password = "nulled for security" defaultCredentials = "nulled for security" />
                       </ smtp >
                   </ mailSettings >
               </ system.net >
           */

            #endregion

        }

        // End of the class: EmailErrorNotify
    }

    // End of file
}
