using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using OpenerCreator.Hooks;

namespace OpenerCreator
{
    public sealed class OpenerCreator : IDalamudPlugin
    {
        public string Name => "Opener Creator";
        private readonly string command = "/ocrt";
        public Configuration Configuration { get; init; }
        private Gui.OpenerCreator OpenerCreatorGui { get; init; }

        private OnUsedActionHook Hook { get; init; }

        [PluginService][RequiredVersion("1.0")] public static DalamudPluginInterface PluginInterface { get; private set; } = null!;
        [PluginService][RequiredVersion("1.0")] public static ICommandManager CommandManager { get; private set; } = null!;
        [PluginService][RequiredVersion("1.0")] public static IChatGui ChatGui { get; private set; } = null!;
        [PluginService][RequiredVersion("1.0")] public static IGameInteropProvider GameInteropProvider { get; private set; } = null!;
        [PluginService][RequiredVersion("1.0")] public static IDataManager DataManager { get; private set; } = null!;
        [PluginService][RequiredVersion("1.0")] public static IClientState ClientState { get; private set; } = null!;
        [PluginService][RequiredVersion("1.0")] public static IPluginLog PluginLog { get; private set; } = null!;

        public OpenerCreator()
        {
            this.Hook = new OnUsedActionHook();

            this.Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            this.Configuration.Initialize(PluginInterface);

            OpenerCreatorGui = new Gui.OpenerCreator();

            PluginInterface.UiBuilder.Draw += Draw;


            CommandManager.AddHandler(command, new CommandInfo(CommandParser)
            {
                HelpMessage = @" create|run
'create' to create your opener.
'run' to record your actions and run them against your opener, if defined."
            });
        }

        public void Dispose()
        {

            CommandManager.RemoveHandler(command);
            this.Hook.Dispose();
            OpenerCreatorGui.Dispose();
        }

        private void Draw()
        {
            OpenerCreatorGui.Draw();
        }

        private void CommandParser(string command, string args)
        {
            var sargs = args.ToLower().Split(null);
            if (sargs.Length != 1) { return; }

            switch (sargs[0])
            {
                case "create":
                    OnCreateCommand();
                    break;
                case "run":
                    OnRunCommand();
                    break;
                default:
                    break;
            }
        }

        private void OnRunCommand()
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

        private void OnCreateCommand()
        {
            OpenerCreatorGui.Enabled = true;
        }
    }
}
