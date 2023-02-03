using System;
using System.Numerics;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace RSVfinder.Windows;

public class MainWindow : Window, IDisposable
{
 
    private Plugin Plugin;

    public MainWindow(Plugin plugin) : base(
        "RSVfinder", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        this.SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(375, 330),
            MaximumSize = new Vector2(1920,1080)
        };

        this.Plugin = plugin;
    }

    public void Dispose()
    {

    }

    public override void Draw()
    {
        ImGui.Text($"RSVa1={Plugin.RSVa1:X} RSFa1={Plugin.RSFa1:X}");
        ImGui.Text($"已储存 {Plugin.Configuration.ZoneData.Count} 组Zone数据记录");
        var index = 0;
        foreach (var (zoneID,zoneData) in Plugin.Configuration.ZoneData)
        {
            
            ImGui.Text($"{index}:{zoneID}包含{zoneData.RSVs.Count}条RSV数据,{zoneData.RSFs.Count}条RSF数据");
            ImGui.SameLine(ImGui.GetWindowWidth()-50f);
            if (ImGui.Button($"重放###{index}"))
            {
                foreach (var rsv in zoneData.RSVs)
                {
                    Plugin.SendRSV(rsv.Key, rsv.Value, rsv.Size);
                }

                foreach (var rsf in zoneData.RSFs)
                {
                    Plugin.SendRSF(rsf.ID, rsf.Data);
                }
            }
            index++;
        }

        if (ImGui.Button($"关闭"))
        {
            IsOpen = false;
        }
    }
}
