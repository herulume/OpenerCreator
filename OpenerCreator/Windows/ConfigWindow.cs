using System;
using Dalamud.Interface.Windowing;
using FFXIVClientStructs.FFXIV.Common.Math;
using ImGuiNET;

namespace OpenerCreator.Windows;

public class ConfigWindow : Window, IDisposable
{
    public ConfigWindow() : base("OpenerCreator Configuration###OCRTConfig")
    {
        Flags = ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar |
                ImGuiWindowFlags.NoScrollWithMouse;

        Size = new Vector2(500, 300);
        SizeCondition = ImGuiCond.Always;
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    public override void Draw()
    {
        ImGui.BeginGroup();
        ImGuiHelpers.CollapsingHeader("Countdown", () =>
        {
            if (ImGui.Checkbox("Enable countdown", ref OpenerCreator.Config.IsCountdownEnabled))
                OpenerCreator.Config.Save();

            if (ImGui.InputInt("Countdown timer", ref OpenerCreator.Config.CountdownTime))
            {
                OpenerCreator.Config.CountdownTime = Math.Clamp(OpenerCreator.Config.CountdownTime, 0, 30);
                OpenerCreator.Config.Save();
            }
        });
        ImGui.EndGroup();
        ImGui.Spacing();
        ImGui.BeginGroup();
        ImGuiHelpers.CollapsingHeader("Action Recording",
                                      () =>
                                      {
                                          if (ImGui.Checkbox("Stop recording at first mistake",
                                                             ref OpenerCreator.Config.StopAtFirstMistake))
                                              OpenerCreator.Config.Save();

                                          if (ImGui.Checkbox("Ignore True North if it isn't present on the opener.",
                                                             ref OpenerCreator.Config.IgnoreTrueNorth))
                                              OpenerCreator.Config.Save();
                                          if (ImGui.Checkbox("Use ability ants for next opener action.",
                                                             ref OpenerCreator.Config.AbilityAnts))
                                              OpenerCreator.Config.Save();
                                      });
        ImGui.EndGroup();
    }
}
