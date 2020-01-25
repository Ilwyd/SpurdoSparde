using System;
using System.Threading.Tasks;
using System.Reflection;
using System.Timers;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;
using RestSharp;
using RestSharp.Deserializers;
using Newtonsoft.Json;

namespace Spurdoo
{
    class Program
    {
        private CommandService commands;
        private DiscordSocketClient client;
        private IServiceProvider services;
        
        static void Main(string[] args)
            => new Program().Start().GetAwaiter().GetResult();

        public async Task Start()
        {
            client = new DiscordSocketClient();
            commands = new CommandService();
            client.Log += Log;

            var token = System.IO.File.ReadAllText("./token.txt");

            services = new ServiceCollection()
                    .BuildServiceProvider();

            await InstallCommands();
            
            await client.LoginAsync(TokenType.Bot, token);
            await client.StartAsync();
            await client.SetGameAsync("!spurdo | !acquiresparde");

            //Timer with length 5 mins
            //Checks to see if I'm streaming every 5 mins
            var timer = new Timer(300 * 1000);
            timer.AutoReset = true;
            timer.Elapsed += new ElapsedEventHandler(OnTimedEvent);
            timer.Start();

            await Task.Delay(-1);
        }

        private async void OnTimedEvent(Object source, ElapsedEventArgs e) {
            //Building request
            var restClient = new RestClient("https://api.twitch.tv/helix/");
            var request = new RestRequest("streams");
            request.AddParameter("user_login", "custardgod");
            request.AddHeader("Client-ID", System.IO.File.ReadAllText("./clientid.txt"));

            //Async get
            var response = await restClient.ExecuteGetTaskAsync(request);
            var content = response.Content;

            //If I'm live the JSON will have "type":"live" in it
            if(content.Contains("\"type\":\"live\"")) {
                await client.SetGameAsync("!spurdo | !acquiresparde", "https://twitch.tv/CustardGod", StreamType.Twitch);
            }
            else {
                await client.SetGameAsync("!spurdo | !acquiresparde");
            }
        }

        public async Task InstallCommands()
        {
            // Hook the MessageReceived Event into our Command Handler
            client.MessageReceived += HandleCommand;
            // Discover all of the commands in this assembly and load them.
            await commands.AddModulesAsync(Assembly.GetEntryAssembly());
        }

        public async Task HandleCommand(SocketMessage messageParam)
        {
            // Don't process the command if it was a System Message or anoother bot
            var message = messageParam as SocketUserMessage;
            if (message == null || message.Author.IsBot) 
            {
                return;
            }
            
            // Create a number to track where the prefix ends and the command begins
            int argPos = 0;

            //Getting the guild Id to find its specific prefix
            var channel = message.Channel as ITextChannel;
            var guildId = channel.GuildId;

            // Determine if the message is a command, based on if it starts with '!' or a mention prefix
            if (!(message.HasCharPrefix('!', ref argPos) || message.HasMentionPrefix(client.CurrentUser, ref argPos))) 
            {
                return;
            }

            // Create a Command Context
            var context = new CommandContext(client, message);

            // Execute the command. (result does not indicate a return value, 
            // rather an object stating if the command executed successfully)
            
            var result = await commands.ExecuteAsync(context, argPos, services);
            /*
            if (!result.IsSuccess)
            {
                var time = message.Timestamp.LocalDateTime.TimeOfDay.ToString(@"hh\:mm\:ss");
                var errorReason = result.ErrorReason;

                if(!(errorReason == "Unknown command.")) {
                    Console.WriteLine("{0} Error\t     {1}\tUser's message: {2}", time, errorReason, message);
                }
            }
            */
        }

        private Task Log(LogMessage msg)
        {
            if(!msg.ToString().Contains("Unknown User (VOICE_STATE_UPDATE")) {
                Console.WriteLine(msg.ToString());
            }
            return Task.CompletedTask;
        }
    }
}
