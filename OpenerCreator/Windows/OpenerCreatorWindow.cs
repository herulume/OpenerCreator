using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using Dalamud.Game;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Windowing;
using Dalamud.Utility;
using ImGuiNET;
using OpenerCreator.Helpers;
using OpenerCreator.Managers;

namespace OpenerCreator.Windows;

public class OpenerCreatorWindow : Window, IDisposable
{
    private static readonly Vector2 IconSize = new(32);
    private static readonly Vector2 CountdownNumberSize = new(240, 320);
    private readonly ISharedImmediateTexture countdownGo;
    private readonly ISharedImmediateTexture countdownNumbers;
    
    private readonly Action<int, Action<Feedback>, Action<int>> startRecording;
    private readonly Action stopRecording;
    private readonly HashSet<int> wrongActions;
    private int? actionDnd;
    private List<uint> actions;
    private Stopwatch? countdownStart;
    private List<string> feedback;
    private List<uint> filteredActions;
    private JobCategory jobCategoryFilter = JobCategory.None;
    private Jobs jobFilter;

    private string name;
    private List<Tuple<Jobs, List<string>>> savedOpeners;
    private bool recording;
    private string search;

    public OpenerCreatorWindow(Action<int, Action<Feedback>, Action<int>> startRecording, Action stopRecording)
        : base("Opener Creator###ocrt", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
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
        filteredActions = Actions.Instance.NonRepeatedIdList();
        wrongActions = [];

        this.startRecording = startRecording;
        this.stopRecording = stopRecording;

        countdownNumbers = OpenerCreator.TextureProvider.GetFromGame("ui/uld/ScreenInfo_CountDown_hr1.tex");
        var languageCode = OpenerCreator.DataManager.Language switch
        {
            ClientLanguage.French => "fr",
            ClientLanguage.German => "de",
            ClientLanguage.Japanese => "ja",
            _ => "en"
        };
        countdownGo = OpenerCreator.TextureProvider.GetFromGame($"ui/icon/121000/{languageCode}/121841_hr1.tex");
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    public override void Draw()
    {
        //ImGui.SetNextWindowSizeConstraints(new Vector2(100, 100), new Vector2(4000, 2000));
        //ImGui.Begin("Opener Creator", ref Enabled);
        DrawActionsGui();
        ImGui.BeginTabBar("OpenerCreatorMainTabBar");
        DrawOpenerLoader();
        DrawAbilityFilter();
        DrawRecordActions();
        ImGui.EndTabBar();
        ImGui.Spacing();
       // ImGui.End();

        DrawCountdown();
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
                    ImGui.SetTooltip(Actions.Instance.GetActionName(actions[i]));
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

    private void DrawOpenerLoader()
    {
        if (!ImGui.BeginTabItem("Loader"))
            return;

        ImGui.BeginChild("loadopener");
        DrawClear();
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

    private void DrawAbilityFilter()
    {
        if (!ImGui.BeginTabItem("Creator"))
            return;

        ImGui.BeginChild("allactions",
                         new Vector2(0, ImGui.GetContentRegionAvail().Y - ImGui.GetStyle().WindowPadding.Y));


        // Save opener
        if (ImGui.Button("Save") && !name.IsNullOrEmpty())
        {
            OpenerManager.Instance.AddOpener(name, jobFilter, actions);
            OpenerManager.Instance.SaveOpeners();
        }

        ImGui.SameLine();
        ImGui.InputText("Opener name", ref name, 32);

        //  Filter by job
        if (ImGui.BeginCombo("Job Filter", jobFilter.ToString()))
        {
            foreach (Jobs job in Enum.GetValues(typeof(Jobs)))
                if (ImGui.Selectable(job.ToString()))
                {
                    jobFilter = job;
                    filteredActions = Actions.Instance.GetNonRepeatedActionsByName(search, jobFilter);
                }

            ImGui.EndCombo();
        }

        // Search bar
        if (ImGui.InputText("Search", ref search, 64))
            filteredActions = search.Length > 0
                                  ? Actions.Instance.GetNonRepeatedActionsByName(search, jobFilter)
                                  : Actions.Instance.NonRepeatedIdList();

        ImGui.Text($"{filteredActions.Count} Results");
        ImGui.SameLine();
        DrawClear();
        ImGui.SameLine();
        if (ImGui.Button("Add catch-all action")) actions.Add(0);

        for (var i = 0; i < Math.Min(20, filteredActions.Count); i++)
        {
            var action = Actions.Instance.GetAction(filteredActions[i]);
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

    private void DrawRecordActions()
    {
        if (!ImGui.BeginTabItem("Record Actions"))
            return;

        ImGui.BeginChild("recordactions");
        ImGui.Text("Start a countdown, record your actions and compare them with your opener.");

        if (ImGui.InputInt("Countdown timer", ref OpenerCreator.Config.CountdownTime))
        {
            OpenerCreator.Config.CountdownTime = Math.Clamp(OpenerCreator.Config.CountdownTime, 0, 30);
            OpenerCreator.Config.Save();
        }

        if (ImGui.Button("Start Recording"))
        {
            feedback.Clear();
            wrongActions.Clear();
            recording = true;
            countdownStart = Stopwatch.StartNew();
            startRecording(OpenerCreator.Config.CountdownTime, AddFeedback, WrongAction);
        }

        ImGui.SameLine();
        if (ImGui.Button("Stop Recording"))
        {
            recording = false;
            countdownStart = null;
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

    private void DrawCountdown()
    {
        if (countdownStart == null || OpenerCreator.ClientState.LocalPlayer!.StatusFlags.ToString()
                                                   .Contains(StatusFlags.InCombat.ToString()))
            return;

        var drawlist = ImGui.GetForegroundDrawList();
        var timer = OpenerCreator.Config.CountdownTime - (countdownStart.ElapsedMilliseconds / 1000.0f);
        var ceil = (float)Math.Ceiling(timer);
        const float uspacing = 1.0f / 6.0f;

        ceil = timer switch
        {
            <= 0 => 0,
            > 5 => (int)Math.Ceiling(timer / 5.0) * 5.0f,
            _ => ceil
        };

        var anim = 1.0f - Math.Clamp(ceil - timer - 0.5f, 0.0f, 1.0f);
        var color = 0x00FFFFFF + ((uint)(anim * 255) << 24);

        if (timer < -2)
        {
            countdownStart = null;
            return;
        }

        var center = ImGui.GetMainViewport().GetCenter();
        switch (timer)
        {
            case <= 0:
                drawlist.AddImage(countdownGo.GetWrapOrEmpty().ImGuiHandle, center - (countdownGo.GetWrapOrEmpty().Size / 2),
                                  center + (countdownGo.GetWrapOrEmpty().Size / 2), Vector2.Zero, Vector2.One, color);
                break;
            case <= 5:
                drawlist.AddImage(countdownNumbers.GetWrapOrEmpty().ImGuiHandle, center - (CountdownNumberSize / 2),
                                  center + (CountdownNumberSize / 2), new Vector2(ceil * uspacing, 0.0f),
                                  new Vector2((ceil * uspacing) + uspacing, 1.0f), color);
                break;
            default:
            {
                var dig1 = (int)Math.Floor(ceil / 10.0f);
                var dig2 = ceil % 10;
                drawlist.AddImage(countdownNumbers.GetWrapOrEmpty().ImGuiHandle,
                                  center - CountdownNumberSize with { Y = CountdownNumberSize.Y / 2 },
                                  center + new Vector2(0.0f, CountdownNumberSize.Y / 2),
                                  new Vector2(dig1 * uspacing, 0.0f), new Vector2((dig1 * uspacing) + uspacing, 1.0f),
                                  color);
                drawlist.AddImage(countdownNumbers.GetWrapOrEmpty().ImGuiHandle, center - new Vector2(0.0f, CountdownNumberSize.Y / 2),
                                  center + CountdownNumberSize with { Y = CountdownNumberSize.Y / 2 },
                                  new Vector2(dig2 * uspacing, 0.0f), new Vector2((dig2 * uspacing) + uspacing, 1.0f),
                                  color);
                break;
            }
        }
    }

    private void DrawClear()
    {
        if (ImGui.Button("Clear"))
        {
            actions.Clear();
            feedback.Clear();
            wrongActions.Clear();
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
            if (ImGui.Button(label)) jobCategoryFilter = JobsExtensions.Toggle(jobCategoryFilter, jobCategory);
        }
    }

    private void WrongAction(int i)
    {
        wrongActions.Add(i);
    }

    public void AddFeedback(Feedback f)
    {
        countdownStart = null;
        recording = false;
        feedback = f.GetMessages();
    }

    private static nint GetIcon(uint id)
    {
        return Actions.GetIconTexture(id).GetWrapOrEmpty().ImGuiHandle;
    }

    private static void CollapsingHeader(string label, Action action)
    {
        if (ImGui.CollapsingHeader(label, ImGuiTreeNodeFlags.DefaultOpen)) action();
    }
}
