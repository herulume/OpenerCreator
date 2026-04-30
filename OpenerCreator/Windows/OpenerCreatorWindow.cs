using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using OpenerCreator.Actions;
using OpenerCreator.Helpers;
using OpenerCreator.Managers;

namespace OpenerCreator.Windows;

public class OpenerCreatorWindow : Window, IDisposable
{
    private readonly Plugin Plugin;

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

    public OpenerCreatorWindow(
        Plugin plugin,
        Action<int, Action<Feedback>, Action<int>, Action<int>, bool, Action<int>> startRecording, Action stopRecording,
        Action enableAbilityAnts, Action disableAbilityAnts, Action<int> updateAbilityAnts)
        : base("Opener Creator###ocrt", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        Plugin = plugin;

        ForceMainWindow = true; // Centre countdown
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(350, 200),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        actionsIds = PvEActions.Instance.ActionsIdList(actionTypeFilter);
        recordingConfig = new Recording(startRecording, stopRecording, enableAbilityAnts, disableAbilityAnts,
                                        updateAbilityAnts);

        TitleBarButtons.Add(new TitleBarButton
        {
            Icon = FontAwesomeIcon.Cog,
            Click = _ => { Plugin.OpenConfigUi(); }
        });
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    public override void Draw()
    {
        DrawActionsGui();

        ImGui.Spacing();

        using (var tabBar = ImRaii.TabBar("OpenerCreatorMainTabBar"))
        {
            if (!tabBar.Success)
                return;

            DrawOpenerLoaderTab();
            DrawCreatorTab();
            DrawRecordActionsTab();
            DrawInfoTab();
        }

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
        using var childFrame = ImRaii.ChildFrame(2426787, new Vector2(frameW, (lines * ((IconSize.Y * 1.7f) + spacing.Y)) - spacing.Y + (padding.Y * 2)), ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);
        if (!childFrame.Success)
            return;

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
                var color = 0xFFFFFFFF;
                if (loadedActions.IsCurrentActionAt(i)) color = 0xFFFF7F50;
                if (loadedActions.IsWrongActionAt(i)) color = 0xFF6464FF;

                using (var child = ImRaii.Child($"{i}##actionIconChild", IconSize with { Y = IconSize.Y * 1.7f }))
                {
                    if (child.Success)
                    {
                        if (!PvEActions.Instance.IsActionOGCD(actionAt))
                            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (IconSize.Y * 0.5f));
                        DrawIcon(actionAt, IconSize, color);
                    }
                }

                if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
                    actionDragAndDrop = i;

                if (ImGui.IsItemHovered())
                {
                    if (actionAt >= 0)
                    {
                        ImGui.SetTooltip(PvEActions.Instance.GetActionName(actionAt));
                    }
                    else if (GroupOfActions.TryGetDefault(actionAt, out var group))
                    {
                        // ImGui.SetTooltip($"{group.Name}");
                        using var tooltip = ImRaii.Tooltip();
                        ImGui.TextUnformatted(group.Name);

                        using var indent = ImRaii.PushIndent();
                        foreach (var action in group.Actions)
                            ImGui.TextUnformatted(PvEActions.Instance.GetActionName((int)action));
                    }
                    else
                    {
                        ImGui.SetTooltip($"Invalid action id ({actionAt})");
                    }
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
                    loadedActions.InsertActionAt(dndTarget.Value + 1, loadedActions.GetActionAt(actionDragAndDrop.Value));
                    loadedActions.RemoveActionAt(actionDragAndDrop.Value);
                }

                actionDragAndDrop = null;
            }
        }

        ImGuiHelpers.ScaledDummy(0);
    }

    private void DrawOpenerLoaderTab()
    {
        using var tabItem = ImRaii.TabItem("Loader");
        if (!tabItem.Success)
            return;

        using var child = ImRaii.Child("###LoadOpeners");
        if (!child.Success)
            return;

        var defaultOpeners = OpenerManager.Instance.GetDefaultNames();
        customOpeners = OpenerManager.Instance.GetNames();

        using var tabBar = ImRaii.TabBar("###OpenersTab");
        if (tabBar.Success)
        {
            DrawOpeners(defaultOpeners, "Default", OpenerManager.Instance.GetDefaultOpener);
            DrawOpeners(customOpeners, "Saved", OpenerManager.Instance.GetOpener, true);
        }
    }

    private void DrawOpeners(
        List<Tuple<Jobs, List<string>>> openers, string prefix, Func<string, Jobs, List<int>> getOpener,
        bool delete = false)
    {
        using var tabItem = ImRaii.TabItem($"{prefix} Openers");
        if (!tabItem.Success)
            return;

        DrawJobCategoryFilters();

        foreach (var openerJob in openers)
        {
            if (JobsExtensions.FilterBy(jobCategoryFilter, openerJob.Item1))
            {
                Helper.CollapsingHeader($"{prefix} {openerJob.Item1} Openers", () =>
                {
                    foreach (var opener in openerJob.Item2)
                    {
                        ImGui.TextUnformatted(opener);
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
        }
    }

    private void DrawCreatorTab()
    {
        using var tabItem = ImRaii.TabItem("Creator");
        if (!tabItem.Success)
            return;

        using var child = ImRaii.Child("###AllActions", new Vector2(0, ImGui.GetContentRegionAvail().Y - ImGui.GetStyle().WindowPadding.Y));
        if (!child.Success)
            return;

        ImGui.InputText("Opener name", ref loadedActions.Name, 32);

        ListFilter("Job filter", jobFilter, JobsExtensions.PrettyPrint, ref jobFilter);
        ListFilter("Action type filter", actionTypeFilter, ActionTypesExtension.PrettyPrint, ref actionTypeFilter);

        // Search bar
        if (ImGui.InputText("Search", ref searchAction, 32))
        {
            actionsIds = PvEActions.Instance.GetNonRepeatedActionsByName(searchAction, jobFilter, actionTypeFilter);
            actionsIds.AddRange(GroupOfActions.GetFilteredGroups(searchAction, jobFilter, actionTypeFilter));
        }

        ImGui.TextUnformatted($"{actionsIds.Count} Results");
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
                ImGui.TextUnformatted($"{PvEActions.Instance.GetAction((uint)actionId).Name}");
            else if (GroupOfActions.TryGetDefault(actionId, out var group))
                ImGui.TextUnformatted($"{group.Name}");
            else
                ImGui.TextUnformatted($"Invalid action id ({actionId})");
        }

        if (actionsIds.Count > 50)
            ImGui.TextUnformatted("More than 50 results, limiting results shown");
    }

    private void DrawRecordActionsTab()
    {
        using var tabItem = ImRaii.TabItem("Record Actions");
        if (!tabItem.Success)
            return;

        using var child = ImRaii.Child("###RecordActions");
        if (!child.Success)
            return;

        ImGui.TextUnformatted("Start a countdown, record your actions and compare them with your opener");
        ImGui.Spacing();
        if (ImGui.Button("Start Recording"))
        {
            loadedActions.ClearWrongActions();
            countdown.StartCountdown();
            recordingConfig.StartRecording(Plugin.Config.CountdownTime,
                                           AddFeedback,
                                           loadedActions.AddWrongActionAt,
                                           loadedActions.UpdateCurrentAction,
                                           Plugin.Config.IgnoreTrueNorth && !loadedActions.HasTrueNorth());
            Plugin.PluginLog.Info($"Is recording? {recordingConfig.IsRecording()}");
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
            ImGui.TextUnformatted("RECORDING");
        }

        foreach (var line in recordingConfig.GetFeedback())
            ImGui.TextUnformatted(line);
    }

    private static void DrawInfoTab()
    {
        using var tabItem = ImRaii.TabItem("Info");
        if (!tabItem.Success)
            return;

        ImGui.TextUnformatted("Supported actions' groups:");
        foreach (var groupsName in GroupOfActions.GroupsNames)
            ImGui.TextUnformatted($"- {groupsName}");

        using var child = ImRaii.Child("###Info");
        if (!child.Success)
            return;
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
            ImGui.TextUnformatted("Error saving opener. Make sure you have selected your job and named the opener.");
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
            using var color = ImRaii.PushColor(ImGuiCol.Button, ImGui.GetColorU32(ImGuiCol.TabActive), active);
            if (ImGui.Button(label))
            {
                jobCategoryFilter = JobsExtensions.Toggle(jobCategoryFilter, jobCategory);
                jobRoleFilterColour[jobCategory] = !active;
            }
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

                var vtx = (ushort)drawList.VtxCurrentIdx;
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
            drawList.PushTextureID(IActionManager.GetUnknownActionTexture.GetWrapOrEmpty().Handle);
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
        using var combo = ImRaii.Combo(label, prettyPrint(filter));
        if (!combo.Success)
            return;

        foreach (TA value in Enum.GetValues(typeof(TA)))
        {
            if (ImGui.Selectable(prettyPrint(value)))
            {
                state = value;
                actionsIds =
                    PvEActions.Instance.GetNonRepeatedActionsByName(searchAction, jobFilter, actionTypeFilter);
            }
        }
    }

    private static ImTextureID GetIcon(uint id)
    {
        return PvEActions.GetIconTexture(id).GetWrapOrEmpty().Handle;
    }
}
