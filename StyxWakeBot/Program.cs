using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace ServerWakeBot {
    public class Program {
        #region Constants

        // Security measures
        public const string TokenFileName = "token.txt"; // Fill with bot token from discord developer site
        public const string UserWhitelist = "users-whitelist.json"; // Get the user-id from Discord or by analysing MessageReceived
        public const string MacsFileName = "-macs.json"; // Automatically generated
        public const string CommandPrefix = "!";

        public const string BroadcastMac = "255.255.255.255"; // Replace with broadcast address for local subnet.

        #endregion

        public static List<ulong> WhitelistedUsers;

        public static void Main(string[] args) => RunBot().GetAwaiter().GetResult();

        private static async Task RunBot() {
            // Ensure token exists.
            if (!File.Exists(TokenFileName)) {
                Console.WriteLine("Please place bot token in a file called \"token.txt\". Press any key to exit...");
                Console.ReadKey();
                return;
            }

            // Read token from file.
            var token = File.ReadAllText(TokenFileName);

            // Get white-listed users.
            WhitelistedUsers = new List<ulong>();
            if (File.Exists(UserWhitelist))
                WhitelistedUsers = JsonConvert.DeserializeObject<List<ulong>>(File.ReadAllText(UserWhitelist));

            // Create Discord client.
            var client = new DiscordSocketClient();
            client.Log += (e) => {
                Console.WriteLine(e.ToString());
                return Task.CompletedTask;
            };

            // Create command service and map.
            var commands = new CommandService();
            var commandMap = new ServiceCollection();
            var multimatch = new MultiMatchHandling();

            // Load commands from assembly.
            await commands.AddModulesAsync(Assembly.GetEntryAssembly(),null);

            // Listen for messages.
            client.MessageReceived += async (message) => {
                // Get the message and check to see if it is a user message.
                var msg = message as IUserMessage;
                if (msg == null)

                    return;

                // Keeps track of where the command begins.
                var pos = 0;


                // Attempt to parse a command.
                if (msg.HasStringPrefixLower(CommandPrefix, ref pos)) {
                    // Check to see if user is in whitelist.
                    if (!WhitelistedUsers.Contains(message.Author.Id)) {
                        Console.WriteLine("not in");
                        Console.WriteLine(message.Author.Id);
                        return;
                    }
                    // Execute command.
                    var result = await commands.ExecuteAsync(new CommandContext(client, msg), msg.Content.Substring(pos),null,multimatch);
                    if (!result.IsSuccess) {
                        // Is the command just unknown? If so, return.
                        if (result.Error == CommandError.UnknownCommand)
                            return;
                        await msg.Channel.SendMessageAsync($"Error: {result.ErrorReason}\n\nIf there are spaces in a parameter, make sure to surround it with quotes.");
                    }
                    return;
                }
            };

            // Login to Discord.
            await client.LoginAsync(TokenType.Bot, token);
            await client.StartAsync();

            // Wait forever.
            await Task.Delay(-1);
        }
    }
}
