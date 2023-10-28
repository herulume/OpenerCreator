using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Interface.Internal;
using ImGuiNET;

namespace SamplePlugin.Gui;

public class OpenerCreator : IDisposable
{
    public bool Enabled;
    public List<uint> Actions;

    private Dictionary<uint, IDalamudTextureWrap> iconCache;
    private Dictionary<uint, Lumina.Excel.GeneratedSheets.Action> actionsSheet;
    private string search;
    private List<uint> filteredActions;

    const int iconSize = 32;

    public OpenerCreator()
    {
        Enabled = true; // TODO for lea: change this to false and add a command
        Actions = new();
        iconCache = new();
        actionsSheet = Plugin.DataManager.GetExcelSheet<Lumina.Excel.GeneratedSheets.Action>()!
            .Where(a =>
                (a.ActionCategory.Row == 2 || a.ActionCategory.Row == 4)
                && a.IsPlayerAction
                && !a.IsPvP
                && a.ClassJobLevel > 0)
            .ToDictionary(a => a.RowId);
        search = "";
        filteredActions = actionsSheet.Select(a => a.Key).ToList();

    }

    public void Dispose()
    {
        foreach (var v in iconCache)
            v.Value.Dispose();
    }

    public void Draw()
    {
        if (!Enabled)
            return;

        ImGui.SetNextWindowSizeConstraints(new Vector2(100, 100), new Vector2(4000, 2000));
        ImGui.Begin("Opener Creator", ref Enabled);

        var spacing = ImGui.GetStyle().ItemSpacing;
        var padding = ImGui.GetStyle().FramePadding;
        var icons_per_line = (int)Math.Floor((ImGui.GetContentRegionAvail().X - padding.X * 2.0 + spacing.X) / (iconSize + spacing.X));
        var lines = (float)Math.Max(Math.Ceiling(Actions.Count / (float)icons_per_line), 1);
        ImGui.BeginChildFrame(2426787, new Vector2(ImGui.GetContentRegionAvail().X, lines * (iconSize + spacing.Y) - spacing.Y + padding.Y * 2), ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);

        int? delete = null;
        for (var i = 0; i < Actions.Count; i++)
        {
            if (i > 0)
            {
                ImGui.SameLine();
                if (ImGui.GetContentRegionAvail().X < iconSize)
                    ImGui.NewLine();
            }

            ImGui.Image(GetIcon(Actions[i]), new Vector2(iconSize, iconSize));
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(actionsSheet[Actions[i]].Name.ToString());
            if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                delete = i;
        }

        if (delete != null)
            Actions.RemoveAt(delete.Value);

        ImGui.Dummy(Vector2.Zero);
        ImGui.EndChildFrame();

        ImGui.BeginChild("allactions");
        if (ImGui.InputText("Search", ref search, 64))
        {
            if (search.Length > 0)
                filteredActions = actionsSheet
                    .Where(a => a.Value.Name.ToString().ToLower().Contains(search.ToLower()))
                    .Select(a => a.Key)
                    .ToList();
            else
                filteredActions = actionsSheet.Select(a => a.Key).ToList();
        }

        ImGui.Text($"{filteredActions.Count} Results");

        for (var i = 0; i < filteredActions.Count; i++)
        {
            var action = actionsSheet[filteredActions[i]];
            ImGui.Image(GetIcon(filteredActions[i]), new Vector2(iconSize, iconSize));
            if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
                Actions.Add(filteredActions[i]);

            ImGui.SameLine();
            ImGui.Text(action.Name.ToString());
        }

        ImGui.EndChild();

        ImGui.End();
    }

    private nint GetIcon(uint id)
    {
        if (!iconCache.ContainsKey(id))
        {
            var icon = actionsSheet[id].Icon.ToString("D6");
            var path = $"ui/icon/{icon[0]}{icon[1]}{icon[2]}000/{icon}_hr1.tex";
            // Dalamud.Logging.PluginLog.Log(path);
            var data = Plugin.DataManager.GetFile<Lumina.Data.Files.TexFile>(path)!;
            var pixels = new byte[data.Header.Width * data.Header.Height * 4];
            for (var i = 0; i < data.Header.Width * data.Header.Height; i++)
            {
                pixels[i * 4 + 0] = data.ImageData[i * 4 + 2];
                pixels[i * 4 + 1] = data.ImageData[i * 4 + 1];
                pixels[i * 4 + 2] = data.ImageData[i * 4 + 0];
                pixels[i * 4 + 3] = data.ImageData[i * 4 + 3];
            }
            iconCache[id] = Plugin.PluginInterface.UiBuilder.LoadImageRaw(pixels, data.Header.Width, data.Header.Height, 4);
        }

        return iconCache[id].ImGuiHandle;
    }
}
