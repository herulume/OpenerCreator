using System.Collections.Generic;
using Dalamud.Game.Text;

namespace OpenerCreator.Helpers
{
    internal sealed class ChatMessages
    {
        public static void OpenerSaved() => OpenerCreator.ChatGui.Print(new XivChatEntry
        {
            Message = "Opener saved.",
            Type = XivChatType.Echo
        });

        public static void RecordingActions() => OpenerCreator.ChatGui.Print(new XivChatEntry
        {
            Message = "Recording Actions.",
            Type = XivChatType.Echo
        });

        public static void ActionsUsed(IEnumerable<string> actions) => OpenerCreator.ChatGui.Print(new XivChatEntry
        {
            Message = $"Actions used: {string.Join(" => ", actions)}",
            Type = XivChatType.Echo
        });

        public static void NoOpener() => OpenerCreator.ChatGui.Print(new XivChatEntry
        {
            Message = "No opener to compare to.",
            Type = XivChatType.Echo
        });

        public static void ActionDiff(int i, string? intended, string? actual) => OpenerCreator.ChatGui.Print(new XivChatEntry
        {
            Message = $"Difference in action {i + 1}: Substituted {intended} for {actual}",
            Type = XivChatType.Echo
        });

        public static void SuccessExec() => OpenerCreator.ChatGui.Print(new XivChatEntry
        {
            Message = "Great job! Opener executed perfectly.",
            Type = XivChatType.Echo
        });

        public static void OpenerShift(int shift) => OpenerCreator.ChatGui.Print(new XivChatEntry
        {
            Message = $"You shifted your opener by {shift} actions.",
            Type = XivChatType.Echo
        });
    }
}
