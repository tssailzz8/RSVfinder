using System;
using System.Numerics;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;

namespace RSVfinder.Windows;

public class MainWindow : Window, IDisposable
{
 
    private Plugin Plugin;
    private ExcelSheet<TerritoryType> territorySheet = DalamudApi.DataManager.GetExcelSheet<TerritoryType>()!;

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
            ImGui.SetCursorPosX(10f);
            ImGui.Text($"{zoneID:000}:{territorySheet.GetRow(zoneID)?.Name.RawString}:{zoneID}包含{zoneData.RSVs.Count}条RSV数据,{zoneData.RSFs.Count}条RSF数据");
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
        ImGui.SetCursorPosY(ImGui.GetWindowHeight()-30f);
        if (ImGui.Button($"关闭"))
        {
            IsOpen = false;
        }
    }
}
