﻿using DiningLUISNS;
using Microsoft.Bot.Connector;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web.Http;

namespace msftbot
{
    [BotAuthentication]
    public class MessagesController : ApiController
    {
        // Populate a list of all cafes, this will be used through the project
        static string allCafe2 = File.ReadAllText("C:\\Users\\neilp\\Desktop\\redmond-dining-bot\\redmond-dining-bot\\support-json\\all-cafe.json");
        List<Cafe> list2 = JsonConvert.DeserializeObject<List<Cafe>>(allCafe2);

        public async Task<HttpResponseMessage> Post([FromBody]Activity activity)
        {
            if (activity.Type == "message")
            {
                // This is new to V3
                ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));

                #region LUIS
                string diningoption;
                Luis diLUIS = await GetEntityFromLUIS(activity.Text);
                
                if (diLUIS.intents.Count() > 0 && diLUIS.entities.Count() > 0)
                {
                    switch (diLUIS.intents[0].intent)
                    {
                        case "list-all-cafe": //find-food is an intent from LUIS
                            diningoption = ListAllCafe();
                            break;

                        case "find-food": //find-food is an intent from LUIS
                            diningoption = await GetCafe(diLUIS.entities[0].entity);
                            break;

                        // change this back to GetMenu if test does not work out
                        case "get-menu": //find-food is an intent from LUIS
                            diningoption = await GetCafeMenu(diLUIS.entities[0].entity);
                            break;

                        default:
                            diningoption = "Sorry, I am not getting you...";
                            break;
                    }
                }
                else
                {
                    diningoption = "Sorry, I am not getting you...";
                }
                #endregion               

                Activity reply = activity.CreateReply(diningoption);
                await connector.Conversations.ReplyToActivityAsync(reply);
            }
            else
            {
                HandleSystemMessage(activity);
            }
            var response = Request.CreateResponse(HttpStatusCode.OK);
            return response;
        }

        private string ListAllCafe()
        {

            //string allCafe = File.ReadAllText("C:\\Users\\neilp\\Desktop\\redmond-dining-bot\\redmond-dining-bot\\support-json\\all-cafe.json");
            //List<Cafe> list = JsonConvert.DeserializeObject<List<Cafe>>(allCafe);

            string allcafes = string.Empty;

            foreach (var item in list2)
            {
                allcafes += "[" + item.CafeName + "](https://microsoft.sharepoint.com/sites/refweb/Pages/Dining-Menus.aspx?cafe=" + item.CafeName + ")" + "\n\n";
            }

            // Return list
            return allcafes;
        }

        private async Task<string> GetCafe(string dining)
        {
            // String café - empty string will be populating from json response.
            string cafe = string.Empty;            

            // Get authentication token from authentication.cs
            Authentication auth = new Authentication();
            string authtoken = await auth.GetAuthHeader();

            // Get cafe from refdinign API
            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authtoken);                        
            HttpResponseMessage response = await httpClient.GetAsync("https://msrefdiningint.azurewebsites.net/api/v1/cafe/Name/" + dining);
            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();

            // De-serialize response into list of objects with type cafe (cafe.cs). 
            List<Cafe> list = JsonConvert.DeserializeObject<List<Cafe>>(responseBody);
            
            // Populate string with cafe’s. 
            foreach (var item in list)
            {
                cafe += item.CafeName + "\n\n";                
            }

            // Return list
            return cafe;
        }

        private async Task<string> GetCafeMenu(string location)
        {

            var buildingid =
                from n in list2
                    //where n.CafeName == "Cafe 16"
                where n.CafeName.Equals(location, StringComparison.OrdinalIgnoreCase)
                select n;

            string newid = string.Empty;

            foreach (Cafe item in buildingid)
            {
                newid = item.BuildingId.ToString();
            }

            // Get the day of the week (1 – 5) for use in API URI. 
            DateTime day = DateTime.Now;
            int today = (int)day.DayOfWeek;

            // String menu - empty string will be populating from json response.
            string menu = string.Empty;

            // Get authentication token from authentication.cs
            Authentication auth = new Authentication();            
            string authtoken = await auth.GetAuthHeader();

            // Get menu from refdinign API
            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authtoken);

            try
            {
                HttpResponseMessage response = await httpClient.GetAsync("https://msrefdiningint.azurewebsites.net/api/v1/menus/building/" + newid + "/weekday/" + today);
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();

                // De-serialize response into list of objects with type cafe (menu.cs).
                List<CafeMenu> list = JsonConvert.DeserializeObject<List<CafeMenu>>(responseBody);

                menu += "#[Cafe " + location + "](https://microsoft.sharepoint.com/sites/refweb/Pages/Dining-Menus.aspx?cafe=Café " + location + ")" + "\n\n";

                // Populate string with menu item description. 
                foreach (var item in list)
                {
                    menu += "**" + item.Name + "** \n\n";

                    foreach (var item2 in item.CafeItems)
                    {
                        menu += "- " + item2.Name + "\n\n";
                    }
                }
            }
            catch
            {
                menu += "Menu not found.";
            }

            // Return list
            return menu;
        }

        private async Task<Luis> GetEntityFromLUIS(string Query)
        {
            Query = Uri.EscapeDataString(Query);
            Luis Data = new Luis();
            using (HttpClient client = new HttpClient())
            {
                string RequestURI = "https://api.projectoxford.ai/luis/v1/application?id=c2546bcf-7f12-42d6-9f38-909ebcbc84f2&subscription-key=9dd14d788e9b4bf0acf0a2a4aa34e7d3&q=" + Query;
                HttpResponseMessage msg = await client.GetAsync(RequestURI);

                if (msg.IsSuccessStatusCode)
                {
                    var JsonDataResponse = await msg.Content.ReadAsStringAsync();
                    Data = JsonConvert.DeserializeObject<Luis>(JsonDataResponse);
                }
            }

            return Data;
        }

        private Activity HandleSystemMessage(Activity message)
        {
            if (message.Type == ActivityTypes.DeleteUserData)
            {
                // Implement user deletion here
                // If we handle user deletion, return a real message
            }
            else if (message.Type == ActivityTypes.ConversationUpdate)
            {
                // Handle conversation state changes, like members being added and removed
                // Use Activity.MembersAdded and Activity.MembersRemoved and Activity.Action for info
                // Not available in all channels
            }
            else if (message.Type == ActivityTypes.ContactRelationUpdate)
            {
                // Handle add/remove from contact lists
                // Activity.From + Activity.Action represent what happened
            }
            else if (message.Type == ActivityTypes.Typing)
            {
                // Handle knowing tha the user is typing
            }
            else if (message.Type == ActivityTypes.Ping)
            {
            }

            return null;
        }
    }
}