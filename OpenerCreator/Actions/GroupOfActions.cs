using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Interface.Textures;

namespace OpenerCreator.Actions;

public readonly struct GroupOfActions(string name, IEnumerable<uint> actions, bool isGCD = true)
{
    public ISharedImmediateTexture texture => throw new NotImplementedException();

    public bool isMember(uint a)
    {
        return actions.Contains(a);
    }
}
