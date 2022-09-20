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

namespace RSVfinder
{
    public sealed class Plugin : IDalamudPlugin
    {
        public string Name => "Sample Plugin";
        private const string CommandName = "/pmycommand";

        private DalamudPluginInterface PluginInterface { get; init; }
        private CommandManager CommandManager { get; init; }
        public Configuration Configuration { get; init; }
        public WindowSystem WindowSystem = new("RSVfinder");
        public unsafe delegate long RSVDelegate1(long a1);
        public unsafe delegate long RSVDelegate2(long a1, long a2,long a3,long a4);
        public static Hook<RSVDelegate1> RSVHook1;
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

            RSVHook1 = Hook<RSVDelegate1>.FromAddress(DalamudApi.SigScanner.ScanText("44 8B 09 4C 8D 41 34"), RSVDe1) ;
            RSVHook1.Enable();
            RSVHook2 = Hook<RSVDelegate2>.FromAddress(DalamudApi.SigScanner.ScanText(" E9 ?? ?? ?? ?? CC CC CC CC CC CC CC CC CC 48 8B 11 "), RSVDe2);
            RSVHook2.Enable();
            WindowSystem.AddWindow(new ConfigWindow(this));
            WindowSystem.AddWindow(new MainWindow(this));
            DalamudApi.GameNetwork.NetworkMessage += OnNetworkMessage;
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
            public uint unknown1;
            [FieldOffset(0x4)]
            public fixed byte key[keySize];
            [FieldOffset(0x34)]
            public fixed byte value[valueSize];

            public override string ToString()
            {
                fixed (byte* key = this.key) fixed (byte* value = this.value)
                {
                    return
                        $"{unknown1:X8}|" +
                        $"{GetStringFromBytes(key, keySize)}|" +
                        $"{GetStringFromBytes(value, valueSize)}";
                }
            }
        }
        public unsafe static string GetStringFromBytes(byte* source, int size)
        {
            var bytes = new byte[size];
            Marshal.Copy((IntPtr)source, bytes, 0, size);
            var realSize = 0;
            for (var i = 0; i < size; i++)
            {
                if (bytes[i] != 0)
                {
                    continue;
                }
                realSize = i;
                break;
            }
            Array.Resize(ref bytes, realSize);
            return System.Text.Encoding.UTF8.GetString(bytes);
        }
        private void OnNetworkMessage(IntPtr dataPtr, ushort opCode, uint sourceActorId, uint targetActorId, NetworkMessageDirection direction)
        {
            if (direction == NetworkMessageDirection.ZoneDown)
            {
                if (opCode== 0x03D6)
                {
                    var data=Marshal.PtrToStructure<RSV_v62>(dataPtr);
                    var a = $"网络日志{dataPtr:x}:{data.ToString()}";
                    log.WriteLog(a);
                }
            }
        }

        private long RSVDe2(long a1, long a2, long a3, long a4)
        {
            var a = $"第二个hook参数为{a1:x}:{a2:x}:{a3:x}:{a4:x}";
            return RSVHook2.Original(a1,a2,a3,a4);
        }

        private unsafe long RSVDe1(long a1)
        {
            var data = Marshal.PtrToStructure<RSV_v62>((IntPtr)a1);
            var a = $"第一个内存hook{a1:x}:{(long)data.key:x}:{(long)data.value:x}:{data.ToString()}";
            log.WriteLog(a);
            return RSVHook1.Original(a1);
        }

        public void Dispose()
        {
            this.WindowSystem.RemoveAllWindows();
            this.CommandManager.RemoveHandler(CommandName);
            RSVHook2.Disable();
            RSVHook1.Dispose();
            DalamudApi.Dispose();
            log.Dispose();
            DalamudApi.GameNetwork.NetworkMessage -= OnNetworkMessage;
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
