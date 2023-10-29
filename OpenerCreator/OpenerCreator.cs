using System;
using System.Linq;
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
        private Gui.OpenerLoaderWindow OpenerLoaderGui { get; init; }
        private OnUsedActionHook OnUsedHook { get; init; }
        private CountdownChatHook CdHook { get; init; }

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
            this.OnUsedHook = new OnUsedActionHook();
            this.CdHook = new CountdownChatHook();

            this.Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            this.Configuration.Initialize(PluginInterface);

            OpenerCreatorGui = new Gui.OpenerCreatorWindow();
            OpenerLoaderGui = new Gui.OpenerLoaderWindow();

            PluginInterface.UiBuilder.Draw += Draw;


            CommandManager.AddHandler(command, new CommandInfo(CommandParser)
            {
                HelpMessage = @" create|load|run
'create' to create your opener.
'load' to load saved openers.
'run' to record your actions and run them against your opener, if defined."
            });
        }

        public void Dispose()
        {

            CommandManager.RemoveHandler(command);
            this.OnUsedHook.Dispose();
            OpenerCreatorGui.Dispose();
        }

        private void Draw()
        {
            OpenerCreatorGui.Draw();
            OpenerLoaderGui.Draw();
        }

        private void CommandParser(string command, string args)
        {
            var sargs = args.ToLower().Split(null);
            if (sargs.Length < 1 || sargs.Length > 2) { return; }

            var cd = 7;
            if (sargs.Length == 2 && int.TryParse(sargs[1], out cd) && !Enumerable.Range(5, 30).Contains(cd)) { /* ugly */}

            switch (sargs[0])
            {
                case "create":
                    OnCreateCommand();
                    break;
                case "load":
                    OnLoadCommand();
                    break;
                case "run":
                    OnRunCommand(cd);
                    break;
                default:
                    break;
            }
        }

        private void OnRunCommand(int cd)
        {
            if (this.OnUsedHook.IsActive())
                this.OnUsedHook.Disable();
            else
            {
                this.CdHook.StartCountdown(cd);
                this.OnUsedHook.Enable();
            }
        }

        private void OnCreateCommand()
        {
            OpenerCreatorGui.Enabled = true;
        }

        private void OnLoadCommand()
        {
            OpenerLoaderGui.Enabled = true;
        }
    }
}
