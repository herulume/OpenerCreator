using System.Collections.Generic;
using System.Linq;
using Dalamud.Interface.Textures;

namespace OpenerCreator.Actions;

public readonly struct GroupOfActions(string name, string iconLocation, IEnumerable<uint> actions, bool isGCD = true)
{
    public ISharedImmediateTexture texture => OpenerCreator.TextureProvider.GetFromGame(iconLocation);

    public bool isMember(uint a)
    {
        return actions.Contains(a);
    }
}
