using DiningLUISNS;
using Microsoft.Bot.Connector;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace msftbot
{
    [BotAuthentication]
    public class MessagesController : ApiController
    {
        #region Shuttle Variables
        static bool ContextCallShuttle = false;
        static string Destination = String.Empty;
        static string Origin = String.Empty;
        #endregion

        public async Task<HttpResponseMessage> Post([FromBody]Activity activity)
        {
            
            if (activity.Type == "message")
            {
                ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));

                //quick response
                Activity reply = activity.CreateReply("Working on your request now!");
                await connector.Conversations.ReplyToActivityAsync(reply);

                #region LUIS
                string BotResponse = "Sorry. I don't understand what you are saying.";
                Luis diLUIS = await GetEntityFromLUIS(activity.Text);

                if (diLUIS.intents.Count() > 0)
                {                    
                    switch (diLUIS.intents[0].intent)
                    {
                        case "list-all-cafe": //find-food is an intent from LUIS
                            if (diLUIS.entities.Count() > 0) //Expect entities
                                BotResponse = await GetAllCafes();
                            break;

                        case "find-food": //find-food is an intent from LUIS
                            if (diLUIS.entities.Count() > 0) //Expect entities
                                BotResponse = await GetCafeForItem(diLUIS.entities[0].entity);
                            break;

                        // change this back to GetMenu if test does not work out
                        case "find-menu": //find-food is an intent from LUIS
                            if (diLUIS.entities.Count() > 0) //Expect entities
                                BotResponse = await GetCafeMenu(diLUIS.entities[0].entity);
                            break;

                        case "schedule shuttle":
                            if (diLUIS.entities.Count() == 0) //"get me a shuttle"
                                BotResponse = "I need to know where to pick you up and drop you off. Please state from where to where do you need the shuttle";
                            else if ((diLUIS.entities.Count() == 1) ||(!(diLUIS.entities[0].type == "Destination Building" && diLUIS.entities[1].type == "Origin Building")))
                            {
                                //bot ask user to clearly state from where do you want me to take you and to where. 
                                if (diLUIS.entities[0].type == "Destination Building")
                                {
                                    //if destination given
                                    BotResponse = "I need to know where to pick you up. Can you state from where to where do you need the shuttle?";
                                }
                                else if (diLUIS.entities[0].type == "Origin Building")
                                {
                                    //if origin given
                                    BotResponse = "I need to know where to drop you off. Can you state from where to where do you need the shuttle?";
                                }
                                else
                                {
                                    //if nothing given
                                }
                            }
                            else if (diLUIS.entities.Count() > 0 && diLUIS.entities[0].type == "Destination Building" && diLUIS.entities[1].type == "Origin Building")
                            {
                                BotResponse = await SetShuttleRequest(diLUIS.entities[0].entity, diLUIS.entities[1].entity);
                            }
                            else
                                BotResponse = "I think you wanted a shuttle, but I'm not sure. Let's start over. What do you want me to do?";
                            break;

                        case "yes":
                            if (ContextCallShuttle && diLUIS.entities.Count() == 0)
                            {
                                BotResponse = "Okay, I scheduled a shuttle for you from building " + Origin + " to building " + Destination + ".";
                                ResetShuttleVariables();
                            }
                            break;

                        case "no":
                            if (ContextCallShuttle && diLUIS.entities.Count() == 0)
                            {
                                BotResponse = "I'm sorry, let's start over then. What do you want me to do?";
                                ResetShuttleVariables();
                            }
                            break;

                        default:
                            BotResponse = "Sorry, I can't understand your intent.";
                            break;
                    }
                }
                else
                {
                    BotResponse = "Sorry, I am not getting you...";
                }
                #endregion               

                reply = activity.CreateReply(BotResponse);
                await connector.Conversations.ReplyToActivityAsync(reply);
            }
            else
            {
                HandleSystemMessage(activity);
            }

            var response = Request.CreateResponse(HttpStatusCode.OK);
            return response;
        }

        private void ResetShuttleVariables()
        {
            ContextCallShuttle = false;
            Destination = String.Empty;
            Origin = String.Empty;
            return;
        }

        private async Task<string> SetShuttleRequest(string arg_destination, string arg_origin)
        {
            string response = string.Empty;
            //assert that these variables are reset
            System.Diagnostics.Debug.Assert((Destination == String.Empty) && (Origin == String.Empty) && (!ContextCallShuttle));

            //set context variables
            ContextCallShuttle = true;
            Destination = arg_destination;
            Origin = arg_origin;

            response = "Shall I schedule a shuttle for you from building "+Origin+" to "+Destination+"?";
            return response;
        }

        private async Task<string> GetAllCafes()
        {
            // Get authentication token from authentication.cs
            Authentication auth = new Authentication();
            string authtoken = await auth.GetAuthHeader();

            // Get JSON – List of all Cafes
            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authtoken);
            HttpResponseMessage response = await httpClient.GetAsync("https://msrefdiningint.azurewebsites.net/api/v1/cafes");
            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();

            // Convert JSON to list
            List<Cafe> allCafeList = JsonConvert.DeserializeObject<List<Cafe>>(responseBody);

            // Format list
            StringBuilder allcafes = new StringBuilder();
            allCafeList.ForEach(i => {
                allcafes.AppendFormat("[{0}]({1}{0}){2}{2}", i.CafeName, "https://microsoft.sharepoint.com/sites/refweb/Pages/Dining-Menus.aspx?cafe=", Environment.NewLine);
            });

            return allcafes.ToString();
        }

        private async Task<string> GetCafeForItem(string dining)
        {        
            // Get authentication token from authentication.cs
            Authentication auth = new Authentication();
            string authtoken = await auth.GetAuthHeader();

            // Get JSON – List of all Cafe serving the requested item
            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authtoken);
            HttpResponseMessage response = await httpClient.GetAsync("https://msrefdiningint.azurewebsites.net/api/v1/cafe/Name/" + dining);
            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();

            // Convert JSON to list
            List<Cafe> list = JsonConvert.DeserializeObject<List<Cafe>>(responseBody);

            // Format list
            StringBuilder cafe = new StringBuilder();
            list.ForEach(i =>
            {
                cafe.AppendFormat("[{0}]({1}{0}){2}{2}", i.CafeName, "https://microsoft.sharepoint.com/sites/refweb/Pages/Dining-Menus.aspx?cafe=", Environment.NewLine);
            });
            
            return cafe.ToString();
        }

        private async Task<string> GetCafeMenu(string location)
        {
            //do this first to avoid lots of extra processing.
            // Get the day of the week (1 – 5) for use in API URI. 
            DateTime day = DateTime.Now;
            int today = (int)day.DayOfWeek;

            // String menu - empty string will be populating from json response.
            StringBuilder menu = new StringBuilder();
            
            if ((day.DayOfWeek == DayOfWeek.Saturday) || (day.DayOfWeek == DayOfWeek.Sunday))
            {
                menu.AppendLine("Cafes are not open on the weekend. Sorry!");
                return menu.ToString();
            }


            // Get authentication token from authentication.cs
            Authentication auth = new Authentication();
            string authtoken = await auth.GetAuthHeader();

            // Get JSON – List of all Cafes
            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authtoken);
            HttpResponseMessage ResponseAllCafe = await httpClient.GetAsync("https://msrefdiningint.azurewebsites.net/api/v1/cafes");
            ResponseAllCafe.EnsureSuccessStatusCode();
            string RespnseBodyAllCafe = await ResponseAllCafe.Content.ReadAsStringAsync();

            // Convert JSON to list
            List<Cafe> allCafeList = JsonConvert.DeserializeObject<List<Cafe>>(RespnseBodyAllCafe);

            if (location.Contains("building")){
                location = location.Replace("building", "cafe");
            }

            var buildingid =
                from n in allCafeList
                where n.CafeName.Equals(location, StringComparison.OrdinalIgnoreCase)
                select n;

            string newid = string.Empty;

            foreach (Cafe item in buildingid)
            {
                newid = item.BuildingId.ToString();
            }

            try
            {

                //Get JSON – Cafe menu
                HttpResponseMessage response = await httpClient.GetAsync("https://msrefdiningint.azurewebsites.net/api/v1/menus/building/" + newid + "/weekday/" + today);
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();

                // Convert JSON to list
                List<CafeMenu> list = JsonConvert.DeserializeObject<List<CafeMenu>>(responseBody);

                // Format header – URL to café menu of dining site
                menu.AppendFormat("#[{0}](https://microsoft.sharepoint.com/sites/refweb/Pages/Dining-Menus.aspx?cafe=Café{0}){1}{1}", location, Environment.NewLine);

                // Populate string with menu item description - convert to LINQ query
                list.ForEach(i =>
                {
                    menu.AppendFormat("**{0}**{1}{1}", i.Name, Environment.NewLine);
                    i.CafeItems.ToList().ForEach(ci => menu.AppendFormat("- {0}{1}{1}", ci.Name, Environment.NewLine));
                });

            }
            catch
            {
                menu.AppendLine("Menu not found.");
            }
            // Return list
            return menu.ToString();
        }

        private async Task<Luis> GetEntityFromLUIS(string Query)
        {
            Query = Uri.EscapeDataString(Query);
            Luis Data = new Luis();
            using (HttpClient client = new HttpClient())
            {
                string RequestURI = "https://api.projectoxford.ai/luis/v1/application?id=f11f7c0a-e4b1-47a3-9842-e825dc6b9922&subscription-key=daaf89e73e87447a9d5c45e24c23dbde&q=" + Query;
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
