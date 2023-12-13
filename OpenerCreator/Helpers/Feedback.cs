using System.Collections.Generic;
using System.Linq;

namespace OpenerCreator.Helpers;

public class Feedback
{
    public enum MessageType
    {
        Success,
        Info,
        Error
    }

    private readonly List<(MessageType type, string message)> messages = [];

    public void AddMessage(MessageType type, string message)
    {
        messages.Add((type, message));
    }

    public List<string> GetMessages()
    {
        return messages.Select(ToMessage).ToList();
    }

    public List<(MessageType, string)> GetList()
    {
        return messages;
    }

    public static string ToMessage((MessageType, string) m)
    {
        return $"{m.Item1}: {m.Item2}";
    }
}
