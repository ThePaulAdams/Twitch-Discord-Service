using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using TDSBot;
using System.Threading;

namespace Bot
{

    public class Discord
    {


        private DiscordSocketClient _client;
        public IConfigurationRoot _config;
        public Program Controller;
        public Twitch Twitch { get; set; }
        Random random = new Random();
             

        public Discord(Program controller)
        {
            Controller = controller;
            _config = controller._config;
            _client = new DiscordSocketClient();
            _client.Log += LogAsync;
            _client.Ready += ReadyAsync;
            //_client.MessageReceived += MessageReceivedAsync;

       
            Task DiscordTask = MainAsync();

        }

        private ServiceProvider ConfigureServices()
        {
            return new ServiceCollection()
                .AddSingleton<DiscordSocketClient>()
                .AddSingleton<CommandService>()
                .AddSingleton<CommandHandlingService>()
                .AddSingleton<HttpClient>()
                .AddSingleton<Program>(Controller)
                .BuildServiceProvider();
        }



        public async Task MainAsync()
        {
            var services = ConfigureServices();            
            var client = services.GetRequiredService<DiscordSocketClient>();

            client.Log += LogAsync;
            services.GetRequiredService<CommandService>().Log += LogAsync;
            _client = client;
            // Tokens should be considered secret data and never hard-coded.
            // We can read from the environment variable to avoid hardcoding.
            await client.LoginAsync(TokenType.Bot, _config["Token"]);
            await client.StartAsync();

            // Here we initialize the logic required to register our commands.
            await services.GetRequiredService<CommandHandlingService>().InitializeAsync();
            await Task.Delay(Timeout.Infinite);

        }

        private Task LogAsync(LogMessage log)
        {
            Console.WriteLine(log.ToString());
            return Task.CompletedTask;
        }

        private Task ReadyAsync()
        {
            Console.WriteLine($"Connected as -> [] :)");
            return Task.CompletedTask;
        }

        public void SendMessage(TwitchLib.PubSub.Events.OnBitsReceivedArgs e, string message)
        {
            foreach (var integration in Controller.Integrations)
            {
                if (integration.TwitchChannel == e.ChannelName)
                {
                    _client?.GetGuild(ulong.Parse(integration.DiscordGuild)).GetTextChannel(ulong.Parse(integration.DiscordChannel))
                        .SendMessageAsync("[" + integration.TwitchChannel + "] " + "Just received {e.BitsUsed} bits from {e.Username}. That brings their total to {e.TotalBitsUsed} bits!");
                }
            }
        }

        public void SendMessage(string ChannelName, string message)
        {
            foreach (var Integration in Controller.Integrations)
            {
                if (Integration.TwitchChannel == ChannelName)
                {
                    _client?.GetGuild(ulong.Parse(Integration.DiscordGuild)).GetTextChannel(ulong.Parse(Integration.DiscordChannel))
                        .SendMessageAsync(message);
                }
            }
        }

       
    }
}
