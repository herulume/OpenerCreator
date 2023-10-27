using System;
using Dalamud.Game.Text;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;

namespace SamplePlugin.Hooks
{
    public unsafe class OnActionHook : IDisposable
    {
        private delegate bool UseActionDelegate(nint actionManagerPtr, ActionType actionType, uint actionID, long targetID, uint a4, uint a5, uint a6, void* a7);

        private readonly Hook<UseActionDelegate>? useActionHook;

        private IChatGui ChatGui { get; init; }

        public OnActionHook(
            IGameInteropProvider gameInterop,
            IChatGui chatGui)

        {
            this.ChatGui = chatGui;

            var useActionFunctionPtr = (nint)ActionManager.Addresses.UseAction.Value;
            this.useActionHook = gameInterop.HookFromAddress<UseActionDelegate>(useActionFunctionPtr, this.DetourUseAction);
        }

        public void Dispose()
        {
            this.useActionHook?.Dispose();
            GC.SuppressFinalize(this);
        }

        public void Enable() => this.useActionHook?.Enable();

        public void Disable() => this.useActionHook?.Disable();
    
        private bool DetourUseAction(nint actionManagerPtr, ActionType actionType, uint actionID, long targetID, uint a4, uint a5, uint a6, void* a7)
        {
            if (IsCombatType(actionType))
            {
                this.ChatGui.Print(new XivChatEntry
                {
                    Message = $"Action = {actionID}",
                    Type = XivChatType.Echo
                });
            }
            if (useActionHook != null)
            {
                return this.useActionHook.Original(actionManagerPtr, actionType, actionID, targetID, a4, a5, a6, a7);
            }
            return false;
        }

        // What's ActionType.Ability for?
        private static bool IsCombatType(ActionType actionType) => actionType is ActionType.Action;
    }
}
