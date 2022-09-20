using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using System.IO;
using System.Reflection;
using Dalamud.Interface.Windowing;
using RSVfinder.Windows;
using System;
using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.Game;
using System.Security.Cryptography;
using Dalamud.Game.Network;
using System.Runtime.InteropServices;
using Dalamud.Logging;

namespace RSVfinder
{
    public sealed class Plugin : IDalamudPlugin
    {
        public string Name => "RSVfinder";
        private const string CommandName = "/pmycommand";

        private DalamudPluginInterface PluginInterface { get; init; }
        private CommandManager CommandManager { get; init; }
        public Configuration Configuration { get; init; }
        public WindowSystem WindowSystem = new("RSVfinder");
        public unsafe delegate long RSVDelegate2(long a1, long a2,long a3, int size);
        public static Hook<RSVDelegate2> RSVHook2;
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
            RSVHook2.Enable();
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


        private unsafe long RSVDe2(long a1, long key, long value, int size)
        {
            var b1 = Marshal.PtrToStringUTF8((IntPtr)key, 0x30);
            var b2= Marshal.PtrToStringUTF8((IntPtr)value, size);
            var a = $"{a1:x}:{size:x}:{b1}:{b2}";
            log.WriteLog(a);
            return RSVHook2.Original(a1, key, value, size);
        }


        public void Dispose()
        {
            this.WindowSystem.RemoveAllWindows();
            this.CommandManager.RemoveHandler(CommandName);
            RSVHook2.Disable();
            DalamudApi.Dispose();
            log.Dispose();

        }

        private void OnCommand(string command, string args)
        {
            // in response to the slash command, just display our main ui
            WindowSystem.GetWindow("My Amazing Window").IsOpen = true;
        }

        private void DrawUI()
        {
            this.WindowSystem.Draw();
        }

        public void DrawConfigUI()
        {
            WindowSystem.GetWindow("A Wonderful Configuration Window").IsOpen = true;
        }
    }
}
