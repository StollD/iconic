using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using LiteDB;

namespace Iconic
{
    public class Commands
    {
        [Command("info")]
        [Description("Outputs information about the bots configuration.")]
        public async Task Info(CommandContext ctx)
        {
            // Access the stored data
            using (LiteDatabase db = new LiteDatabase("data.db"))
            {
                LiteCollection<ServerConfig> servers = db.GetCollection<ServerConfig>("servers");
                
                // Search for the active server
                ServerConfig server = servers.FindOne(s => s.Server == ctx.Guild.Id);
                
                // No server config found
                if (server == null)
                {
                    await ctx.RespondAsync(":warning: No server config available! Add an icon first!");
                    return;
                }
                
                // Compose a reply
                String reply = ":information_source: **" + ctx.Guild.Name + "**\n";
                reply += "**Interval**: `" + server.IntervalMin + "` - `" + server.IntervalMax + "`\n\n";
                for (Int32 i = 0; i < server.Thumbnails.Count; i++)
                {
                    reply += "**" + (i + 1) + "**: `" + server.Thumbnails[i] + "`\n";
                }

                await ctx.RespondAsync(reply);
            }
        }

        [Command("add")]
        [Description("Adds a new icon.")]
        [RequirePermissions(Permissions.ManageGuild)]
        public async Task Add(CommandContext ctx, [Description("A direct link to a discord compatible image file.")] String url)
        {
            try
            {
                // Try to download the icon to see if the url is invalid
                WebClient client = new WebClient();
                await client.DownloadDataTaskAsync(url);
                
                // Store it in the database
                using (LiteDatabase db = new LiteDatabase("data.db"))
                {
                    // Search for the active server
                    LiteCollection<ServerConfig> servers = db.GetCollection<ServerConfig>("servers");
                    ServerConfig server = servers.FindOne(s => s.Server == ctx.Guild.Id);
                
                    // No server config found, add one
                    if (server == null)
                    {
                        server = new ServerConfig
                        {
                            Server = ctx.Guild.Id,
                            NextSet = DateTime.Now,
                            Thumbnails = new List<String>()
                        };
                        servers.Insert(server);
                    }
                    
                    // Add the icon ID
                    server.Thumbnails.Add(url);
                    servers.Update(server);
                    
                    // Reply
                    await ctx.RespondAsync(":white_check_mark: Added `" + url + "` as Icon **" +
                                           server.Thumbnails.Count +
                                           "**");
                }
            }
            catch
            {
                await ctx.RespondAsync(":warning: The specified URL seems to be invalid!");
            }
        }

        [Command("remove")]
        [Description("Removes an icon.")]
        [RequirePermissions(Permissions.ManageGuild)]
        public async Task Remove(CommandContext ctx, [Description("The id of the icon to delete.")] Int32 id)
        {
            // Look up the server config in the database
            using (LiteDatabase db = new LiteDatabase("data.db"))
            {
                LiteCollection<ServerConfig> servers = db.GetCollection<ServerConfig>("servers");
                ServerConfig server = servers.FindOne(s => s.Server == ctx.Guild.Id);

                // No server config found, add one
                if (server == null)
                {
                    await ctx.RespondAsync(":warning: No server config available! Add an icon first!");
                    return;
                }

                // Remove the icon ID
                server.Thumbnails.RemoveAt(id - 1);
                
                if (server.Thumbnails.Count == 0)
                {
                    servers.Delete(s => s.Id == server.Id);
                }
                else
                {
                    servers.Update(server);
                }

                // Reply
                await ctx.RespondAsync(":white_check_mark: Removed Icon **" + id + "**");
            }
        }

        [Command("interval")]
        [Description("Edits the interval in which icons are changed.")]
        [RequirePermissions(Permissions.ManageGuild)]
        public async Task Interval(CommandContext ctx, [Description("The min. amount of days between icon changes.")] Single min, [Description("The max. amount of days between icon changes.")] Single max)
        {
            // Search for the active server
            using (LiteDatabase db = new LiteDatabase("data.db"))
            {
                LiteCollection<ServerConfig> servers = db.GetCollection<ServerConfig>("servers");
                ServerConfig server = servers.FindOne(s => s.Server == ctx.Guild.Id);

                // No server config found, add one
                if (server == null)
                {
                    await ctx.RespondAsync(":warning: No server config available! Add an icon first!");
                    return;
                }

                // Add the icon ID
                server.IntervalMax = max;
                server.IntervalMin = min;
                servers.Update(server);

                // Reply
                await ctx.RespondAsync(":white_check_mark: Updated the interval.");
            }
        }

        [Command("shuffle")]
        [Description("Changes the server icon immediately")]
        [RequirePermissions(Permissions.ManageGuild)]
        public async Task Shuffle(CommandContext ctx)
        {
            // Search for the active server
            using (LiteDatabase db = new LiteDatabase("data.db"))
            {
                LiteCollection<ServerConfig> servers = db.GetCollection<ServerConfig>("servers");
                ServerConfig server = servers.FindOne(s => s.Server == ctx.Guild.Id);

                // No server config found, add one
                if (server == null)
                {
                    await ctx.RespondAsync(":warning: No server config available! Add an icon first!");
                    return;
                }

                // Add the icon ID
                server.NextSet = DateTime.Now;
                servers.Update(server);

                // Reply
                await ctx.RespondAsync(
                    ":white_check_mark: Shuffled the icon. It can take up to one minute until it actually changes.");
            }
        }

        [Command("invite")]
        [Description("Ask for an invite link to add Iconic to your own server")]
        [RequirePermissions(Permissions.ManageGuild)]
        public async Task Invite(CommandContext ctx)
        {
            await ctx.RespondAsync(":information_source: To add Iconic to your own server, please open this link in a browser: https://bit.ly/iconic-invite.");
        }
    }
}