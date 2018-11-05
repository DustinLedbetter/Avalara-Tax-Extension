# Avalara-Tax-Extension
This extension checks if we are on the shipping page, prints to the logs what we have passed to calculate tax rates, retrieves all of the user's shipping information, connects to the DB and gets all of the totals for the current order, connects to avalara and sends the data of user to retrieve tax amount. and then displays the amount back to the storefront.

*(This version has been updated to add logging features and has added commenting for use in debugging )*

*(Added feature that sends out emails when errors occur in the extension)*

Methods:
1. Lots of variable fields from storefront
2. DisplayName()
2. UniqueName()
3. PARAMS_WE_WANT
4. private ISINI GetSf () (reduces code throughout project)
5. LogMessageToFile
6. GetConfigurationHtml
7. IsModuleType (string x) (determines if at shipping step)
8. CalculateTax2 () 
