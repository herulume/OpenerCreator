using System;
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
        private readonly Tuple<string, string>[] commands =
           {
                Tuple.Create("/ocrt create", "Create your opener."),
                Tuple.Create("/ocrt run", "Toggle to record your actions and run them against your opener, if defined."),
            };

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
        [PluginService][RequiredVersion("1.0")] public static IPluginLog PluginLog { get; private set; } = null!;

        public OpenerCreator()
        {
            this.Hook = new OnUsedActionHook();

            this.Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            this.Configuration.Initialize(PluginInterface);

            OpenerCreatorGui = new Gui.OpenerCreator();

            PluginInterface.UiBuilder.Draw += Draw;

            CommandManager.AddHandler(commands[0].Item1, new CommandInfo(OnCreateCommand)
            {
                HelpMessage = commands[0].Item2
            });

            CommandManager.AddHandler(commands[1].Item1, new CommandInfo(OnRunCommand)
            {
                HelpMessage = commands[1].Item2
            });
        }

        public void Dispose()
        {
            for (var i = 0; i < this.commands.Length; i++)
            {
                CommandManager.RemoveHandler(commands[i].Item1);
            }
            this.Hook.Dispose();
            OpenerCreatorGui.Dispose();
        }

        private void Draw()
        {
            OpenerCreatorGui.Draw();
        }

        private void OnRunCommand(string command, string args)
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

        private void OnCreateCommand(string command, string args)
        {
            OpenerCreatorGui.Enabled = true;
        }
    }
}
