using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using OpenerCreator.Hooks;
using OpenerCreator.Windows;

namespace OpenerCreator;

public sealed class OpenerCreator : IDalamudPlugin
{
    private const string Command = "/ocrt";

    public readonly WindowSystem WindowSystem = new("OpenerCreator");

    public OpenerCreator()
    {
        Config = Configuration.Load();

        UsedActionHook = new UsedActionHook();

        ConfigWindow = new ConfigWindow();
        OpenerCreatorWindow = new OpenerCreatorWindow(UsedActionHook.StartRecording, UsedActionHook.StopRecording);
        WindowSystem.AddWindow(ConfigWindow);
        WindowSystem.AddWindow(OpenerCreatorWindow);

        PluginInterface.UiBuilder.Draw += WindowSystem.Draw;
        PluginInterface.UiBuilder.OpenConfigUi += () => ConfigWindow.Toggle();
        PluginInterface.UiBuilder.OpenMainUi += () => OpenerCreatorWindow.Toggle();

        CommandManager.AddHandler(Command, new CommandInfo(OnCommand)
        {
            HelpMessage = "Create, save, and practice your openers."
        });
    }


    private ConfigWindow ConfigWindow { get; init; }
    private OpenerCreatorWindow OpenerCreatorWindow { get; init; }
    private UsedActionHook UsedActionHook { get; init; }
    public static Configuration Config { get; set; } = null!;

    [PluginService]
    public static IDalamudPluginInterface PluginInterface { get; private set; } = null!;

    [PluginService]
    public static ITextureProvider TextureProvider { get; private set; } = null!;

    [PluginService]
    public static ICommandManager CommandManager { get; private set; } = null!;

    [PluginService]
    public static IGameInteropProvider GameInteropProvider { get; private set; } = null!;

    [PluginService]
    public static IDataManager DataManager { get; private set; } = null!;

    [PluginService]
    public static IClientState ClientState { get; private set; } = null!;

    [PluginService]
    public static IPluginLog PluginLog { get; private set; } = null!;

    public void Dispose()
    {
        CommandManager.RemoveHandler(Command);
        UsedActionHook.Dispose();
        WindowSystem.RemoveAllWindows();
        ConfigWindow.Dispose();
        OpenerCreatorWindow.Dispose();
    }

    private void OnCommand(string command, string args)
    {
        if (args == "config")
            ConfigWindow.Toggle();
        else
            OpenerCreatorWindow.Toggle();
    }
}
