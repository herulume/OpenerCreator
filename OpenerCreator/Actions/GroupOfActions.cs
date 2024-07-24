using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Interface.Textures;
using OpenerCreator.Helpers;

namespace OpenerCreator.Actions;

public readonly struct GroupOfActions(int id, string name, Jobs job, IEnumerable<uint> actions, bool isGCD = true)
{
    public ISharedImmediateTexture texture => throw new NotImplementedException();

    public bool IsMember(uint a)
    {
        return actions.Contains(a);
    }

    public bool HasId(int i)
    {
        return id == i;
    }

    public static IEnumerable<GroupOfActions> DefaultGroups()
    {
        return new[]
        {
            new GroupOfActions(
                -1,
                "Dancer Steps",
                Jobs.DNC,
                new List<uint>
                {
                    15999,  // Emboite
                    16000,  // Entrechat
                    160001, // Jete
                    160002  // Pirouette
                }
            ),
            new GroupOfActions(
                -2,
                "Mudras",
                Jobs.NIN,
                new List<uint>
                {
                    2259,  // Ten
                    18805, // Ten
                    2261,  // Chi
                    18806, // Chi
                    2263,  // Jin
                    18807  // Jin
                }
            ),
            new GroupOfActions(
                -3,
                "Refulgent Arrow Proc",
                Jobs.BRD,
                new List<uint>
                {
                    16495, // Burst Shot
                    7409   // Refulgent Arrow
                }
            ),
            new GroupOfActions(
                -4,
                "Venthunder/Veraero",
                Jobs.RDM,
                new List<uint>
                {
                    25855, // Verthunder III
                    25855  // Veraero III
                }
            )
        };
    }
}
