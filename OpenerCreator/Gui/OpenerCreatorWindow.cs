using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Interface.Internal;
using Dalamud.Utility;
using ImGuiNET;
using OpenerCreator.Helpers;
using OpenerCreator.Managers;

namespace OpenerCreator.Gui;

public class OpenerCreatorWindow : IDisposable
{
    public bool Enabled;
    private List<uint> actions;

    private Dictionary<uint, IDalamudTextureWrap> iconCache;
    private string search;
    private string name;
    private List<uint> filteredActions;

    private const int IconSize = 32;

    public OpenerCreatorWindow()
    {
        Enabled = false;
        actions = new();
        iconCache = new();
        search = "";
        name = "";
        filteredActions = ActionDictionary.Instance.NonRepeatedIdList();
    }

    public void Dispose()
    {
        foreach (var v in iconCache)
            v.Value.Dispose();
        GC.SuppressFinalize(this);

    }

    public void Draw()
    {
        if (!Enabled)
            return;

        ImGui.SetNextWindowSizeConstraints(new Vector2(100, 100), new Vector2(4000, 2000));
        ImGui.Begin("Opener Creator", ref Enabled);

        ActionSlideChildFrame.DrawActionsGui(actions, iconCache, IconSize);

        // draw opener loader
        // OpenerLoaderChild.DrawOpenerLoader(actions);

        DrawAbilityFilter();
    }

    private void DrawAbilityFilter()
    {
        ImGui.BeginChild("allactions");
        if (ImGui.InputText("Search", ref search, 64))
        {
            if (search.Length > 0)
                filteredActions = ActionDictionary.Instance.GetNonRepeatedActionsByName(search);
            else
                filteredActions = ActionDictionary.Instance.NonRepeatedIdList();
        }

        ImGui.Text($"{filteredActions.Count} Results");
        ImGui.SameLine();
        if (ImGui.Button("Lock opener"))
        {
            OpenerManager.Instance.Loaded = actions;
            ChatMessages.OpenerLoaded();
        }
        ImGui.SameLine();
        if (ImGui.Button("Save") && !name.IsNullOrEmpty())
        {
            OpenerManager.Instance.AddOpener(name, actions);
            OpenerManager.Instance.SaveOpeners();
            ChatMessages.OpenerSaved();
        }
        ImGui.SameLine();
        ImGui.InputText("Opener name", ref name, 32);


        for (var i = 0; i < Math.Min(20, filteredActions.Count); i++) // at max 5
        {
            var action = ActionDictionary.Instance.GetAction(filteredActions[i]);
            ImGui.Image(GetIcon(filteredActions[i]), new Vector2(IconSize, IconSize));
            if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
                actions.Add(filteredActions[i]);

            ImGui.SameLine();
            ImGui.Text(action.Name.ToString());
        }

        ImGui.EndChild();
        ImGui.End();
    }

    private nint GetIcon(uint id)
    {
        if (!iconCache.ContainsKey(id))
            iconCache[id] = ActionDictionary.Instance.GetIconTexture(id);
        return iconCache[id].ImGuiHandle;
    }
}
