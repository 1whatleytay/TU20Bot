﻿using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Discord;
using TU20Bot.Configuration;

namespace TU20Bot {
    internal class Program {
        private readonly string token;

        private Client client;
        private Config config;
        private Server server;
        private Handler handler;

        // Initializes Discord.Net
        private async Task start(string[] args) {
            // Convenience...
            if (args.FirstOrDefault() == "purge")
                File.Delete(Config.defaultPath);
            
            config = Config.load(Config.defaultPath) ?? Config.configure(args);
            
            // Start database...
            if (config.mongoUrl != null) {
                mongo = new MongoClient(config.mongoUrl);
                database = mongo.GetDatabase(config.databaseName);
            } else {
                Console.WriteLine("Skipping database initialization, no URL provided...");
            }

            client = new Client(config, database);
            server = new Server(client);
            handler = new Handler(client);

            await handler.init();

            await client.LoginAsync(TokenType.Bot, config.token);
            await client.StartAsync();

            // Run server on another thread.
            new Thread(() => server.RunAsync().GetAwaiter().GetResult()).Start();

            await Task.Delay(-1);
        }

        // Program entry.
        public static void Main(string[] args) {
            // Pass control to async method.
            new Program().start(args).GetAwaiter().GetResult();
        }
    }
}
