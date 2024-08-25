using System;
using System.Collections.Generic;

namespace OpenerCreator.Helpers;

internal class Recording(
    Action<int, Action<Feedback>, Action<int>, Action<int>, bool, Action<int>> startRecording,
    Action stopRecording,
    Action enableAbilityAnts,
    Action disableAbilityAnts,
    Action<int> updateAbilityAnts
)
{
    private readonly List<string> feedback = [];
    private bool recording;

    internal void StopRecording()
    {
        recording = false;
        disableAbilityAnts();
        stopRecording();
    }

    internal void StartRecording(
        int countdownTime, Action<Feedback> addFeedback, Action<int> indexWrongAction, Action<int> currentIndex,
        bool ignoreTrueNorth)
    {
        feedback.Clear();
        recording = true;
        enableAbilityAnts();
        startRecording(countdownTime, addFeedback, indexWrongAction, currentIndex, ignoreTrueNorth, updateAbilityAnts);
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
