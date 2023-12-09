using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Dalamud.Hooking;
using Lumina.Excel;
using OpenerCreator.Helpers;
using OpenerCreator.Managers;
using LuminaAction = Lumina.Excel.GeneratedSheets.Action;


namespace OpenerCreator.Hooks
{
    public unsafe class OnUsedActionHook : IDisposable
    {
        private delegate void UsedActionDelegate(uint sourceId, IntPtr sourceCharacter, IntPtr pos, IntPtr effectHeader, IntPtr effectArray, IntPtr effectTrail);
        private readonly Hook<UsedActionDelegate>? usedActionHook;

        private readonly ExcelSheet<LuminaAction>? sheet;

        private int nactions;
        private static readonly int MaxItemCount = 50;
        private readonly List<uint> used = new(MaxItemCount);
        private Action<Feedback> provideFeedback;
        private Action<int> wrongAction;

        public OnUsedActionHook()
        {
            sheet = OpenerCreator.DataManager.GetExcelSheet<LuminaAction>();

            // credits to Tischel for the sig
            // https://github.com/Tischel/ActionTimeline/blob/master/ActionTimeline/Helpers/TimelineManager.cs
            usedActionHook = OpenerCreator.GameInteropProvider.HookFromSignature<UsedActionDelegate>(
                "40 55 53 57 41 54 41 55 41 56 41 57 48 8D AC 24 ?? ?? ?? ?? 48 81 EC ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 45 70",
                DetourUsedAction
                );
            nactions = 0;
            provideFeedback = (_) => { };
            wrongAction = (_) => { };
        }

        public void Dispose()
        {
            usedActionHook?.Disable();
            usedActionHook?.Dispose();
            GC.SuppressFinalize(this);
        }

        public void StartRecording(int cd, Action<Feedback> provideFeedback, Action<int> wrongAction)
        {
            if (usedActionHook!.IsEnabled)
                return;

            this.provideFeedback = provideFeedback;
            this.wrongAction = wrongAction;
            usedActionHook?.Enable();
            nactions = OpenerManager.Instance.Loaded.Count;
        }

        public void StopRecording()
        {
            if (!usedActionHook!.IsEnabled)
                return;

            usedActionHook?.Disable();
            nactions = 0;
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
            nactions = 0;
            OpenerManager.Instance.Compare(used, provideFeedback, wrongAction);
            used.Clear();
        }

        private void DetourUsedAction(uint sourceId, IntPtr sourceCharacter, IntPtr pos, IntPtr effectHeader, IntPtr effectArray, IntPtr effectTrail)
        {
            usedActionHook?.Original(sourceId, sourceCharacter, pos, effectHeader, effectArray, effectTrail);

            var player = OpenerCreator.ClientState.LocalPlayer;
            if (player == null || sourceId != player.ObjectId) { return; }

            var actionId = (uint)Marshal.ReadInt32(effectHeader, 0x8);
            var action = sheet!.GetRow(actionId);
            if (action != null && Actions.IsPvEAction(action))
            {
                if (nactions == 0) // opener not defined
                {
                    StopRecording();
                    return;
                }
                used.Add(actionId);
                nactions--;
                if (nactions <= 0)
                {
                    Compare();
                    return;
                }
            }
        }
    }
}
