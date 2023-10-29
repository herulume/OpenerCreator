using System;
using System.Runtime.InteropServices;
using System.Text;
using Dalamud.Hooking;

namespace OpenerCreator.Hooks;

// Credits to Anna https://git.anna.lgbt/anna/XivCommon/src/branch/main/XivCommon/Functions/Chat.cs

public class CountdownChatHook : IDisposable
{
    private delegate void ProcessChatBoxDelegate(nint uiModule, nint message, nint unused, byte a4);
    private readonly Hook<ProcessChatBoxDelegate>? processChatBoxHook;
    public CountdownChatHook()
    {
        processChatBoxHook = OpenerCreator.GameInteropProvider.HookFromSignature<ProcessChatBoxDelegate>(
                "48 89 5C 24 ?? 57 48 83 EC 20 48 8B FA 48 8B D9 45 84 C9",
                processChatBox
                );

        processChatBoxHook.Enable();
    }

    private void processChatBox(nint uiModule, nint message, nint unused, byte a4) => processChatBoxHook?.Original(uiModule, message, unused, a4);
    public void Dispose()
    {
        processChatBoxHook?.Disable();
        processChatBoxHook?.Dispose();
        GC.SuppressFinalize(this);
    }

    private unsafe void SendMessageUnsafe(byte[] message)
    {

        var uiModule = OpenerCreator.GameUI.GetUIModule();

        using var payload = new ChatPayload(message);
        var mem1 = Marshal.AllocHGlobal(400);
        Marshal.StructureToPtr(payload, mem1, false);

        processChatBox(uiModule, mem1, nint.Zero, 0);

        Marshal.FreeHGlobal(mem1);
    }

    public void StartCountdown(int cd)
    {
        var command = Encoding.ASCII.GetBytes($"/cd {cd}");
        SendMessageUnsafe(command);
    }

    [StructLayout(LayoutKind.Explicit)]
    private readonly struct ChatPayload : IDisposable
    {
        [FieldOffset(0)]
        private readonly nint textPtr;

        [FieldOffset(16)]
        private readonly ulong textLen;

        [FieldOffset(8)]
        private readonly ulong unk1;

        [FieldOffset(24)]
        private readonly ulong unk2;

        internal ChatPayload(byte[] stringBytes)
        {
            textPtr = Marshal.AllocHGlobal(stringBytes.Length + 30);
            Marshal.Copy(stringBytes, 0, textPtr, stringBytes.Length);
            Marshal.WriteByte(textPtr + stringBytes.Length, 0);

            textLen = (ulong)(stringBytes.Length + 1);

            unk1 = 64;
            unk2 = 0;
        }

        public void Dispose()
        {
            Marshal.FreeHGlobal(textPtr);
        }
    }
}
