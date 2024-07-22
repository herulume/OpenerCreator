using Dalamud.Interface.Textures;

namespace OpenerCreator.Actions;

public interface IActionManager
{
    static uint CatchAllActionId => 0;
    static string CatchAllActionName => "Catch-All Action";
    static string OldActionName => "Old Action";

    static ISharedImmediateTexture GetUnknownActionTexture =>
        OpenerCreator.TextureProvider.GetFromGame("ui/icon/000000/000786_hr1.tex");

    static ushort GetCatchAllIcon => 405;

    string GetActionName(uint action);
    bool SameActionsByName(string action1, uint action2);
}
