using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using SamplePlugin.Hooks;

namespace SamplePlugin
{
    public sealed class Plugin : IDalamudPlugin
    {
        public string Name => "Sample Plugin";
        private const string HookCommand = "/lea";

        // private DalamudPluginInterface PluginInterface { get; init; }
        // private ICommandManager CommandManager { get; init; }
        public Configuration Configuration { get; init; }
        private Gui.OpenerCreator OpenerCreatorGui { get; init; }

        private OnUsedActionHook Hook { get; init; }

        [PluginService][RequiredVersion("1.0")] public static DalamudPluginInterface PluginInterface { get; private set; } = null!;
        [PluginService][RequiredVersion("1.0")] public static ICommandManager CommandManager { get; private set; } = null!;
        [PluginService][RequiredVersion("1.0")] public static IChatGui ChatGui { get; private set; } = null!;
        [PluginService][RequiredVersion("1.0")] public static IGameInteropProvider GameInteropProvider { get; private set; } = null!;
        [PluginService][RequiredVersion("1.0")] public static IDataManager DataManager { get; private set; } = null!;
        [PluginService][RequiredVersion("1.0")] public static IClientState ClientState { get; private set; } = null!;

        public Plugin()
        {
            this.Hook = new OnUsedActionHook();

            this.Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            this.Configuration.Initialize(PluginInterface);

            OpenerCreatorGui = new Gui.OpenerCreator();

            PluginInterface.UiBuilder.Draw += Draw;

            CommandManager.AddHandler(HookCommand, new CommandInfo(OnCommand)
            {
                HelpMessage = "A useful message to display in /xlhelp"
            });
        }

        public void Dispose()
        {
            CommandManager.RemoveHandler(HookCommand);
            this.Hook.Dispose();
            OpenerCreatorGui.Dispose();
        }

        private void Draw()
        {
            OpenerCreatorGui.Draw();
        }

        private void OnCommand(string command, string args)
        {
            if (this.Hook.IsActive())
            {
                this.Hook.Disable();
            }
            else
            {
                this.Hook.Enable();
            }
        }
    }
}
