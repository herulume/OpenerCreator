using System;
using System.Collections.Generic;
using System.Linq;
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

    private List<int> actionsIds;
    private ActionTypes actionTypeFilter = ActionTypes.ANY;
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

        actionsIds = PvEActions.Instance.ActionsIdList(actionTypeFilter);
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
        DrawInfoTab();
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

        var frameW = ImGui.GetContentRegionAvail().X;
        ImGui.BeginChildFrame(
            2426787,
            new Vector2(frameW, (lines * ((IconSize.Y * 1.7f) + spacing.Y)) - spacing.Y + (padding.Y * 2)),
            ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);

        var drawList = ImGui.GetWindowDrawList();
        for (var i = 0; i < lines; i++)
        {
            var pos = ImGui.GetCursorScreenPos();
            drawList.AddRectFilled(pos + new Vector2(0, (IconSize.Y * 0.9f) + (i * ((IconSize.Y * 1.7f) + spacing.Y))),
                                   pos + new Vector2(
                                       frameW, (IconSize.Y * 1.1f) + (i * ((IconSize.Y * 1.7f) + spacing.Y))),
                                   0x64000000);
        }

        int? dndTarget = null;
        if (actionDragAndDrop != null)
        {
            var pos = ImGui.GetMousePos() - ImGui.GetCursorScreenPos();
            var x = (int)Math.Floor(pos.X / (IconSize.X + spacing.X));
            var y = (int)Math.Floor(pos.Y / ((IconSize.Y * 1.7f) + spacing.Y));
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
                var actionAt = loadedActions.GetActionAt(actionDragAndDrop!.Value);
                if (!PvEActions.Instance.IsActionOGCD(actionAt))
                    ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (IconSize.Y * 0.5f));
                DrawIcon(actionAt, IconSize, 0x64FFFFFF);

                if (actionDragAndDrop != i)
                {
                    ImGui.SameLine();
                    if (ImGui.GetContentRegionAvail().X < IconSize.X)
                        ImGui.NewLine();
                }
            }

            if (actionDragAndDrop != i && i < loadedActions.ActionsCount())
            {
                var actionAt = loadedActions.GetActionAt(i);
                var color = loadedActions.IsWrongActionAt(i) ? 0xFF6464FF : 0xFFFFFFFF;

                ImGui.BeginChild((uint)i, new Vector2(IconSize.X, IconSize.Y * 1.7f));
                if (!PvEActions.Instance.IsActionOGCD(actionAt))
                    ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (IconSize.Y * 0.5f));
                DrawIcon(actionAt, IconSize, color);
                ImGui.EndChild();

                if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
                    actionDragAndDrop = i;

                if (ImGui.IsItemHovered())
                {
                    if (actionAt >= 0)
                        ImGui.SetTooltip(PvEActions.Instance.GetActionName(actionAt));
                    else if (GroupOfActions.TryGetDefault(actionAt, out var group))
                    {
                        // ImGui.SetTooltip($"{group.Name}");
                        ImGui.BeginTooltip();
                        ImGui.Text(group.Name);
                        ImGui.Indent();
                        foreach (var action in group.Actions)
                            ImGui.Text(PvEActions.Instance.GetActionName((int)action));
                        ImGui.Unindent();
                        ImGui.EndTooltip();
                    }
                    else
                        ImGui.SetTooltip($"Invalid action id ({actionAt})");
                }

                if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                    delete = i;
            }
        }

        if (delete != null)
            loadedActions.RemoveActionAt(delete.Value);

        // Handle dnd
        if (actionDragAndDrop != null)
        {
            var action = loadedActions.GetActionAt(actionDragAndDrop.Value);
            DrawIcon(action, IconSize, 0xFFFFFFFF, ImGui.GetMousePos());

            if (!ImGui.IsMouseDown(ImGuiMouseButton.Left))
            {
                if (dndTarget < actionDragAndDrop)
                {
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
        List<Tuple<Jobs, List<string>>> openers, string prefix, Func<string, Jobs, List<int>> getOpener,
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

        ListFilter("Job filter", jobFilter, JobsExtensions.PrettyPrint, ref jobFilter);
        ListFilter("Action type filter", actionTypeFilter, ActionTypesExtension.PrettyPrint, ref actionTypeFilter);

        // Search bar
        if (ImGui.InputText("Search", ref searchAction, 32))
        {
            actionsIds = PvEActions.Instance.GetNonRepeatedActionsByName(searchAction, jobFilter, actionTypeFilter);
            actionsIds.AddRange(GroupOfActions.GetFilteredGroups(searchAction, jobFilter, actionTypeFilter));
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

        for (var i = 0; i < Math.Min(50, actionsIds.Count); i++)
        {
            var actionId = actionsIds[i];
            DrawIcon(actionId, IconSize);
            if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
            {
                loadedActions.AddAction(actionId);
                OpenerManager.Instance.Loaded = loadedActions.GetActionsByRef();
            }

            ImGui.SameLine();
            if (actionId >= 0)
                ImGui.Text($"{PvEActions.Instance.GetAction((uint)actionId).Name}");
            else if (GroupOfActions.TryGetDefault(actionId, out var group))
                ImGui.Text($"{group.Name}");
            else
                ImGui.Text($"Invalid action id ({actionId})");
        }

        if (actionsIds.Count > 50)
            ImGui.Text("More than 50 results, limiting results shown");

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

        foreach (var line in recordingConfig.GetFeedback())
            ImGui.Text(line);

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

    private static void DrawInfoTab()
    {
        if (!ImGui.BeginTabItem("Info"))
            return;

        ImGui.Text("Supported actions' groups:");
        foreach (var groupsName in GroupOfActions.GroupsNames) ImGui.Text($"- {groupsName}");
        ImGui.BeginChild("###Info");
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

    private static void DrawIcon(int id, Vector2 size, uint color = 0xFFFFFFFF, Vector2? pos = null)
    {
        var realPos = pos ?? ImGui.GetCursorScreenPos();
        var drawList = pos == null ? ImGui.GetWindowDrawList() : ImGui.GetForegroundDrawList();

        if (id >= 0)
        {
            drawList.PushTextureID(GetIcon((uint)id));
            drawList.PrimReserve(6, 4);
            drawList.PrimRectUV(realPos, realPos + size, Vector2.Zero, Vector2.One, color);
            drawList.PopTextureID();
        }
        else if (GroupOfActions.TryGetDefault(id, out var group))
        {
            // could do it the "proper" way of making an actual rectangle... or do this
            // will break if the group only contains a single action, but why use a group at that point??
            var center = realPos + (size / 2);
            var actionCount = group.Actions.Count();
            drawList.PushClipRect(realPos, realPos + size, true);
            for (var i = 0; i < actionCount; ++i)
            {
                var action = group.Actions.ElementAt(i);
                drawList.PushTextureID(GetIcon(action));
                drawList.PrimReserve(6, 4);

                var vtx = (ushort)drawList._VtxCurrentIdx;
                drawList.PrimWriteVtx(center, new Vector2(0.5f, 0.5f), color);

                for (var j = 0; j < 3; j++)
                {
                    var (s, c) =
                        MathF.SinCos(((i - 1.0f + (j * 0.5f)) / actionCount * MathF.PI * 2.0f) - (MathF.PI / 4));
                    drawList.PrimWriteVtx(center + new Vector2(s * size.X, c * size.Y), new Vector2(0.5f + s, 0.5f + c),
                                          color);
                }

                drawList.PrimWriteIdx((ushort)(vtx + 2));
                drawList.PrimWriteIdx((ushort)(vtx + 1));
                drawList.PrimWriteIdx(vtx);
                drawList.PrimWriteIdx((ushort)(vtx + 3));
                drawList.PrimWriteIdx((ushort)(vtx + 2));
                drawList.PrimWriteIdx(vtx);
            }

            drawList.PopClipRect();
        }
        else
        {
            drawList.PushTextureID(IActionManager.GetUnknownActionTexture.GetWrapOrEmpty().ImGuiHandle);
            drawList.PrimReserve(6, 4);
            drawList.PrimRectUV(realPos, realPos + size, Vector2.Zero, Vector2.One, color);
            drawList.PopTextureID();
        }

        if (pos == null)
            ImGui.Dummy(size);
    }

    public void AddFeedback(Feedback f)
    {
        countdown.StopCountdown();
        recordingConfig.StopRecording();
        recordingConfig.AddFeedback(f.GetMessages());
    }

    public void ListFilter<TA>(string label, TA filter, Func<TA, String> prettyPrint, ref TA state) where TA : Enum
    {
        if (ImGui.BeginCombo(label, prettyPrint(filter)))
        {
            foreach (TA value in Enum.GetValues(typeof(TA)))
                if (ImGui.Selectable(prettyPrint(value)))
                {
                    state = value;
                    actionsIds =
                        PvEActions.Instance.GetNonRepeatedActionsByName(searchAction, jobFilter, actionTypeFilter);
                }

            ImGui.EndCombo();
        }
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
