using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Dalamud.Hooking;
using Lumina.Excel;
using OpenerCreator.Actions;
using OpenerCreator.Helpers;
using OpenerCreator.Managers;
using LuminaAction = Lumina.Excel.GeneratedSheets.Action;


namespace OpenerCreator.Hooks;

public class UsedActionHook : IDisposable
{
    private const int MaxItemCount = 50;

    private readonly ExcelSheet<LuminaAction>? sheet = OpenerCreator.DataManager.GetExcelSheet<LuminaAction>();
    private readonly List<int> used = new(MaxItemCount);
    private readonly Hook<UsedActionDelegate>? usedActionHook;
    private Action<int> currentIndex = _ => { };
    private bool ignoreTrueNorth;

    private int nActions;
    private Action<Feedback> provideFeedback = _ => { };
    private Action<int> updateAbilityAnts = _ => { };
    private Action<int> wrongAction = _ => { };


    public UsedActionHook()
    {
        // credits to Tischel for the original sig
        // https://github.com/Tischel/ActionTimeline/blob/master/ActionTimeline/Helpers/TimelineManager.cs
        usedActionHook = OpenerCreator.GameInteropProvider.HookFromSignature<UsedActionDelegate>(
            "40 55 56 57 41 54 41 55 41 56 48 8D AC 24",
            DetourUsedAction
        );
    }

    public void Dispose()
    {
        usedActionHook?.Disable();
        usedActionHook?.Dispose();
        GC.SuppressFinalize(this);
    }

    public void StartRecording(
        int cd, Action<Feedback> provideFeedbackA, Action<int> wrongActionA, Action<int> currentIndexA, bool ignoreTn,
        Action<int> updateAbilityAntsA)
    {
        if (usedActionHook?.IsEnabled ?? true)
            return;

        provideFeedback = provideFeedbackA;
        wrongAction = wrongActionA;
        currentIndex = currentIndexA;
        usedActionHook?.Enable();
        nActions = OpenerManager.Instance.Loaded.Count;
        ignoreTrueNorth = ignoreTn;
        updateAbilityAnts = updateAbilityAntsA;
    }

    public void StopRecording()
    {
        if (!(usedActionHook?.IsEnabled ?? false))
            return;

        usedActionHook?.Disable();
        nActions = 0;
        used.Clear();
    }

    private void Compare()
    {
        if (!(usedActionHook?.IsEnabled ?? false))
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

        var actionId = Marshal.ReadInt32(effectHeader, 0x8);
        var action = sheet!.GetRow((uint)actionId);
        var isActionTrueNorth = actionId == PvEActions.TrueNorthId;
        var analyseWhenTrueNorth = !(ignoreTrueNorth && isActionTrueNorth); //nand
        if (action != null && PvEActions.IsPvEAction(action) && analyseWhenTrueNorth)
        {
            if (nActions == 0) // Opener not defined or fully processed
            {
                StopRecording();
                return;
            }

            // Leave early
            var loadedLength = OpenerManager.Instance.Loaded.Count;
            var index = loadedLength - nActions;
            var intendedAction = OpenerManager.Instance.Loaded[index];
            if (index + 1 < OpenerManager.Instance.Loaded.Count)
                updateAbilityAnts(OpenerManager.Instance.Loaded[index + 1]);
            var intendedName = PvEActions.Instance.GetActionName(intendedAction);

            currentIndex(index);

            if (OpenerCreator.Config.StopAtFirstMistake &&
                !OpenerManager.Instance.AreActionsEqual(intendedAction, intendedName, actionId)
               )
            {
                wrongAction(index);
                var f = new Feedback();
                f.AddMessage(
                    Feedback.MessageType.Error,
                    $"Difference in action {index + 1}: Substituted {intendedName} for {PvEActions.Instance.GetActionName(actionId)}"
                );
                provideFeedback(f);
                StopRecording();
                return;
            }

            // Process the opener
            used.Add(actionId);
            nActions--;
            if (nActions <= 0) Compare();
        }
    }

    private delegate void UsedActionDelegate(
        uint sourceId, IntPtr sourceCharacter, IntPtr pos, IntPtr effectHeader, IntPtr effectArray, IntPtr effectTrail);
}
