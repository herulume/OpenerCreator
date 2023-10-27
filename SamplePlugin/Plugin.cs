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

        private DalamudPluginInterface PluginInterface { get; init; }
        private ICommandManager CommandManager { get; init; }
        public Configuration Configuration { get; init; }

        private OnActionHook Hook { get; init; }

        public Plugin(
            [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
            [RequiredVersion("1.0")] ICommandManager commandManager,
            [RequiredVersion("1.0")] IChatGui chatGui,
            [RequiredVersion("1.0")] IGameInteropProvider gameInteropProvider,
            [RequiredVersion("1.0")] IDataManager dataManager,
            [RequiredVersion("1.0")] IClientState clientState)
        {
            this.PluginInterface = pluginInterface;
            this.CommandManager = commandManager;

            this.Hook = new OnActionHook(gameInteropProvider, chatGui, dataManager, clientState, new Helpers.ActionDictionary(dataManager));

            this.Configuration = this.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            this.Configuration.Initialize(this.PluginInterface);



            this.CommandManager.AddHandler(HookCommand, new CommandInfo(OnCommand)
            {
                HelpMessage = "A useful message to display in /xlhelp"
            });
        }

        public void Dispose()
        {
            this.CommandManager.RemoveHandler(HookCommand);
            this.Hook.Dispose();
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
