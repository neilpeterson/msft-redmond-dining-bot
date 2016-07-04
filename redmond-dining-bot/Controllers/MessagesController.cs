using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Connector.Utilities;
using Newtonsoft.Json;
using DiningLUISNS;
using cafemenu;
using System.Collections.Generic;
using cafenamespace;
using System.Collections;

namespace redmond_dining_bot
{
    [BotAuthentication]
    public class MessagesController : ApiController
    {
        public async Task<Message> Post([FromBody]Message message)
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

                    case "get-menu": //find-food is an intent from LUIS
                        diningoption = await GetMenu(diLUIS.entities[0].entity);
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

        private async Task<string> GetDining(string dining)
        {
            // String café - empty string will be populating from json response.
            string cafe = string.Empty;

            // Unsecure get from dining api.
            HttpClient httpClient = new HttpClient();
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

        private async Task<string> GetMenu(string location)
        {
            // String menu - empty string will be populating from json response.
            string menu = string.Empty;

            // Unsecure get from dining api.
            HttpClient httpClient = new HttpClient();
            HttpResponseMessage response = await httpClient.GetAsync("https://msrefdiningint.azurewebsites.net/api/v1/CafeName/cafe%20" + location + "/items");
            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();

            // De-serialize response into list of objects with type cafe (menu.cs).
            List<Rootobject> list = JsonConvert.DeserializeObject<List<Rootobject>>(responseBody);

            // Populate string with menu item description. 
            foreach (var item in list)
            {
                foreach (var item2 in item.CafeItems)
                {
                    menu += item2.Description + "\n\n";
                }
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