﻿using DiningLUISNS;
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
using System.Diagnostics;

namespace msftbot.Controllers.Messages
{
    [BotAuthentication]
    public class MessagesController : ApiController
    {
        #region Variables
        static bool ContextCallShuttle = false;
        static string Destination = String.Empty;
        static string Origin = String.Empty;
        CafeActions CafeAction = new CafeActions();
        ShuttleActions ShuttleAction = new ShuttleActions();
        FoodTruckActions FoodTruckAction = new FoodTruckActions();
        #endregion

        #if DEBUG
        public Stopwatch stopwatch = new Stopwatch();
        #endif

        public async Task<HttpResponseMessage> Post([FromBody]Activity activity)
        {
            
            if (activity.Type == Constants.messageActivityType)
            {
                ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));

                #if DEBUG
                stopwatch.Start();
                Debug.WriteLine("MC Timer start, time elapsed at start: {0}", stopwatch.Elapsed);
                #endif

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
                        case Constants.listFoodTruckIntent: //"show me all food trucks"
                            if (diLUIS.entities.Count() > 0) //Expect entities
                            {
                                #region DEBUG
                                Debug.WriteLine("MC food trucks - Time elapsed at start: {0}", stopwatch.Elapsed);
                                #endregion
                                BotResponse = await FoodTruckAction.GetAllFoodTruck();
                            }
                            break;

                        case Constants.listCafeIntent: //"get all cafes"
                            if (diLUIS.entities.Count() > 0)
                            { //Expect entities
                                #if DEBUG
                                Debug.WriteLine("MC get all cafes - Time elapsed at start: {0}", stopwatch.Elapsed);
                                #endif
                                BotResponse = await CafeAction.GetAllCafes();
                            }
                            break;

                        case Constants.findFoodIntent: //"where can i get pizza"
                            if (diLUIS.entities.Count() > 0)
                            { //Expect entities
                                #region DEBUG
                                Debug.WriteLine("MC food look up - Time elapsed at start: {0}", stopwatch.Elapsed);
                                #endregion
                                Activity quickReply = activity.CreateReply("Searching for locations with " + diLUIS.entities[0].entity);
                                connector.Conversations.ReplyToActivity(quickReply); //assume this is synchronous
                                BotResponse = await CafeAction.GetCafeForItem(diLUIS.entities[0].entity);
                            }
                            break;

                        // change this back to GetMenu if test does not work out
                        case Constants.findMenuIntent: //"what can i eat in building 16"
                            if (diLUIS.entities.Count() > 0)
                            { //Expect entities 
                                #region DEBUG
                                Debug.WriteLine("MC cafe look up - Time elapsed at start: {0}", stopwatch.Elapsed);
                                #endregion
                                Activity quickReply = activity.CreateReply("Gathering menus from " + diLUIS.entities[0].entity);
                                //await connector.Conversations.ReplyToActivityAsync();
                                connector.Conversations.ReplyToActivity(quickReply); //assume this is synchronous 
                                BotResponse = await CafeAction.GetCafeMenu(diLUIS.entities[0].entity);
                            }
                            break;

                        case Constants.scheduleShuttleIntent:
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
                                if(await ShuttleAction.SetShuttleRequest(diLUIS.entities[0].entity, diLUIS.entities[1].entity))
                                {
                                    BotResponse = string.Format("Shuttle has been booked from {0} to {1}", diLUIS.entities[1].entity, diLUIS.entities[0].entity);
                                }
                            }
                            else
                                BotResponse = "I think you wanted a shuttle, but I'm not sure. Let's start over. What do you want me to do?";
                            break;

                        case "yes":
                            if (ContextCallShuttle && diLUIS.entities.Count() == 0)
                            {
                                BotResponse = "Okay, I scheduled a shuttle for you from building " + Origin + " to building " + Destination + ". Your Confirmation Number is "+ RandomNumber(10000, 99999)+".";
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

                        case "help":
                            if (diLUIS.entities.Count() == 0)
                            {
                                BotResponse = "I'm RefBot and I'm here to help you get food and get around campus. Try the following commands:" + Environment.NewLine + Environment.NewLine +
                                    " \"Show me all cafes.\"," + Environment.NewLine + Environment.NewLine +
                                    "\"What can I eat in cafe 16? \"," + Environment.NewLine + Environment.NewLine +
                                    "\"Where can I find pizza?\", " + Environment.NewLine + Environment.NewLine +
                                    "\"Find food trucks.\", " + Environment.NewLine + Environment.NewLine +
                                    " \"get me from building 1 to 92\" ";
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

        private void ResetShuttleVariables()
        {
            ContextCallShuttle = false;
            Destination = String.Empty;
            Origin = String.Empty;
            return;
        }

        private int RandomNumber(int min, int max)
        {
            Random random = new Random();
            return random.Next(min, max);
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

            response = string.Format(Constants.scheudleShuttleDialogue,Origin,Destination);
            return response;
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
