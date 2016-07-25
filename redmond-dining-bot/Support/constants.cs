using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace msftbot.Support
{
    public static class Constants
    {
        /// <summary>
        /// Message Type Strings
        /// </summary>
        public const string messageActivityType = "message";

        public const string buildingEntity = "building";
        public const string cafeEntity = "cafe";

        /// <summary>
        /// Dialogue Strings
        /// </summary>
        public const string workingDialogue = "Working on your request now!";
        public const string scheudleShuttleDialogue = "Shall I schedule a shuttle for you from building {0} to {1}?";
        public const string cafeNotOpenWeekendDialogue = "Cafes are not open on the weekend. Sorry!";
        public const string noMenuFoundDialogue = "Menu not found.";

        /// <summary>
        /// List Formatting for Dialogue
        /// </summary>
        public const string cafeListFormat = "[{0}]({1}{0}){2}{2}";
        public const string menuItemLocationFormat = "**{0}**{1}{1}";
        public const string menuItemTypeFormat = "- {0}{1}{1}";
        public const string linkToCafeMenuFormat = "#[{0}]({1}){2}{2}";

        /// <summary>
        /// API URL calls
        /// </summary>
        public const string singleCafeMenuApi = "https://microsoft.sharepoint.com/sites/refweb/Pages/Dining-Menus.aspx?cafe=";
        public const string listAllCafeNames = "https://msrefdiningint.azurewebsites.net/api/v1/cafes";
        public const string listCafesServingItem = "https://msrefdiningint.azurewebsites.net/api/v1/cafe/Name/{0}";
        public const string listCafeMenu = "https://msrefdiningint.azurewebsites.net/api/v1/menus/building/{0}/weekday/{1}";

        public const string dinningMenuWebsiteUrl = "https://microsoft.sharepoint.com/sites/refweb/Pages/Dining-Menus.aspx?cafe=Café";

        /// <summary>
        /// Not sure how this is used.....
        /// </summary>
        public const string AuthHeaderValueScheme = "Bearer";
    }
}