using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using OpenerCreator.Actions;
using OpenerCreator.Helpers;
using OpenerCreator.Managers;

namespace OpenerCreator.Windows;

public class OpenerCreatorWindow : Window, IDisposable
{
    private static readonly Vector2 IconSize = new(32);

    private readonly Dictionary<JobCategory, bool> jobRoleFilterColour = new()
    {
        { JobCategory.Tank, false },
        { JobCategory.Healer, false },
        { JobCategory.Melee, false },
        { JobCategory.PhysicalRanged, false },
        { JobCategory.MagicalRanged, false }
    };

    private readonly LoadedActions loadedActions = new();

    private readonly Recording recordingConfig;
    private int? actionDragAndDrop;

    private List<uint> actionsIds = PvEActions.Instance.ActionsIdList();
    private Countdown countdown = new();
    private List<Tuple<Jobs, List<string>>> customOpeners = OpenerManager.Instance.GetNames();
    private JobCategory jobCategoryFilter = JobCategory.None;
    private Jobs jobFilter = Jobs.ANY;
    private bool saveOpenerInvalidConfig;
    private string searchAction = "";

    public OpenerCreatorWindow(Action<int, Action<Feedback>, Action<int>, bool> startRecording, Action stopRecording)
        : base("Opener Creator###ocrt", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        ForceMainWindow = true; // Centre countdown
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(100, 100),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        recordingConfig = new Recording(startRecording, stopRecording);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    public override void Draw()
    {
        DrawActionsGui();
        ImGui.Spacing();
        ImGui.BeginTabBar("OpenerCreatorMainTabBar");
        DrawOpenerLoaderTab();
        DrawCreatorTab();
        DrawRecordActionsTab();
        DrawSettingsTab();
        ImGui.EndTabBar();

        countdown.DrawCountdown();
    }

    private void DrawActionsGui()
    {
        var spacing = ImGui.GetStyle().ItemSpacing;
        var padding = ImGui.GetStyle().FramePadding;
        var iconsPerLine = (int)Math.Floor((ImGui.GetContentRegionAvail().X - (padding.X * 2.0) + spacing.X) /
                                           (IconSize.X + spacing.X));
        var lines = (float)Math.Max(Math.Ceiling(loadedActions.ActionsCount() / (float)iconsPerLine), 1);
        ImGui.BeginChildFrame(
            2426787,
            new Vector2(ImGui.GetContentRegionAvail().X,
                        (lines * (IconSize.Y + spacing.Y)) - spacing.Y + (padding.Y * 2)),
            ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);

        int? dndTarget = null;
        if (actionDragAndDrop != null)
        {
            var pos = ImGui.GetMousePos() - ImGui.GetCursorScreenPos();
            var x = (int)Math.Floor(pos.X / (IconSize.X + spacing.X));
            var y = (int)Math.Floor(pos.Y / (IconSize.Y + spacing.Y));
            dndTarget = Math.Clamp((y * iconsPerLine) + x, 0, loadedActions.ActionsCount() - 1);
        }

        int? delete = null;
        for (var i = 0; i < loadedActions.ActionsCount() + (actionDragAndDrop == null ? 0 : 1); i++)
        {
            if (i > 0)
            {
                ImGui.SameLine();
                if (ImGui.GetContentRegionAvail().X < IconSize.X)
                    ImGui.NewLine();
            }

            if ((dndTarget <= actionDragAndDrop && dndTarget == i) ||
                (dndTarget > actionDragAndDrop && dndTarget == i - 1))
            {
                ImGui.Image(GetIcon(loadedActions.GetActionAt(actionDragAndDrop!.Value)), IconSize, Vector2.Zero,
                            Vector2.One,
                            new Vector4(255, 255, 255, 100));

                if (actionDragAndDrop != i)
                {
                    ImGui.SameLine();
                    if (ImGui.GetContentRegionAvail().X < IconSize.X)
                        ImGui.NewLine();
                }
            }

            if (actionDragAndDrop != i && i < loadedActions.ActionsCount())
            {
                var color = loadedActions.IsWrongActionAt(i)
                                ? new Vector4(255, 0, 0, 255)
                                : new Vector4(255, 255, 255, 255);
                ImGui.Image(GetIcon(loadedActions.GetActionAt(i)), IconSize, Vector2.Zero, Vector2.One, color);
                if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
                    actionDragAndDrop = i;

                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip(PvEActions.Instance.GetActionName(loadedActions.GetActionAt(i)));
                if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                    delete = i;
            }
        }

        if (delete != null)
            loadedActions.RemoveActionAt(delete.Value);

        // Handle dnd
        if (actionDragAndDrop != null)
        {
            var pos = ImGui.GetMousePos();
            var drawList = ImGui.GetWindowDrawList();
            drawList.PushTextureID(GetIcon(loadedActions.GetActionAt(actionDragAndDrop.Value)));
            drawList.PrimReserve(6, 4);
            drawList.PrimRectUV(pos, pos + IconSize, Vector2.Zero, Vector2.One, 0xFFFFFFFF);
            drawList.PopTextureID();

            if (!ImGui.IsMouseDown(ImGuiMouseButton.Left))
            {
                if (dndTarget < actionDragAndDrop)
                {
                    var action = loadedActions.GetActionAt(actionDragAndDrop.Value);
                    loadedActions.RemoveActionAt(actionDragAndDrop.Value);
                    loadedActions.InsertActionAt(dndTarget.Value, action);
                }
                else if (dndTarget > actionDragAndDrop)
                {
                    loadedActions.InsertActionAt(dndTarget.Value + 1,
                                                 loadedActions.GetActionAt(actionDragAndDrop.Value));
                    loadedActions.RemoveActionAt(actionDragAndDrop.Value);
                }

                actionDragAndDrop = null;
            }
        }

        ImGui.Dummy(Vector2.Zero);
        ImGui.EndChildFrame();
    }

    private void DrawOpenerLoaderTab()
    {
        if (!ImGui.BeginTabItem("Loader"))
            return;

        ImGui.BeginChild("###LoadOpeners");
        var defaultOpeners = OpenerManager.Instance.GetDefaultNames();
        customOpeners = OpenerManager.Instance.GetNames();

        ImGui.BeginTabBar("###DefaultAndCustomTab");
        DrawOpeners(defaultOpeners, "Default", OpenerManager.Instance.GetDefaultOpener);
        DrawOpeners(customOpeners, "Saved", OpenerManager.Instance.GetOpener, true);
        ImGui.EndTabBar();

        ImGui.EndChild();
        ImGui.EndTabItem();
    }

    private void DrawOpeners(
        List<Tuple<Jobs, List<string>>> openers, string prefix, Func<string, Jobs, List<uint>> getOpener,
        bool delete = false)
    {
        if (!ImGui.BeginTabItem($"{prefix} Openers"))
            return;

        DrawJobCategoryFilters();

        foreach (var openerJob in openers)
            if (JobsExtensions.FilterBy(jobCategoryFilter, openerJob.Item1))
            {
                CollapsingHeader($"{prefix} {openerJob.Item1} Openers", () =>
                {
                    foreach (var opener in openerJob.Item2)
                    {
                        ImGui.Text(opener);
                        ImGui.SameLine();
                        if (ImGui.Button($"Load##{prefix}#{opener}#{openerJob.Item1}"))
                        {
                            loadedActions.AddActionsByRef(getOpener(opener, openerJob.Item1));
                            OpenerManager.Instance.Loaded = loadedActions.GetActionsByRef();
                        }

                        if (delete)
                        {
                            ImGui.SameLine();
                            if (ImGui.Button($"Delete##{prefix}#{opener}"))
                            {
                                OpenerManager.Instance.DeleteOpener(opener, openerJob.Item1);
                                OpenerManager.Instance.SaveOpeners();
                                openers = OpenerManager.Instance.GetNames();
                            }
                        }
                    }
                });
            }

        ImGui.EndTabItem();
    }

    private void DrawCreatorTab()
    {
        if (!ImGui.BeginTabItem("Creator"))
            return;

        ImGui.BeginChild("###AllActions",
                         new Vector2(0, ImGui.GetContentRegionAvail().Y - ImGui.GetStyle().WindowPadding.Y));

        ImGui.InputText("Opener name", ref loadedActions.Name, 32);

        //  Filter by job
        if (ImGui.BeginCombo("Job Filter", jobFilter.ToString()))
        {
            foreach (Jobs job in Enum.GetValues(typeof(Jobs)))
                if (ImGui.Selectable(job.ToString()))
                {
                    jobFilter = job;
                    actionsIds = PvEActions.Instance.GetNonRepeatedActionsByName(searchAction, jobFilter);
                }

            ImGui.EndCombo();
        }

        // Search bar
        if (ImGui.InputText("Search", ref searchAction, 32))
        {
            actionsIds = searchAction.Length > 0
                             ? PvEActions.Instance.GetNonRepeatedActionsByName(searchAction, jobFilter)
                             : PvEActions.Instance.ActionsIdList();
        }

        ImGui.Text($"{actionsIds.Count} Results");
        ImGui.SameLine();
        if (ImGui.Button("Add catch-all action"))
        {
            loadedActions.AddAction(0);
            OpenerManager.Instance.Loaded = loadedActions.GetActionsByRef();
        }

        ImGui.SameLine();
        DrawClearActionsAndFeedback();
        ImGui.SameLine();
        DrawSaveOpener();

        for (var i = 0; i < Math.Min(20, actionsIds.Count); i++)
        {
            var action = PvEActions.Instance.GetAction(actionsIds[i]);
            if (ImGui.ImageButton(GetIcon(actionsIds[i]), IconSize))
            {
                loadedActions.AddAction(actionsIds[i]);
                OpenerManager.Instance.Loaded = loadedActions.GetActionsByRef();
            }

            ImGui.SameLine();
            ImGui.Text(action.Name.ToString());
        }

        ImGui.EndChild();
        ImGui.EndTabItem();
    }

    private void DrawRecordActionsTab()
    {
        if (!ImGui.BeginTabItem("Record Actions"))
            return;

        ImGui.BeginChild("###RecordActions");
        ImGui.Text("Start a countdown, record your actions and compare them with your opener");
        ImGui.Spacing();
        if (ImGui.Button("Start Recording"))
        {
            loadedActions.ClearWrongActions();
            countdown.StartCountdown();
            recordingConfig.StartRecording(OpenerCreator.Config.CountdownTime, AddFeedback,
                                           loadedActions.AddWrongActionAt,
                                           OpenerCreator.Config.IgnoreTrueNorth && !loadedActions.HasTrueNorth());
            OpenerCreator.PluginLog.Info($"Is recording? {recordingConfig.IsRecording()}");
        }

        ImGui.SameLine();
        if (ImGui.Button("Stop Recording"))
        {
            countdown.StopCountdown();
            recordingConfig.StopRecording();
        }

        ImGui.SameLine();
        if (ImGui.Button("Clear Feedback"))
        {
            recordingConfig.ClearFeedback();
            loadedActions.ClearWrongActions();
        }

        if (recordingConfig.IsRecording())
        {
            ImGui.SameLine();
            ImGui.Text("RECORDING");
        }

        foreach (var line in recordingConfig.GetFeedback()) ImGui.Text(line);

        ImGui.EndChild();
        ImGui.EndTabItem();
    }

    private static void DrawSettingsTab()
    {
        if (!ImGui.BeginTabItem("Settings"))
            return;

        ImGui.BeginChild("###Settings");
        ImGui.BeginGroup();
        CollapsingHeader("Countdown", () =>
        {
            ImGui.Checkbox("Enable countdown", ref OpenerCreator.Config.IsCountdownEnabled);

            if (ImGui.InputInt("Countdown timer", ref OpenerCreator.Config.CountdownTime))
            {
                OpenerCreator.Config.CountdownTime = Math.Clamp(OpenerCreator.Config.CountdownTime, 0, 30);
                OpenerCreator.Config.Save();
            }
        });
        ImGui.EndGroup();
        ImGui.Spacing();
        ImGui.BeginGroup();
        CollapsingHeader("Action Recording",
                         () =>
                         {
                             ImGui.Checkbox("Stop recording at first mistake",
                                            ref OpenerCreator.Config.StopAtFirstMistake);
                             ImGui.Checkbox("Ignore True North if it isn't present on the opener.",
                                            ref OpenerCreator.Config.IgnoreTrueNorth);
                         });
        ImGui.EndGroup();
        ImGui.EndChild();
        ImGui.EndTabItem();
    }

    private void DrawClearActionsAndFeedback()
    {
        if (ImGui.Button("Clear Actions"))
        {
            loadedActions.ClearWrongActions();
            loadedActions.ClearActions();
            recordingConfig.ClearFeedback();
        }
    }

    private void DrawSaveOpener()
    {
        if (ImGui.Button("Save Opener"))
        {
            if (jobFilter != Jobs.ANY && loadedActions.HasName())
            {
                OpenerManager.Instance.AddOpener(loadedActions.Name, jobFilter, loadedActions.GetActionsByRef());
                OpenerManager.Instance.SaveOpeners();
                saveOpenerInvalidConfig = false;
            }
            else
                saveOpenerInvalidConfig = true;
        }

        if (saveOpenerInvalidConfig)
        {
            ImGui.SameLine();
            ImGui.Text("Error saving opener. Make sure you have selected your job and named the opener.");
        }
    }

    private void DrawJobCategoryFilters()
    {
        DrawJobCategoryToggle("Tanks", JobCategory.Tank);
        ImGui.SameLine();
        DrawJobCategoryToggle("Healers", JobCategory.Healer);
        ImGui.SameLine();
        DrawJobCategoryToggle("Melees", JobCategory.Melee);
        ImGui.SameLine();
        DrawJobCategoryToggle("Physical Ranged", JobCategory.PhysicalRanged);
        ImGui.SameLine();
        DrawJobCategoryToggle("Casters", JobCategory.MagicalRanged);
        return;

        void DrawJobCategoryToggle(string label, JobCategory jobCategory)
        {
            var active = jobRoleFilterColour[jobCategory];
            if (active)
                ImGui.PushStyleColor(ImGuiCol.Button, ImGui.GetStyle().Colors[(int)ImGuiCol.TabActive]);

            if (ImGui.Button(label))
            {
                jobCategoryFilter = JobsExtensions.Toggle(jobCategoryFilter, jobCategory);
                jobRoleFilterColour[jobCategory] = !active;
            }

            if (active) ImGui.PopStyleColor(1);
        }
    }

    public void AddFeedback(Feedback f)
    {
        countdown.StopCountdown();
        recordingConfig.StopRecording();
        recordingConfig.AddFeedback(f.GetMessages());
    }

    private static nint GetIcon(uint id)
    {
        return PvEActions.GetIconTexture(id).GetWrapOrEmpty().ImGuiHandle;
    }

    private static void CollapsingHeader(string label, Action action)
    {
        if (ImGui.CollapsingHeader(label, ImGuiTreeNodeFlags.DefaultOpen)) action();
    }
}
