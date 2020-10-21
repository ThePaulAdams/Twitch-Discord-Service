using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Timers;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Models;
using TwitchLib.PubSub;
using TwitchLib.Api.V5.Models.Users;


namespace Bot
{

    public class Program
    {
        string cs;

        public static Discord Twitchat;

        internal object MainAsync(Program controller)
        {
            throw new NotImplementedException();
        }

        public static Twitch Discchat;
        public List<Integration> Integrations { get; set; }
        public IConfigurationRoot _config { get; set; }

        static void Main(string[] args)
        {
            var Controller = new Program();

            Twitchat = new Discord(Controller);
            Discchat = new Twitch(Controller);
            Twitchat.Twitch = Discchat;
            Discchat.Discord = Twitchat;
            Discchat.setupTwitch();
            Console.ReadLine();
        }

        public Twitch GetTwitch()
        {
            return Discchat;
        }

        public Discord GetDiscord()
        {
            return Twitchat;
        }

        

        public Program()
        {

            IConfigurationBuilder builder = new ConfigurationBuilder()
                                 .AddJsonFile("config.json");
            _config = builder.Build();
            cs = _config["dbConString"];
            var _builder = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile(path: "config.json");
            _config = _builder.Build();

            Integrations = new List<Integration>();
          

            setupMysql();
            
        }


        
        public bool integrationExists(string Twitch, string DiscordGuild, string DiscordChannel)
        {

            using (MySqlConnection con = new MySqlConnection(cs))
            {
                con.Open();
                var sql = "SELECT * FROM Integrations;";
                using var cmd = new MySqlCommand(sql, con);
                using MySqlDataReader rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    Integration newIntegration = new Integration()
                    {
                        TwitchChannel = rdr["TwitchChannel"].ToString(),
                        DiscordChannel = rdr["DiscordChannel"].ToString(),
                        DiscordGuild = rdr["DiscordGuild"].ToString()
                    };

                    if (Twitch == newIntegration.TwitchChannel && DiscordChannel == newIntegration.DiscordChannel && DiscordGuild == newIntegration.DiscordGuild) return true;

                }
            }
            return false;
        }

        public void AddIntegration(string Integrator, string Twitch, string DiscordGuild, string DiscordChannel)
        {
            if(integrationExists(Twitch, DiscordGuild,DiscordChannel)) return;

            MySqlConnection mySqlConnection = new MySqlConnection(cs);
            using (MySqlConnection con = mySqlConnection) {
                con.Open();

                var sql = "INSERT INTO Integrations(TwitchChannel, DiscordGuild, DiscordChannel, IntegrationManager) VALUES(@twitch, @discordGuild, @discordChannel, @integrator)";

                using (MySqlCommand cmd = new MySqlCommand(sql, con))
                {
                    cmd.Parameters.AddWithValue("@twitch", Twitch);
                    cmd.Parameters.AddWithValue("@discordGuild", DiscordGuild);
                    cmd.Parameters.AddWithValue("@discordChannel", DiscordChannel);
                    cmd.Parameters.AddWithValue("@integrator", Integrator);
                    cmd.Prepare();
                    cmd.ExecuteNonQuery();
                }

                User details = Twitchat.Twitch.GetUserDetails(Twitch);
                sql = "INSERT INTO Channels(twitchId, name, type, bio, logo) VALUES(@_twitchId, @_name, @_type, @_bio, @_logo)";
                using (MySqlCommand cmd = new MySqlCommand(sql, con))
                {
                    cmd.Parameters.AddWithValue("@_twitchId", details.Id);
                    cmd.Parameters.AddWithValue("@_name", details.Name);
                    cmd.Parameters.AddWithValue("@_type", details.Type);
                    cmd.Parameters.AddWithValue("@_bio", details.Bio);
                    cmd.Parameters.AddWithValue("@_logo", details.Logo);
                    cmd.Prepare();
                    cmd.ExecuteNonQuery();
                }
            }
            Console.WriteLine("row inserted");



            
        }


        public void removeIntegration(Integration integration)
        {
            MySqlConnection mySqlConnection = new MySqlConnection(cs);
            using (MySqlConnection con = mySqlConnection)
            {
                con.Open();

                var sql = "DELETE FROM Integrations WHERE TwitchChannel=@twitch";

                using (MySqlCommand cmd = new MySqlCommand(sql, con))
                {
                    cmd.Parameters.AddWithValue("@twitch", integration.TwitchChannel);
                    cmd.Prepare();
                    cmd.ExecuteNonQuery();
                }
            }
            Discchat.LeaveIntegrations(integration.TwitchChannel);
        }

        public void setupMysql()
        {
            using (MySqlConnection con = new MySqlConnection(cs))
            {
                con.Open();
                var sql = "SELECT * FROM Integrations;";
                using (var cmd = new MySqlCommand(sql, con))
                {
                    using (MySqlDataReader rdr = cmd.ExecuteReader())
                    {

                        while (rdr.Read())
                        {
                            Integration newIntegration = new Integration()
                            {
                                TwitchChannel = rdr["TwitchChannel"].ToString(),
                                DiscordChannel = rdr["DiscordChannel"].ToString(),
                                DiscordGuild = rdr["DiscordGuild"].ToString()
                            };                            
                            Integrations.Add(newIntegration);                            
                        }
                    }
                }
            }
        }              

        public static bool PingHost(string nameOrAddress)
        {
            bool pingable = false;
            Ping pinger = null;
            try
            {
                pinger = new Ping();
                PingReply reply = pinger.Send(nameOrAddress);
                pingable = reply.Status == IPStatus.Success;
                if (!pingable)
                { //just try again because we are lazy
                    reply = pinger.Send(nameOrAddress);
                    pingable = reply.Status == IPStatus.Success;
                }
            }
            catch (PingException)
            {
                // Discard PingExceptions and return false;
            }
            finally
            {
                if (pinger != null)
                {
                    pinger.Dispose();
                }
            }
            return pingable;
        }

        internal object MainAsync()
        {
            throw new NotImplementedException();
        }

        public void SendDiscordMessage(string ChannelName, string message)
        {
            Twitchat.SendMessage(ChannelName, message);
        }

        public void SendTwitchMessage(string ChannelName, string message)
        {
            Discchat.SendMessage(ChannelName, message);
        }        
        
        public void JoinTwitchChannel(string ChannelName)
        {
            Discchat.JoinChannel(ChannelName);
        }


        public async Task<string> GetQuote(string person)
        {
            string quote = string.Empty;
            MySqlConnection mySqlConnection = new MySqlConnection(cs);
            using (MySqlConnection con = mySqlConnection)
            {
                con.Open();

                var sql = "SELECT * FROM Quotes WHERE person =\"" + person + "\" ORDER BY RAND() LIMIT 1;";
                using (var cmd = new MySqlCommand(sql, con))
                {
                    using (MySqlDataReader rdr = cmd.ExecuteReader())
                    {
                        while (await rdr.ReadAsync())
                        {
                            quote = rdr["quote"].ToString();
                        }
                    }
                }
            }
            return quote;
        }

        public async Task<bool> AddQuote(string person, string quote, string addedby)
        {            
            MySqlConnection mySqlConnection = new MySqlConnection(cs);
            using (MySqlConnection con = mySqlConnection)
            {
                con.Open();
                var sql = "INSERT INTO Quotes(person, quote, guid, addedby) VALUES(?person, ?quote, ?guid, ?addedby)";
                await using var cmd = new MySqlCommand(sql, con);

                cmd.Parameters.Add("?guid", MySqlDbType.Guid).Value = Guid.NewGuid();
                cmd.Parameters.Add("?person", MySqlDbType.VarChar).Value = person;
                cmd.Parameters.Add("?quote", MySqlDbType.VarChar).Value = quote;
                cmd.Parameters.Add("?addedby", MySqlDbType.VarChar).Value = addedby;
                try
                {
                    cmd.ExecuteNonQuery();
                }
                catch (Exception e) { 
                    return false; }
            }
            return true;
        }

    }
}
