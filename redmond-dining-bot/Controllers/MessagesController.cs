using DiningLUISNS;
using Microsoft.Bot.Connector;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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
                            diningoption = await ListAllCafe();
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

        private async Task<string> ListAllCafe()
        {
            // Get authentication token from authentication.cs
            Authentication auth = new Authentication();
            string authtoken = await auth.GetAuthHeader();

            // Get all cafes from refdinign API
            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authtoken);
            HttpResponseMessage response = await httpClient.GetAsync("https://msrefdiningint.azurewebsites.net/api/v1/cafes");
            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();

            List<Cafe> list = JsonConvert.DeserializeObject<List<Cafe>>(responseBody);

            string allcafe = string.Empty;

            foreach (var item in list)
            {
                allcafe += "[" + item.CafeName + "](https://microsoft.sharepoint.com/sites/refweb/Pages/Dining-Menus.aspx?cafe=" + item.CafeName + ")" + "\n\n";
            }

            // Return list
            return allcafe;
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

            // Building id dictionary – not all buildings have logical building id’s 
            Dictionary<string, string> buildingid = new Dictionary<string, string>();
            buildingid.Add("4", "4");
            buildingid.Add("9", "8");
            buildingid.Add("10", "8");
            buildingid.Add("22", "21"); //not found
            buildingid.Add("25", "22"); //not found
            buildingid.Add("26", "24");
            buildingid.Add("31", "197");
            buildingid.Add("Studio X", "233"); //not found
            buildingid.Add("50", "350");
            buildingid.Add("113", "355"); //not found
            buildingid.Add("112", "358");
            buildingid.Add("16", "436");
            buildingid.Add("17", "436");
            buildingid.Add("18", "436");
            buildingid.Add("42", "438");
            buildingid.Add("43", "438");
            buildingid.Add("44", "438");
            buildingid.Add("SAMM-D", "473"); //not found
            buildingid.Add("92", "100128"); //not found

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
                HttpResponseMessage response = await httpClient.GetAsync("https://msrefdiningint.azurewebsites.net/api/v1/menus/building/" + buildingid[location] + "/weekday/" + today);
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
                // Friendly message vs. 404 for a more ‘conversational’ like response. 
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