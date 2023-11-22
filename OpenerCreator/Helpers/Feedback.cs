using System.Collections.Generic;
using System.Linq;

namespace OpenerCreator.Helpers
{
    public class Feedback
    {
        public enum MessageType
        {
            Success,
            Info,
            Error,
        }

        private readonly List<(MessageType type, string message)> messages = new();

        public void AddMessage(MessageType type, string message) => messages.Add((type, message));

        public List<string> GetMessages() => messages.Select(ToMessage).ToList();

        public static string ToMessage((MessageType, string) m) => $"{m.Item1}: {m.Item2}";
    }
}
