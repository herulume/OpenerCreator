using System;
using System.Collections.Generic;
using System.Linq;
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
        private readonly List<Tuple<string, uint>> items = new(MaxItemCount);

        private CountdownChatHook CdHook { get; init; }

        public OnUsedActionHook(CountdownChatHook cdHook)
        {
            sheet = OpenerCreator.DataManager.GetExcelSheet<LuminaAction>();

            // credits to Tischel for the sig
            // https://github.com/Tischel/ActionTimeline/blob/master/ActionTimeline/Helpers/TimelineManager.cs
            this.usedActionHook = OpenerCreator.GameInteropProvider.HookFromSignature<UsedActionDelegate>(
                "40 55 53 57 41 54 41 55 41 56 41 57 48 8D AC 24 ?? ?? ?? ?? 48 81 EC ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 45 70",
                this.DetourUsedAction
                );
            this.nactions = 0;
            CdHook = cdHook;
        }

        public void Dispose()
        {
            this.CdHook?.Dispose();
            this.usedActionHook?.Disable();
            this.usedActionHook?.Dispose();
            GC.SuppressFinalize(this);
        }

        public List<string> Toggle(int cd)
        {
            if (this.usedActionHook!.IsEnabled)
                return Disable();
            else
            {
                CdHook.StartCountdown(cd);
                Enable();
                return new List<string>();
            }
        }

        private void Enable()
        {
            this.usedActionHook?.Enable();
            this.nactions = OpenerManager.Instance.Loaded.Count;
            ChatMessages.RecordingActions();
        }

        private List<String> Disable()
        {
            var feedback = new List<String>();
            this.usedActionHook?.Disable();
            this.nactions = 0;

            ChatMessages.ActionsUsed(items.Select(x => x.Item1));

            var opener = OpenerManager.Instance.Loaded;
            if (opener.Count > 0)
            {
                var used = items.Select(x => x.Item2).ToList();
                feedback = OpenerManager.Compare(opener, used);
            }
            else
            {
                feedback.Add(ChatMessages.NoOpener);
            }
            items.Clear();
            return feedback;
        }

        private void DetourUsedAction(uint sourceId, IntPtr sourceCharacter, IntPtr pos, IntPtr effectHeader, IntPtr effectArray, IntPtr effectTrail)
        {
            this.usedActionHook?.Original(sourceId, sourceCharacter, pos, effectHeader, effectArray, effectTrail);


            var player = OpenerCreator.ClientState.LocalPlayer;
            if (player == null || sourceId != player.ObjectId) { return; }

            var actionId = (uint)Marshal.ReadInt32(effectHeader, 0x8);
            var action = sheet!.GetRow(actionId);
            if (action != null && ActionDictionary.IsPvEAction(action))
            {
                items.Add(Tuple.Create(action.Name.ToString(), actionId));
                nactions--;
                if (this.nactions == 0)
                {
                    this.Disable();
                    return;
                }

            }
        }
    }
}
