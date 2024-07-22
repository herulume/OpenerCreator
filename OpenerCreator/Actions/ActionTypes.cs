using LuminaAction = Lumina.Excel.GeneratedSheets.Action;


namespace OpenerCreator.Actions;

public enum ActionTypes
{
    ANY,
    GCD,
    OGCD
}

public static class ActionTypesExtension
{
    public static string PrettyPrint(this ActionTypes actionType)
    {
        return actionType switch
        {
            ActionTypes.GCD => "GCD",
            ActionTypes.OGCD => "oGCD",
            _ => "Any"
        };
    }

    public static ActionTypes GetType(LuminaAction action)
    {
        return action.ActionCategory.Row switch
        {
            2 or 3 => ActionTypes.GCD,
            4 => ActionTypes.OGCD,
            _ => ActionTypes.ANY
        };
    }
}
