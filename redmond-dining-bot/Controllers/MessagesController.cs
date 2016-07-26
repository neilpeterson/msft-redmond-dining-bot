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
using msftbot;
using msftbot.Controllers;
using msftbot.Support;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.FormFlow;
using System.Diagnostics;

namespace msftbot.Controllers.Messages
{
    [BotAuthentication]
    public class MessagesController : ApiController
    {
        CafeActions CafeAction = new CafeActions();
        ShuttleActions ShuttleAction = new ShuttleActions();
        FoodTruckActions FoodTruckAction = new FoodTruckActions();
        #if DEBUG
                public Stopwatch stopwatch = new Stopwatch();
        #endif

        public async Task<HttpResponseMessage> Post([FromBody]Activity activity)
        {
            
            if (activity.Type == ActivityTypes.Message)
            {
                ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));
                #if DEBUG
                        stopwatch.Start();
                        Debug.WriteLine("MC Timer start, time elapsed at start: {0}", stopwatch.Elapsed);
                #endif
                //For picking up from a shuttle booking in progress
                StateClient stateClient = activity.GetStateClient();
                BotData userData = await stateClient.BotState.GetUserDataAsync(activity.ChannelId, activity.From.Id);
                
                //quick response
                Activity reply = activity.CreateReply(Constants.workingDialogue);
                //await connector.Conversations.ReplyToActivityAsync(reply);

                #region LUIS
                string BotResponse = Constants.doNotUnderstandDialogue;
                Luis diLUIS = await GetEntityFromLUIS(activity.Text);

                if (diLUIS.intents.Count() > 0)
                {                    
                    switch (diLUIS.intents[0].intent)
                    {
                        case Constants.listFoodTruckIntent: //find-food is an intent from LUIS
                            if (diLUIS.entities.Count() > 0) //Expect entities
                            {
                                #region DEBUG
                                Debug.WriteLine("MC food trucks - Time elapsed at start: {0}", stopwatch.Elapsed);
                                #endregion
                                BotResponse = await FoodTruckAction.GetAllFoodTruck();
                            }
                            break;

                        case Constants.listCafeIntent: //find-food is an intent from LUIS
                            SetConversationToOngoingActivity(stateClient, userData, activity);
                            if (diLUIS.entities.Count() > 0) //Expect entities
                            { 
                                #if DEBUG
                                Debug.WriteLine("MC get all cafes - Time elapsed at start: {0}", stopwatch.Elapsed);
                                #endif
                                BotResponse = await CafeAction.GetAllCafes();
                            }
                            break;

                        case Constants.findFoodIntent: //find-food is an intent from LUIS
                            SetConversationToOngoingActivity(stateClient, userData, activity);
                            if (diLUIS.entities.Count() > 0) //Expect entities
                            {
                                #region DEBUG
                                Debug.WriteLine("MC food look up - Time elapsed at start: {0}", stopwatch.Elapsed);
                                #endregion
                                Activity quickReply = activity.CreateReply("Searching for locations with " + diLUIS.entities[0].entity);
                                connector.Conversations.ReplyToActivity(quickReply); //assume this is synchronous
                                BotResponse = await CafeAction.GetCafeForItem(diLUIS.entities[0].entity);
                            }
                            break;

                        // change this back to GetMenu if test does not work out
                        case Constants.findMenuIntent: //find-food is an intent from LUIS
                            SetConversationToOngoingActivity(stateClient, userData, activity);
                            if (diLUIS.entities.Any(e => e.type == "Day of Week") && diLUIS.entities.Any(e => e.type == "Cafe Name"))
                            {
                                string dayOfWeek = diLUIS.entities.Single(e => e.type == "Day of Week").entity;
                                string cafeName = diLUIS.entities.Single(e => e.type == "Cafe Name").entity;
                                BotResponse = await CafeAction.GetCafeMenu(cafeName, dayOfWeek);
                            }
                            else
                            {
                                BotResponse = await CafeAction.GetCafeMenu(diLUIS.entities[0].entity, "today");
                            }
                            break;

                        case Constants.scheduleShuttleIntent:
                            //Setting the state of the conversation to active session.
                            SetConversationToOngoingActivity(stateClient,userData,activity);
                            
                            BotResponse = "Starting to book a shuttle.";

                            break;

                        case "help":
                            if (diLUIS.entities.Count() == 0)
                            {
                                BotResponse = string.Format(Constants.helpDialogue,Environment.NewLine);
                            }
                            break;
                        default:
                            BotResponse = "Sorry, I can't understand your intent.";
                            break;
                    }
                }
                else
                {
                    BotResponse = "Sorry, I am not getting you..." + Environment.NewLine + string.Format(Constants.helpDialogue, Environment.NewLine); ;
                }
                #endregion

                #region DEBUG
                Debug.WriteLine("MC End region LUIS: {0}", stopwatch.Elapsed);
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

        private async void SetConversationToOngoingActivity(StateClient state, BotData userData, Activity activity)
        {
            //Setting the state of the conversation to active session.
            userData.SetProperty<bool>("OngoingActivity", true);
            await state.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
        }

        private async Task<Luis> GetEntityFromLUIS(string Query)
        {
            Query = Uri.EscapeDataString(Query);
            Luis Data = new Luis();
            using (HttpClient client = new HttpClient())
            {
                string RequestURI = string.Format(Constants.luisCallApi,Query);
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
                ConnectorClient connector = new ConnectorClient(new Uri(message.ServiceUrl));

                Activity reply = message.CreateReply("Hello! I'm REFBot. Type help to see what I can do!");
                connector.Conversations.ReplyToActivityAsync(reply);

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
