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
            dining test = new dining();
            return test.getDiningHall(dining);
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
                //return message.CreateReplyMessage("This is a test message...");
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