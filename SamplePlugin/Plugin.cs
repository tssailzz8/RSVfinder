using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Interface.Windowing;
using RSVfinder.Windows;
using System;
using Dalamud.Hooking;
using System.Runtime.InteropServices;

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
        public unsafe delegate long RSVDelegate2(IntPtr a1, IntPtr a2,IntPtr a3, int size);
        public unsafe delegate byte RSFDelegate(IntPtr a1, IntPtr a2, IntPtr a3);

        public static Hook<RSVDelegate2> RSVHook2;
        public static Hook<RSFDelegate> RSFHook;
        public IntPtr RSVa1;
        public IntPtr RSFa1;


        public Log log;
        public Plugin(
            [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
            [RequiredVersion("1.0")] CommandManager commandManager)
        {
            DalamudApi.Initialize(this, pluginInterface);
            this.PluginInterface = pluginInterface;
            this.CommandManager = commandManager;

            this.Configuration = this.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            this.Configuration.Initialize(this.PluginInterface);
            RSVHook2 = Hook<RSVDelegate2>.FromAddress(DalamudApi.SigScanner.ScanText(" E9 ?? ?? ?? ?? CC CC CC CC CC CC CC CC CC 48 8B 11 "), RSVDe2);
            RSVa1 = DalamudApi.SigScanner.GetStaticAddressFromSig(
                "E9 ?? ?? ?? ?? CC CC CC CC CC CC CC CC CC 48 8B 11 ");
            RSFHook = Hook<RSFDelegate>.FromAddress(DalamudApi.SigScanner.ScanText("48 89 5C 24 10 48 89 74 24 18 57 48 83 EC 40 48 83 B9 20 02 00 00 00"),RSFReceiver);
            RSFa1 = DalamudApi.SigScanner.GetStaticAddressFromSig(
                "48 89 5C 24 10 48 89 74 24 18 57 48 83 EC 40 48 83 B9 20 02 00 00 00");


            RSVHook2?.Enable();
            RSFHook?.Enable();

            WindowSystem.AddWindow(new ConfigWindow(this));
            WindowSystem.AddWindow(new MainWindow(this));
            log = new(DalamudApi.PluginInterface.ConfigDirectory);
            log.WriteLog($"RSVa1={RSVa1:X} RSFa1={RSFa1:X}");
            this.CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
            {
                HelpMessage = "A useful message to display in /xlhelp"
            });

            this.PluginInterface.UiBuilder.Draw += DrawUI;
            this.PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;
        }
        [Serializable]
        [StructLayout(LayoutKind.Explicit, Size = structSize, Pack = 1)]
        internal unsafe struct RSV_v62
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


        private unsafe long RSVDe2(IntPtr a1, IntPtr key, IntPtr value, int size)
        {
            var b1 = Marshal.PtrToStringUTF8((IntPtr)key, 0x30);
            var b2= Marshal.PtrToStringUTF8((IntPtr)value, size);
            var a = $"{a1:x}:{size:x}:{b1}:{b2}";

            var rsv = new RSV(b1, b2, size);
            var zone = DalamudApi.ClientState.TerritoryType;
            if (Configuration.ZoneData.TryGetValue(zone,out var zoneData))
            {
                if (!zoneData.RSVs.Contains(rsv)) {
                    zoneData.RSVs.Add(rsv);
                }
            }
            else
            {
                Configuration.ZoneData.Add(zone, new ZoneData());
                Configuration.ZoneData[zone].RSVs.Add(rsv);
            }

            log.WriteLog(a);
            return RSVHook2.Original(a1, key, value, size);
        }

        public unsafe byte RSFReceiver(IntPtr a1, IntPtr a2, IntPtr a3)
        {
            var b1 = new byte[8];
            var b2 = new byte[64];
            Marshal.Copy(a2, b1, 0, 8);
            Marshal.Copy(a3, b2, 0, 64);
            var str = $"RSF:{b1:X8}:";
            foreach (var b in b2)
            {
                str += $"{b:X2}";
            }

            var rsf = new RSF(b1, b2);
            var zone = DalamudApi.ClientState.TerritoryType;
            if (Configuration.ZoneData.TryGetValue(zone, out var zoneData))
            {
                if (!zoneData.RSFs.Contains(rsf))
                {
                    zoneData.RSFs.Add(rsf);
                }
            }
            else
            {
                Configuration.ZoneData.Add(zone, new ZoneData());
                Configuration.ZoneData[zone].RSFs.Add(rsf);
            }

            log.WriteLog(str);
            return RSFHook.Original(a1, a2, a3);
        }

        public unsafe void SendRSV(byte[] key, byte[] value, int size)
        {
            var a2 = Marshal.AllocHGlobal(0x30);
            var a3 = Marshal.AllocHGlobal(0x404);
            Marshal.Copy(key,0,a2,0x30);
            Marshal.Copy(value,0,a3,0x404);

            RSVHook2.Original(RSVa1, a2, a3, size);

            Marshal.FreeHGlobal(a2);
            Marshal.FreeHGlobal(a3);
        }

        public unsafe void SendRSF(byte[] id, byte[] data)
        {
            var a2 = Marshal.AllocHGlobal(0x8);
            var a3 = Marshal.AllocHGlobal(0x40);
            Marshal.Copy(id, 0, a2, 0x8);
            Marshal.Copy(data, 0, a3, 0x40);

            RSFHook.Original(RSFa1, a2, a3);

            Marshal.FreeHGlobal(a2);
            Marshal.FreeHGlobal(a3);
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
