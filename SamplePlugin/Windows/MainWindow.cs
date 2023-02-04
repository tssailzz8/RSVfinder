using System;
using System.Data;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using Dalamud.Interface;
using Dalamud.Interface.Windowing;
using Dalamud.Logging;
using ImGuiNET;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;

namespace RSVfinder.Windows;

public class MainWindow : Window, IDisposable
{
 
    private Plugin Plugin;
    private ExcelSheet<TerritoryType> territorySheet = DalamudApi.DataManager.GetExcelSheet<TerritoryType>()!;

    private byte[] data = new byte[0x30];
    private byte[] data2 = new byte[0x404];
    private IntPtr ptr = Marshal.AllocHGlobal(1080);

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
        Marshal.FreeHGlobal(ptr);
    }

    public override unsafe void Draw()
    {
        ImGui.Text($"已储存:\n " +
                   $"{Plugin.Configuration.ZoneData.RSVs.Count} 组rsv记录\n " +
                   $"{Plugin.Configuration.ZoneData.RSFs.Count} 组rsf记录");
        ImGui.SameLine(ImGui.GetWindowWidth() - 80f);
        if (ImGui.Button($"重放"))
        {
            Plugin.Configuration.Save();
            foreach (var rsv in Plugin.Configuration.ZoneData.RSVs)
            {
                Marshal.Copy(Encoding.UTF8.GetBytes(rsv), 0, ptr, sizeof(Plugin.RSV_v62));
                Plugin.SendRSV((Plugin.RSV_v62*)ptr);
            }

            foreach (var rsf in Plugin.Configuration.ZoneData.RSFs)
            {
                Marshal.Copy(Encoding.UTF8.GetBytes(rsf), 0, ptr, sizeof(Plugin.RSFData));
                Plugin.SendRSF((Plugin.RSFData*)ptr);
            }
        }
        ImGui.SameLine();
        if (ImGui.Button($"清除"))
        {
            Plugin.Configuration.ZoneData.RSFs.Clear();
            Plugin.Configuration.ZoneData.RSVs.Clear();
            Plugin.Configuration.Save();
        }

        ImGuiHelpers.ScaledDummy(10f);
        if (ImGui.TreeNode($"RSV"))
        {
            
            foreach (var rsv in Plugin.Configuration.ZoneData.RSVs)
            {
                var str = Encoding.UTF8.GetBytes(rsv);
                Marshal.Copy(str,0,ptr,sizeof(Plugin.RSV_v62));
                var rsvdata = Marshal.PtrToStructure<Plugin.RSV_v62>(ptr);
                var i = 0;
                while (*(rsvdata.key + i) != 0) i++;
                ImGui.Text($"{Encoding.UTF8.GetString(rsvdata.key, i)} -> {Encoding.UTF8.GetString(rsvdata.value, 0x40)}");
            }
            ImGui.TreePop();

            
        }
        ImGui.Separator();

        ImGui.InputText($"Key", data, 0x30);
        ImGui.InputText($"Value", data2, 0x404);
        if (ImGui.Button($"新增"))
        {
            var rsv = new Plugin.RSV_v62();
            rsv.size = 0xC;
            Marshal.Copy(data,0,(IntPtr)rsv.key,0x30);
            Marshal.Copy(data2, 0, (IntPtr)rsv.value, 0x404);
            var json = Encoding.UTF8.GetString((byte*)&rsv,sizeof(Plugin.RSV_v62));
            Plugin.Configuration.ZoneData.RSVs.Add(json);
            Array.Clear(data);
            Array.Clear(data2);
            Plugin.Configuration.Save();

        }


        
        ImGui.SetCursorPosY(ImGui.GetWindowHeight()-30f);
        if (ImGui.Button($"关闭"))
        {
            IsOpen = false;
            Plugin.Configuration.Save();
        }
    }
}
