using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using OpenerCreator.Gui;
using OpenerCreator.Hooks;

namespace OpenerCreator
{
    public sealed class OpenerCreator : IDalamudPlugin
    {
        private const string command = "/ocrt";
        public static Configuration Config { get; set; } = null!;
        private OpenerCreatorWindow OpenerCreatorGui { get; init; }
        private UsedActionHook UsedHook { get; init; }

        [PluginService][RequiredVersion("1.0")] public static DalamudPluginInterface PluginInterface { get; private set; } = null!;
        [PluginService][RequiredVersion("1.0")] public static ICommandManager CommandManager { get; private set; } = null!;
        [PluginService][RequiredVersion("1.0")] public static IGameInteropProvider GameInteropProvider { get; private set; } = null!;
        [PluginService][RequiredVersion("1.0")] public static IDataManager DataManager { get; private set; } = null!;
        [PluginService][RequiredVersion("1.0")] public static IClientState ClientState { get; private set; } = null!;
        [PluginService][RequiredVersion("1.0")] public static IPluginLog PluginLog { get; private set; } = null!;

        public OpenerCreator()
        {
            this.UsedHook = new UsedActionHook();

            OpenerCreatorGui = new OpenerCreatorWindow(this.UsedHook.StartRecording, this.UsedHook.StopRecording);

            Config = Configuration.Load();

            PluginInterface.UiBuilder.Draw += OpenerCreatorGui.Draw;
            PluginInterface.UiBuilder.OpenConfigUi += () => OpenerCreatorGui.Enabled = true;

            CommandManager.AddHandler(command, new CommandInfo((_, _) => OpenerCreatorGui.Enabled = true)
            {
                HelpMessage = "Create, save, and practice your openers."
            });
        }

        public void Dispose()
        {
            CommandManager.RemoveHandler(command);
            this.UsedHook.Dispose();
            OpenerCreatorGui.Dispose();
        }
    }
}
