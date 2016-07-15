using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Connector.Utilities;
using Newtonsoft.Json;
using DiningLUISNS;
using System.Collections.Generic;
using cafenamespace;
using System.Net.Http.Headers;
using cafemenudays;
using diningauthentication;

namespace redmond_dining_bot
{
    [BotAuthentication]
    public class MessagesController : ApiController
    {
        public async Task<Message> Post([FromBody]Message message)
        {
            if (message.Type == "Message")
            {

                string diningoption;
                DiningLUIS diLUIS = await GetEntityFromLUIS(message.Text);

                if (diLUIS.intents.Count() > 0 && diLUIS.entities.Count() > 0)
                {
                    switch (diLUIS.intents[0].intent)
                    {
                        case "find-food": //find-food is an intent from LUIS
                            diningoption = await GetDining(diLUIS.entities[0].entity);
                            break;

                        // change this back to GetMenu if test does not work out
                        case "get-menu": //find-food is an intent from LUIS
                            diningoption = await GetMenuDay(diLUIS.entities[0].entity);
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

                return message.CreateReplyMessage(diningoption);
            }
            else
            {
                HandleSystemMessage(message);
                return message;
            }
        }

        private async Task<string> GetDining(string dining)
        {
            // String café - empty string will be populating from json response.
            string cafe = string.Empty;

            // Get authentication token from authentication.cs
            diningauth auth = new diningauth();
            string authtoken = await auth.GetAuthHeader();

            // Get cafe from refdinign API
            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authtoken);                        
            HttpResponseMessage response = await httpClient.GetAsync("https://msrefdiningint.azurewebsites.net/api/v1/cafe/Name/" + dining);
            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();

            // De-serialize response into list of objects with type cafe (cafe.cs). 
            List<cafe> list = JsonConvert.DeserializeObject<List<cafe>>(responseBody);
            
            // Populate string with cafe’s. 
            foreach (var item in list)
            {
                cafe += item.CafeName + "\n\n";                
            }

            // Return list
            return cafe;
        }

        private async Task<string> GetMenuDay(string location)
        {

            // Building id dictionary – not all buildings have logical building id’s 
            Dictionary<string, string> buildingid = new Dictionary<string, string>();
            buildingid.Add("4", "4");
            buildingid.Add("9", "8");
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
            diningauth auth = new diningauth();            
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
                List<menudays> list = JsonConvert.DeserializeObject<List<menudays>>(responseBody);

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

        private async Task<DiningLUIS> GetEntityFromLUIS(string Query)
        {
            Query = Uri.EscapeDataString(Query);
            DiningLUIS Data = new DiningLUIS();
            using (HttpClient client = new HttpClient())
            {
                string RequestURI = "https://api.projectoxford.ai/luis/v1/application?id=c2546bcf-7f12-42d6-9f38-909ebcbc84f2&subscription-key=9dd14d788e9b4bf0acf0a2a4aa34e7d3&q=" + Query;
                HttpResponseMessage msg = await client.GetAsync(RequestURI);

                if (msg.IsSuccessStatusCode)
                {
                    var JsonDataResponse = await msg.Content.ReadAsStringAsync();
                    Data = JsonConvert.DeserializeObject<DiningLUIS>(JsonDataResponse);
                }
            }

            return Data;
        }

        private Message HandleSystemMessage(Message message)
        {
            if (message.Type == "Ping")
            {
                Message reply = message.CreateReplyMessage();
                reply.Type = "Ping";
                return reply;
            }
            else if (message.Type == "DeleteUserData")
            {
                // Implement user deletion here
                // If we handle user deletion, return a real message
            }
            else if (message.Type == "BotAddedToConversation")
            {
                return message.CreateReplyMessage("This is a test message...");
            }
            else if (message.Type == "BotRemovedFromConversation")
            {
            }
            else if (message.Type == "UserAddedToConversation")
            {
            }
            else if (message.Type == "UserRemovedFromConversation")
            {
            }
            else if (message.Type == "EndOfConversation")
            {
            }

            return null;
        }
    }
}