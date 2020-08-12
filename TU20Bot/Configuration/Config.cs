using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace TU20Bot.Configuration {
    public enum LogEvent {
        UserJoin,
        UserLeave,
    }
    
    public class LogEntry {
        public LogEvent logEvent;
        public ulong id;
        public string name;
        public ushort discriminator;
        public DateTime time;
    }

    public class Config {
        public const string defaultPath = "config.xml";
        
        public ulong guildId = 230737273350520834; // TU20
        
        public ulong welcomeChannelId = 736741911150198835; // #bot-testing
        
        public List<string> welcomeMessages = new List<string> {
            "Hello there!",
            "Whats poppin",
            "Wagwan",
            "Hi",
            "AHOY",
            "Welcome",
            "Greetings",
            "Howdy"
        };

        public readonly List<LogEntry> logs = new List<LogEntry>();

        public static void save(string path, Config config) {
            var serializer = new XmlSerializer(typeof(Config));
            var stream = new FileStream(path, FileMode.Create);
            serializer.Serialize(stream, config);
        }

        public static Config load(string path) {
            if (!File.Exists(path))
                return null;
            var serializer = new XmlSerializer(typeof(Config));
            var stream = new FileStream(path, FileMode.Open);
            return (Config)serializer.Deserialize(stream);
        }
    }
}