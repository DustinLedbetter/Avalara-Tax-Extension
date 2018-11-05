/***************************************************************************************************************************************************
*                                                 GOD First                                                                                        *
* Author: Dustin Ledbetter                                                                                                                         *
* Release Date: 10-31-2018                                                                                                                         *
* Version: 1.0                                                                                                                                     *
* Purpose: This class is used to send out emails to inform  an error has occurred within the extension                                             *
***************************************************************************************************************************************************/

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
               "from@email.com",
               "to@email.com",
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
                client.Host = "host.email.com";
                //client.EnableSsl = true;
                NetworkCredential NetworkCred = new NetworkCredential(); 
                NetworkCred.UserName = "";
                NetworkCred.Password = "";
                client.UseDefaultCredentials = false;
                client.Credentials = NetworkCred;
                client.Port = 587;
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
                           < network host = "host.email.com" port = "587" userName = "" password = "" defaultCredentials = "false" />
                       </ smtp >
                   </ mailSettings >
               </ system.net >
           */

            #endregion

        }


    //end of the class: EmailErrorNotify
    }
//end of the file
}

