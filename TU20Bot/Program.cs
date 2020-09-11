﻿using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Discord;
using Org.BouncyCastle.Math.EC.Rfc7748;
using TU20Bot.Configuration;

namespace TU20Bot {
    internal class Program {
        private readonly string token;

        private Client client;
        private Config config;
        private Server server;
        private Handler handler;
        private CSVReader csvReader;

        // Initializes Discord.Net
        private async Task start() {
            config = new Config();

            client = new Client(config);
            server = new Server(client);
            handler = new Handler(client);

            csvReader = new CSVReader(config);

            await handler.init();

            await client.LoginAsync(TokenType.Bot, token);
            await client.StartAsync();

            // Run server on another thread.
            new Thread(() => server.RunAsync().GetAwaiter().GetResult()).Start();

            // Run email checker on another thread every 15 min
            new Thread(new ThreadStart(async () => {

                do {
                    // Reading and assigning data from csv file every 15 min
                    config.userDataCsv = csvReader.readFile();

                    await new EmailChecker(config, client).emailCheck(config.userDataCsv);
                    await Task.Delay(1800000);

                } while (true);
            })).Start();

            await Task.Delay(-1);
        }

        private Program(string token) {
            this.token = token;
        }

        // Entry
        public static void Main(string[] args) {
            // Init command with token.
            if (args.Length >= 2 && args[0] == "init") {
                File.WriteAllText("token.txt", args[1]);
            }

            // Start bot with token from "token.txt" in working folder.
            try {
                var token = File.ReadAllText("token.txt").Trim();
                new Program(token).start().GetAwaiter().GetResult();
            } catch (IOException) {
                Console.WriteLine("Could not read from token.txt. Did you run `init <token>`?");
            }
        }
    }
}
