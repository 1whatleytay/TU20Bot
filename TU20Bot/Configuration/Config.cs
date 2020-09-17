using System;
using System.Collections.Generic;

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

        public readonly ulong speakerRoleID = 753311645585113258;

        public readonly ulong attendeeRoleID = 753311315623411764;

        public List<CSVData> userDataCsv;

        public List<LogEntry> logs = new List<LogEntry>();
    }
}
