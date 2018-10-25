/***************************************************************************************************************************************************
*                                                 GOD First                                                                                        *
* Author: Dustin Ledbetter                                                                                                                         *
* Release Date: 10-23-2018                                                                                                                         *
* Version: 1.0                                                                                                                                     *
* Purpose: This is an extension for pageflex storefronts that pulls the user's shipping info and order totals from the shipping page of the store, *
*          sends this information out to Avalara to calculate the orders taxes due amount, and returns it to the storefront for the user's order.  *
***************************************************************************************************************************************************/

/*
    References: There are five dlls referenced by this template:
    First three are added references
    1. PageflexServices.dll
    2. StorefrontExtension.dll
    3. SXI.dll
    Last two are part of our USING Avalara.AvaTax.RestClient (These are added From NuGet Package Management)
    4. Avalara.AvaTax.RestClient.net45.dll
    5. Newtonsoft.Json.dll
*/
using Avalara.AvaTax.RestClient;
using Pageflex.Interfaces.Storefront;
using PageflexServices;
using System;
using System.Data.SqlClient;
using System.IO;

namespace AvalaraTaxExtension
{

    public class AvalaraTax : SXIExtension
    {

        #region |--Fields--|
        // This section holds variables for code used throughout the program for quick refactoring as needed

        // Used to setup the minimum required fields
        private const string _UNIQUE_NAME = @"Avalara.Tax.Extension";
        private const string _DISPLAY_NAME = @"Services: Avalara Tax Extension";

        // Used to get the DB logon information
        private const string _DATA_SOURCE = @"ATDataSource";
        private const string _INITIAL_CATALOG = @"ATInitialCatalog";
        private const string _USER_ID = @"ATUserID";
        private const string _USER_PASSWORD = @"ATUserPassword";

        // Used to get the login info for the Avalara Site
        private const string _AV_ENVIRONMENT = @"AVEnvironment";
        private const string _AV_COMPANY_CODE = @"AVCompanyCode";
        private const string _AV_CUSTOMER_CODE = @"AVCustomerCode";
        private const string _AV_USER_ID = @"AVUserID";
        private const string _AV_USER_PASSWORD = @"AVUserPassword";

        // Used to setup if in debug mode and the logging path for if we are 
        private const string _AT_DEBUGGING_MODE = @"ATDebuggingMode";
        private static readonly string LOG_FILENAME1 = "D:\\Pageflex\\Deployments\\";
        private static readonly string LOG_FILENAME2 = "\\Logs\\Avalara_Extension_Log_File_";

        // Variables to hold our totals from the DB retrieval step
        decimal subTotal = 0;
        decimal shippingCharge = 0;
        decimal handlingCharge = 0;
        decimal totalTaxableAmount = 0;

        #endregion


        #region |--Properties and Logging--|
        // At a minimum your extension must override the DisplayName and UniqueName properties.


        // The UniqueName is used to associate a module with any data that it provides to Storefront.
        public override string UniqueName
        {
            get
            {
                return _UNIQUE_NAME;
            }
        }

        // The DisplayName will be shown on the Extensions and Site Options pages of the Administrator site as the name of your module.
        public override string DisplayName
        {
            get
            {
                return _DISPLAY_NAME;
            }
        }

        // Gets the parameters entered on the extension page for this extension
        protected override string[] PARAMS_WE_WANT
        {
            get
            {
                return new string[10]
                {
                    _AT_DEBUGGING_MODE,
                    _DATA_SOURCE,
                    _INITIAL_CATALOG,
                    _USER_ID,
                    _USER_PASSWORD,
                    _AV_COMPANY_CODE,
                    _AV_CUSTOMER_CODE,
                    _AV_ENVIRONMENT,
                    _AV_USER_ID,
                    _AV_USER_PASSWORD
                };
            }
        }

        // Used to access the storefront to retrieve variables
        ISINI SF { get { return Storefront; } }

        // This Method is used to write all of our logs to a txt file
        public void LogMessageToFile(string msg)
        {
            // Get the Date and time stamps as desired
            string currentLogDate = DateTime.Now.ToString("MMddyyyy");
            string currentLogTimeInsertMain = DateTime.Now.ToString("HH:mm:ss tt");

            // Get the storefront's name from storefront to send logs to correct folder
            string sfName = SF.GetValue(FieldType.SystemProperty, SystemProperty.STOREFRONT_NAME, null);

            // Setup Message to display in .txt file 
            msg = string.Format("Time: {0:G}:  Message: {1}{2}", currentLogTimeInsertMain, msg, Environment.NewLine);

            // Add message to the file 
            File.AppendAllText(LOG_FILENAME1 + sfName + LOG_FILENAME2 + currentLogDate + ".txt", msg);
        }

        #endregion


        #region |--This section setups up the extension config page on the storefront to takes input for variables from the user at setup to be used in our extension--|

        // This section sets up on the extension page on the storefront a check box for users to turn on or off debug mode and text fields to get logon info for DB and Avalara
        public override int GetConfigurationHtml(KeyValuePair[] parameters, out string HTML_configString)
        {
            // Load and check if we already have a parameter set
            LoadModuleDataFromParams(parameters);

            // If not then we setup one 
            if (parameters == null)
            {
                SConfigHTMLBuilder sconfigHtmlBuilder = new SConfigHTMLBuilder();
                sconfigHtmlBuilder.AddHeader();

                // Add checkbox to let user turn on and off debug mode
                sconfigHtmlBuilder.AddServicesHeader("Debug Mode:", "");
                sconfigHtmlBuilder.AddCheckboxField("Debugging Information", _AT_DEBUGGING_MODE, "true", "false", (string)ModuleData[_AT_DEBUGGING_MODE] == "true");
                sconfigHtmlBuilder.AddTip(@"This box should be checked if you wish for debugging information to be output to the Logs.");

                // Add textboxes to get the DB login info
                sconfigHtmlBuilder.AddServicesHeader("DataBase Logon Info:", "");
                sconfigHtmlBuilder.AddTextField("DataBase Data Source", _DATA_SOURCE, (string)ModuleData[_DATA_SOURCE], true, true, "");
                sconfigHtmlBuilder.AddTip(@"This field should contain the path that the deployment can be found with. <br> &nbsp&nbsp&nbsp&nbsp&nbsp&nbsp Example for a site deployed on dev: 172.18.0.67\pageflexdev");
                sconfigHtmlBuilder.AddTextField("DataBase Initial Catalog", _INITIAL_CATALOG, (string)ModuleData[_INITIAL_CATALOG], true, true, "");
                sconfigHtmlBuilder.AddTip(@"The field should contain the name used to reference this storefront's Database. <br> &nbsp&nbsp&nbsp&nbsp&nbsp&nbsp Example for a site deployed on dev: Interface");
                sconfigHtmlBuilder.AddTextField("DataBase User ID", _USER_ID, (string)ModuleData[_USER_ID], true, true, "");
                sconfigHtmlBuilder.AddTip(@"This field should contain the User ID for logging into the storefront's database");
                sconfigHtmlBuilder.AddPasswordField("DataBase User Password", _USER_PASSWORD, (string)ModuleData[_USER_PASSWORD], true, true, "");
                sconfigHtmlBuilder.AddTip(@"This field should contain the Password for logging into the storefront's database");

                // Add textboxes to get the Avalara login info
                sconfigHtmlBuilder.AddServicesHeader("Avalara Logon Info:", "");
                sconfigHtmlBuilder.AddTextField("Avalara Environment", _AV_ENVIRONMENT, (string)ModuleData[_AV_ENVIRONMENT], true, true, "");
                sconfigHtmlBuilder.AddTip(@"The field should contain the environment used on the Avalara Site <br> &nbsp&nbsp&nbsp&nbsp&nbsp&nbsp Types: 'Production' or 'Sandbox'");
                sconfigHtmlBuilder.AddTextField("Avalara Company Code", _AV_COMPANY_CODE, (string)ModuleData[_AV_COMPANY_CODE], true, true, "");
                sconfigHtmlBuilder.AddTip(@"The field should contain the company code used on the Avalara Site");
                sconfigHtmlBuilder.AddTextField("Avalara Customer Code", _AV_CUSTOMER_CODE, (string)ModuleData[_AV_CUSTOMER_CODE], true, true, "");
                // Added spacing is to ensure that the textboxes for this and DB logon section are aligned
                sconfigHtmlBuilder.AddTip(@"This field should contain the customer code on the Avalara Site&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp");
                sconfigHtmlBuilder.AddTextField("Avalara User ID", _AV_USER_ID, (string)ModuleData[_AV_USER_ID], true, true, "");
                sconfigHtmlBuilder.AddTip(@"This field should contain the User ID for logging into the Avalara Site");
                sconfigHtmlBuilder.AddPasswordField("Avalara Password", _AV_USER_PASSWORD, (string)ModuleData[_AV_USER_PASSWORD], true, true, "");
                sconfigHtmlBuilder.AddTip(@"This field should contain the Password for logging into the Avalara Site");

                // Footer info and set to configstring
                sconfigHtmlBuilder.AddServicesFooter();
                HTML_configString = sconfigHtmlBuilder.html;
            }
            // If we do then move along
            else
            {
                SaveModuleData();
                HTML_configString = null;
            }
            return 0;
        }

        #endregion


        #region |--This section is used to determine if we are in the "shipping" or "payment" module on the storefront or not--|

        public override bool IsModuleType(string x)
        {
            // If we are in the shipping module return true to begin processes for this module
            if (x == "Shipping")
            {
                return true;
            }
            // If there is no shipping step and we go straight to the payment module return true to begin processes for this module here
            else if (x == "Payment")
            {
                return true;
            }
            // if we are not in either then just keep waiting
            else
                return false;
        }

        #endregion


        #region |--This section is used to figure out the tax rates and get the zipcode entered on the shipping form--|

        // This method is used to get adjust the tax rate for the user's order
        public override int CalculateTax2(string OrderID, double taxableAmount, string currencyCode, string[] priceCategories, string[] priceTaxLocales, double[] priceAmount, string[] taxLocaleId, ref double[] taxAmount)
        {

            #region |--This section of code shows what we have been passed if debug mode is "on"--|

            // Check if debug mode is turned on; If it is then we log these messages
            if ((string)ModuleData[_AT_DEBUGGING_MODE] == "true")
            {
                // Used to help to see where these mesages are in the Storefront logs page
                LogMessage($"*                        *");                                            // Adds a space for easier viewing
                LogMessage($"*          START         *");                                            // Show when we start this process
                LogMessage($"*                        *");

                // Shows what values are passed at beginning
                LogMessage($"OrderID is:              {OrderID}");                                    // Tells the id for the order being calculated
                LogMessage($"TaxableAmount is:        {taxableAmount.ToString()}");                   // Tells the amount to be taxed (currently set to 0)
                LogMessage($"CurrencyCode is:         {currencyCode}");                               // Tells the type of currency used
                LogMessage($"PriceCategories is:      {priceCategories.Length.ToString()}");          // Not Null, but empty
                LogMessage($"PriceTaxLocales is:      {priceTaxLocales.Length.ToString()}");          // Not Null, but empty
                LogMessage($"PriceAmount is:          {priceAmount.Length.ToString()}");              // Not Null, but empty
                LogMessage($"TaxLocaleId is:          {taxLocaleId.Length.ToString()}");              // Shows the number of tax locales found for this order
                LogMessage($"TaxLocaleId[0] is:       {taxLocaleId[0].ToString()}");                  // Sends a number value which corresponds to the tax rate row in the tax rates table excel file  

                // These Log the messages to a log .txt file
                // The logs an be found in the Logs folder in the storefront's deployment
                LogMessageToFile($"*                        *");                                      // Adds a space for easier viewing
                LogMessageToFile($"*          START         *");                                      // Show when we start this process
                LogMessageToFile($"*                        *");

                // Shows what values are passed at beginning in .txt file
                LogMessageToFile($"OrderID is:              {OrderID}");                              // Tells the id for the order being calculated
                LogMessageToFile($"TaxableAmount is:        {taxableAmount.ToString()}");             // Tells the amount to be taxed (currently set to 0)
                LogMessageToFile($"CurrencyCode is:         {currencyCode}");                         // Tells the type of currency used
                LogMessageToFile($"PriceCategories is:      {priceCategories.Length.ToString()}");    // Not Null, but empty
                LogMessageToFile($"PriceTaxLocales is:      {priceTaxLocales.Length.ToString()}");    // Not Null, but empty
                LogMessageToFile($"PriceAmount is:          {priceAmount.Length.ToString()}");        // Not Null, but empty
                LogMessageToFile($"TaxLocaleId is:          {taxLocaleId.Length.ToString()}");        // Shows the number of tax locales found for this order
                LogMessageToFile($"TaxLocaleId[0] is:       {taxLocaleId[0].ToString()}");            // Sends a number value which corresponds to the tax rate row in the tax rates table excel file  
            }

            #endregion


            #region |--This section is where we get and set the values from the shipping page where the user has entered their address info--|

            // Check if debug mode is turned on; If it is then we log these messages
            if ((string)ModuleData[_AT_DEBUGGING_MODE] == "true")
            {
                // Shows the section where we get and display what has been added to the shipping page fields
                LogMessage($"*                        *");                                            // Adds a space for easier viewing
                LogMessage($"*Shipping Fields Section *");                                            // Show when we reach the shipping fields section
                LogMessage($"*                        *");

                // Shows the section where we get and display what has been added to the shipping page fields in the .txt file
                LogMessageToFile($"*                        *");                                      // Adds a space for easier viewing
                LogMessageToFile($"*Shipping Fields Section *");                                      // Show when we reach the shipping fields section
                LogMessageToFile($"*                        *");
            }

            // This section saves the user's shipping info to variables to use with calculating the tax rate to return 
            // Listed in the same order as in the address book on the site
            var SFirstName = Storefront.GetValue("OrderField", "ShippingFirstName", OrderID);         // This gets the first name that the user has on the shipping page
            var SLastName = Storefront.GetValue("OrderField", "ShippingLastName", OrderID);           // This gets the last name that the user has on the shipping page
            var SAddress1 = Storefront.GetValue("OrderField", "ShippingAddress1", OrderID);           // This gets the address field 1 that the user has on the shipping page
            var SAddress2 = Storefront.GetValue("OrderField", "ShippingAddress2", OrderID);           // This gets the address field 2 that the user has on the shipping page 
            var SCity = Storefront.GetValue("OrderField", "ShippingCity", OrderID);                   // This gets the city that the user has on the shipping page
            var SState = Storefront.GetValue("OrderField", "ShippingState", OrderID);                 // This gets the state that the user has on the shipping page
            var SPostalCode = Storefront.GetValue("OrderField", "ShippingPostalCode", OrderID);       // This gets the zip code that the user has on the shipping page
            var SCountry = Storefront.GetValue("OrderField", "ShippingCountry", OrderID);             // This gets the country that the user has on the shipping page                                                                                       // Get the handling charge to use with taxes
            var hCharge = Storefront.GetValue("OrderField", "HandlingCharge", OrderID);               // This gets the handling charge for the order
            var sCharge = Storefront.GetValue("OrderField", "ShippingCharge", OrderID);               // This gets the shipping charge for the order

            // Check if debug mode is turned on; If it is then we log these messages
            if ((string)ModuleData[_AT_DEBUGGING_MODE] == "true")
            {
                // Log to show that we have retrieved the zipcode form the shipping page
                LogMessage($"Shipping FirstName:      {SFirstName}");                                 // This logs the first name that the user has on the shipping page
                LogMessage($"Shipping LastName:       {SLastName}");                                  // This logs the last name that the user has on the shipping page
                LogMessage($"Shipping Address1:       {SAddress1}");                                  // This logs the address field 1 that the user has on the shipping page
                LogMessage($"Shipping Address2:       {SAddress2}");                                  // This logs the address field 2 that the user has on the shipping page 
                LogMessage($"Shipping City:           {SCity}");                                      // This logs the city that the user has on the shipping page
                LogMessage($"Shipping State:          {SState}");                                     // This logs the state that the user has on the shipping page
                LogMessage($"Shipping PostalCode:     {SPostalCode}");                                // This logs the zip code that the user has on the shipping page
                LogMessage($"Shipping Country:        {SCountry}");                                   // This logs the country that the user has on the shipping page
                LogMessage($"Handling Charge:         {hCharge}");                                    // This logs the zip code that the user has on the shipping page
                LogMessage($"Shipping Charge:         {sCharge}");                                    // This logs the country that the user has on the shipping page

                // Log to show that we have retrieved the zipcode form the shipping page in the .txt file
                LogMessageToFile($"Shipping FirstName:      {SFirstName}");                           // This logs the first name that the user has on the shipping page
                LogMessageToFile($"Shipping LastName:       {SLastName}");                            // This logs the last name that the user has on the shipping page
                LogMessageToFile($"Shipping Address1:       {SAddress1}");                            // This logs the address field 1 that the user has on the shipping page
                LogMessageToFile($"Shipping Address2:       {SAddress2}");                            // This logs the address field 2 that the user has on the shipping page 
                LogMessageToFile($"Shipping City:           {SCity}");                                // This logs the city that the user has on the shipping page
                LogMessageToFile($"Shipping State:          {SState}");                               // This logs the state that the user has on the shipping page
                LogMessageToFile($"Shipping PostalCode:     {SPostalCode}");                          // This logs the zip code that the user has on the shipping page
                LogMessageToFile($"Shipping Country:        {SCountry}");                             // This logs the country that the user has on the shipping page
                LogMessageToFile($"Handling Charge:         {hCharge}");                              // This logs the zip code that the user has on the shipping page
                LogMessageToFile($"Shipping Charge:         {sCharge}");                              // This logs the country that the user has on the shipping page
            }

            #endregion


            #region |--Database call to retrieve the subtotal--|

            // Check if debug mode is turned on; If it is then we log these messages
            if ((string)ModuleData[_AT_DEBUGGING_MODE] == "true")
            {
                // Shows the section where we change the tax rate
                LogMessage($"*                        *");                                            // Adds a space for easier viewing
                LogMessage($"*Tax Rates Section Part 1*");                                            // Show when we reach the tax rates section
                LogMessage($"*                        *");

                // Set the tax amount based on a few zipcodes and send it back to pageflex
                LogMessage($"Tax amount is:           " + taxAmount[0].ToString() + " before we make our DB or Avalara calls");          // Shows the current tax amount (currently set to 0)

                // Shows the section where we change the tax rate in the .txt file
                LogMessageToFile($"*                        *");                                      // Adds a space for easier viewing
                LogMessageToFile($"*Tax Rates Section Part 1*");                                      // Show when we reach the tax rates section
                LogMessageToFile($"*                        *");

                // Set the tax amount based on a few zipcodes and send it back to pageflex in the .txt file
                LogMessageToFile($"Tax amount is:           " + taxAmount[0].ToString() + " before we make our DB or Avalara calls");    // Shows the current tax amount (currently set to 0)
            }

            // Get our DB logon info from the storefront
            string dataSource = Storefront.GetValue("ModuleField", _DATA_SOURCE, _UNIQUE_NAME);        // This variable holds the datasource the user provided from the extension setup page
            string initialCatalog = Storefront.GetValue("ModuleField", _INITIAL_CATALOG, _UNIQUE_NAME);// This variable holds the datasource the user provided from the extension setup page
            string userID = Storefront.GetValue("ModuleField", _USER_ID, _UNIQUE_NAME);                // This variable holds the datasource the user provided from the extension setup page
            string userPassword = Storefront.GetValue("ModuleField", _USER_PASSWORD, _UNIQUE_NAME);    // This variable holds the datasource the user provided from the extension setup page

            // Check if debug mode is turned on; If it is then we log these messages
            if ((string)ModuleData[_AT_DEBUGGING_MODE] == "true")
            {
                // Log messages to show what was retrieved from the storefront 
                LogMessage($"DB logon information:");                                                  // This logs that we are logging the DB Logon information
                LogMessage($"Data Source is:          {dataSource}");                                  // This logs the data source that the user has on the extension configuration page
                LogMessage($"Initial Catalog is:      {initialCatalog}");                              // This logs the initial catalog that the user has on the extension configuration page
                LogMessage($"User ID is:              {userID}");                                      // This logs the user id that the user has on the extension configuration page
                LogMessage($"User Password is:        {userPassword}");                                // This logs the user password that the user has on the extension configuration page
                LogMessageToFile($"DB logon information:");                                            // This logs that we are logging the DB Logon information
                LogMessageToFile($"Data Source is:          {dataSource}");                            // This logs the data source that the user has on the extension configuration page
                LogMessageToFile($"Initial Catalog is:      {initialCatalog}");                        // This logs the initial catalog that the user has on the extension configuration page
                LogMessageToFile($"User ID is:              {userID}");                                // This logs the user id that the user has on the extension configuration page
                LogMessageToFile($"User Password is:        {userPassword}");                          // This logs the user password that the user has on the extension configuration page
            }

            // Our credentials to connect to the DB
            string connectionString = $"Data Source={dataSource};" +
                                      $"Initial Catalog={initialCatalog};" +
                                      $"User ID={userID};" +
                                      $"Password={userPassword};" +
                                      "Persist Security Info=True;" +
                                      "Connection Timeout=15";

            // Check if debug mode is turned on; If it is then we log these messages
            if ((string)ModuleData[_AT_DEBUGGING_MODE] == "true")
            {
                LogMessage($"Full Connection String:  {connectionString}");                            // This logs the full connection string to see it is all together properly
                LogMessageToFile($"Full Connection String:  {connectionString}");                      // This logs the full connection string to see it is all together properly
            }

            // The query to be ran on the DB
            /*
             * Query:   SELECTS the "Order Group ID", the "Price"(Cast as money)  , the "Shipping Charge"(Cast as money)  , and the "Handling Charge"(Cast as money)
             *          FROM three tables: "OrderedDocuments", "OrderGroups", and "Shipments" that have been INNER JOINED together based on the "OrderGroupID"
             *          WHERE the "OrderGroupID" equals the orderID of our current order on the storefront
             */
            string queryString = "SELECT OrderedDocuments.OrderGroupID," +
                                        "CAST(OrderedDocuments.Price / 100.00 as money) as Price," +
                                        "CAST(Shipments.ShippingAmount / 100.00 as money) as Shipping," +
                                        "CAST(OrderGroups.HandlingCharge / 100.00 as money) as Handling " +
                                    "FROM OrderedDocuments " +
                                        "INNER JOIN OrderGroups ON OrderGroups.OrderGroupID = OrderedDocuments.OrderGroupID " +
                                        "INNER JOIN Shipments ON Shipments.OrderGroupID = OrderedDocuments.OrderGroupID " +
                                    "WHERE OrderedDocuments.OrderGroupID = @orderID";

            // This is how to call it using a stored procedure I currently have setup on "Interface DB"
            //string queryString = "USE [Interface] " + "DECLARE @return_value Int EXEC @return_value = [dbo].[GetRates] @OrderID = 1787 SELECT @return_value as 'Return Value'";

            if ((string)ModuleData[_AT_DEBUGGING_MODE] == "true")
            {
                // Log to let the developer know we are trying to connect to DB using connection string
                LogMessage("starting DB connection");                                                  // This logs that we are starting the DB connection process
                LogMessageToFile("starting DB connection");                                            // This logs that we are starting the DB connection process
            }

            try
            {
                // Setup the connection to the db using our credentials
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    // Open the connection using what we have setup
                    connection.Open();

                    if ((string)ModuleData[_AT_DEBUGGING_MODE] == "true")
                    {
                        // Log to let the developer know connection to DB was successful
                        LogMessage("DB connection successful");                                        // This logs that the DB connection process was a success
                        LogMessageToFile("DB connection successful");                                  // This logs that the DB connection process was a success
                    }

                    // Setup our query to call
                    SqlCommand commandGetRates = new SqlCommand(queryString, connection);
                    commandGetRates.Parameters.AddWithValue("@orderID", OrderID);

                    if ((string)ModuleData[_AT_DEBUGGING_MODE] == "true")
                    {
                        // Log to let the developer know we finished running our query
                        LogMessage("Query finished running");                                          // This logs that the query run has finished
                        LogMessageToFile("Query finished running");                                    // This logs that the query run has finished
                    }

                    // Setup a reader to handle what our query returns
                    using (SqlDataReader reader = commandGetRates.ExecuteReader())
                    {
                        // Read back what our query retrieves
                        while (reader.Read())
                        {
                            if ((string)ModuleData[_AT_DEBUGGING_MODE] == "true")
                            {
                                // Log and show the results we retrieved from the DB
                                LogMessage(string.Format("From DB Reader: OrderID: {0}, Price: {1}, Shipping: {2}, Handling: {3}", reader["OrderGroupID"], reader["Price"], reader["Shipping"], reader["Handling"]));
                                LogMessageToFile(string.Format("From DB Reader: OrderID: {0}, Price: {1}, Shipping: {2}, Handling: {3}", reader["OrderGroupID"], reader["Price"], reader["Shipping"], reader["Handling"]));
                            }

                            // Filling the variables to hold our totals from the DB retrieval step
                            subTotal += Convert.ToDecimal(reader["Price"]);                            // This gets the subtotal that the user has on the extension configuration page
                            // Make subtotal += so it can handle multiple items from the shopping cart when an order is placed
                            shippingCharge = Convert.ToDecimal(reader["Shipping"]);                    // This gets the shipping charge that the user has on the extension configuration page
                            handlingCharge = Convert.ToDecimal(reader["Handling"]);                    // This gets the handling charge that the user has on the extension configuration page
                        }

                        // Calculate the total amount to be taxed by Avalara
                        totalTaxableAmount = subTotal + shippingCharge + handlingCharge;

                        if ((string)ModuleData[_AT_DEBUGGING_MODE] == "true")
                        {
                            // Log messages to show what we retrieved from DB and have stored into variables
                            LogMessage($"Totals read from the DB and stored in variables");            // This logs that the DB read has finished and we are showing what is in the variables saved
                            LogMessage($"subTotal:                {subTotal}");                        // This logs the subtotal variable
                            LogMessage($"shippingCharge:          {shippingCharge}");                  // This logs the shipping charge variable
                            LogMessage($"handlingCharge:          {handlingCharge}");                  // This logs the handling charge variable
                            LogMessageToFile($"Totals read from the DB and stored in variables");      // This logs that the DB read has finished and we are showing what is in the variables saved
                            LogMessageToFile($"subTotal:                {subTotal}");                  // This logs the subtotal variable
                            LogMessageToFile($"shippingCharge:          {shippingCharge}");            // This logs the shipping charge variable
                            LogMessageToFile($"handlingCharge:          {handlingCharge}");            // This logs the handling charge variable

                            // Log message to show what the total taxable amount that will be sent to Avalara is
                            LogMessage($"Total Taxable Amount:    {totalTaxableAmount}");              // This logs the total taxable amount
                            LogMessageToFile($"Total Taxable Amount:    {totalTaxableAmount}");        // This logs the total taxable amount
                        }
                        // Always call Close when done reading.
                        reader.Close();
                    }
                    // Called by dispose, but good practice to close when done with connection.
                    connection.Close();
                }
            }
            catch
            {
                // Log issue with storefront and to file regardless of whether in debug mode or not
                LogMessage("Error in DB connection and data retrieval process");                       // This logs that there was an error in the DB process
                LogMessageToFile("Error in DB connection and data retrieval process");                 // This logs that there was an error in the DB process
            }

            #endregion


            #region |--This is the section that connects and pulls info from avalara--|

            // Check if debug mode is turned on; If it is then we log these messages
            if ((string)ModuleData[_AT_DEBUGGING_MODE] == "true")
            {
                // Shows the section where we change the tax rate
                LogMessage($"*                        *");                                             // Adds a space for easier viewing
                LogMessage($"*Tax Rates Section Part 2*");                                             // Show when we reach the tax rates 2 section
                LogMessage($"*                        *");

                // Set the tax amount based on a few zipcodes and send it back to pageflex
                LogMessage($"Tax amount is:           " + taxAmount[0].ToString() + " before we make our Avalara call");          // Shows the current tax amount (currently set to 0)

                // Shows the section where we change the tax rate in the .txt file
                LogMessageToFile($"*                        *");                                       // Adds a space for easier viewing
                LogMessageToFile($"*Tax Rates Section Part 2*");                                       // Show when we reach the tax rates 2 section
                LogMessageToFile($"*                        *");

                // Set the tax amount based on a few zipcodes and send it back to pageflex in the .txt file
                LogMessageToFile($"Tax amount is:           " + taxAmount[0].ToString() + " before we make our Avalara call");    // Shows the current tax amount (currently set to 0)
            }

            // Get our Avalara logon info from the storefront
            string AVEnvironment = Storefront.GetValue("ModuleField", _AV_ENVIRONMENT, _UNIQUE_NAME);   // This gets the environment type that the user has on the extension configuration page
            string AVCompanyCode = Storefront.GetValue("ModuleField", _AV_COMPANY_CODE, _UNIQUE_NAME);  // This gets the company code that the user has on the extension configuration page
            string AVCustomerCode = Storefront.GetValue("ModuleField", _AV_CUSTOMER_CODE, _UNIQUE_NAME);// This gets the customer code that the user has on the extension configuration page
            string AVuserID = Storefront.GetValue("ModuleField", _AV_USER_ID, _UNIQUE_NAME);            // This gets the user id that the user has on the extension configuration page
            string AVuserPassword = Storefront.GetValue("ModuleField", _AV_USER_PASSWORD, _UNIQUE_NAME);// This gets the user password that the user has on the extension configuration page

            // Check if debug mode is turned on; If it is then we log these messages
            if ((string)ModuleData[_AT_DEBUGGING_MODE] == "true")
            {
                // Log messages to show what was retrieved from the storefront 
                LogMessage($"Avalara logon information:");                                             // This logs that we are displaying the Avalara logon information
                LogMessage($"Environment:             {AVEnvironment}");                               // This logs the environment variable
                LogMessage($"Company Code:            {AVCompanyCode}");                               // This logs the company code variable
                LogMessage($"Customer Code:           {AVCustomerCode}");                              // This logs the company name variable
                LogMessage($"Avalara User ID:         {AVuserID}");                                    // This logs the user id variable
                LogMessage($"Avalara User Password:   {AVuserPassword}");                              // This logs the user password variable
                LogMessageToFile($"Avalara logon information:");                                       // This logs that we are displaying the Avalara logon information
                LogMessageToFile($"Environment:             {AVEnvironment}");                         // This logs the environment variable
                LogMessageToFile($"Company Code:            {AVCompanyCode}");                         // This logs the company code variable
                LogMessageToFile($"Customer Code:           {AVCustomerCode}");                        // This logs the company name variable
                LogMessageToFile($"Avalara User ID:         {AVuserID}");                              // This logs the user id variable
                LogMessageToFile($"Avalara User Password:   {AVuserPassword}");                        // This logs the user password variable
            }

            // Create client to be setup with user defined environment variable
            var client = new AvaTaxClient("", "", null, null);

            // Create a client and set up authentication
            if (AVEnvironment == "Production" || AVEnvironment == "production")
            {
                // Set the Avalara environment to be on Production
                client = new AvaTaxClient("AvalaraTaxExtension", "1.0", Environment.MachineName, AvaTaxEnvironment.Production)
                    .WithSecurity($"{AVuserID}", $"{AVuserPassword}");
            }
            else if (AVEnvironment == "Sandbox" || AVEnvironment == "sandbox")
            {
                // Set the Avalara environment to be on Sandbox
                client = new AvaTaxClient("AvalaraTaxExtension", "1.0", Environment.MachineName, AvaTaxEnvironment.Sandbox)
                    .WithSecurity($"{AVuserID}", $"{AVuserPassword}");
            }
            else
            {
                // Log and let it be known that the environment variable is not setup correctly
                LogMessage("The Avalara Environment Variable has not been set properly");
                LogMessageToFile("The Avalara Environment Variable has not been set properly");
            }


            // Check if debug mode is turned on; If it is then we log these messages
            if ((string)ModuleData[_AT_DEBUGGING_MODE] == "true")
            {
                // Show user creation passed 
                LogMessage("Client created");                                                          // This logs that the Avalara client has been created

                // Show user creation passed in the .txt file
                LogMessageToFile("Client created");                                                    // This logs that the Avalara client has been created
            }

            // This creates the transaction that reaches out to alavara and gets the amount of tax for the user based on info we send
            // send client we created above in code, the company code on alavara I created, type of transaction, and the customer code
            var transaction = new TransactionBuilder(client, $"{AVCompanyCode}", DocumentType.SalesOrder, $"{AVCustomerCode}")

                        // Pass the variables we pulled from pageflex in the address line 
                        .WithAddress(TransactionAddressType.SingleLocation, SAddress1, SAddress2, null, SCity, SState, SPostalCode, SCountry)

                        // Pass the amount of money to calculate tax on (This should be a variable once figure out what it is)
                        .WithLine(totalTaxableAmount)

                        // Run transaction
                        .Create();

            // Check if debug mode is turned on; If it is then we log these messages
            if ((string)ModuleData[_AT_DEBUGGING_MODE] == "true")
            {
                // Log that we have created a transaction
                LogMessage("The transaction has been created");                                        // This logs that the Avalara transaction has been created

                // Log that we have created a transaction in the .txt file
                LogMessageToFile("The transaction has been created");                                  // This logs that the Avalara transaction has been created
            }

            // Retrieves the tax amount from Avalara and sets it to a variable
            // (It is returned as a decimal?  so we havve to convert it to a decimal)
            // (The ?? 0 sets it to 0 if transaction.totalTax is null)
            decimal tax2 = transaction.totalTax ?? 0;

            // Check if debug mode is turned on; If it is then we log these messages
            if ((string)ModuleData[_AT_DEBUGGING_MODE] == "true")
            {
                //Log what is returned
                LogMessage($"Your calculated tax was: {tax2}");                                        // This logs that the calculated tax for the order

                //Log what is returned in the .txt file
                LogMessageToFile($"Your calculated tax was: {tax2}");                                  // This logs that the calculated tax for the order
            }

            //Set the tax amount on pageflex to the returned value from Avalara
            taxAmount[0] = decimal.ToDouble(tax2);                                                     // Set our new tax rate to a value we choose (for testing) 

            // Check if debug mode is turned on; If it is then we log these messages
            if ((string)ModuleData[_AT_DEBUGGING_MODE] == "true")
            {
                LogMessage($"The zipcode was:         " + SPostalCode);                                // Log message to inform we used this zipcode to get the amount returned
                LogMessage($"The new TaxAmount is:    " + taxAmount[0].ToString());                    // Shows the tax amount after we changed it

                // Send message saying we have completed this process
                LogMessage($"*                        *");                                             // Adds a space for easier viewing
                LogMessage($"*           end          *");                                             // Show when we reach the end of the extension
                LogMessage($"*                        *");

                // Log for .txt file
                LogMessageToFile($"The zipcode was:         " + SPostalCode);                          // Log message to inform we used this zipcode to get the amount returned
                LogMessageToFile($"The new TaxAmount is:    " + taxAmount[0].ToString());              // Shows the tax amount after we changed it

                // Send message saying we have completed this process in the .txt file
                LogMessageToFile($"*                        *");                                       // Adds a space for easier viewing
                LogMessageToFile($"*           end          *");                                       // Show when we reach the end of the extension
                LogMessageToFile($"*                        *");
            }

            // Kept for reference and future use
            // avalara logging doesn't work from inside extension
            // client.LogToFile("MySixthExtension\\avataxapi.log"); 

            #endregion


            return eSuccess;
        }
        #endregion


        #region |--Used when testing to see if the depreciated version would work (It does not work)--|

        /*
        public override int CalculateTax(string orderID, double taxableAmount, double prevTaxableAmount, ref TaxValue[] tax)
        {
            // Used to help to see where these mesages are in the Storefront logs page
            LogMessage($"*      space       *");                // Adds a space for easier viewing
            LogMessage($"*      START       *");                // Show when we start this process
            LogMessage($"*      space       *");

            // Shows what values are passed at beginning
            LogMessage($"OrderID is: {orderID}");                                      // Tells the id for the order being calculated
            LogMessage($"TaxableAmount is: {taxableAmount.ToString()}");               // Tells the amount to be taxed (currently set to 0)
            LogMessage($"prevTaxableAmount is: {prevTaxableAmount.ToString()}");       // Tells the previous amount to be taxed (currently set to 0)

            return eSuccess;
        }
        */

        #endregion


        //end of the class: ExtensionSeven
    }
    //end of the file
}
