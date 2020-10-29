using System;
using System.IO;
using System.Timers;
using System.Xml.Serialization;
using System.Collections.Generic;

using TU20Bot.Configuration.Payloads;

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

    public class FactoryDescription {
        public ulong id;
        public string name;
        public int maxChannels;
        public readonly List<ulong> channels = new List<ulong>();
        
        [XmlIgnore]
        public Timer timer;
    }
    
    public class UserDetails {
        public string firstName;
        public string lastName;
        public string email;

        public string fullName => firstName + " " + lastName;
        public string fullNameNoSpace => firstName + lastName;
    }

    public class UserMatch {
        public List<UserDetails> details = new List<UserDetails>();
        public ulong role;
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

        public readonly List<UserMatchPayload> matches = new List<UserMatchPayload>();
        public readonly List<LogEntry> logs = new List<LogEntry>();
        public readonly List<FactoryDescription> factories = new List<FactoryDescription>();

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
