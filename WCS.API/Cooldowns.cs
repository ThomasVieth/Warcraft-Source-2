using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WCS.API
{
    public interface Cooldowns
    {
        public void AddCooldownExtension(CCSPlayerController controller, String cooldownName, Action<float> extension);

        public float GetCooldown(CCSPlayerController controller, String cooldownName);

        public void SetCooldown(CCSPlayerController controller, String cooldownName, float value);
    }
}
