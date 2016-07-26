using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace msftbot.Support
{
    public static class Constants
    {
        /// <summary>
        /// Luis Entities and Intents
        /// </summary>
        public const string buildingEntity = "building";
        public const string cafeEntity = "cafe";
        public const string listCafeIntent = "list-all-cafe";
        public const string listFoodTruckIntent = "food-truck";
        public const string findFoodIntent = "find-food";
        public const string findMenuIntent = "find-menu";
        public const string scheduleShuttleIntent = "schedule shuttle";
        
        /// <summary>
        /// Dialogue Strings
        /// </summary>
        public const string workingDialogue = "Working on your request now!";
        public const string scheudleShuttleDialogue = "Shall I schedule a shuttle for you from building {0} to {1}?";
        public const string cafeNotOpenWeekendDialogue = "Cafes are not open on the weekend. Sorry!";
        public const string noMenuFoundDialogue = "Menu not found.";
        public const string doNotUnderstandDialogue = "Sorry. I don't understand what you are saying.";
        public const string helpDialogue = "I'm RefBot and I'm here to help you get food and get around campus. Try the following commands:{0}{0} \"Show me all cafes.\",{0}{0}" + 
            "\"What can I eat in cafe 16? \",{0}{0}\"Where can I find pizza?\",{0}{0}" +
            "\"Find food trucks.\",{0}{0}" +
            " \"get me from building 1 to 92\" ";

        /// <summary>
        /// List Formatting for Dialogue
        /// </summary>
        public const string cafeListFormat = "[{0}]({1}{0}){2}{2}";
        public const string menuItemLocationFormat = "**{0}**{1}{1}";
        public const string menuItemTypeFormat = "- {0}{1}{1}";
        public const string linkToCafeMenuFormat = "#[{0}]({1}{0}){2}{2}";
        public const string foodTruckFormat = "{0} - {1}{2}{2}";

        /// <summary>
        /// API URL calls
        /// </summary>
        public const string singleCafeMenuApi = "https://microsoft.sharepoint.com/sites/refweb/Pages/Dining-Menus.aspx?cafe=";
        public const string listAllCafeNames = "https://msrefdiningint.azurewebsites.net/api/v1/cafes";
        public const string listCafesServingItem = "https://msrefdiningint.azurewebsites.net/api/v1/cafe/Name/{0}";
        public const string listCafeMenu = "https://msrefdiningint.azurewebsites.net/api/v1/menus/building/{0}/weekday/{1}";
        public const string luisCallApi = "https://api.projectoxford.ai/luis/v1/application?id=f11f7c0a-e4b1-47a3-9842-e825dc6b9922&subscription-key=daaf89e73e87447a9d5c45e24c23dbde&q={0}";

        public const string dinningMenuWebsiteUrl = "https://microsoft.sharepoint.com/sites/refweb/Pages/Dining-Menus.aspx?cafe=Café";

        /// <summary>
        /// Not sure how this is used.....
        /// </summary>
        public const string AuthHeaderValueScheme = "Bearer";
    }
}