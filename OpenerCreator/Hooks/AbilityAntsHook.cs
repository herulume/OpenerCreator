using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.Game;
using OpenerCreator.Actions;

namespace OpenerCreator.Hooks;

public unsafe class AbilityAntsHook
{
    private readonly Hook<ActionManager.Delegates.IsActionHighlighted> IsActionHighlightedHook;

    public AbilityAntsHook()
    {
        IsActionHighlightedHook =
            OpenerCreator.GameInteropProvider.HookFromAddress<ActionManager.Delegates.IsActionHighlighted>(
                ActionManager.MemberFunctionPointers.IsActionHighlighted, HandleIsActionHighlighted);
    }

    public int CurrentAction { get; set; } = 0;

    public void Enable()
    {
        IsActionHighlightedHook?.Enable();
    }

    public void Disable()
    {
        IsActionHighlightedHook?.Disable();
    }

    public void Dispose()
    {
        IsActionHighlightedHook?.Disable();
        IsActionHighlightedHook?.Dispose();
    }


    private bool HandleIsActionHighlighted(ActionManager* manager, ActionType actionType, uint actionId)
    {
        var original = IsActionHighlightedHook.Original(manager, actionType, actionId);
        return CurrentAction switch
        {
            > 0 when actionId == CurrentAction => true,
            < 0 when GroupOfActions.TryGetDefault(CurrentAction, out var group) && group.IsMember(actionId) => true,
            _ => original
        };
    }
}
