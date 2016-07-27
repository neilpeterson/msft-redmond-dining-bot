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
                string BotResponse = Constants.doNotUnderstandDialogue;
                Activity reply = activity.CreateReply(Constants.workingDialogue);

                if (userData.GetProperty<bool>("OngoingActivity"))
                {
                    if(activity.Text.ToUpper() == "CANCEL")
                    {
                        EndConversationOngoingActivity(stateClient, userData, activity);
                        BotResponse = "Ending current activity.";
                        reply = activity.CreateReply(BotResponse);
                        await connector.Conversations.ReplyToActivityAsync(reply);
                        var endActivity = Request.CreateResponse(HttpStatusCode.OK);
                        return endActivity;
                    }
                    else if (activity.Text.ToUpper() == "HELP")
                    {
                        BotResponse = string.Format("You are currently working on a {0} activity. Type 'cancel' to exit.",userData.GetProperty<string>("ActivityType"));
                        reply = activity.CreateReply(BotResponse);
                        await connector.Conversations.ReplyToActivityAsync(reply);
                        var endActivity = Request.CreateResponse(HttpStatusCode.OK);
                        return endActivity;
                    }
                    
                    BotResponse = ContinueActivity(connector, stateClient, activity, ref userData);
                    await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                    reply = activity.CreateReply(BotResponse);
                    await connector.Conversations.ReplyToActivityAsync(reply);
                    var response2 = Request.CreateResponse(HttpStatusCode.OK);
                    return response2;
            }

                #region LUIS
                Luis diLUIS = await GetEntityFromLUIS(activity.Text);

                if (diLUIS.intents.Count() > 0)
                {                    
                    switch (diLUIS.intents[0].intent)
                    {
                        case Constants.listFoodTruckIntent: //find-food is an intent from LUIS
                            //SetConversationToOngoingActivity(stateClient, userData, activity, "listFoodtruck");
                            if (diLUIS.entities.Count() > 0) //Expect entities
                            {
                                #region DEBUG
                                Debug.WriteLine("MC food trucks - Time elapsed at start: {0}", stopwatch.Elapsed);
                                #endregion
                                BotResponse = await FoodTruckAction.GetAllFoodTruck();
                            }
                            break;

                        case Constants.listCafeIntent: //find-food is an intent from LUIS
                            //SetConversationToOngoingActivity(stateClient, userData, activity,"listCafe");
                            if (diLUIS.entities.Count() > 0) //Expect entities
                            { 
                                #if DEBUG
                                Debug.WriteLine("MC get all cafes - Time elapsed at start: {0}", stopwatch.Elapsed);
                                #endif
                                BotResponse = await CafeAction.GetAllCafes();
                            }
                            break;

                        case Constants.findFoodIntent: //find-food is an intent from LUIS
                            //SetConversationToOngoingActivity(stateClient, userData, activity, "findFood");

                            if (diLUIS.entities.Any(e => e.type == "Food Item") && diLUIS.entities.Any(e => e.type == "Cafe Name")) //Expect entities 
                            {
                                Activity quickReply1 = activity.CreateReply("Checking...");
                                connector.Conversations.ReplyToActivity(quickReply1); //assume this is synchronous
                                string location = diLUIS.entities.Single(e => e.type == "Cafe Name").entity;
                                string dining = diLUIS.entities.Single(e => e.type == "Food Item").entity;
                                BotResponse = await CafeAction.findItemInCafe(dining, location);
                                //TODO: check order of entity
                            }
                            else if (diLUIS.entities.Count() > 0) //Expect entities
                            {
                                #region DEBUG
                                Debug.WriteLine("MC food look up - Time elapsed at start: {0}", stopwatch.Elapsed);
                                #endregion
                                Activity quickReply2 = activity.CreateReply("Searching for locations with " + diLUIS.entities[0].entity);
                                connector.Conversations.ReplyToActivity(quickReply2); //assume this is synchronous
                                BotResponse = await CafeAction.GetCafeForItem(diLUIS.entities[0].entity);
                            }
                            break;

                        // change this back to GetMenu if test does not work out
                        case Constants.findMenuIntent: //find-food is an intent from LUIS
                            //SetConversationToOngoingActivity(stateClient, userData, activity, "findMenu");


                            if (diLUIS.entities.Any(e => e.type == "Food Item") && diLUIS.entities.Any(e => e.type == "Cafe Name")) //Expect entities 
                            {
                                Activity quickReply3 = activity.CreateReply("Checking...");
                                connector.Conversations.ReplyToActivity(quickReply3); //assume this is synchronous
                                string location = diLUIS.entities.Single(e => e.type == "Cafe Name").entity;
                                string dining = diLUIS.entities.Single(e => e.type == "Food Item").entity;
                                BotResponse = await CafeAction.findItemInCafe(dining, location);
                                //TODO: check order of entity
                            }
                            else if (diLUIS.entities.Any(e => e.type == "Day of Week") && diLUIS.entities.Any(e => e.type == "Cafe Name"))
                            {
                                string dayOfWeek = diLUIS.entities.Single(e => e.type == "Day of Week").entity;
                                string cafeName = diLUIS.entities.Single(e => e.type == "Cafe Name").entity;
                                Activity quickReply4 = activity.CreateReply("Gathering menus from " + diLUIS.entities[0].entity);
                                connector.Conversations.ReplyToActivity(quickReply4); //assume this is synchronous  
                                BotResponse = await CafeAction.GetCafeMenu(cafeName, dayOfWeek);
                            }
                            else
                            {
                                Activity quickReply5 = activity.CreateReply("Gathering menus from " + diLUIS.entities[0].entity);
                                connector.Conversations.ReplyToActivity(quickReply5); //assume this is synchronous  
                                BotResponse = await CafeAction.GetCafeMenu(diLUIS.entities[0].entity, "today");
                            }
                            break;

                        case Constants.scheduleShuttleIntent:
                            //set variables for shuttles.
                            if (diLUIS.entities.Any(e => e.type == "Destination Building"))
                            {
                                userData.SetProperty<string>("DestinationBuilding", diLUIS.entities.Single(e => e.type == "Destination Building").entity);
                            }
                            if (diLUIS.entities.Any(e => e.type == "Origin Building"))
                            {
                                userData.SetProperty<string>("OriginBuilding", diLUIS.entities.Single(e => e.type == "Origin Building").entity);
                            }

                            //Setting the state of the conversation to active session.
                            SetConversationToOngoingActivity(stateClient,userData,activity,"bookShuttle");

                            BotResponse = ContinueActivity(connector, stateClient, activity, ref userData);
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

        private string ContinueActivity(ConnectorClient connector, StateClient stateClient, Activity activity, ref BotData userData)
        {
            string activityType = userData.GetProperty<string>("ActivityType");
            string response = string.Empty;

            switch(activityType)
            {
                case "bookShuttle":
                    response = continueShuttle(connector, stateClient, activity, ref userData);
                    break;
            }

            return response;
            
        }

        private string continueShuttle(ConnectorClient connector, StateClient stateClient, Activity activity, ref BotData userData)
        {
            string BotResponse = string.Empty;
            if(userData.GetProperty<bool>("GetDestination"))
            {
                userData.SetProperty<bool>("GetDestination", false);
                userData.SetProperty<string>("DestinationBuilding", activity.Text);
            }
            else if (userData.GetProperty<bool>("GetOrigin"))
            {
                userData.SetProperty<bool>("GetOrigin", false);
                userData.SetProperty<string>("OriginBuilding", activity.Text);
            }

            if (userData.GetProperty<string>("DestinationBuilding") == null || userData.GetProperty<string>("DestinationBuilding") == "")
            {
                BotResponse = "I need to know where you are going. Please state your destination.";
                userData.SetProperty<bool>("GetDestination",true);
            }
            else if (userData.GetProperty<string>("OriginBuilding") == null || userData.GetProperty<string>("OriginBuilding") == "")
            {
                BotResponse = "I need to know where to pick you up. Please state your origin.";
                userData.SetProperty<bool>("GetOrigin", true);
            }
            else
            {
                BotResponse = string.Format("Booked a shuttle from {0} to {1}. Your confirmation number is {2} and the shuttle {4} will pick you up from {1} at 12:{3}", userData.GetProperty<string>("OriginBuilding"), userData.GetProperty<string>("DestinationBuilding"), RandomNumber(10000,99999), RandomNumber(10, 59), RandomNumber(201, 250));
                userData.SetProperty<bool>("OngoingActivity", false);

                userData.SetProperty<string>("DestinationBuilding", "");
                userData.SetProperty<string>("OriginBuilding", "");
            }

            stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);

            return BotResponse;
        }

        private int RandomNumber(int min, int max)
        {
            Random random = new Random();
            return random.Next(min, max);
        }

        private async void SetConversationToOngoingActivity(StateClient state, BotData userData, Activity activity, string activityType)
        {
            //Setting the state of the conversation to active session.
            userData.SetProperty<bool>("OngoingActivity", true);
            userData.SetProperty<string>("ActivityType",activityType);
            await state.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
        }

        private async void EndConversationOngoingActivity(StateClient state, BotData userData, Activity activity)
        {
            //Setting the state of the conversation to active session.
            userData.SetProperty<bool>("OngoingActivity", false);
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
