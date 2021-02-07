using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

using Discord;
using Discord.Commands;
using Discord.WebSocket;

using MongoDB.Driver;

using TU20Bot.Models;
using TU20Bot.Support;
using TU20Bot.Configuration;

namespace TU20Bot {
    public class Handler {
        // Config
        private const char prefix = '-';
        
        private Client client;
        
        private CommandService commands;
        private ServiceProvider services;

        private readonly Random random = new Random();

        // Checks the state of a factory; if it's currently unused, all the voice channels are removed
        // Triggered by a factory timer
        private async Task checkFactory(FactoryDescription factory) {
            var inUse = false;

            foreach (var channelId in factory.channels) {
                var channel = (IVoiceChannel)client.GetChannel(channelId);
                if (channel == null)
                    continue;
                
                var users = channel.GetUsersAsync();
                if (!await users.AnyAsync(user => user.Count != 0))
                    continue;
                
                inUse = true;
                break;
            }

            // If all channels are currently not in use, remove all of them
            if (!inUse) {
                factory.timer.Stop();
                factory.timer = null;
                
                foreach (var voiceChannel in factory.channels
                    .Select(channel => client.GetChannel(channel))
                    .OfType<SocketVoiceChannel>()) {
                    await voiceChannel.DeleteAsync();
                }

                factory.channels.Clear();
            }
        }

        // Called by Discord.Net when it wants to log something.
        private static Task log(LogMessage message) {
            Console.WriteLine(message.Message);
            return Task.CompletedTask;
        }

        private async Task sendErrorMessage(string problem, IMessage userMessage = null) {
            if (userMessage != null) {
                try {
                    await userMessage.AddReactionAsync(new Emoji("❌"));
                } catch (Exception) { /* Oh well, what can you do. */ }
            }
            
            if (client.GetChannel(client.config.errorChannelId) is IMessageChannel errorChannel) {
                try {
                    await errorChannel.SendMessageAsync(problem);
                    return;
                } catch (Exception) { /* Go back! */ }
            }
            
            await Console.Error.WriteLineAsync(problem);
        }
        
        // Called by Discord.Net when the bot receives a message.
        private async Task messageReceived(SocketMessage message) {
            if (!(message is SocketUserMessage userMessage)) return;

            var prefixStart = 0;

            if (userMessage.HasCharPrefix(prefix, ref prefixStart)) {
                // Create Context and Execute Commands
                var context = new SocketCommandContext(client, userMessage);
                var result = await commands.ExecuteAsync(context, prefixStart, services);
                
                // Handle any errors. Returns are there to skip event builder entries.
                if (!result.IsSuccess) {
                    if (result.Error != CommandError.UnknownCommand) {
                        if (result.Error == CommandError.Exception
                            && result is ExecuteResult execution) {
                            await sendErrorMessage(
                                $"```\n{execution.Exception.Message}\n\n{execution.Exception.StackTrace}\n```",
                                userMessage);
                        } else {
                            await sendErrorMessage(
                                $"Halt We've hit an error.\n```\n{result.ErrorReason}\n```",
                                userMessage);
                        }

                        return;
                    }
                } else {
                    return;
                }
            }

            if (client.config.eventBuilders.TryGetValue(message.Author.Id, out var builder))
                await builder.sendMessage(message);
        }

        private Task userLeft(SocketGuildUser user) {
            client.config.logs.Add(new LogEntry {
                logEvent = LogEvent.UserLeave,
                id = user.Id,
                name = user.Username,
                discriminator = user.DiscriminatorValue,
                time = DateTime.UtcNow
            });

            return Task.CompletedTask;
        }
        
        // Called when a user joins the server.
        private async Task userJoined(SocketGuildUser user) {
            // Log
            client.config.logs.Add(new LogEntry {
                logEvent = LogEvent.UserJoin,
                id = user.Id,
                name = user.Username,
                discriminator = user.DiscriminatorValue,
                time = DateTime.UtcNow
            });
            
            // Greetings
            var channel = (IMessageChannel)client.GetChannel(client.config.welcomeChannelId);

            var greetings = client.config.welcomeMessages;

            // Send welcome message.
            await channel.SendMessageAsync(
                greetings[random.Next(0, greetings.Count)] + $" <@{user.Id}>");

            string email = null;

            // Check database for users that have registered their email previously.
            if (client.database != null) {
                var collection = client.database.GetCollection<UserModel>(UserModel.collectionName);

                var emailModel = await collection.Find(
                    Builders<UserModel>.Filter.Eq(x => x.discordId, user.Id.ToString())
                ).Limit(1).FirstOrDefaultAsync();

                if (emailModel != null)
                    email = emailModel.email;
            }

            // Try to match the new user by their name to any existing roleMatch lists
            var nameMatches = client.config.userRoleMatches
                .Where(x => NameMatcher.matchName(user, x.userDetailInformation).level != NameMatcher.MatchLevel.NoMatch) // drop
                .Select(x => x.role); // grab roles

            // If the email has been found, find all role matches to which the email belongs to
            var emailMatches = (email == null) ? new List<ulong>() : client.config.userRoleMatches
                .Where(x => x.userDetailInformation.Any(u => u.email == email)) // check for email
                .Select(x => x.role); // grab roles

            var guild = user.Guild;

            var roles = nameMatches
                .Concat(emailMatches)
                .Distinct()
                .Select(x => guild.GetRole(x))
                .ToList();

            // Add the roles.
            if (roles.Any())
                await user.AddRolesAsync(roles);
        }

        private async Task voiceUpdated(
            SocketUser user, SocketVoiceState before, SocketVoiceState after) {
            var factory = client.config.factories.FirstOrDefault(x => x.id == after.VoiceChannel?.Id);

            if (factory == null)
                return;
            
            var guild = client.GetGuild(after.VoiceChannel.Guild.Id);
            
            IVoiceChannel moveTo = null;
            
            if (factory.channels.Count < factory.maxChannels) {
                const double timeoutTime = 1000 * 60;
                
                // Create a voice channel in the format of: "name count"
                var channel = await guild.CreateVoiceChannelAsync(
                    $"{factory.name} {factory.channels.Count + 1}");
                factory.channels.Add(channel.Id);

                // If no timer exists, create one.
                // For an existing factory the timer will be set to null when all voice channels are no longer in use
                if (factory.timer == null) {
                    factory.timer = new System.Timers.Timer(timeoutTime);
                    factory.timer.Elapsed += (sender, args) => {
                        checkFactory(factory).RunSynchronously();
                    };
                    factory.timer.AutoReset = true;
                    factory.timer.Start();
                }

                await channel.ModifyAsync(x => x.CategoryId = after.VoiceChannel.CategoryId);

                moveTo = channel;
            } else if (factory.channels.Count != 0) {
                moveTo = guild.GetChannel(
                    factory.channels[random.Next(factory.channels.Count)]) as IVoiceChannel;
            }

            if (moveTo != null) {
                await ((SocketGuildUser)user).ModifyAsync(
                    x => x.Channel = new Discord.Optional<IVoiceChannel>(moveTo));
            }
        }

        private enum ModifyRoleOp {
            Add,
            Remove
        }

        private async Task modifyRole(ulong messageId,
            ISocketMessageChannel channel, SocketReaction reaction, ModifyRoleOp op) {
            var reactor = client.config.reactors.FirstOrDefault(x => x.id == messageId);
            
            // Must be in a guild to assign roles, must has a user attributed to reaction, must have matched a reactor
            if (!reaction.User.IsSpecified
                || !(reaction.User.Value is IGuildUser)
                || !(channel is IGuildChannel)
                || reactor == null)
                return;

            // Search to see if the reactor has a role defined for this emoticon
            var name = reactor.pairs.FirstOrDefault(x => x.emote == reaction.Emote.Name);
            if (name == null)
                return;

            var role = ((IGuildChannel)channel).Guild.GetRole(name.roleId);

            switch (op) {
                case ModifyRoleOp.Add:
                    await ((IGuildUser)reaction.User.Value).AddRoleAsync(role);
                    break;
                case ModifyRoleOp.Remove:
                    await ((IGuildUser)reaction.User.Value).RemoveRoleAsync(role);
                    break;
                default:
                    return;
            }
        }

        private async Task reactionAdded(Cacheable<IUserMessage, ulong> message,
            ISocketMessageChannel channel, SocketReaction reaction) {
            await modifyRole(message.Id, channel, reaction, ModifyRoleOp.Add);

            if (client.config.eventBuilders.TryGetValue(reaction.UserId, out var builder))
                await builder.addReaction(message.Id, reaction);

            if (reaction.UserId != client.CurrentUser.Id
                && channel.Id == client.config.approvalChannelId
                && channel is IGuildChannel guildChannel) {
                var user = await guildChannel.Guild.GetUserAsync(reaction.UserId);
                var permissions = user.GetPermissions(guildChannel);
                
                // Just some requirement for approving messages.
                if (permissions.ManageChannel) {
                    switch (reaction.Emote.Name) {
                        case "✅":
                            await EventBuilder.approve(client, user, message.Id);
                            break;

                        case "❌":
                            await EventBuilder.reject(client, user, message.Id);
                            break;
                    }
                }
            }
        }
        
        private async Task reactionRemove(Cacheable<IUserMessage, ulong> message,
            ISocketMessageChannel channel, SocketReaction reaction) {
            await modifyRole(message.Id, channel, reaction, ModifyRoleOp.Remove);

            if (client.config.eventBuilders.TryGetValue(reaction.UserId, out var builder))
                await builder.removeReaction(message.Id, reaction);
        }

        // Initializes the Message Handler, subscribe to events, etc.
        public async Task init(Client discordClient) {
            client = discordClient;
            
            client.Log += log;
            client.UserLeft += userLeft;
            client.UserJoined += userJoined;
            client.MessageReceived += messageReceived;
            client.UserVoiceStateUpdated += voiceUpdated;
            client.ReactionAdded += reactionAdded;
            client.ReactionRemoved += reactionRemove;
            
            commands = new CommandService();
            services = new ServiceCollection()
                .AddSingleton(client)
                .AddSingleton(commands)
                .BuildServiceProvider();

            await commands.AddModulesAsync(Assembly.GetEntryAssembly(), services);
        }
    }
}
