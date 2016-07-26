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

namespace msftbot.Controllers.Messages
{
    [BotAuthentication]
    public class MessagesController : ApiController
    {
        CafeActions CafeAction = new CafeActions();
        ShuttleActions ShuttleAction = new ShuttleActions();
        FoodTruckActions FoodTruckAction = new FoodTruckActions();

        public async Task<HttpResponseMessage> Post([FromBody]Activity activity)
        {
            
            if (activity.Type == ActivityTypes.Message)
            {
                ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));

                //For picking up from in progress activity
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

                    switch(userData.GetProperty<string>("ActivityType"))
                    {
                        case "bookShuttle":
                            break;
                    }

                    ContinueActivity(connector, stateClient, activity, userData);
                }

                //quick response
                await connector.Conversations.ReplyToActivityAsync(reply);

                #region LUIS
                Luis diLUIS = await GetEntityFromLUIS(activity.Text);

                if (diLUIS.intents.Count() > 0)
                {                    
                    switch (diLUIS.intents[0].intent)
                    {
                        case Constants.listFoodTruckIntent: //find-food is an intent from LUIS
                            SetConversationToOngoingActivity(stateClient, userData, activity, "listFoodtruck");
                            if (diLUIS.entities.Count() > 0) //Expect entities
                                BotResponse = await FoodTruckAction.GetAllFoodTruck();
                            break;

                        case Constants.listCafeIntent: //find-food is an intent from LUIS
                            SetConversationToOngoingActivity(stateClient, userData, activity,"listCafe");
                            if (diLUIS.entities.Count() > 0) //Expect entities
                                BotResponse = await CafeAction.GetAllCafes();
                            break;

                        case Constants.findFoodIntent: //find-food is an intent from LUIS
                            SetConversationToOngoingActivity(stateClient, userData, activity,"findFood");
                            if (diLUIS.entities.Count() > 0) //Expect entities
                                BotResponse = await CafeAction.GetCafeForItem(diLUIS.entities[0].entity);
                            break;

                        // change this back to GetMenu if test does not work out
                        case Constants.findMenuIntent: //find-food is an intent from LUIS
                            SetConversationToOngoingActivity(stateClient, userData, activity,"findMenu");
                            if (diLUIS.entities.Count() > 0) //Expect entities
                                BotResponse = await CafeAction.GetCafeMenu(diLUIS.entities[0].entity);
                            break;

                        case Constants.scheduleShuttleIntent:

                            //set variables for shuttles.
                            if (diLUIS.entities.Any(e => e.type == "Destination Building"))
                            {
                                userData.SetProperty<string>("DestinationBuilding", diLUIS.entities.Single(e => e.type == "Destination Building").type);
                            }
                            if (diLUIS.entities.Any(e => e.type == "Origin Building"))
                            {
                                userData.SetProperty<string>("OriginBuilding", diLUIS.entities.Single(e => e.type == "Origin Building").type);
                            }

                            //Setting the state of the conversation to active session.
                            SetConversationToOngoingActivity(stateClient,userData,activity,"bookShuttle");
                            
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

        private void ContinueActivity(ConnectorClient connector, StateClient stateClient, Activity activity, BotData userData)
        {
            string activityType = userData.GetProperty<string>("ActivityType");
            
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
