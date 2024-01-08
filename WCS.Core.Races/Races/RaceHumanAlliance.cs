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
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Events;
using CounterStrikeSharp.API.Modules.Utils;
using System.Drawing;
using WCS.API;

namespace WCS.Races
{
    public class SkillBash : WarcraftSkill
    {
        public override string InternalName => "bash";
        public override string DisplayName => "Bash";
        public override string Description => $"Strike the {ChatColors.Red}enemy {ChatColors.Default}and knock them off balance.";

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
            Vector origin = victim.PlayerPawn.Value.CBodyComponent.SceneNode.AbsOrigin;
            QAngle angle = victim.PlayerPawn.Value.EyeAngles;
            Vector velocity = victim.PlayerPawn.Value.AbsVelocity;

            int chance = Convert.ToInt32(Random.Shared.NextSingle() * 100);
            int playerChance = 10 + (5 * Level);
            if (chance < playerChance)
            {
                velocity.Z += 100;
                origin.Z += 2;
                victim.PlayerPawn.Value.Teleport(origin, angle, velocity);

                Player.Controller.PrintToChat($"{WCS.Instance.ModuleChatPrefix}{ChatColors.Gold}You {ChatColors.Default}bashed {ChatColors.Red}{victim.PlayerName}{ChatColors.Default}.");
            }
        }
    }

    public class SkillDevotionAura : WarcraftSkill
    {
        public override string InternalName => "devotion_aura";
        public override string DisplayName => "Devotion Aura";
        public override string Description => $"Gain {ChatColors.Blue}15-120{ChatColors.Default} extra health.";

        public override int MaxLevel => 8;
        public override int RequiredLevel => 0;

        public override void Load(IWarcraftPlayer player)
        {
            Player = player;

            HookEvent<EventPlayerSpawn>("player_spawn", PlayerSpawn);
        }

        private void PlayerSpawn(GameEvent @event)
        {
            int auraLevel = Level;
            int healthAddition = 15 * auraLevel;
            Player.Controller.PlayerPawn.Value.Health += healthAddition;
            Player.Controller.PrintToChat($"{WCS.Instance.ModuleChatPrefix}{ChatColors.Gold}Health {ChatColors.Default}increased by {ChatColors.Green}{healthAddition} {ChatColors.Default}HP.");
        }
    }

    public class SkillInvisibility : WarcraftSkill
    {
        public override string InternalName => "invisibility";
        public override string DisplayName => "Invisibility";
        public override string Description => $"Reduce visibility by {ChatColors.LightBlue}70-40%{ChatColors.Default}.";

        public override int MaxLevel => 8;
        public override int RequiredLevel => 0;

        public override void Load(IWarcraftPlayer player)
        {
            Player = player;

            HookEvent<EventPlayerSpawn>("player_spawn", PlayerSpawn);
        }

        private void PlayerSpawn(GameEvent @event)
        {
            if (Level < 1) return;
            int[] alpha = { 180, 170, 160, 150, 140, 135, 130, 130 };
            Player.Controller.PlayerPawn.Value.RenderMode = RenderMode_t.kRenderTransColor;
            Player.Controller.PlayerPawn.Value.Render = Color.FromArgb(alpha[Level - 1], Player.Controller.PlayerPawn.Value.Render.R, Player.Controller.PlayerPawn.Value.Render.G, Player.Controller.PlayerPawn.Value.Render.B);
        }
    }

    public class SkillTeleport : WarcraftSkill
    {
        public override string InternalName => "teleport";
        public override string DisplayName => "Teleport";
        public override string Description => $"{ChatColors.Purple}Teleport a short distance.";

        public override int MaxLevel => 8;
        public override int RequiredLevel => 8;

        public Dictionary<IntPtr, int> Cooldowns = new Dictionary<IntPtr, int>();

        public override void Load(IWarcraftPlayer player)
        {
            Player = player;

            HookEvent<EventPlayerSpawn>("player_spawn", PlayerSpawn);

            HookAbility(1, PlayerUltimate);
        }

        public double ConvertToRadians(float angle)
        {
            return (Math.PI / 180) * angle;
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
                Player.Controller.PrintToCenterHtml($"<font color=#FFFFFF>Teleport on Cooldown for {cooldown} seconds.</font>");
                return;
            }
            Vector origin = Player.Controller.PlayerPawn.Value.CBodyComponent.SceneNode.AbsOrigin;
            origin.Z += 5;
            QAngle angle = Player.Controller.PlayerPawn.Value.EyeAngles;

            double yaw = ConvertToRadians(angle.Y);
            double pitch = ConvertToRadians(angle.X);
            var Y = Math.SinCos(yaw);
            var P = Math.SinCos(pitch);

            Vector currentDirection = new Vector((float)(Y.Cos * P.Cos), (float)(Y.Sin * P.Cos), ((float)P.Sin * -1));
            currentDirection.X *= 1000 + (Level * 100);
            currentDirection.Y *= 1000 + (Level * 100);
            currentDirection.Z += 150;

            Player.Controller.PlayerPawn.Value.Teleport(origin, angle, currentDirection);

            Cooldowns[Player.Controller.Handle] = 5;
            new Timer(1.0f, () => {
                Cooldowns[Player.Controller.Handle] = 4;
                Player.SetStatusMessage("Teleport on Cooldown for 4 seconds.");
            }, TimerFlags.STOP_ON_MAPCHANGE);
            new Timer(2.0f, () => {
                Cooldowns[Player.Controller.Handle] = 3;
                Player.SetStatusMessage("Teleport on Cooldown for 3 seconds.");
            }, TimerFlags.STOP_ON_MAPCHANGE);
            new Timer(3.0f, () => {
                Cooldowns[Player.Controller.Handle] = 2;
                Player.SetStatusMessage("Teleport on Cooldown for 2 seconds.");
            }, TimerFlags.STOP_ON_MAPCHANGE);
            new Timer(4.0f, () => {
                Cooldowns[Player.Controller.Handle] = 1;
                Player.SetStatusMessage("Teleport on Cooldown for 1 seconds.");
            }, TimerFlags.STOP_ON_MAPCHANGE);
            new Timer(5.0f, () => {
                Cooldowns[Player.Controller.Handle] = 0;
                Player.SetStatusMessage("Teleport no longer on Cooldown.");
            }, TimerFlags.STOP_ON_MAPCHANGE);
        }
    }

    public class RaceHumanAlliance : WarcraftRace
    {
        public override string InternalName => "human_alliance";
        public override string DisplayName => "Human Alliance";
        public override string Description => "Human Alliance";

        public override int MaxLevel => 32;

        public override void Load(IWarcraftPlayer player)
        {
            Player = player;

            AddSkill(new SkillInvisibility());
            AddSkill(new SkillDevotionAura());
            AddSkill(new SkillBash());
            AddSkill(new SkillTeleport());
        }
    }
}