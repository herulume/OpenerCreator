using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Interface.Windowing;
using Dalamud.Utility;
using ImGuiNET;
using OpenerCreator.Actions;
using OpenerCreator.Helpers;
using OpenerCreator.Managers;

namespace OpenerCreator.Windows;

public class OpenerCreatorWindow : Window, IDisposable
{
    private static readonly Vector2 IconSize = new(32);

    private readonly Dictionary<JobCategory, bool> currentColor = new()
    {
        { JobCategory.Tank, false },
        { JobCategory.Healer, false },
        { JobCategory.Melee, false },
        { JobCategory.PhysicalRanged, false },
        { JobCategory.MagicalRanged, false }
    };

    private readonly Action<int, Action<Feedback>, Action<int>> startRecording;
    private readonly Action stopRecording;
    private readonly HashSet<int> wrongActions;
    private int? actionDnd;
    private List<uint> actions;

    private Countdown countdown = new();
    private List<string> feedback;
    private List<uint> filteredActions;
    private JobCategory jobCategoryFilter = JobCategory.None;
    private Jobs jobFilter;

    private string name;
    private bool recording;
    private List<Tuple<Jobs, List<string>>> savedOpeners;
    private bool saveOpenerInvalidConfig;
    private string search;


    public OpenerCreatorWindow(Action<int, Action<Feedback>, Action<int>> startRecording, Action stopRecording)
        : base("Opener Creator###ocrt", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        ForceMainWindow = true; // Centre countdown
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(100, 100),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        name = "";
        search = "";
        jobFilter = Jobs.ANY;
        recording = false;
        feedback = [];
        actions = [];
        savedOpeners = OpenerManager.Instance.GetNames();
        filteredActions = PvEActions.Instance.NonRepeatedIdList();
        wrongActions = [];

        this.startRecording = startRecording;
        this.stopRecording = stopRecording;
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
        var lines = (float)Math.Max(Math.Ceiling(actions.Count / (float)iconsPerLine), 1);
        ImGui.BeginChildFrame(
            2426787,
            new Vector2(ImGui.GetContentRegionAvail().X,
                        (lines * (IconSize.Y + spacing.Y)) - spacing.Y + (padding.Y * 2)),
            ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);

        int? dndTarget = null;
        if (actionDnd != null)
        {
            var pos = ImGui.GetMousePos() - ImGui.GetCursorScreenPos();
            var x = (int)Math.Floor(pos.X / (IconSize.X + spacing.X));
            var y = (int)Math.Floor(pos.Y / (IconSize.Y + spacing.Y));
            dndTarget = Math.Clamp((y * iconsPerLine) + x, 0, actions.Count - 1);
        }

        int? delete = null;
        for (var i = 0; i < actions.Count + (actionDnd == null ? 0 : 1); i++)
        {
            if (i > 0)
            {
                ImGui.SameLine();
                if (ImGui.GetContentRegionAvail().X < IconSize.X)
                    ImGui.NewLine();
            }

            if ((dndTarget <= actionDnd && dndTarget == i) || (dndTarget > actionDnd && dndTarget == i - 1))
            {
                ImGui.Image(GetIcon(actions[actionDnd!.Value]), IconSize, Vector2.Zero, Vector2.One,
                            new Vector4(255, 255, 255, 100));

                if (actionDnd != i)
                {
                    ImGui.SameLine();
                    if (ImGui.GetContentRegionAvail().X < IconSize.X)
                        ImGui.NewLine();
                }
            }

            if (actionDnd != i && i < actions.Count)
            {
                var color = wrongActions.Contains(i) ? new Vector4(255, 0, 0, 255) : new Vector4(255, 255, 255, 255);
                ImGui.Image(GetIcon(actions[i]), IconSize, Vector2.Zero, Vector2.One, color);
                if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
                    actionDnd = i;

                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip(PvEActions.Instance.GetActionName(actions[i]));
                if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                    delete = i;
            }
        }

        if (delete != null)
            actions.RemoveAt(delete.Value);

        // Handle dnd
        if (actionDnd != null)
        {
            var pos = ImGui.GetMousePos();
            var drawlist = ImGui.GetWindowDrawList();
            drawlist.PushTextureID(GetIcon(actions[actionDnd.Value]));
            drawlist.PrimReserve(6, 4);
            drawlist.PrimRectUV(pos, pos + IconSize, Vector2.Zero, Vector2.One, 0xFFFFFFFF);
            drawlist.PopTextureID();

            if (!ImGui.IsMouseDown(ImGuiMouseButton.Left))
            {
                if (dndTarget < actionDnd)
                {
                    var action = actions[actionDnd.Value];
                    actions.RemoveAt(actionDnd.Value);
                    actions.Insert(dndTarget.Value, action);
                }
                else if (dndTarget > actionDnd)
                {
                    actions.Insert(dndTarget.Value + 1, actions[actionDnd.Value]);
                    actions.RemoveAt(actionDnd.Value);
                }

                actionDnd = null;
            }
        }

        ImGui.Dummy(Vector2.Zero);
        ImGui.EndChildFrame();
    }

    private void DrawOpenerLoaderTab()
    {
        if (!ImGui.BeginTabItem("Loader"))
            return;

        ImGui.BeginChild("loadopener");
        var defaultOpeners = OpenerManager.Instance.GetDefaultNames();
        savedOpeners = OpenerManager.Instance.GetNames();

        ImGui.BeginTabBar("OpenerCreatorLoaderTabBar");
        DrawOpeners(defaultOpeners, "Default", OpenerManager.Instance.GetDefaultOpener);
        DrawOpeners(savedOpeners, "Saved", OpenerManager.Instance.GetOpener, true);
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
                            actions = getOpener(opener, openerJob.Item1);
                            actions = OpenerManager.Instance.GetDefaultOpener(opener, openerJob.Item1);
                            OpenerManager.Instance.Loaded = actions;
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

        ImGui.BeginChild("allactions",
                         new Vector2(0, ImGui.GetContentRegionAvail().Y - ImGui.GetStyle().WindowPadding.Y));

        ImGui.InputText("Opener name", ref name, 32);

        //  Filter by job
        if (ImGui.BeginCombo("Job Filter", jobFilter.ToString()))
        {
            foreach (Jobs job in Enum.GetValues(typeof(Jobs)))
                if (ImGui.Selectable(job.ToString()))
                {
                    jobFilter = job;
                    filteredActions = PvEActions.Instance.GetNonRepeatedActionsByName(search, jobFilter);
                }

            ImGui.EndCombo();
        }

        // Search bar
        if (ImGui.InputText("Search", ref search, 32))
        {
            filteredActions = search.Length > 0
                                  ? PvEActions.Instance.GetNonRepeatedActionsByName(search, jobFilter)
                                  : PvEActions.Instance.NonRepeatedIdList();
        }

        ImGui.Text($"{filteredActions.Count} Results");
        ImGui.SameLine();
        if (ImGui.Button("Add catch-all action")) actions.Add(0);
        ImGui.SameLine();
        DrawClear();
        ImGui.SameLine();
        DrawSaveOpener();

        for (var i = 0; i < Math.Min(20, filteredActions.Count); i++)
        {
            var action = PvEActions.Instance.GetAction(filteredActions[i]);
            if (ImGui.ImageButton(GetIcon(filteredActions[i]), IconSize))
            {
                actions.Add(filteredActions[i]);
                OpenerManager.Instance.Loaded = actions;
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

        ImGui.BeginChild("recordactions");
        ImGui.Text("Start a countdown, record your actions and compare them with your opener.");
        ImGui.Spacing();
        if (ImGui.Button("Start Recording"))
        {
            feedback.Clear();
            wrongActions.Clear();
            recording = true;
            countdown.StartCountdown();
            startRecording(OpenerCreator.Config.CountdownTime, AddFeedback, WrongAction);
        }

        ImGui.SameLine();
        if (ImGui.Button("Stop Recording"))
        {
            recording = false;
            countdown.StopCountdown();
            stopRecording();
        }

        if (recording)
        {
            ImGui.SameLine();
            ImGui.Text("RECORDING");
        }

        foreach (var line in feedback) ImGui.Text(line);

        ImGui.EndChild();
        ImGui.EndTabItem();
    }

    private static void DrawSettingsTab()
    {
        if (!ImGui.BeginTabItem("Settings"))
            return;

        ImGui.BeginChild("settings");
        ImGui.BeginGroup();
        ImGui.Text("Countdown");
        ImGui.Checkbox("Enable countdown", ref OpenerCreator.Config.IsCountdownEnabled);

        if (ImGui.InputInt("Countdown timer", ref OpenerCreator.Config.CountdownTime))
        {
            OpenerCreator.Config.CountdownTime = Math.Clamp(OpenerCreator.Config.CountdownTime, 0, 30);
            OpenerCreator.Config.Save();
        }

        ImGui.EndGroup();

        ImGui.Spacing();
        ImGui.BeginGroup();
        ImGui.Text("Action Recording");
        ImGui.Checkbox("Stop recording at first mistake", ref OpenerCreator.Config.StopAtFirstMistake);
        ImGui.EndGroup();

        ImGui.EndChild();
        ImGui.EndTabItem();
    }

    private void DrawClear()
    {
        if (ImGui.Button("Clear Actions"))
        {
            actions.Clear();
            feedback.Clear();
            wrongActions.Clear();
        }
    }

    private void DrawSaveOpener()
    {
        if (ImGui.Button("Save Opener"))
        {
            if (jobFilter != Jobs.ANY && !name.IsNullOrEmpty())
            {
                OpenerManager.Instance.AddOpener(name, jobFilter, actions);
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
            var active = currentColor[jobCategory];
            if (active)
                ImGui.PushStyleColor(ImGuiCol.Button, ImGui.GetStyle().Colors[(int)ImGuiCol.TabActive]);

            if (ImGui.Button(label))
            {
                jobCategoryFilter = JobsExtensions.Toggle(jobCategoryFilter, jobCategory);
                currentColor[jobCategory] = !active;
            }

            if (active) ImGui.PopStyleColor(1);
        }
    }

    private void WrongAction(int i)
    {
        wrongActions.Add(i);
    }

    public void AddFeedback(Feedback f)
    {
        countdown.StopCountdown();
        recording = false;
        feedback = f.GetMessages();
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
