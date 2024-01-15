using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WCS.API
{
    public interface Restrictions
    {
        public void Initialize(bool hotLoad);

        public void Restrict(CCSPlayerController controller, List<string> weapons);

        public void Unrestrict(CCSPlayerController controller, List<string> weapons);

        public void Clear(CCSPlayerController controller);

        public List<string> GetAllWeapons();
    }
}
