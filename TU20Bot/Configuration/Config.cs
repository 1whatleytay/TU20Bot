using System;
using System.IO;
using System.Xml.Serialization;
using System.Collections.Generic;

using System.Timers;

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

    // Factories -> Special voice channels that are configurable to set up new channels.
    public class FactoryDescription {
        public ulong id;
        public string name;
        public int maxChannels;
        public readonly List<ulong> channels = new List<ulong>();
        
        [XmlIgnore]
        public Timer timer;
    }

    public class EmoteRolePair {
        public string emote;
        public ulong roleId;
    }

    // Reactors -> Special messages that can assign you roles when you react to them.
    public class ReactorDescription {
        public ulong id;
        public List<EmoteRolePair> pairs = new List<EmoteRolePair>();
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
        public readonly List<ReactorDescription> reactors = new List<ReactorDescription>();
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