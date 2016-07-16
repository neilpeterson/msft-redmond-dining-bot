using DiningLUISNS;
using Microsoft.Bot.Connector;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
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
                DiningLUIS diLUIS = await GetEntityFromLUIS(activity.Text);
                
                if (diLUIS.intents.Count() > 0 && diLUIS.entities.Count() > 0)
                {
                    switch (diLUIS.intents[0].intent)
                    {
                        case "find-food": //find-food is an intent from LUIS
                            diningoption = await GetCafeByItem(diLUIS.entities[0].entity);
                            break;

                        // change this back to GetMenu if test does not work out
                        case "get-menu": //find-food is an intent from LUIS
                            diningoption = await GetCafeMenuByDay(diLUIS.entities[0].entity);
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

        private async Task<string> GetCafeByItem(string dining)
        {
            // String café - empty string will be populating from json response.
            string cafe = string.Empty;

            // Get authentication token from authentication.cs
            authentication auth = new authentication();
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

        private async Task<string> GetCafeMenuByDay(string location)
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

            Dictionary<int, string> dayofweek = new Dictionary<int, string>();
            dayofweek.Add(1, "MON");
            dayofweek.Add(2, "TUE");
            dayofweek.Add(3, "WED");
            dayofweek.Add(4, "THU");
            dayofweek.Add(5, "FRI");
            dayofweek.Add(6, "FRI");
            dayofweek.Add(7, "FRI");

            // String menu - empty string will be populating from json response.
            string menu = string.Empty;

            try
            {
                // Retrieves JSON from Azure blob
                string blobconnection = ConfigurationManager.AppSettings["azureblob"];
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(blobconnection);
                CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
                CloudBlobContainer container = blobClient.GetContainerReference("diningjson");
                CloudBlockBlob blockBlob = container.GetBlockBlobReference(location + ".json");
                string text = blockBlob.DownloadText();                

                // De-serialize response into list of objects with type cafe (menu-days.cs).
                List<menu> list = JsonConvert.DeserializeObject<List<menu>>(text);


                // Create menu header 	
                menu += "#[Cafe " + location + "](https://microsoft.sharepoint.com/sites/refweb/Pages/Dining-Menus.aspx?cafe=Café " + location + ")" + "\n\n";

                // Populate string with menu item description. 
                foreach (var item in list)
                {
                    menu += "**" + item.Name + "** \n\n";

                    foreach (var item2 in item.CafeItems)
                    {
                        foreach (var item3 in item2.WeekDays)
                        {
                            if (item3 == dayofweek[today])
                            {
                                menu += "- " + item2.Name + "\n\n";
                            }
                        }
                    }
                }
            }
            catch
            {
                menu += "Menu not found.";
            }

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