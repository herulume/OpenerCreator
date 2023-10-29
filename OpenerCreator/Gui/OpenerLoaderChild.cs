using System.Collections.Generic;
using ImGuiNET;
using OpenerCreator.Helpers;
using OpenerCreator.Managers;

namespace OpenerCreator.Gui
{
    internal class OpenerLoaderChild
    {
        public static void DrawOpenerLoader(List<uint> actions)
        {
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
                    ChatMessages.OpenerLoaded();
                }
            }
            ImGui.EndChild();
        }
    }
}
