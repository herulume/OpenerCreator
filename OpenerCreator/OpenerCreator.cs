using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using OpenerCreator.Hooks;

namespace OpenerCreator
{
    public sealed class OpenerCreator : IDalamudPlugin
    {
        public string Name => "OpenerCreator";
        private readonly string command = "/ocrt";
        public Configuration Configuration { get; init; }
        private Gui.OpenerCreatorWindow OpenerCreatorGui { get; init; }
        private OnUsedActionHook OnUsedHook { get; init; }

        [PluginService][RequiredVersion("1.0")] public static DalamudPluginInterface PluginInterface { get; private set; } = null!;
        [PluginService][RequiredVersion("1.0")] public static ICommandManager CommandManager { get; private set; } = null!;
        [PluginService][RequiredVersion("1.0")] public static IChatGui ChatGui { get; private set; } = null!;
        [PluginService][RequiredVersion("1.0")] public static IGameInteropProvider GameInteropProvider { get; private set; } = null!;
        [PluginService][RequiredVersion("1.0")] public static IDataManager DataManager { get; private set; } = null!;
        [PluginService][RequiredVersion("1.0")] public static IClientState ClientState { get; private set; } = null!;
        [PluginService][RequiredVersion("1.0")] public static IGameGui GameUI { get; private set; } = null!;
        [PluginService][RequiredVersion("1.0")] public static IPluginLog PluginLog { get; private set; } = null!;

        public OpenerCreator()
        {
            this.OnUsedHook = new OnUsedActionHook(new CountdownChatHook());

            this.Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            this.Configuration.Initialize(PluginInterface);

            OpenerCreatorGui = new Gui.OpenerCreatorWindow(this.OnUsedHook.Toggle);

            PluginInterface.UiBuilder.Draw += OpenerCreatorGui.Draw;
            PluginInterface.UiBuilder.OpenConfigUi += () => OpenerCreatorGui.Enabled = true;

            CommandManager.AddHandler(command, new CommandInfo((string _, string _) => OpenerCreatorGui.Enabled = true)
            {
                HelpMessage = "Create, save, and practice your openers."
            });
        }

        public void Dispose()
        {
            CommandManager.RemoveHandler(command);
            this.OnUsedHook.Dispose();
            OpenerCreatorGui.Dispose();
        }
    }
}
