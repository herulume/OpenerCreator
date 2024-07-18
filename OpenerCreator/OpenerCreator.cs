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

        OpenerCreatorWindow = new OpenerCreatorWindow(UsedActionHook.StartRecording, UsedActionHook.StopRecording);
        WindowSystem.AddWindow(OpenerCreatorWindow);

        PluginInterface.UiBuilder.Draw += WindowSystem.Draw;
        PluginInterface.UiBuilder.OpenConfigUi += () => OpenerCreatorWindow.Toggle();
        PluginInterface.UiBuilder.OpenMainUi += () => OpenerCreatorWindow.Toggle();

        CommandManager.AddHandler(Command, new CommandInfo((_, _) => OpenerCreatorWindow.Toggle())
        {
            HelpMessage = "Create, save, and practice your openers."
        });
    }

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
        OpenerCreatorWindow.Dispose();
    }
}
