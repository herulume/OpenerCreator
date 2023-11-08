using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Interface.Internal;
using Dalamud.Utility;
using ImGuiNET;
using OpenerCreator.Helpers;
using OpenerCreator.Managers;

// TODO: perhabs use custom countdown icons to have finer control and allow accurate countdown above 5

namespace OpenerCreator.Gui;

public class OpenerCreatorWindow : IDisposable
{
    public bool Enabled;

    private string name;
    private string search;
    private Jobs jobFilter;
    private int countdown;
    private bool showDefault;
    private Stopwatch? countdownStart;
    private bool recording;
    private List<string> feedback;
    private int? actionDnd;
    private List<uint> actions;
    private List<uint> filteredActions;
    private List<Tuple<Jobs, List<string>>> openers;
    private readonly HashSet<int> wrongActions;

    private readonly Action<int, Action<List<string>>, Action<int>> startRecording;
    private readonly Action stopRecording;

    private Dictionary<uint, IDalamudTextureWrap> iconCache;
    private IDalamudTextureWrap countdownNumbers;
    private IDalamudTextureWrap countdownGo;

    private static Vector2 iconSize = new(32);
    private static Vector2 countdownNumberSize = new(240, 320);

    public OpenerCreatorWindow(Action<int, Action<List<string>>, Action<int>> startRecording, Action stopRecording)
    {
        Enabled = false;

        name = "";
        search = "";
        jobFilter = Jobs.ANY;
        countdown = 7;
        recording = false;
        showDefault = false;
        feedback = new();
        actions = new();
        openers = OpenerManager.Instance.GetNames();
        filteredActions = Actions.Instance.NonRepeatedIdList();
        wrongActions = new();

        this.startRecording = startRecording;
        this.stopRecording = stopRecording;

        iconCache = new();
        countdownNumbers = Actions.Instance.GetTexture("ui/uld/ScreenInfo_CountDown_hr1.tex");
        var languageCode = OpenerCreator.DataManager.Language switch
        {
            Dalamud.ClientLanguage.French => "fr",
            Dalamud.ClientLanguage.German => "de",
            Dalamud.ClientLanguage.Japanese => "ja",
            _ => "en"
        };
        countdownGo = Actions.Instance.GetTexture($"ui/icon/121000/{languageCode}/121841_hr1.tex");
    }

    public void Dispose()
    {
        foreach (var v in iconCache)
            v.Value.Dispose();
        countdownNumbers.Dispose();
        countdownGo.Dispose();

        GC.SuppressFinalize(this);

    }

    public void Draw()
    {
        if (!Enabled)
            return;

        ImGui.SetNextWindowSizeConstraints(new Vector2(100, 100), new Vector2(4000, 2000));
        ImGui.Begin("Opener Creator", ref Enabled);
        DrawActionsGui();
        ImGui.BeginTabBar("OpenerCreatorMainTabBar");
        DrawOpenerLoader();
        DrawAbilityFilter();
        DrawRecordActions();
        ImGui.EndTabBar();
        ImGui.Spacing();
        ImGui.End();

        DrawCountdown();
    }

    public void DrawActionsGui()
    {
        var spacing = ImGui.GetStyle().ItemSpacing;
        var padding = ImGui.GetStyle().FramePadding;
        var iconsPerLine = (int)Math.Floor((ImGui.GetContentRegionAvail().X - padding.X * 2.0 + spacing.X) / (iconSize.X + spacing.X));
        var lines = (float)Math.Max(Math.Ceiling(actions.Count / (float)iconsPerLine), 1);
        ImGui.BeginChildFrame(2426787, new Vector2(ImGui.GetContentRegionAvail().X, lines * (iconSize.Y + spacing.Y) - spacing.Y + padding.Y * 2), ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);

        int? dndTarget = null;
        if (actionDnd != null)
        {
            var pos = ImGui.GetMousePos() - ImGui.GetCursorScreenPos();
            var x = (int)Math.Floor(pos.X / (iconSize.X + spacing.X));
            var y = (int)Math.Floor(pos.Y / (iconSize.Y + spacing.Y));
            dndTarget = Math.Clamp(y * iconsPerLine + x, 0, actions.Count - 1);
        }

        int? delete = null;
        for (var i = 0; i < actions.Count + (actionDnd == null ? 0 : 1); i++)
        {
            if (i > 0)
            {
                ImGui.SameLine();
                if (ImGui.GetContentRegionAvail().X < iconSize.X)
                    ImGui.NewLine();
            }

            if ((dndTarget <= actionDnd && dndTarget == i) || (dndTarget > actionDnd && dndTarget == i - 1))
            {
                ImGui.Image(GetIcon(actions[actionDnd!.Value]), iconSize, Vector2.Zero, Vector2.One, new Vector4(255, 255, 255, 100));

                if (actionDnd != i)
                {
                    ImGui.SameLine();
                    if (ImGui.GetContentRegionAvail().X < iconSize.X)
                        ImGui.NewLine();
                }
            }

            if (actionDnd != i && i < actions.Count)
            {
                var color = this.wrongActions.Contains(i) ? new Vector4(255, 0, 0, 255) : new Vector4(255, 255, 255, 255);
                ImGui.Image(GetIcon(actions[i]), iconSize, Vector2.Zero, Vector2.One, color);
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
            drawlist.PrimRectUV(pos, pos + iconSize, Vector2.Zero, Vector2.One, 0xFFFFFFFF);
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

    public void DrawOpenerLoader()
    {
        if (!ImGui.BeginTabItem("Loader"))
            return;

        ImGui.BeginChild("loadopener");
        DrawClear();

        var defaultOpeners = OpenerManager.Instance.GetDefaultNames();
        openers = OpenerManager.Instance.GetNames();
        ImGui.Checkbox("Default Openers", ref showDefault);
        if (showDefault)
        {
            DrawOpeners(defaultOpeners, "Default", OpenerManager.Instance.GetDefaultOpener);

        }
        DrawOpeners(openers, "Saved", OpenerManager.Instance.GetOpener, true);


        ImGui.EndChild();
        ImGui.EndTabItem();
    }

    private void DrawOpeners(List<Tuple<Jobs, List<string>>> openers, string prefix, Func<string, Jobs, List<uint>> getOpener, bool delete = false)
    {
        foreach (var openerJob in openers)
        {
            CollapsingHeader($"{prefix} {openerJob.Item1} Openers", () =>
            {
                foreach (var opener in openerJob.Item2)
                {

                    ImGui.Text(opener);
                    ImGui.SameLine();
                    if (ImGui.Button($"Load##{prefix}#{opener}"))
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
    }

    private void DrawAbilityFilter()
    {
        if (!ImGui.BeginTabItem("Creator"))
            return;

        ImGui.BeginChild("allactions", new Vector2(0, ImGui.GetContentRegionAvail().Y - ImGui.GetStyle().WindowPadding.Y));


        // Save opener
        if (ImGui.Button("Save") && !name.IsNullOrEmpty())
        {
            OpenerManager.Instance.AddOpener(name, jobFilter, actions);
            OpenerManager.Instance.SaveOpeners();
        }
        ImGui.SameLine();
        ImGui.InputText("Opener name", ref name, 32);
        //  Filter by job
        if (ImGui.BeginCombo($"Job Filter", jobFilter.ToString()))
        {
            foreach (Jobs job in Enum.GetValues(typeof(Jobs)))
            {
                if (ImGui.Selectable(job.ToString()))
                {
                    jobFilter = job;
                    filteredActions = Actions.Instance.GetNonRepeatedActionsByName(search, jobFilter);
                }
            }
            ImGui.EndCombo();
        }
        // Search bar
        if (ImGui.InputText("Search", ref search, 64))
        {
            if (search.Length > 0)
                filteredActions = Actions.Instance.GetNonRepeatedActionsByName(search, jobFilter);
            else
                filteredActions = Actions.Instance.NonRepeatedIdList();
        }

        ImGui.Text($"{filteredActions.Count} Results");
        ImGui.SameLine();
        DrawClear();
        ImGui.SameLine();
        if (ImGui.Button("Add catch-all action"))
        {
            actions.Add(0);
        }

        for (var i = 0; i < Math.Min(20, filteredActions.Count); i++)
        {
            var action = Actions.Instance.GetAction(filteredActions[i]);
            if (ImGui.ImageButton(GetIcon(filteredActions[i]), iconSize))
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
        if (ImGui.InputInt("Countdown timer", ref countdown))
            countdown = Math.Clamp(countdown, 0, 30);

        if (ImGui.Button("Start Recording"))
        {
            this.feedback.Clear();
            this.wrongActions.Clear();
            this.recording = true;
            this.countdownStart = Stopwatch.StartNew();
            startRecording(countdown, AddFeedback, WrongAction);
        }
        ImGui.SameLine();
        if (ImGui.Button("Stop Recording"))
        {
            this.recording = false;
            this.countdownStart = null;
            stopRecording();
        }
        if (recording)
        {
            ImGui.SameLine();
            ImGui.Text("RECORDING");
        }

        foreach (var line in feedback)
        {
            ImGui.Text(line);
        }

        ImGui.EndChild();
        ImGui.EndTabItem();
    }

    private void DrawCountdown()
    {
        if (countdownStart == null || OpenerCreator.ClientState.LocalPlayer!.StatusFlags.ToString().Contains(StatusFlags.InCombat.ToString()))
            return;

        var drawlist = ImGui.GetForegroundDrawList();
        var timer = countdown - countdownStart.ElapsedMilliseconds / 1000.0f;
        var ceil = (float)Math.Ceiling(timer);
        var uspacing = 1.0f / 6.0f;

        if (timer <= 0)
            ceil = 0;
        if (timer > 5)
            ceil = (int)Math.Ceiling(timer / 5.0) * 5.0f;

        var anim = 1.0f - Math.Clamp((ceil - timer) - 0.5f, 0.0f, 1.0f);
        var color = 0x00FFFFFF + ((uint)(anim * 255) << 24);

        if (timer < -2)
        {
            countdownStart = null;
            return;
        }

        var center = ImGui.GetIO().DisplaySize / 2;
        if (timer <= 0)
            drawlist.AddImage(countdownGo.ImGuiHandle, center - countdownGo.Size / 2, center + countdownGo.Size / 2, Vector2.Zero, Vector2.One, color);
        else if (timer <= 5)
            drawlist.AddImage(countdownNumbers.ImGuiHandle, center - countdownNumberSize / 2, center + countdownNumberSize / 2, new(ceil * uspacing, 0.0f), new(ceil * uspacing + uspacing, 1.0f), color);
        else
        {
            var dig1 = (int)Math.Floor(ceil / 10.0f);
            var dig2 = ceil % 10;
            drawlist.AddImage(countdownNumbers.ImGuiHandle, center - new Vector2(countdownNumberSize.X, countdownNumberSize.Y / 2), center + new Vector2(0.0f, countdownNumberSize.Y / 2), new(dig1 * uspacing, 0.0f), new(dig1 * uspacing + uspacing, 1.0f), color);
            drawlist.AddImage(countdownNumbers.ImGuiHandle, center - new Vector2(0.0f, countdownNumberSize.Y / 2), center + new Vector2(countdownNumberSize.X, countdownNumberSize.Y / 2), new(dig2 * uspacing, 0.0f), new(dig2 * uspacing + uspacing, 1.0f), color);
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

    private void WrongAction(int i)
    {
        this.wrongActions.Add(i);
    }

    public void AddFeedback(List<string> feedback)
    {
        this.countdownStart = null;
        this.recording = false;
        this.feedback = feedback;
    }

    private nint GetIcon(uint id)
    {
        if (!iconCache.ContainsKey(id))
            iconCache[id] = Actions.Instance.GetIconTexture(id);
        return iconCache[id].ImGuiHandle;
    }

    private static void CollapsingHeader(string label, Action action)
    {
        if (ImGui.CollapsingHeader(label, ImGuiTreeNodeFlags.DefaultOpen))
        {
            action();
        }
    }
}
