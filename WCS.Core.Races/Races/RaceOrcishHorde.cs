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
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Modules.Memory;
using System.Runtime.InteropServices;
using System.Drawing;

using static WCS.API.Effects;
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

            if (chance < playerChance)
            {
                CTakeDamageInfo damageInfo = hookData.GetParam<CTakeDamageInfo>(1);
                damageInfo.Damage *= 2;
                hookData.SetParam(1, damageInfo);

                CEntityInstance victimEnt = hookData.GetParam<CEntityInstance>(0);
                CCSPlayerPawn victimPawn = new CCSPlayerPawn(victimEnt.Handle);

                Player.Controller.PrintToChat($"{WCS.Instance.ModuleChatPrefix}{ChatColors.Gold}Critical Strike {ChatColors.Default}on {ChatColors.Green}{victimPawn.Controller.Value.PlayerName}{ChatColors.Gold}!");

                DrawLaserBetween(
                    Player.Controller,
                    Player.Controller.PlayerPawn.Value.WeaponServices.ActiveWeapon.Value.AbsOrigin,
                    damageInfo.DamagePosition,
                    Color.GreenYellow,
                    0.25f,
                    3
                );
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
            if (Level == 0)
            {
                return;
            }

            CTakeDamageInfo damageInfo = hookData.GetParam<CTakeDamageInfo>(1);

            if (!damageInfo.Inflictor.Value.DesignerName.Contains("grenade") && !damageInfo.Inflictor.Value.DesignerName.Contains("projectile"))
            {
                return;
            }

            float modifier = 1.6f + (0.05f * Level);
            damageInfo.Damage *= modifier;

            CEntityInstance victimEnt = hookData.GetParam<CEntityInstance>(0);
            CCSPlayerPawn victimPawn = new CCSPlayerPawn(victimEnt.Handle);

            hookData.SetParam<CTakeDamageInfo>(1, damageInfo);

            Server.NextFrame(() =>
            {
                Player.Controller.PrintToChat($"{WCS.Instance.ModuleChatPrefix}{ChatColors.Gold}Critical Grenade {ChatColors.Default}on {ChatColors.Green}{victimPawn.Controller.Value.PlayerName}{ChatColors.Gold}!");
            }
            );
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

        public override void Load(IWarcraftPlayer player)
        {
            Player = player;

            if (player != null)
            {
                IntPtr identifier = player.Controller.Handle;

                PlayerWeapons[identifier] = new List<string>();
            }

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

            IntPtr identifier = player.Handle;

            new Timer(0.1f, () => { player.RemoveWeapons(); });

            foreach (string weapon in PlayerWeapons[identifier])
            {
                new Timer(0.2f, () =>
                {
                    player.GiveNamedItem(weapon);
                }
                );
            }

            PlayerWeapons[identifier] = new List<string>();
        }

        public void PrePlayerHurt(DynamicHook hookData)
        {
            CEntityInstance victimEnt = hookData.GetParam<CEntityInstance>(0);
            CCSPlayerPawn victimPawn = new CCSPlayerPawn(victimEnt.Handle);

            if (Level == 0 || PlayerWeapons[victimPawn.Controller.Value.Handle].Count() != 0) return;

            List<string> weapons = new List<string>();

            victimPawn.WeaponServices.MyWeapons.ToList().ForEach((weapon) =>
            {
                weapons.Add(weapon.Value.DesignerName);
            });

            PlayerWeapons[victimPawn.Controller.Value.Handle] = weapons;
        }
    }

    public class SkillChainLightning : WarcraftSkill
    {
        public override string InternalName => "chain_lightning";
        public override string DisplayName => "Chain Lightning";
        public override string Description => $"Press ultimate to attack players within 300 units around you for {ChatColors.LightBlue}30-70{ChatColors.Default} damage.";

        public override int MaxLevel => 8;
        public override int RequiredLevel => 0;

        public Dictionary<IntPtr, int> Cooldowns = new Dictionary<IntPtr, int>();

        public override void Load(IWarcraftPlayer player)
        {
            Player = player;

            HookEvent<EventPlayerSpawn>("player_spawn", PlayerSpawn);

            HookAbility(1, PlayerUltimate);
        }

        private void PlayerSpawn(GameEvent @event)
        {
            Cooldowns[Player.Controller.Handle] = 0;
        }

        private void PlayerUltimate()
        {
            if (Level < 1) return;
            int cooldown = Cooldowns[Player.Controller.Handle];
            if (cooldown != 0)
            {
                Player.Controller.PrintToCenterHtml($"<font color=#FFFFFF>Chain Lightning on Cooldown for {cooldown} seconds.</font>");
                return;
            }
            Vector origin = Player.Controller.PlayerPawn.Value.CBodyComponent.SceneNode.AbsOrigin;
            origin.Z += 5;

            List<CCSPlayerController> targets = new List<CCSPlayerController>();
            foreach (CCSPlayerController player in Utilities.GetPlayers())
            {
                if (player.Handle == Player.Controller.Handle)
                    continue;

                if (player.TeamNum == Player.Controller.TeamNum)
                    continue;

                float distance = (origin - player.PlayerPawn.Value.CBodyComponent.SceneNode.AbsOrigin).Length();
                if (distance > 300)
                    continue;

                targets.Add(player);
            }

            foreach (CCSPlayerController ply in targets)
            {
                IntPtr ptr = Marshal.AllocHGlobal(0x98);
                CTakeDamageInfo damageInfo = new CTakeDamageInfo(ptr);
                nint attackerHandle = Schema.GetRef<nint>(damageInfo.Handle, "CTakeDamageInfo", "m_hAttacker");
                attackerHandle = Player.Controller.PlayerPawn.Value.Handle;
                damageInfo.DamageFlags = TakeDamageFlags_t.DFLAG_IGNORE_ARMOR;
                damageInfo.Damage = 30 + (5 * Level);
                VirtualFunctions.CBaseEntity_TakeDamageOld(ply.PlayerPawn.Value, damageInfo);
                Marshal.FreeHGlobal(ptr);
                DrawLaserBetween(
                    Player.Controller,
                    Player.Controller.PlayerPawn.Value.WeaponServices.ActiveWeapon.Value.AbsOrigin,
                    ply.PlayerPawn.Value.AbsOrigin + new Vector(0, 0, 30),
                    Color.Green,
                    0.25f,
                    3
                );
                if (ply.IsBot)
                    Player.Controller.PrintToChat($"{WCS.Instance.ModuleChatPrefix}{ChatColors.Green}Chain Lightning {ChatColors.Red}attacked {ChatColors.Magenta}{ply.PlayerPawn.Value.Bot.Name}!");
                else
                    Player.Controller.PrintToChat($"{WCS.Instance.ModuleChatPrefix}{ChatColors.Green}Chain Lightning {ChatColors.Red}attacked {ChatColors.Magenta}{ply.PlayerName}!");
            }

            if (targets.Count() > 0)
            {
                Cooldowns[Player.Controller.Handle] = 8;
                new Timer(1.0f, () => { Cooldowns[Player.Controller.Handle] = 7; }, TimerFlags.STOP_ON_MAPCHANGE);
                new Timer(2.0f, () => { Cooldowns[Player.Controller.Handle] = 6; }, TimerFlags.STOP_ON_MAPCHANGE);
                new Timer(3.0f, () => { Cooldowns[Player.Controller.Handle] = 5; }, TimerFlags.STOP_ON_MAPCHANGE);
                new Timer(4.0f, () => { Cooldowns[Player.Controller.Handle] = 4; }, TimerFlags.STOP_ON_MAPCHANGE);
                new Timer(5.0f, () => { Cooldowns[Player.Controller.Handle] = 3; }, TimerFlags.STOP_ON_MAPCHANGE);
                new Timer(6.0f, () => { Cooldowns[Player.Controller.Handle] = 2; }, TimerFlags.STOP_ON_MAPCHANGE);
                new Timer(7.0f, () => { Cooldowns[Player.Controller.Handle] = 1; }, TimerFlags.STOP_ON_MAPCHANGE);
                new Timer(8.0f, () => { Cooldowns[Player.Controller.Handle] = 0; }, TimerFlags.STOP_ON_MAPCHANGE);
            }
            else
            {
                Player.Controller.PrintToChat($"{WCS.Instance.ModuleChatPrefix}{ChatColors.Green}Chain Lightning {ChatColors.Default}hit no enemies!");
            }
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

            HookEvent<EventPlayerSpawn>("player_spawn", PlayerSpawn);
        }

        private void PlayerSpawn(GameEvent @obj)
        {
            var @event = (EventPlayerSpawn)@obj;
            var player = @event.Userid;
            player.GiveNamedItem("weapon_hegrenade");
        }
    }
}