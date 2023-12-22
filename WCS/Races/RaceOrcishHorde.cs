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

using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Timers;
using WCS.API;

namespace WCS.Races
{
    public class SkillCriticalStrike : WarcraftSkill
    {
        public override string InternalName => "critical_strike";
        public override string DisplayName => "Critical Strike";
        public override string Description => $"Chance {ChatColors.Green}8-40%{ChatColors.Default} to deal extra to enemies.";

        public override int MaxLevel => 8;
        public override int RequiredLevel => 0;

        public override void Load(IWarcraftPlayer player)
        {
            Player = player;

            HookVirtual("player_pre_hurt_other", PrePlayerHurtOther);
        }

        public void PrePlayerHurtOther(DynamicHook hookData)
        {
            if (Level == 0) return;

            int playerChance = 8 + (4 * Level);
            int chance = Convert.ToInt32(Random.Shared.NextSingle() * 100);
            CTakeDamageInfo damageInfo = hookData.GetParam<CTakeDamageInfo>(1);

            if (damageInfo.Inflictor.Value.DesignerName.Contains("grenade") || damageInfo.Inflictor.Value.DesignerName.Contains("projectile"))
                return;

            if (chance < playerChance)
            {
                damageInfo.Damage *= 2;
                hookData.SetParam(1, damageInfo);

                CEntityInstance victimEnt = hookData.GetParam<CEntityInstance>(0);
                CCSPlayerPawn victimPawn = new CCSPlayerPawn(victimEnt.Handle);

                Player.Controller.PrintToChat($"{WCS.Instance.ModuleChatPrefix}{ChatColors.Gold}Critical Strike {ChatColors.Default}on {ChatColors.Green}{victimPawn.Controller.Value.PlayerName}{ChatColors.Gold}!");
            }
        }
    }

    public class SkillCriticalGrenade : WarcraftSkill
    {
        public override string InternalName => "critical_grenade";
        public override string DisplayName => "Critical Grenade";
        public override string Description => $"Your HE grenades are {ChatColors.Blue}60-100%{ChatColors.Default} more effective.";

        public override int MaxLevel => 8;
        public override int RequiredLevel => 0;

        public override void Load(IWarcraftPlayer player)
        {
            Player = player;

            HookVirtual("player_pre_hurt_other", PrePlayerHurtOther);
        }

        public void PrePlayerHurtOther(DynamicHook hookData)
        {
            if (Level == 0) return;

            int playerChance = 8 + (4 * Level);
            int chance = Convert.ToInt32(Random.Shared.NextSingle() * 100);
            CTakeDamageInfo damageInfo = hookData.GetParam<CTakeDamageInfo>(1);

            if (!damageInfo.Inflictor.Value.DesignerName.Contains("grenade") && !damageInfo.Inflictor.Value.DesignerName.Contains("projectile"))
                return;

            if (chance < playerChance)
            {
                float modifier = 1.6f + (0.05f * Level);
                damageInfo.Damage *= modifier;
                hookData.SetParam(1, damageInfo);

                CEntityInstance victimEnt = hookData.GetParam<CEntityInstance>(0);
                CCSPlayerPawn victimPawn = new CCSPlayerPawn(victimEnt.Handle);

                Player.Controller.PrintToChat($"{WCS.Instance.ModuleChatPrefix}{ChatColors.Gold}Critical Grenade {ChatColors.Default}on {ChatColors.Green}{victimPawn.Controller.Value.PlayerName}{ChatColors.Gold}!");
            }
        }
    }

    public class SkillReincarnation : WarcraftSkill
    {
        public override string InternalName => "reincarnation";
        public override string DisplayName => "Reincarnation";
        public override string Description => $"Respawn {ChatColors.LightBlue}40-100%{ChatColors.Default} of the time.";

        public override int MaxLevel => 8;
        public override int RequiredLevel => 0;

        public Dictionary<IntPtr, List<string>> PlayerWeapons = new Dictionary<IntPtr, List<string>>();
        public Dictionary<IntPtr, Tuple<Vector, QAngle>> PlayerDeathLocations = new Dictionary<IntPtr, Tuple<Vector, QAngle>>();

        public override void Load(IWarcraftPlayer player)
        {
            Player = player;

            HookEvent<EventPlayerDeath>("player_death", PlayerDeath);
            HookVirtual("player_pre_hurt", PrePlayerHurt);
        }

        public void PlayerDeath(GameEvent @event)
        {
            int playerChance = Convert.ToInt32(40 + (7.5f * Level));
            int chance = Convert.ToInt32(Random.Shared.NextSingle() * 100);

            if (chance < playerChance)
            {
                Player.Controller.PrintToChat($"{WCS.Instance.ModuleChatPrefix}{ChatColors.Gold}Respawning in 2 seconds!");

                new Timer(2.0f, () => { Respawn(Player.Controller); }, TimerFlags.STOP_ON_MAPCHANGE);
            }
        }

        public void Respawn(CCSPlayerController player)
        {
            player.Respawn();

            IntPtr identifier = player.PlayerPawn.Value.Handle;

            foreach (string weapon in PlayerWeapons[identifier])
            {
                player.GiveNamedItem(weapon);
            }

            Tuple<Vector, QAngle> teleportLocationAngles = PlayerDeathLocations[identifier];
            new Timer(0.2f, () => { player.Teleport(teleportLocationAngles.Item1, teleportLocationAngles.Item2, new Vector()); }, TimerFlags.STOP_ON_MAPCHANGE);
        }

        public void PrePlayerHurt(DynamicHook hookData)
        {
            if (Level == 0) return;

            CEntityInstance victimEnt = hookData.GetParam<CEntityInstance>(0);
            CCSPlayerPawn victimPawn = new CCSPlayerPawn(victimEnt.Handle);

            List<string> weapons = new List<string>();

            victimPawn.WeaponServices.MyWeapons.ToList().ForEach((weapon) =>
            {
                weapons.Add(weapon.Value.DesignerName);
            });

            PlayerWeapons[victimPawn.Handle] = weapons;
            PlayerDeathLocations[victimPawn.Handle] = new Tuple<Vector, QAngle>(victimPawn.CBodyComponent.SceneNode.AbsOrigin, Player.Controller.PlayerPawn.Value.EyeAngles);

        }
    }

    public class SkillChainLightning : WarcraftSkill
    {
        public override string InternalName => "chain_lightning";
        public override string DisplayName => "Chain Lightning";
        public override string Description => $"Press ultimate to attack players around you for {ChatColors.LightBlue}30-70{ChatColors.Default} damage.";

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