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
using WCS.Races;
using WCS.API;

namespace WCS.BaseRacePack.Races
{
    public class SkillCriticalStrike : WarcraftSkill
    {
        public override string InternalName => "vampiric_aura";
        public override string DisplayName => "Vampiric Aura";
        public override string Description => $"Heal for {ChatColors.Green}10-80%{ChatColors.Default} of damage dealt to enemies.";

        public override int MaxLevel => 8;
        public override int RequiredLevel => 0;

        public override void Load(IWarcraftPlayer player)
        {
            Player = player;
        }
    }

    public class SkillCriticalGrenade : WarcraftSkill
    {
        public override string InternalName => "unholy_aura";
        public override string DisplayName => "Unholy Aura";
        public override string Description => $"Move {ChatColors.Blue}8-64%{ChatColors.Default} faster. Lower your HP to increase further.";

        public override int MaxLevel => 8;
        public override int RequiredLevel => 0;

        public override void Load(IWarcraftPlayer player)
        {
            Player = player;
        }
    }

    public class SkillReincarnation : WarcraftSkill
    {
        public override string InternalName => "levitation";
        public override string DisplayName => "Levitation";
        public override string Description => $"Experience {ChatColors.LightBlue}93-46%{ChatColors.Default} of gravity. Half damage taken while in the air.";

        public override int MaxLevel => 8;
        public override int RequiredLevel => 0;

        public override void Load(IWarcraftPlayer player)
        {
            Player = player;
        }
    }

    public class SkillChainLightning : WarcraftSkill
    {
        public override string InternalName => "levitation";
        public override string DisplayName => "Levitation";
        public override string Description => $"Experience {ChatColors.LightBlue}93-46%{ChatColors.Default} of gravity. Half damage taken while in the air.";

        public override int MaxLevel => 8;
        public override int RequiredLevel => 0;

        public override void Load(IWarcraftPlayer player)
        {
            Player = player;
        }
    }

    public class RaceOrcishHorde : WarcraftRace
    {
        public override string InternalName => "orcish_horde";
        public override string DisplayName => "Orcish Horde";
        public override string Description => "Orcish Horde";

        public override int MaxLevel => 32;

        public override void Load(IWarcraftPlayer player)
        {
            Player = player;

            AddSkill(new SkillCriticalStrike());
            AddSkill(new SkillCriticalGrenade());
            AddSkill(new SkillReincarnation());
            AddSkill(new SkillChainLightning());
        }
    }
}