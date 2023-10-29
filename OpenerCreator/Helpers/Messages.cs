namespace OpenerCreator.Helpers
{
    internal sealed class Messages
    {

        public static string NoOpener = "No opener to compare to.";

        public static string ActionDiff(int i, string? intended, string? actual) => $"Difference in action {i + 1}: Substituted {intended} for {actual}";

        public static string SuccessExec() => "Great job! Opener executed perfectly.";

        public static string OpenerShift(int shift) => $"You shifted your opener by {shift} actions.";
    }
}
