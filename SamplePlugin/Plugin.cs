using Dalamud.Game.Command;
using Dalamud.Hooking;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Logging;
using Dalamud.Plugin;
using RSVfinder.Windows;
using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace RSVfinder
{
    public sealed class Plugin : IDalamudPlugin
    {
        public string Name => "RSVfinder";
        private const string CommandName = "/rsv";

        private DalamudPluginInterface PluginInterface { get; init; }
        private CommandManager CommandManager { get; init; }
        public Configuration Configuration { get; init; }
        public WindowSystem WindowSystem = new("RSVfinder");
        public unsafe delegate long RSVDelegate2(RSV_v62* a1);
        public unsafe delegate byte RSFDelegate(RSFData* a1);

        public static Hook<RSVDelegate2> RSVHook2;
        public static Hook<RSFDelegate> RSFHook;


        public Log log;
        public unsafe Plugin(
            [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
            [RequiredVersion("1.0")] CommandManager commandManager)
        {
            DalamudApi.Initialize(this, pluginInterface);
            this.PluginInterface = pluginInterface;
            this.CommandManager = commandManager;

            this.Configuration = this.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            this.Configuration.Initialize(this.PluginInterface);
            RSVHook2 = Hook<RSVDelegate2>.FromAddress(DalamudApi.SigScanner.ScanText("44 8B 09 4C 8D 41 34"), RSVDe2);
            //RSVa1 = DalamudApi.SigScanner.GetStaticAddressFromSig(
            //    "48 8B 0D ?? ?? ?? ?? E9 ?? ?? ?? ?? CC CC CC CC CC CC CC CC CC 48 8B 11");
            RSFHook = Hook<RSFDelegate>.FromAddress(DalamudApi.SigScanner.ScanText("48 8B 11 4C 8D 41 08"),RSFReceiver);
            //RSFa1 = DalamudApi.SigScanner.GetStaticAddressFromSig(
            //    "48 8B 0D ?? ?? ?? ?? E9 ?? ?? ?? ?? CC CC CC CC CC CC CC CC CC CC CC CC CC 4C 8B C2");


            RSVHook2?.Enable();
            RSFHook?.Enable();

            WindowSystem.AddWindow(new ConfigWindow(this));
            WindowSystem.AddWindow(new MainWindow(this));
            log = new(DalamudApi.PluginInterface.ConfigDirectory);

            this.CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
            {
                HelpMessage = "A useful message to display in /xlhelp"
            });

            this.PluginInterface.UiBuilder.Draw += DrawUI;
            this.PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;
        }
        [Serializable]
        [StructLayout(LayoutKind.Explicit, Size = structSize, Pack = 1)]
        public unsafe struct RSV_v62
        {
            public const int structSize = 1080;
            public const int keySize = 0x30;
            public const int valueSize = 0x404;
            [FieldOffset(0x0)]
            public uint size;
            [FieldOffset(0x4)]
            public fixed byte key[keySize];
            [FieldOffset(0x34)]
            public fixed byte value[valueSize];
        }


        private unsafe long RSVDe2(RSV_v62* a1)
        {
            //var data = Marshal.PtrToStructure<RSV_v62>(a1);
            PluginLog.Log(
                $"RSV:{Encoding.UTF8.GetString(a1->key, 0x30)}:{Encoding.UTF8.GetString(a1->key, (int)a1->size)}");

            var data = Encoding.UTF8.GetString((byte*)a1, sizeof(RSV_v62));
            if (!Configuration.ZoneData.RSVs.Contains(data))
            {
                Configuration.ZoneData.RSVs.Add(data);
                DalamudApi.ChatGui.Print($"New RSV:{Encoding.UTF8.GetString(a1->value,(int)a1->size)}");
            }

            return RSVHook2.Original(a1);
        }

        [Serializable]
        [StructLayout(LayoutKind.Explicit, Size = structSize, Pack = 1)]
        public unsafe struct RSFData
        {
            public const int structSize = 0x8+0x40+0x20;
            public const int keySize = 0x8;
            public const int valueSize = 0x40;
            [FieldOffset(0x0)]
            public fixed byte key[keySize];
            [FieldOffset(keySize)]
            public fixed byte value[valueSize];
        }

        public unsafe byte RSFReceiver(RSFData* a1)
        {
            //var data = JsonSerializer.Serialize(a1);
            var data = Encoding.UTF8.GetString((byte*)a1, sizeof(RSFData));
            if (!Configuration.ZoneData.RSFs.Contains(data))
            {
                Configuration.ZoneData.RSFs.Add(data);
            }
            
            return RSFHook.Original(a1);
        }

        public unsafe void SendRSV(RSV_v62* data)
        {
            try
            {
                RSVHook2.Original(data);
            }
            catch (Exception e)
            {
                PluginLog.Error($"{e}");
            }
        }

        public unsafe void SendRSF(RSFData* data)
        {
            try
            {
                RSFHook.Original(data);
            }
            catch (Exception e)
            {
                PluginLog.Error($"{e}");
            }

        }


        public void Dispose()
        {
            this.WindowSystem.RemoveAllWindows();
            this.CommandManager.RemoveHandler(CommandName);
            RSVHook2?.Disable();
            RSFHook?.Dispose();
            Configuration.Save();
            DalamudApi.Dispose();
            log.Dispose();

        }

        private void OnCommand(string command, string args)
        {
            // in response to the slash command, just display our main ui
            WindowSystem.GetWindow("RSVfinder").IsOpen = true;
        }

        private void DrawUI()
        {
            this.WindowSystem.Draw();
        }

        public void DrawConfigUI()
        {
            WindowSystem.GetWindow("RSVfinder").IsOpen = true;
        }
    }
}
