using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Dalamud.Hooking;
using Lumina.Excel;
using OpenerCreator.Helpers;
using OpenerCreator.Managers;
using LuminaAction = Lumina.Excel.GeneratedSheets.Action;


namespace OpenerCreator.Hooks;

public class UsedActionHook : IDisposable
{
    private static readonly int MaxItemCount = 50;

    private readonly ExcelSheet<LuminaAction>? sheet;
    private readonly List<uint> used = new(MaxItemCount);
    private readonly Hook<UsedActionDelegate>? usedActionHook;

    private int nActions;
    private Action<Feedback> provideFeedback;
    private Action<int> wrongAction;

    public UsedActionHook()
    {
        sheet = OpenerCreator.DataManager.GetExcelSheet<LuminaAction>();

        // credits to Tischel for the original sig
        // https://github.com/Tischel/ActionTimeline/blob/master/ActionTimeline/Helpers/TimelineManager.cs
        usedActionHook = OpenerCreator.GameInteropProvider.HookFromSignature<UsedActionDelegate>(
            "40 55 56 57 41 54 41 55 41 56 48 8D AC 24",
            DetourUsedAction
        );
        nActions = 0;
        provideFeedback = _ => { };
        wrongAction = _ => { };
    }

    public void Dispose()
    {
        usedActionHook?.Disable();
        usedActionHook?.Dispose();
        GC.SuppressFinalize(this);
    }

    public void StartRecording(int cd, Action<Feedback> provideFeedbackAction, Action<int> wrongActionAction)
    {
        if (usedActionHook!.IsEnabled)
            return;

        provideFeedback = provideFeedbackAction;
        wrongAction = wrongActionAction;
        usedActionHook?.Enable();
        nActions = OpenerManager.Instance.Loaded.Count;
    }

    public void StopRecording()
    {
        if (!usedActionHook!.IsEnabled)
            return;

        usedActionHook?.Disable();
        nActions = 0;
        used.Clear();

        var feedback = new Feedback();
        feedback.AddMessage(Feedback.MessageType.Info, "No opener defined.");
        provideFeedback(feedback);
    }

    private void Compare()
    {
        if (!usedActionHook!.IsEnabled)
            return;

        usedActionHook?.Disable();
        nActions = 0;
        OpenerManager.Instance.Compare(used, provideFeedback, wrongAction);
        used.Clear();
    }

    private void DetourUsedAction(
        uint sourceId, IntPtr sourceCharacter, IntPtr pos, IntPtr effectHeader, IntPtr effectArray, IntPtr effectTrail)
    {
        usedActionHook?.Original(sourceId, sourceCharacter, pos, effectHeader, effectArray, effectTrail);

        var player = OpenerCreator.ClientState.LocalPlayer;
        if (player == null || sourceId != player.EntityId) return;

        var actionId = (uint)Marshal.ReadInt32(effectHeader, 0x8);
        var action = sheet!.GetRow(actionId);
        if (action != null && Actions.IsPvEAction(action))
        {
            if (nActions == 0) // opener not defined
            {
                StopRecording();
                return;
            }

            used.Add(actionId);
            nActions--;
            if (nActions <= 0) Compare();
        }
    }

    private delegate void UsedActionDelegate(
        uint sourceId, IntPtr sourceCharacter, IntPtr pos, IntPtr effectHeader, IntPtr effectArray, IntPtr effectTrail);
}
