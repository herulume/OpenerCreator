using System;
using System.Collections.Generic;
using OpenerCreator.Helpers;

namespace OpenerCreator.Windows;

internal class Recording(Action<int, Action<Feedback>, Action<int>> startRecording, Action stopRecording)
{
    private readonly List<string> feedback = [];
    private bool recording;

    internal void StopRecording()
    {
        recording = false;
        stopRecording();
    }

    internal void StartRecording(int countdownTime, Action<Feedback> addFeedback, Action<int> indexWrongAction)
    {
        feedback.Clear();
        recording = true;
        startRecording(countdownTime, addFeedback, indexWrongAction);
    }

    internal bool IsRecording()
    {
        return recording;
    }

    internal void ClearFeedback()
    {
        feedback.Clear();
    }

    internal void AddFeedback(IEnumerable<string> f)
    {
        feedback.AddRange(f);
    }

    internal IEnumerable<string> GetFeedback()
    {
        return [..feedback];
    }
}
