using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/*
 *  This file is part of CounterStrikeSharp.
 *  CounterStrikeSharp is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  CounterStrikeSharp is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with CounterStrikeSharp.  If not, see <https://www.gnu.org/licenses/>. *
 */

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Events;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;

namespace WCS.Races
{
    public class SkillPhoenix : WarcraftSkill
    {
        public override string InternalName => "phoenix";
        public override string DisplayName => "Phoenix";
        public override string Description => $"Respawn the first teammate that dies.";

        public override int MaxLevel => 8;
        public override int RequiredLevel => 0;

        public override void Load(WarcraftPlayer player)
        {
            Player = player;
        }
    }

    public class SkillArcaneBrilliance : WarcraftSkill
    {
        public override string InternalName => "arcane_brilliance";
        public override string DisplayName => "Arcane Brilliance";
        public override string Description => $"Give your entire friendly team increased $ upon spawning.";

        public override int MaxLevel => 8;
        public override int RequiredLevel => 0;

        public override void Load(WarcraftPlayer player)
        {
            Player = player;
        }
    }

    public class SkillIceBarrier : WarcraftSkill
    {
        public override string InternalName => "ice_barrier";
        public override string DisplayName => "Ice Barrier";
        public override string Description => $"Absorb 10 - 45 damage over 10 seconds. Ability.";

        public override int MaxLevel => 8;
        public override int RequiredLevel => 0;

        public override void Load(WarcraftPlayer player)
        {
            Player = player;
        }
    }

    public class SkillCuringRitual : WarcraftSkill
    {
        public override string InternalName => "levitation";
        public override string DisplayName => "Levitation";
        public override string Description => $"Sacrifice $100 to heal yourself. Ultimate.";

        public override int MaxLevel => 8;
        public override int RequiredLevel => 0;

        public override void Load(WarcraftPlayer player)
        {
            Player = player;
        }
    }

    public class RaceBloodElfMage : WarcraftRace
    {
        public override string InternalName => "blood_elf_mage";
        public override string DisplayName => "Blood Elf Mage";
        public override string Description => "Blood Elf Mage";

        public override int MaxLevel => 32;

        public override void Load(WarcraftPlayer player)
        {
            Player = player;

            AddSkill(new SkillPhoenix());
            AddSkill(new SkillArcaneBrilliance());
            AddSkill(new SkillIceBarrier());
            AddSkill(new SkillCuringRitual());
        }
    }
}