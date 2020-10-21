using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TwitchLib.Api;
using TwitchLib.Api.V5.Models.Channels;
using TwitchLib.Api.V5.Models.Users;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Models;
using TwitchLib.PubSub;


namespace Bot
{
    
    
    public class Twitch
    {
        private List<JoinedChannel> joinedChannels = new List<JoinedChannel>();
        private JoinedChannel Channel;
        TwitchClient client;
        TwitchPubSub pubSub;


        public Discord Discord { get; set; }
        public Program Controller;
        TwitchAPI twitchApi;
        public IConfigurationRoot _config;

        public Twitch(Program controller)
        {
            _config = controller._config;
            Controller = controller;
            twitchApi = new TwitchAPI();
        }



        public void setupTwitch()
        {
            ConnectionCredentials credentials = new ConnectionCredentials(_config["twitchName"], _config["twitchOAuth"]);
            var clientOptions = new ClientOptions
            {
                MessagesAllowedInPeriod = 750,
                ThrottlingPeriod = TimeSpan.FromSeconds(30)
            };
            WebSocketClient customClient = new WebSocketClient(clientOptions);
            client = new TwitchClient(customClient);
            client.Initialize(credentials, _config["defaultTwitchChannel"]);

            twitchApi.Settings.ClientId = _config["twitchClientId"];
            twitchApi.Settings.AccessToken = _config["twitchToken"];


            client.SetConnectionCredentials(credentials);

            client.OnLog += Client_OnLog;
            client.OnJoinedChannel += Client_OnJoinedChannel;
            client.OnMessageReceived += Client_OnMessageReceived;
            client.OnWhisperReceived += Client_OnWhisperReceived;
            client.OnNewSubscriber += Client_OnNewSubscriber;
            client.OnConnected += Client_OnConnected;


            pubSub = new TwitchPubSub();
            pubSub.OnPubSubServiceConnected += OnPubSubServiceConnected;

            // Connect
            pubSub.Connect();

            client.Connect();


            JoinIntegrations();

        }


        public void SendMessage(string Channel, string message)
        {
            client.SendMessage(Channel, message);
        }


        public void JoinIntegrations()
        {
            foreach (Integration integration in Controller.Integrations)
            {
                if (integration.TwitchChannel != "Paul1337noob") client.JoinChannel(integration.TwitchChannel);
            }
        }

        public void LeaveIntegrations(string Channel)
        {           
           client.LeaveChannel(Channel);           
        }

        #region Twitch-Client
        private void Client_OnLog(object sender, OnLogArgs e)
        {
            if (e.Data.Contains("Joining channel"))
            {
                Console.WriteLine($"{e.DateTime.ToString()}: {e.BotUsername} - {e.Data}");
            }
        }

        private void OnPubSubServiceConnected(object sender, System.EventArgs e)
        {
            Console.WriteLine("PubSubServiceConnected!");



            //this needs to be the twitch streamer oath token, not the bots... pubSub.SendTopics(Controller._config["twitchOAuth"]);
            //this needs to be the twitch streamer ID pubSub.ListenToBitsEvents("");

            pubSub.OnBitsReceived += OnBitsReceived;

            // SendTopics accepts an oauth optionally, which is necessary for some topics, such as bit events.
            pubSub.SendTopics("some long string here, probably oauth, cant remember");
        }

        private void OnBitsReceived(object sender, TwitchLib.PubSub.Events.OnBitsReceivedArgs e)
        {
            Console.WriteLine($"{e.Username} cheered {e.BitsUsed} bits");
        }


        private void Client_OnConnected(object sender, OnConnectedArgs e)
        {
            //client.SendMessage(e.AutoJoinChannel, "Discord Integration Connected: " + e.AutoJoinChannel);
        }

        private void Client_OnJoinedChannel(object sender, OnJoinedChannelArgs e)
        {
            //client.SendMessage(e.Channel, "Discord Integration Connected: " + e.Channel);
            //PubSub.ListenToFollows(GetUserId(e.Channel));
            //PubSub.ListenToBitsEvents(GetUserId(e.Channel));
            Channel = new JoinedChannel(e.Channel);
            joinedChannels.Add(Channel);
        }

        private void Client_OnMessageReceived(object sender, OnMessageReceivedArgs e)
        {
            if (e.ChatMessage.IsMe) return;
            if (e.ChatMessage.DisplayName.ToLower() != "moobot" && e.ChatMessage.DisplayName.ToLower() != "nightbot")
            {
                Console.WriteLine("[" + DateTime.Now.ToString("HH:mm:ss") + "] " + e.ChatMessage.Username + ": " + e.ChatMessage.Message);
                string command = e.ChatMessage.Message.Split(" ")[0];
                if (e.ChatMessage.IsBroadcaster || e.ChatMessage.Username.ToLower() == "paul1337noob")
                {
                    if (command == "!remove")
                    {
                        for (int i = 0; i < Controller.Integrations.Count; i++)
                        {
                            Integration Integration = Controller.Integrations[i];
                            if (Integration.TwitchChannel == e.ChatMessage.Channel)
                            {
                                Controller.removeIntegration(Integration);
                            }
                        }
                    }
                }
                string timenow = DateTime.Now.ToString("HH:mm:ss");
                foreach (Integration integration in Controller.Integrations.Where(x => x.TwitchChannel.ToLower() == e.ChatMessage.Channel.ToString().ToLower()))
                {

                    string message = "["+ timenow + "][" + integration.TwitchChannel + "] " + "(" + e.ChatMessage.Username + ") " + e.ChatMessage.Message;
                    Controller.SendDiscordMessage(integration.TwitchChannel, message);
                    
                }
            }
        }



        private void Client_OnWhisperReceived(object sender, OnWhisperReceivedArgs e)
        {
            // replace this with your own bot details.
            client.SendWhisper(e.WhisperMessage.Username, "Hey! I'm a bot, Add me to your server by clicking this link https://discord.com/api/oauth2/authorize?client_id=719923553482571836&permissions=8&scope=bot");
        }

        private void Client_OnNewSubscriber(object sender, OnNewSubscriberArgs e)
        {
            //if (e.Subscriber.SubscriptionPlan == SubscriptionPlan.Prime)
            //client.SendMessage(e.Channel, $"Welcome {e.Subscriber.DisplayName} to the substers! You just earned 500 points! So kind of you to use your Twitch Prime on this channel!");
            //else
            //client.SendMessage(e.Channel, $"Welcome {e.Subscriber.DisplayName} to the substers! You just earned 500 points!");
        }

        #endregion

        #region Twitch-PubSub
        private void Pubsub_OnPubSubServiceConnected(object sender, System.EventArgs e)
        {
            Console.WriteLine("[PubSub] connected.");
        }

        private void Pubsub_OnPubSubServiceDisconnected(object sender, System.EventArgs e)
        {
            Console.WriteLine("[PubSub] disconnected.");
        }

        private void Pubsub_OnPubSubServiceError(object sender, TwitchLib.PubSub.Events.OnPubSubServiceErrorArgs e)
        {
            Console.WriteLine("[PubSub] error. " + e.Exception.Message);
        }

        private void Pubsub_OnListenResponse(object sender, TwitchLib.PubSub.Events.OnListenResponseArgs e)
        {
            if (e.Successful)
            {
                Console.WriteLine($"[PubSub] Successfully verified listening to topic: {e.Topic}");
            }
            else
            {
                Console.WriteLine($"Failed to subscribe to {e.Topic} - Error: {e.Response.Error}");
            }
        }

        private void Pubsub_OnBitsReceived(object sender, TwitchLib.PubSub.Events.OnBitsReceivedArgs e)
        {
            string message = $"[EVENT] Just received {e.BitsUsed} bits from {e.Username}. That brings their total to {e.TotalBitsUsed} bits!";
            Console.WriteLine(message);
            Discord.SendMessage(e.ChannelName, message);
        }

        private void Pubsub_OnChannelSubscription(object sender, TwitchLib.PubSub.Events.OnChannelSubscriptionArgs e)
        {
            string message = $"[EVENT] {e.Subscription.Username} just subscribed to the channel for {e.Subscription.Months}. time!";
            Console.WriteLine(message);
            Discord.SendMessage(e.Subscription.ChannelName, message);
        }

        private void Pubsub_OnNewFollower(object sender, TwitchLib.PubSub.Events.OnFollowArgs e)
        {
            string message = $"[EVENT] {e.Username} is now a new follower of your channel.";
            Console.WriteLine(message);
            Discord.SendMessage(e.FollowedChannelId, message);
        }

        private void Pubsub_OnStreamUp(object sender, TwitchLib.PubSub.Events.OnStreamUpArgs e)
        {
            Console.WriteLine($"[EVENT] Stream is now UP - Have fun!");
        }

        private void Pubsub_OnStreamDown(object sender, TwitchLib.PubSub.Events.OnStreamDownArgs e)
        {
            Console.WriteLine($"[EVENT] Stream is now down.");
        }
        #endregion

        public void JoinChannel(string ChannelName)
        {
            client.JoinChannel(ChannelName);
        }

        public string GetUserId(string username) {
            User[] userList = twitchApi.V5.Users.GetUserByNameAsync(username).Result.Matches;
            if(userList == null || userList.Length == 0)
            {
                return null;
            }
            else
            {
                return userList[0].Id.ToString();
            }
        }

        public User GetUserDetails(string username)
        {
            User[] userList = twitchApi.V5.Users.GetUserByNameAsync(username).Result.Matches;
            if (userList == null || userList.Length == 0)
            {
                return null;
            }
            else
            {
                return userList[0];
            }
        }


    }
}
