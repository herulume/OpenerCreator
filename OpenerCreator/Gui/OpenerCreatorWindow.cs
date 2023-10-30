using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
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
    private int countdown;
    private Stopwatch? countdownStart;
    private bool recording;
    private List<string> openers;
    private List<string> feedback;

    private List<uint> actions;
    private List<uint> filteredActions;

    private readonly Action<int, Action<List<string>>> onRunCommand;
    private Dictionary<uint, IDalamudTextureWrap> iconCache;
    private IDalamudTextureWrap countdownNumbers;
    private IDalamudTextureWrap countdownGo;

    private const int IconSize = 32;
    private static Vector2 countdownNumberSize = new(240, 320);

    public OpenerCreatorWindow(Action<int, Action<List<string>>> a)
    {
        Enabled = false;

        name = "";
        search = "";
        countdown = 7;
        recording = false;
        openers = new();
        feedback = new();

        actions = new();
        filteredActions = ActionDictionary.Instance.NonRepeatedIdList();

        onRunCommand = a;
        iconCache = new();
        countdownNumbers = ActionDictionary.Instance.GetTexture("ui/uld/ScreenInfo_CountDown_hr1.tex");
        var languageCode = OpenerCreator.DataManager.Language switch
        {
            Dalamud.ClientLanguage.French => "fr",
            Dalamud.ClientLanguage.German => "de",
            Dalamud.ClientLanguage.Japanese => "ja",
            _ => "en"
        };
        countdownGo = ActionDictionary.Instance.GetTexture($"ui/icon/121000/{languageCode}/121841_hr1.tex");
    }

    public void Dispose()
    {
        foreach (var v in iconCache)
            v.Value.Dispose();
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
        var icons_per_line = (int)Math.Floor((ImGui.GetContentRegionAvail().X - padding.X * 2.0 + spacing.X) / (IconSize + spacing.X));
        var lines = (float)Math.Max(Math.Ceiling(actions.Count / (float)icons_per_line), 1);
        ImGui.BeginChildFrame(2426787, new Vector2(ImGui.GetContentRegionAvail().X, lines * (IconSize + spacing.Y) - spacing.Y + padding.Y * 2), ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);

        int? delete = null;
        for (var i = 0; i < actions.Count; i++)
        {
            if (i > 0)
            {
                ImGui.SameLine();
                if (ImGui.GetContentRegionAvail().X < IconSize)
                    ImGui.NewLine();
            }

            var color = new Vector4(255, 0, 0, 255); // assign to this (abgr)
            if (i % 2 == 0)
                ImGui.Image(GetIcon(actions[i]), new Vector2(IconSize, IconSize), Vector2.Zero, Vector2.One, color);
            else
                ImGui.Image(GetIcon(actions[i]), new Vector2(IconSize, IconSize));

            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(ActionDictionary.Instance.GetActionName(actions[i]));
            if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                delete = i;
        }

        if (delete != null)
            actions.RemoveAt(delete.Value);

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
        foreach (var opener in defaultOpeners)
        {
            ImGui.Text(opener);
            ImGui.SameLine();
            if (ImGui.Button($"Load##{opener}"))
            {
                actions = OpenerManager.Instance.GetDefaultOpener(opener);
                OpenerManager.Instance.Loaded = actions;
            }
        }

        foreach (var opener in openers)
        {
            ImGui.Text(opener);
            ImGui.SameLine();
            if (ImGui.Button($"Load##{opener}"))
            {
                actions = OpenerManager.Instance.GetOpener(opener);
                OpenerManager.Instance.Loaded = actions;
            }
            ImGui.SameLine();
            if (ImGui.Button($"Delete##{opener}"))
            {
                OpenerManager.Instance.DeleteOpener(opener);
                OpenerManager.Instance.SaveOpeners();

            }
        }

        ImGui.EndChild();
        ImGui.EndTabItem();
    }

    private void DrawAbilityFilter()
    {
        if (!ImGui.BeginTabItem("Creator"))
            return;

        ImGui.BeginChild("allactions");
        if (ImGui.InputText("Search", ref search, 64))
        {
            if (search.Length > 0)
                filteredActions = ActionDictionary.Instance.GetNonRepeatedActionsByName(search);
            else
                filteredActions = ActionDictionary.Instance.NonRepeatedIdList();
        }

        ImGui.Text($"{filteredActions.Count} Results");
        ImGui.SameLine();
        DrawClear();
        ImGui.SameLine();
        if (ImGui.Button("Save") && !name.IsNullOrEmpty())
        {
            OpenerManager.Instance.AddOpener(name, actions);
            OpenerManager.Instance.SaveOpeners();
        }
        ImGui.SameLine();
        ImGui.InputText("Opener name", ref name, 32);


        for (var i = 0; i < Math.Min(20, filteredActions.Count); i++)
        {
            var action = ActionDictionary.Instance.GetAction(filteredActions[i]);
            ImGui.Image(GetIcon(filteredActions[i]), new Vector2(IconSize, IconSize));
            if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
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
            this.recording = true;
            this.countdownStart = Stopwatch.StartNew();
            onRunCommand(countdown, AddFeedback);
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
        if (countdownStart == null)
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
        }
    }

    public void AddFeedback(List<string> feedback)
    {
        this.recording = false;
        this.feedback = feedback;
    }

    private nint GetIcon(uint id)
    {
        if (!iconCache.ContainsKey(id))
            iconCache[id] = ActionDictionary.Instance.GetIconTexture(id);
        return iconCache[id].ImGuiHandle;
    }
}
