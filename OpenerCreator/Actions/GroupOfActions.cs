using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Interface.Textures;
using OpenerCreator.Helpers;

namespace OpenerCreator.Actions;

public readonly struct GroupOfActions(int id, string name, Jobs job, IEnumerable<uint> actions, bool isGCD = true)
{
    public int Id => id;
    public string Name => name;
    public Jobs Job => job;
    public IEnumerable<uint> Actions => actions;
    public bool IsGCD => isGCD;

    public bool IsMember(uint a)
    {
        return actions.Contains(a);
    }

    public bool HasId(int i)
    {
        return id == i;
    }

    public static bool TryGetDefault(int id, out GroupOfActions value)
    {
    	foreach(var group in DefaultGroups)
            if(group.Id == id)
            {
                value = group;
                return true;
            }
        
        value = new(0, "", Jobs.ANY, []);
        return false;
    }

    public static List<int> GetFilteredGroups(string name, Jobs job, ActionTypes actionType)
    {
        return DefaultGroups
            .AsParallel()
            .Where(a =>
                (name.Length == 0 || a.Name.Contains(name, StringComparison.CurrentCultureIgnoreCase))
                && (job == a.Job || job == Jobs.ANY)
                && ((actionType == ActionTypes.GCD && a.IsGCD)
                    || (actionType == ActionTypes.OGCD && !a.IsGCD)
                    || actionType == ActionTypes.ANY)
            )
            .Select(a => a.Id)
            .OrderBy(id => id)
            .ToList();
    }

    public static readonly GroupOfActions[] DefaultGroups = [
        new(
            -1,
            "Dancer Step",
            Jobs.DNC,
            [
                15999, // Emboite
                16000, // Entrechat
                16001, // Jete
                16002, // Pirouette
            ]
        ),
        new(
            -2,
            "Mudra",
            Jobs.NIN,
            [
                2259,  // Ten
                18805, // Ten
                2261,  // Chi
                18806, // Chi
                2263,  // Jin
                18807, // Jin
            ]
        ),
        new(
            -3,
            "Refulgent Arrow Proc",
            Jobs.BRD,
            [
                16495, // Burst Shot
                7409,  // Refulgent Arrow
            ]
        ),
        new(
            -4,
            "Verthunder/Veraero",
            Jobs.RDM,
            [
                25855, // Verthunder III
                25856, // Veraero III
            ]
        ),
        new(
            -5,
            "Saber/Starfall Dance",
            Jobs.DNC,
            [
                16005, // Saber Dance
                25792, // Starfall Dance
            ]
        ),
        new(
            -6,
            "Dancer Priority GCD",
            Jobs.DNC,
            [
                16005, // Saber Dance
                25792, // Starfall Dance
                36983, // Last Dance
                15992, // Fountainfall
                15991, // Reverse Cascade
            ]
        ),
    ];
}
