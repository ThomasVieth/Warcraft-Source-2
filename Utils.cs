using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;
using System;

namespace WCS
{
    public class Utils
    {
        private WCS _plugin;

        public Func<string, IntPtr, IntPtr> CreateEntityByName;
        public Func<IntPtr, IntPtr, int> DispatchSpawn;

        public Utils(WCS plugin)
        {
            _plugin = plugin;
        }

        public void Initialize()
        {
            CreateEntityByName = VirtualFunction.Create<string, IntPtr, IntPtr>(GameData.GetSignature("UTIL_CreateEntityByName"));
            DispatchSpawn = VirtualFunction.Create<IntPtr, IntPtr, int>(GameData.GetSignature("DispatchSpawn"));
        }
    }
}
