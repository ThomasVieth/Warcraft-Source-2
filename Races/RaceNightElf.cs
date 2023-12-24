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
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Events;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using System.Drawing;

using static WCS.Effects;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API;
using System.Runtime.InteropServices;

namespace WCS.Races
{
    public class SkillEvasion : WarcraftSkill
    {
        public override string InternalName => "evasion";
        public override string DisplayName => "Evasion";
        public override string Description => $"5-21% chance to avoid incoming attacks.";

        public override int MaxLevel => 8;
        public override int RequiredLevel => 0;

        public override void Load(WarcraftPlayer player)
        {
            Player = player;

            HookVirtual("player_pre_hurt", PrePlayerHurt);
        }

        private void PrePlayerHurt(DynamicHook hookData)
        {
            int playerChance = Convert.ToInt32(5 + (2 * Level));
            int chance = Convert.ToInt32(Random.Shared.NextSingle() * 100);

            if (chance < playerChance)
            {
                CTakeDamageInfo damageInfo = hookData.GetParam<CTakeDamageInfo>(1);
                damageInfo.Damage = 0;
                hookData.SetParam<CTakeDamageInfo>(1, damageInfo);
                CCSPlayerPawn attackerPawn = new CCSPlayerPawn(damageInfo.Attacker.Value.Handle);
                Player.Controller.PrintToChat($"{WCS.Instance.ModuleChatPrefix}{ChatColors.Yellow}Evaded {ChatColors.Default}incoming damage from {ChatColors.Red}{attackerPawn.Controller.Value.PlayerName}{ChatColors.Default}.");
            }
        }
    }

    public class SkillTrueshotAura : WarcraftSkill
    {
        public override string InternalName => "trueshot_aura";
        public override string DisplayName => "Trueshot Aura";
        public override string Description => $"Gives a chance to deal 5-21 extra damage, 7-15% chance.";

        public override int MaxLevel => 8;
        public override int RequiredLevel => 0;

        public override void Load(WarcraftPlayer player)
        {
            Player = player;

            HookVirtual("player_pre_hurt_other", PrePlayerHurtOther);
        }

        public void PrePlayerHurtOther(DynamicHook hookData)
        {
            if (Level == 0) return;

            int playerChance = 7 + Level;
            int chance = Convert.ToInt32(Random.Shared.NextSingle() * 100);

            if (chance < playerChance)
            {
                CTakeDamageInfo damageInfo = hookData.GetParam<CTakeDamageInfo>(1);
                damageInfo.Damage += (5 + (2 * Level));
                hookData.SetParam(1, damageInfo);

                CEntityInstance victimEnt = hookData.GetParam<CEntityInstance>(0);
                CCSPlayerPawn victimPawn = new CCSPlayerPawn(victimEnt.Handle);

                Player.Controller.PrintToChat($"{WCS.Instance.ModuleChatPrefix}{ChatColors.Yellow}Trueshot {ChatColors.Default}on {ChatColors.Red}{victimPawn.Controller.Value.PlayerName}{ChatColors.Gold}!");

                DrawLaserBetween(
                    Player.Controller,
                    Player.Controller.PlayerPawn.Value.WeaponServices.ActiveWeapon.Value.AbsOrigin,
                    damageInfo.DamagePosition,
                    Color.Yellow,
                    0.25f,
                    3
                );
            }
        }
    }

    public class SkillThorns : WarcraftSkill
    {
        public override string InternalName => "thorns_aura";
        public override string DisplayName => "Thorns Aura";
        public override string Description => $"Gives a chance to reflect attacks 0-16% dealing 6-22 damage.";

        public override int MaxLevel => 8;
        public override int RequiredLevel => 0;

        public override void Load(WarcraftPlayer player)
        {
            Player = player;

            HookEvent<EventPlayerHurt>("player_hurt", PlayerHurt);
        }

        public void PlayerHurt(GameEvent @obj)
        {
            if (Level == 0) return;

            var @event = (EventPlayerHurt)obj;

            int playerChance = 2 * Level;
            int chance = Convert.ToInt32(Random.Shared.NextSingle() * 100);

            if (chance < playerChance)
            {
                IntPtr ptr = Marshal.AllocHGlobal(0x98);
                CTakeDamageInfo damageInfo = new CTakeDamageInfo(ptr);
                nint attackerHandle = Schema.GetRef<nint>(damageInfo.Handle, "CTakeDamageInfo", "m_hAttacker");
                attackerHandle = Player.Controller.PlayerPawn.Value.Handle;
                damageInfo.DamageFlags = TakeDamageFlags_t.DFLAG_IGNORE_ARMOR;
                damageInfo.Damage = 6 + (2 * Level);
                VirtualFunctions.CBaseEntity_TakeDamageOld(@event.Attacker.PlayerPawn.Value, damageInfo);
                Marshal.FreeHGlobal(ptr);

                Player.Controller.PrintToChat($"{WCS.Instance.ModuleChatPrefix}{ChatColors.Yellow}Thorns {ChatColors.Default}hit {ChatColors.Red}{@event.Attacker.PlayerName}{ChatColors.Gold}!");
            }
        }
    }

    public class SkillEntanglingRoots : WarcraftSkill
    {
        public override string InternalName => "entangling_roots";
        public override string DisplayName => "Entangling Roots";
        public override string Description => $"Roots every enemy within 240-400 range making them unable to move for 2 seconds.";

        public override int MaxLevel => 8;
        public override int RequiredLevel => 0;

        public Dictionary<IntPtr, int> Cooldowns = new Dictionary<IntPtr, int>();

        public override void Load(WarcraftPlayer player)
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
                Player.Controller.PrintToCenterHtml($"<font color=#FFFFFF>Entangling Roots on Cooldown for {cooldown} seconds.</font>");
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
                if (distance > (240 + (20 * Level)))
                    continue;

                targets.Add(player);
            }

            foreach (CCSPlayerController ply in targets)
            {
                float restoreValue = ply.PlayerPawn.Value.VelocityModifier;
                ply.PlayerPawn.Value.VelocityModifier = 0;
                new Timer(2, () => { ply.PlayerPawn.Value.VelocityModifier = restoreValue; });
                DrawLaserBetween(
                    Player.Controller,
                    Player.Controller.PlayerPawn.Value.WeaponServices.ActiveWeapon.Value.AbsOrigin,
                    ply.PlayerPawn.Value.AbsOrigin + new Vector(0, 0, 30),
                    Color.Yellow,
                    0.25f,
                    3
                );
                if (ply.IsBot)
                    Player.Controller.PrintToChat($"{WCS.Instance.ModuleChatPrefix}{ChatColors.Yellow}Entangling Roots {ChatColors.Red}rooted {ChatColors.Magenta}{ply.PlayerPawn.Value.Bot.Name}!");
                else
                {
                    Player.Controller.PrintToChat($"{WCS.Instance.ModuleChatPrefix}{ChatColors.Yellow}Entangling Roots {ChatColors.Red}rooted {ChatColors.Magenta}{ply.PlayerName}!");
                    ply.PrintToChat($"{WCS.Instance.ModuleChatPrefix}{ChatColors.Yellow}Entangling Roots {ChatColors.Red}rooted {ChatColors.Magenta}you!");
                }
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
                Player.Controller.PrintToChat($"{WCS.Instance.ModuleChatPrefix}{ChatColors.Yellow}Entangling Roots {ChatColors.Default}hit no enemies!");
            }
        }
    }

    public class RaceNightElf : WarcraftRace
    {
        public override string InternalName => "night_elf";
        public override string DisplayName => "Night Elf";
        public override string Description => "Night Elf";

        public override int MaxLevel => 32;

        public override void Load(WarcraftPlayer player)
        {
            Player = player;

            AddSkill(new SkillEvasion());
            AddSkill(new SkillTrueshotAura());
            AddSkill(new SkillThorns());
            AddSkill(new SkillEntanglingRoots());
        }
    }
}