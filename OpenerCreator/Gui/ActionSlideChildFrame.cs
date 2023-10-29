using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Interface.Internal;
using ImGuiNET;
using OpenerCreator.Helpers;

namespace OpenerCreator.Gui
{
    internal class ActionSlideChildFrame
    {
        public static void DrawActionsGui(List<uint> actions, Dictionary<uint, IDalamudTextureWrap> iconCache, float iconSize)
        {
            var spacing = ImGui.GetStyle().ItemSpacing;
            var padding = ImGui.GetStyle().FramePadding;
            var icons_per_line = (int)Math.Floor((ImGui.GetContentRegionAvail().X - padding.X * 2.0 + spacing.X) / (iconSize + spacing.X));
            var lines = (float)Math.Max(Math.Ceiling(actions.Count / (float)icons_per_line), 1);
            ImGui.BeginChildFrame(2426787, new Vector2(ImGui.GetContentRegionAvail().X, lines * (iconSize + spacing.Y) - spacing.Y + padding.Y * 2), ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);

            int? delete = null;
            for (var i = 0; i < actions.Count; i++)
            {
                if (i > 0)
                {
                    ImGui.SameLine();
                    if (ImGui.GetContentRegionAvail().X < iconSize)
                        ImGui.NewLine();
                }

                ImGui.Image(GetIcon(actions[i], iconCache), new Vector2(iconSize, iconSize));
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip(ActionDictionary.Instance.GetActionName(actions[i]));
                if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                    delete = i;
            }

            if (delete != null)
                actions.RemoveAt(delete.Value);

            ImGui.Dummy(Vector2.Zero);
            ImGui.EndChildFrame();
        }

        private static nint GetIcon(uint id, Dictionary<uint, IDalamudTextureWrap> iconCache)
        {
            if (!iconCache.ContainsKey(id))
                iconCache[id] = ActionDictionary.Instance.GetIconTexture(id);
            return iconCache[id].ImGuiHandle;
        }
    }
}
