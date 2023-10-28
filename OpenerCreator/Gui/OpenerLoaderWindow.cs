using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Game.Text;
using Dalamud.Interface.Internal;
using ImGuiNET;
using OpenerCreator.Helpers;
using OpenerCreator.Managers;

namespace OpenerCreator.Gui;

public class OpenerLoaderWindow : IDisposable
{
    public bool Enabled;
    private List<uint> actions;

    private readonly Dictionary<uint, IDalamudTextureWrap> iconCache;

    private const int IconSize = 32;

    public OpenerLoaderWindow()
    {
        Enabled = false;
        actions = new();
        iconCache = new();
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
        ImGui.Begin("Opener Loader", ref Enabled);

        var spacing = ImGui.GetStyle().ItemSpacing;
        var padding = ImGui.GetStyle().FramePadding;
        var icons_per_line = (int)Math.Floor((ImGui.GetContentRegionAvail().X - padding.X * 2.0 + spacing.X) / (IconSize + spacing.X));
        var lines = (float)Math.Max(Math.Ceiling(actions.Count / (float)icons_per_line), 1);
        ImGui.BeginChildFrame(2426787, new Vector2(ImGui.GetContentRegionAvail().X, lines * (IconSize + spacing.Y) - spacing.Y + padding.Y * 2), ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);

        int? delete = null;
        for (var i = 0; i < actions.Count; i++)
        {
            if (i > 0)
            {
                ImGui.SameLine();
                if (ImGui.GetContentRegionAvail().X < IconSize)
                    ImGui.NewLine();
            }

            ImGui.Image(GetIcon(actions[i]), new Vector2(IconSize, IconSize));
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(ActionDictionary.Instance.GetActionName(actions[i]));
            if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                delete = i;
        }

        if (delete != null)
            actions.RemoveAt(delete.Value);

        ImGui.Dummy(Vector2.Zero);
        ImGui.EndChildFrame();

        ImGui.BeginChild("loadopener");
        if (ImGui.Button("Clear"))
        {
            actions.Clear();
        }
        var defaultOpeners = OpenerManager.Instance.GetDefaultNames();
        foreach (var opener in defaultOpeners)
        {
            ImGui.Text(opener);
            ImGui.SameLine();
            if (ImGui.Button($"Load##{opener}"))
            {
                actions = OpenerManager.Instance.GetDefaultOpener(opener);
                OpenerManager.Instance.Loaded = actions;
                OpenerCreator.ChatGui.Print(new XivChatEntry
                {
                    Message = "Opener locked.",
                    Type = XivChatType.Echo
                });
            }
        }

        ImGui.EndChild();

        ImGui.End();
    }

    private nint GetIcon(uint id)
    {
        if (!iconCache.ContainsKey(id))
        {
            var icon = ActionDictionary.Instance.GetActionIcon(id).ToString("D6");
            var path = $"ui/icon/{icon[0]}{icon[1]}{icon[2]}000/{icon}_hr1.tex";
            // Dalamud.Logging.PluginLog.Log(path);
            var data = OpenerCreator.DataManager.GetFile<Lumina.Data.Files.TexFile>(path)!;
            var pixels = new byte[data.Header.Width * data.Header.Height * 4];
            for (var i = 0; i < data.Header.Width * data.Header.Height; i++)
            {
                pixels[i * 4 + 0] = data.ImageData[i * 4 + 2];
                pixels[i * 4 + 1] = data.ImageData[i * 4 + 1];
                pixels[i * 4 + 2] = data.ImageData[i * 4 + 0];
                pixels[i * 4 + 3] = data.ImageData[i * 4 + 3];
            }
            iconCache[id] = OpenerCreator.PluginInterface.UiBuilder.LoadImageRaw(pixels, data.Header.Width, data.Header.Height, 4);
        }

        return iconCache[id].ImGuiHandle;
    }
}
