using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Bot;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Newtonsoft.Json.Linq;
using RestSharp;
using TwitchLib.Api.Core.Interfaces;

namespace TDSBot.modules
{
    // Modules must be public and inherit from an IModuleBase
    public class PublicCommands : ModuleBase<SocketCommandContext>
    {
        // Dependency Injection will fill this value in for us
        public Program ProgramService { get; set; }


        /// <summary>
        /// Noob commands
        /// </summary>
        /// <returns></returns>

        [Command("commands")]
        public async Task Commands()
        {
            EmbedBuilder builder = new EmbedBuilder();

            builder.WithTitle("Commands");
            builder.WithDescription("!quote [name] \n" +
                                    "!addquote [name] [quote] [date] \n" +
                                    "!twitch [channel] [message] \n" +
                                    "!twitchadd [channel] \n" +
                                    "!twitchremchannel [channel]");    // true - for inline

            await ReplyAsync("", false, builder.Build());
        }

        //!Quote name
        [Command("quote")]
        public async Task Quote([Remainder] string name)
        {
            string Name = Regex.Replace(name.Split()[0], @"[^0-9a-zA-Z\ ]+", "");
            var quote = await ProgramService.GetQuote(Name);
            //string reponse = 
            await ReplyAsync(quote);
        }




        [Command("twitchsay")]
        [RequireContext(ContextType.Guild)]
        // make sure the user is an admin on the server
        [RequireUserPermission(GuildPermission.ViewChannel)]

        public async Task twitchSay([Remainder] string message)
        {
            var sendChannel = message.Split(" ");
            var index = message.IndexOf(" ");
            string messageToSend = message.Substring(index);
            string messagesender = Context.User.Username.ToString();
            await Task.Run(() => ProgramService.SendTwitchMessage(sendChannel[0], "[" + messagesender + "] " + messageToSend));

        }







        /// <summary>
        /// admin commands
        /// </summary>

        // Ban a user
        [Command("ban")]
        [RequireContext(ContextType.Guild)]
        // make sure the user invoking the command can ban
        [RequireUserPermission(GuildPermission.BanMembers)]
        // make sure the bot itself can ban
        [RequireBotPermission(GuildPermission.BanMembers)]
        public async Task BanUserAsync(IGuildUser user, [Remainder] string reason = null)
        {
            await user.Guild.AddBanAsync(user, reason: reason);
            await ReplyAsync("ok!");
        }



        // Ban a user
        [Command("addquote")]
        [RequireContext(ContextType.Guild)]
        // make sure the user is an admin on the server
        [RequireUserPermission(GuildPermission.Administrator)]

        public async Task addQuoteAsync([Remainder] string quote)
        {
            string addedby = Context.User.Username.ToString();
            int FirstSpace = quote.IndexOf(" ");
            bool state = await ProgramService.AddQuote(quote.Substring(0, FirstSpace), quote.Substring(FirstSpace), addedby);
            if (state) await ReplyAsync("Quote Added!");
            else await ReplyAsync("Quote Failed!");
        }





        [Command("twitchadd")]
        [RequireContext(ContextType.Guild)]
        // make sure the user is an admin on the server
        // this is adding a twitch channel to the integrations
        [RequireUserPermission(GuildPermission.ViewChannel)]

        public async Task twitchadd(SocketMessage message)
        {
            var messageSplit = message.Content.Split(" ");
            var chnl = message.Channel as SocketGuildChannel;
            Integration newIntegration = new Integration()
            {
                TwitchChannel = messageSplit[1],
                DiscordChannel = message.Channel.Id.ToString(),
                DiscordGuild = chnl.Guild.Id.ToString()
            };
            ProgramService.Integrations.Add(newIntegration);

            ProgramService.AddIntegration(message.Author.Username, newIntegration.TwitchChannel, newIntegration.DiscordGuild, newIntegration.DiscordChannel);
            await Task.Run(() => ProgramService.JoinTwitchChannel(newIntegration.TwitchChannel));


        }


        [Command("twitchremchannel")]
        [RequireContext(ContextType.Guild)]
        // make sure the user is an admin on the server
        // this is adding a twitch channel to the integrations
        [RequireUserPermission(GuildPermission.Administrator)]

        public async Task twitchremchannel(SocketMessage message)
        {
            var chanSplit = message.Content.Split(" ");
            foreach (var Integration in ProgramService.Integrations)
            {
                if (Integration.TwitchChannel == chanSplit[1])
                {
                    await Task.Run(() => ProgramService.removeIntegration(Integration));
                }
            }
        }


        [Command("clips")]
        [RequireContext(ContextType.Guild)]
        // make sure the user is an admin on the server
        // this is adding a twitch channel to the integrations
        [RequireUserPermission(GuildPermission.Administrator)]

        public async Task clips([Remainder] string args)
        {

            if (args.Split(" ").Length != 2)
            {
                await ReplyAsync("use !clips channel count");
                return;
            }

            string channel = args.Split(" ")[0];
            string count = args.Split(" ")[1];
            var client = new RestClient("https://api.twitch.tv/helix/users?login=" + channel);
            client.Timeout = -1;
            var request = new RestRequest(Method.GET);
            request.AddHeader("Client-Id", ProgramService._config["twitchClientId"]);
            request.AddHeader("Authorization", "Bearer " + ProgramService._config["twitchOAuth"]);
            IRestResponse response = client.Execute(request);

            JObject getId = JObject.Parse(response.Content);
            var data = getId["data"];
            var id = data[0]["id"];


            client = new RestClient("https://api.twitch.tv/helix/clips?broadcaster_id=" + id + "&first=" + count);
            client.Timeout = -1;
            request = new RestRequest(Method.GET);
            request.AddHeader("Client-Id", ProgramService._config["twitchClientId"]);
            request.AddHeader("Authorization", "Bearer " + ProgramService._config["twitchOAuth"]);
            response = client.Execute(request);
            JObject list = JObject.Parse(response.Content);
            var testList = list["data"];
            System.Text.StringBuilder ListOfClips = new System.Text.StringBuilder();
            foreach (JObject clip in testList)
            {
                var test = clip;
                string name = clip["title"].ToString();
                string thumbURL = clip["thumbnail_url"].ToString();
                string download = thumbURL.ToString().Substring(0, thumbURL.IndexOf("-preview")) + ".mp4";
                ListOfClips.Append(name + ": <" + download + ">\n");
            }
            await ReplyAsync(ListOfClips.ToString());
        }

        [Command("clipsrange")]
        [RequireContext(ContextType.Guild)]
        // make sure the user is an admin on the server
        // this is adding a twitch channel to the integrations
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task clipsrange([Remainder] string args)
        {

            if (args.Split(" ").Length != 3)
            {
                await ReplyAsync("use !clips channel count");
                return;
            }

            string channel = args.Split(" ")[0];
            int start = int.Parse(args.Split(" ")[1]);
            int end = int.Parse(args.Split(" ")[2]);
            var client = new RestClient("https://api.twitch.tv/helix/users?login=" + channel);
            client.Timeout = -1;
            var request = new RestRequest(Method.GET);
            request.AddHeader("Client-Id", ProgramService._config["twitchClientId"]);
            request.AddHeader("Authorization", "Bearer " + ProgramService._config["twitchOAuth"]);
            IRestResponse response = client.Execute(request);

            JObject getId = JObject.Parse(response.Content);
            var data = getId["data"];
            var id = data[0]["id"];


            client = new RestClient("https://api.twitch.tv/helix/clips?broadcaster_id=" + id + "&first=" + end);
            client.Timeout = -1;
            request = new RestRequest(Method.GET);
            request.AddHeader("Client-Id", ProgramService._config["twitchClientId"]);
            request.AddHeader("Authorization", "Bearer " + ProgramService._config["twitchOAuth"]);
            response = client.Execute(request);
            JObject list = JObject.Parse(response.Content);
            var testList = list["data"];
            System.Text.StringBuilder ListOfClips = new System.Text.StringBuilder();
            int count = 1;

            foreach (JObject clip in testList)
            {
                if (count >= start)
                {
                    var test = clip;
                    string name = clip["title"].ToString();
                    string thumbURL = clip["thumbnail_url"].ToString();
                    string download = thumbURL.ToString().Substring(0, thumbURL.IndexOf("-preview")) + ".mp4";
                    ListOfClips.Append(name + ": <" + download + ">\n");
                }
                count++;
            }
            await ReplyAsync(ListOfClips.ToString());
        }

    }
}