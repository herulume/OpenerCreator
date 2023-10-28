using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Dalamud.Game.Text;
using Dalamud.Hooking;
using Lumina.Excel;
using LuminaAction = Lumina.Excel.GeneratedSheets.Action;

namespace SamplePlugin.Hooks
{
    public unsafe class OnUsedActionHook : IDisposable
    {
        private delegate void UsedActionDelegate(uint sourceId, IntPtr sourceCharacter, IntPtr pos, IntPtr effectHeader, IntPtr effectArray, IntPtr effectTrail);
        private readonly Hook<UsedActionDelegate>? usedActionHook;

        private bool isActive;

        private readonly ExcelSheet<LuminaAction>? sheet;

        private static readonly int MaxItemCount = 50;
        private readonly List<string> items = new(MaxItemCount);

        public OnUsedActionHook()

        {
            sheet = Plugin.DataManager.GetExcelSheet<LuminaAction>();

            // credits to Tischel for the sig
            // https://github.com/Tischel/ActionTimeline/blob/master/ActionTimeline/Helpers/TimelineManager.cs#L87
            this.usedActionHook = Plugin.GameInteropProvider.HookFromSignature<UsedActionDelegate>(
                "40 55 53 57 41 54 41 55 41 56 41 57 48 8D AC 24 ?? ?? ?? ?? 48 81 EC ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 45 70",
                this.DetourUseAction
                );
            this.isActive = false;
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
        }

        public void Disable()
        {
            this.usedActionHook?.Disable();
            this.isActive = false;

            Plugin.ChatGui.Print(new XivChatEntry
            {
                Message = $"Actions = {string.Join(", ", items)}",
                Type = XivChatType.Echo
            });

            items.Clear();
        }

        public bool IsActive() => this.isActive;

        private void DetourUseAction(uint sourceId, IntPtr sourceCharacter, IntPtr pos, IntPtr effectHeader, IntPtr effectArray, IntPtr effectTrail)
        {
            this.usedActionHook?.Original(sourceId, sourceCharacter, pos, effectHeader, effectArray, effectTrail);

            var player = Plugin.ClientState.LocalPlayer;
            if (player == null || sourceId != player.ObjectId) { return; }

            var actionId = Marshal.ReadInt32(effectHeader, 0x8);
            var action = sheet?.GetRow((uint)actionId);
            if (action != null && action.IsPlayerAction)
            {
                var name = action.Name.ToString();
                items.Add(name);
            }
        }
    }
}
