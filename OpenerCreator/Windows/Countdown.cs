using System;
using System.Diagnostics;
using System.Numerics;
using Dalamud.Game;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Interface.Textures;
using ImGuiNET;

namespace OpenerCreator.Windows;

// here be dragons
// ask Sevii

internal struct Countdown()
{
    private static readonly Vector2 CountdownNumberSize = new(240, 320);

    private readonly ISharedImmediateTexture countdownGo =
        OpenerCreator.TextureProvider.GetFromGame($"ui/icon/121000/{LanguageCode}/121841_hr1.tex");

    private readonly ISharedImmediateTexture countdownNumbers =
        OpenerCreator.TextureProvider.GetFromGame("ui/uld/ScreenInfo_CountDown_hr1.tex");

    private static readonly string LanguageCode = OpenerCreator.DataManager.Language switch
    {
        ClientLanguage.French => "fr",
        ClientLanguage.German => "de",
        ClientLanguage.Japanese => "ja",
        _ => "en"
    };

    private Stopwatch? countdownStart;

    internal void DrawCountdown()
    {
        if (OpenerCreator.Config.IsCountdownEnabled == false || countdownStart == null ||
            OpenerCreator.ClientState.LocalPlayer!.StatusFlags.ToString()
                         .Contains(StatusFlags.InCombat.ToString()))
            return;

        var foregroundDrawList = ImGui.GetForegroundDrawList();
        var timer = OpenerCreator.Config.CountdownTime - (countdownStart.ElapsedMilliseconds / 1000.0f);
        var ceil = (float)Math.Ceiling(timer);
        const float uSpacing = 1.0f / 6.0f;

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
                foregroundDrawList.AddImage(countdownGo.GetWrapOrEmpty().ImGuiHandle,
                                            center - (countdownGo.GetWrapOrEmpty().Size / 2),
                                            center + (countdownGo.GetWrapOrEmpty().Size / 2), Vector2.Zero, Vector2.One,
                                            color);
                break;
            case <= 5:
                foregroundDrawList.AddImage(countdownNumbers.GetWrapOrEmpty().ImGuiHandle,
                                            center - (CountdownNumberSize / 2),
                                            center + (CountdownNumberSize / 2), new Vector2(ceil * uSpacing, 0.0f),
                                            new Vector2((ceil * uSpacing) + uSpacing, 1.0f), color);
                break;
            default:
            {
                var dig1 = (int)Math.Floor(ceil / 10.0f);
                var dig2 = ceil % 10;
                foregroundDrawList.AddImage(countdownNumbers.GetWrapOrEmpty().ImGuiHandle,
                                            center - CountdownNumberSize with { Y = CountdownNumberSize.Y / 2 },
                                            center + new Vector2(0.0f, CountdownNumberSize.Y / 2),
                                            new Vector2(dig1 * uSpacing, 0.0f),
                                            new Vector2((dig1 * uSpacing) + uSpacing, 1.0f),
                                            color);
                foregroundDrawList.AddImage(countdownNumbers.GetWrapOrEmpty().ImGuiHandle,
                                            center - new Vector2(0.0f, CountdownNumberSize.Y / 2),
                                            center + CountdownNumberSize with { Y = CountdownNumberSize.Y / 2 },
                                            new Vector2(dig2 * uSpacing, 0.0f),
                                            new Vector2((dig2 * uSpacing) + uSpacing, 1.0f),
                                            color);
                break;
            }
        }
    }

    internal void StartCountdown()
    {
        countdownStart = Stopwatch.StartNew();
    }

    internal void StopCountdown()
    {
        countdownStart = null;
    }
}
