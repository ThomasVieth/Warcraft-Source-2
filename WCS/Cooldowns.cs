using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;
using System;
using System.Collections.Generic;

namespace WCS
{
    public class Cooldowns
    {
        private static Dictionary<IntPtr, Dictionary<String, float>> Players = new Dictionary<nint, Dictionary<string, float>>();
        private static Dictionary<IntPtr, Dictionary<String, List<Action<float>>>> Extensions = new Dictionary<nint, Dictionary<string, List<Action<float>>>>();

        private Timer _repeater;

        private static WCS _plugin = null;

        public Cooldowns()
        {
            if (_plugin == null)
            {
                Server.PrintToConsole($"{WCS.Instance.ModuleChatPrefix} Something went wrong loading Cooldown System.");
            }
        }

        public Cooldowns(WCS plugin)
        {
            _plugin = plugin;
        }

        public void Initialize()
        {
            _repeater = new Timer(0.25f, GameTickRepeater, TimerFlags.REPEAT);

            _plugin.RegisterEventHandler<EventRoundStart>(OnRoundStart);
        }

        private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo _)
        {
            var playerEntities = Utilities.FindAllEntitiesByDesignerName<CCSPlayerController>("cs_player_controller");
            foreach (var player in playerEntities)
            {
                if (!player.IsValid || !player.PawnIsAlive) continue;

                if (Players.ContainsKey(player.Handle))
                {
                    List<string> cooldownNames = new List<string>(Players[player.Handle].Keys);
                    foreach (string cooldownName in cooldownNames)
                    {
                        Players[player.Handle][cooldownName] = 5;
                    }
                }
            }

            return HookResult.Continue;
        }

        private void GameTickRepeater()
        {
            List<IntPtr> ptrs = new List<IntPtr>(Players.Keys);
            foreach (IntPtr handle in ptrs)
            {
                List<string> cooldownNames = new List<string>(Players[handle].Keys);
                foreach (string cooldownName in cooldownNames)
                {
                    float oldValue = Players[handle][cooldownName];

                    // If cooldown is 0, do not compute.
                    if (oldValue == 0) continue;

                    float newValue = Math.Max(0, oldValue - 0.25f);

                    Players[handle][cooldownName] = newValue;

                    if (Extensions.ContainsKey(handle))
                    {
                        if (Extensions[handle].ContainsKey(cooldownName))
                        {
                            foreach (Action<float> action in Extensions[handle][cooldownName])
                            {
                                action.Invoke(newValue);
                            }
                        }
                    }
                }
            }
        }

        public void AddCooldownExtension(CCSPlayerController controller, String cooldownName, Action<float> extension)
        {
            IntPtr identifier = controller.Handle;
            if (!Extensions.ContainsKey(identifier))
            {
                Extensions.Add(identifier, new Dictionary<string, List<Action<float>>>());
            }

            if (!Extensions[identifier].ContainsKey(cooldownName))
            {
                Extensions[identifier].Add(cooldownName, new List<Action<float>>());
            }

            Extensions[identifier][cooldownName].Add(extension);
        }

        public float GetCooldown(CCSPlayerController controller, String cooldownName)
        {
            IntPtr identifier = controller.Handle;
            if (!Players.ContainsKey(identifier))
            {
                Players.Add(identifier, new Dictionary<string, float>());
            }

            if (!Players[identifier].ContainsKey(cooldownName))
            {
                Players[identifier].Add(cooldownName, 0);
            }

            return Players[identifier][cooldownName];
        }

        public void SetCooldown(CCSPlayerController controller, String cooldownName, float value)
        {
            IntPtr identifier = controller.Handle;
            if (!Players.ContainsKey(identifier))
            {
                Players.Add(identifier, new Dictionary<string, float>());
            }

            if (!Players[identifier].ContainsKey(cooldownName))
            {
                Players[identifier].Add(cooldownName, 0);
            }

            Players[identifier][cooldownName] = value;
        }
    }
}
