using System;
using Dalamud.Bindings.ImGui;

namespace OpenerCreator.Windows;

public static class Helper
{
    internal static void CollapsingHeader(string label, Action action)
    {
        if (ImGui.CollapsingHeader(label, ImGuiTreeNodeFlags.DefaultOpen))
            action();
    }
}
