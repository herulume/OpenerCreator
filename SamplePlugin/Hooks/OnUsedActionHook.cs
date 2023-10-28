using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Dalamud.Game.Text;
using Dalamud.Hooking;
using Lumina.Excel;
using SamplePlugin.Helpers;
using SamplePlugin.Managers;
using LuminaAction = Lumina.Excel.GeneratedSheets.Action;


namespace SamplePlugin.Hooks
{
    public unsafe class OnUsedActionHook : IDisposable
    {
        private delegate void UsedActionDelegate(uint sourceId, IntPtr sourceCharacter, IntPtr pos, IntPtr effectHeader, IntPtr effectArray, IntPtr effectTrail);
        private readonly Hook<UsedActionDelegate>? usedActionHook;

        private readonly ExcelSheet<LuminaAction>? sheet;

        private bool isActive;
        private int nactions;

        private static readonly int MaxItemCount = 50;
        private readonly List<Tuple<string, uint>> items = new(MaxItemCount);

        public OnUsedActionHook()
        {
            sheet = Plugin.DataManager.GetExcelSheet<LuminaAction>();

            // credits to Tischel for the sig
            // https://github.com/Tischel/ActionTimeline/blob/master/ActionTimeline/Helpers/TimelineManager.cs#L87
            this.usedActionHook = Plugin.GameInteropProvider.HookFromSignature<UsedActionDelegate>(
                "40 55 53 57 41 54 41 55 41 56 41 57 48 8D AC 24 ?? ?? ?? ?? 48 81 EC ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 45 70",
                this.DetourUsedAction
                );
            this.isActive = false;
            this.nactions = 0;
        }

        public void Dispose()
        {
            this.usedActionHook?.Dispose();
            GC.SuppressFinalize(this);
        }

        public void Enable()
        {
            this.usedActionHook?.Enable();
            this.isActive = true;
            this.nactions = OpenerManager.Instance.GetOpener("live").Count;
        }

        public void Disable()
        {
            this.usedActionHook?.Disable();
            this.isActive = false;
            this.nactions = 0;


            var opener = OpenerManager.Instance.GetOpener("live");
            if (opener.Count > 0)
            {
                Plugin.ChatGui.Print(new XivChatEntry
                {
                    Message = $"Actions used = {string.Join(", ", items.Select(x => x.Item1))}",
                    Type = XivChatType.Echo
                });
                var used = items.Select(x => x.Item2).ToList();
                OpenerManager.Compare(opener, used);
            }
            else
            {
                Plugin.ChatGui.Print(new XivChatEntry
                {
                    Message = "No opener to compare to.",
                    Type = XivChatType.Echo
                });
                Plugin.ChatGui.Print(new XivChatEntry
                {
                    Message = $"Actions used = {string.Join(" => ", items.Select(x => x.Item1).ToList())}",
                    Type = XivChatType.Echo
                });
            }
            items.Clear();
        }

        public bool IsActive() => this.isActive;

        private void DetourUsedAction(uint sourceId, IntPtr sourceCharacter, IntPtr pos, IntPtr effectHeader, IntPtr effectArray, IntPtr effectTrail)
        {
            this.usedActionHook?.Original(sourceId, sourceCharacter, pos, effectHeader, effectArray, effectTrail);


            var player = Plugin.ClientState.LocalPlayer;
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
