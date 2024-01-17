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
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using System.Drawing;

using static WCS.API.Effects;
using WCS.API;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Modules.Memory;
using System.Runtime.CompilerServices;
using System.Text;
using CounterStrikeSharp.API.Modules.Entities;
using System.Collections.Generic;
using System.Linq;

namespace WCS.Races
{
    public class SkillVampiricAura : WarcraftSkill
    {
        public override string InternalName => "vampiric_aura";
        public override string DisplayName => "Vampiric Aura";
        public override string Description => $"Heal for {ChatColors.Green}10-80%{ChatColors.Default} of damage dealt to enemies.";

        public override int MaxLevel => 8;
        public override int RequiredLevel => 0;

        public override void Load(IWarcraftPlayer player)
        {
            Player = player;

            HookEvent<EventPlayerHurt>("player_hurt_other", PlayerHurtOther);
        }

        private void PlayerHurtOther(GameEvent @obj)
        {
            var @event = (EventPlayerHurt)obj;
            var victim = @event.Userid;
            var attacker = @event.Attacker;

            int vampiricAuraLevel = Level;
            float lifeStealPercentage = (vampiricAuraLevel * 10.0f) / 100.0f;

            int amountToHeal = Convert.ToInt32(@event.DmgHealth * lifeStealPercentage);
            int newHealth = attacker.PlayerPawn.Value.Health + amountToHeal;

            if (newHealth >= 200)
            {
                amountToHeal = 200 - newHealth + amountToHeal;
                newHealth = 200;
            }

            attacker.PlayerPawn.Value.Health = newHealth;
            Utilities.SetStateChanged(attacker.PlayerPawn.Value, "CBaseEntity", "m_iHealth");

            if (victim != null & !string.IsNullOrEmpty(victim.PlayerName) && amountToHeal > 0)
            {
                Player.SetStatusMessage($"{ChatColors.Green}+{amountToHeal} HP");
                Player.Controller.PrintToChat($"{WCS.Instance.ModuleChatPrefix}{ChatColors.Red}Leeched {ChatColors.Green}{amountToHeal} HP{ChatColors.Default}.");
                DrawLaserBetween(
                    attacker,
                    attacker.PlayerPawn.Value.WeaponServices.ActiveWeapon.Value.AbsOrigin,
                    victim.PlayerPawn.Value.AbsOrigin + new Vector(0, 0, 30),
                    Color.DarkRed,
                    0.25f,
                    3
                );
            }
        }
    }

    public class SkillUnholyAura : WarcraftSkill
    {
        public override string InternalName => "unholy_aura";
        public override string DisplayName => "Unholy Aura";
        public override string Description => $"Move {ChatColors.Blue}8-64%{ChatColors.Default} faster. Lower your HP to increase further.";

        public override int MaxLevel => 8;
        public override int RequiredLevel => 0;

        public override void Load(IWarcraftPlayer player)
        {
            Player = player;

            HookEvent<EventPlayerSpawn>("player_spawn", PlayerSpawn);
            HookEvent<EventPlayerHurt>("player_hurt", PlayerHurt);
        }

        private void PlayerSpawn(GameEvent @event)
        {
            Vector playerOrigin = Player.Controller.PlayerPawn.Value.CBodyComponent.SceneNode.AbsOrigin;
            playerOrigin.Z += 5;
            List<Vector> circleCoords = CalculateCircleEdgeCoords(playerOrigin, 40, 24);
            Vector lastCoord = circleCoords.Last();
            foreach (Vector coord in circleCoords)
            {
                DrawLaserBetween(
                    Player.Controller,
                    lastCoord,
                    coord,
                    Color.DarkRed,
                    2.0f,
                    3.0f
                );
                lastCoord = coord;
            }

            int unholyAuraLevel = Level;
            float speedModifier = 1.0f + (0.08f * unholyAuraLevel);
            Player.Controller.PlayerPawn.Value.VelocityModifier = speedModifier;
            Player.Controller.PrintToChat($"{WCS.Instance.ModuleChatPrefix}{ChatColors.Gold}Speed {ChatColors.Default}increased to {ChatColors.Green}x{speedModifier}{ChatColors.Default}.");
        }
        private void PlayerHurt(GameEvent @event)
        {
            int unholyAuraLevel = Level;
            float healthModifier = Math.Max((float)Player.Controller.PlayerPawn.Value.Health / 100, 0.2f);
            float speedModifier = 0.2f + (0.05f * unholyAuraLevel);
            speedModifier *= Math.Max(healthModifier, 2.0f);
            Player.Controller.PlayerPawn.Value.VelocityModifier = (1 + speedModifier);
        }
    }

    public class SkillLevitation : WarcraftSkill
    {
        public override string InternalName => "levitation";
        public override string DisplayName => "Levitation";
        public override string Description => $"Experience {ChatColors.LightBlue}93-46%{ChatColors.Default} of gravity. Half damage taken while in the air.";

        public override int MaxLevel => 8;
        public override int RequiredLevel => 0;

        public override void Load(IWarcraftPlayer player)
        {
            Player = player;

            HookEvent<EventPlayerSpawn>("player_spawn", PlayerSpawn);

            HookVirtual("player_pre_hurt", PrePlayerHurt);
        }

        private void PlayerSpawn(GameEvent @event)
        {
            int levitationLevel = Level;
            float levitationModifier = 1f - (levitationLevel * 0.07f);
            Player.Controller.PlayerPawn.Value.GravityScale = levitationModifier;
            var str = (levitationModifier * 100).ToString("0.00");
            Player.Controller.PrintToChat($"{WCS.Instance.ModuleChatPrefix}{ChatColors.Gold}Gravity {ChatColors.Default}decreased to {ChatColors.Green}{str}%{ChatColors.Default}.");
        }

        private void PrePlayerHurt(DynamicHook hookData)
        {
            bool onGround = Player.Controller.PlayerPawn.Value.OnGroundLastTick;
            if (!onGround)
            {
                CTakeDamageInfo damageInfo = hookData.GetParam<CTakeDamageInfo>(1);
                damageInfo.Damage *= 0.5f;
                hookData.SetParam<CTakeDamageInfo>(1, damageInfo);
                Player.Controller.PrintToChat($"{WCS.Instance.ModuleChatPrefix}{ChatColors.Gold}Low gravity {ChatColors.Default}blocked {ChatColors.Green}incoming damage{ChatColors.Default}.");
            }
        }
    }

    public class RaceUndeadScourge : WarcraftRace
    {
        public override string InternalName => "undead_scourge";
        public override string DisplayName => "Undead Scourge";
        public override string Description => "Undead Scourge";

        public override int MaxLevel => 32;

        public override void Load(IWarcraftPlayer player)
        {
            Player = player;

            AddSkill(new SkillVampiricAura());
            AddSkill(new SkillUnholyAura());
            AddSkill(new SkillLevitation());

            HookEvent<EventPlayerDeath>("player_death", PlayerDeath);
        }

        public void PlayerDeath(GameEvent @event)
        {
            Player.Controller.PlayerPawn.Value.GravityScale = 1.0f;
        }
    }
}