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

        public static string NoOpener = "No opener to compare to.";

        public static string ActionDiff(int i, string? intended, string? actual) => $"Difference in action {i + 1}: Substituted {intended} for {actual}";

        public static string SuccessExec() => "Great job! Opener executed perfectly.";

        public static string OpenerShift(int shift) => $"You shifted your opener by {shift} actions.";
    }
}
