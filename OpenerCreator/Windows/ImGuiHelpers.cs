using System;
using ImGuiNET;

namespace OpenerCreator.Windows;

public static class ImGuiHelpers
{
    internal static void CollapsingHeader(string label, Action action)
    {
        if (ImGui.CollapsingHeader(label, ImGuiTreeNodeFlags.DefaultOpen)) action();
    }
}
