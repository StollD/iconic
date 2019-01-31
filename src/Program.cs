using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Converters;
using DSharpPlus.Entities;
using LiteDB;

namespace Iconic
{
    internal class Program
    {
        public static void Main(String[] args)
        {
            MainAsync(args).ConfigureAwait(false).GetAwaiter().GetResult();
        }
        
        public static async Task MainAsync(String[] args)
        {
            // Read the discord bot token from the commandline
            String token = args[0];

            // Setup the Discord connection
            DiscordClient discord = new DiscordClient(new DiscordConfiguration
            {
                Token = token,
                TokenType = TokenType.Bot,
                AutoReconnect = true,
                UseInternalLogHandler = true,
                LogLevel = LogLevel.Debug
            });

            // Setup commands
            CommandsNextModule commands = discord.UseCommandsNext(new CommandsNextConfiguration
            {
                EnableDms = false,
                EnableMentionPrefix = true,
                EnableDefaultHelp = true,
                CaseSensitive = false,
                IgnoreExtraArguments = false,
            });

            commands.RegisterCommands<Commands>();
            await discord.ConnectAsync();

            // Start a background thread
            Thread thread = new Thread(() => UpdateIcons(discord)) {IsBackground = true};
            thread.Start();

            await Task.Delay(-1);
        }

        private static async void UpdateIcons(DiscordClient discord)
        {
            // Run forever
            Random random = new Random();
            while (true)
            {
                using (LiteDatabase db = new LiteDatabase("data.db"))
                {
                    // Check all server configs
                    LiteCollection<ServerConfig> servers = db.GetCollection<ServerConfig>("servers");
                    ServerConfig[] configs = servers.FindAll().ToArray();
                    for (Int32 i = 0; i < configs.Length; i++)
                    {
                        ServerConfig server = configs[i];
                        if (server.Thumbnails.Count == 0)
                        {
                            servers.Delete(s => s.Id == server.Id);
                            continue;
                        }
                        if (DateTime.Now > server.NextSet)
                        {
                            // Get the server
                            DiscordGuild guild = await discord.GetGuildAsync(server.Server);
                            
                            // Select a new image
                            String newIcon = server.Thumbnails[random.Next(0, server.Thumbnails.Count)];
                            WebClient client = new WebClient();
                            Byte[] data = await client.DownloadDataTaskAsync(newIcon);
                            MemoryStream stream = new MemoryStream(data);
                            
                            // Apply the modification
                            await guild.ModifyAsync(icon: stream);
                            
                            // Update the next update time
                            Double days = server.IntervalMin +
                                          random.NextDouble() * (server.IntervalMax - server.IntervalMin);
                            server.NextSet = server.NextSet.AddDays(days);
                            servers.Update(server);
                        }
                    }
                }

                // Wait one minute
                Thread.Sleep(60000);
            }
        }
    }
}