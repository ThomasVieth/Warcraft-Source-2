using CounterStrikeSharp.API.Modules.Events;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WCS.API
{   
    public interface IWarcraftSkill
    {
        string InternalName { get; }
        string DisplayName { get; }
        string Description { get; }
        int Level { get; set; }
        int MaxLevel { get; }
        int RequiredLevel { get; }
        IWarcraftPlayer Player { get; set; }
        void Load(IWarcraftPlayer player);
        void InvokeEvent(string eventName, GameEvent @event);
        void InvokeAbility(int abilityIndex);
        void InvokeVirtual(string eventName, DynamicHook hookData);
    }
}
