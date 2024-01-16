using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using Dapper;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using WCS.API;
using WCS.Races;
using static WCS.API.Effects;

namespace WCS.Core.Races.Races
{
    public class SkillEarthquake : WarcraftSkill
    {
        public override string InternalName => "earthquake";
        public override string DisplayName => "Earthquake";
        public override string Description => $"0 - 36% chance upon damaging an enemy to shake them.";

        public override int MaxLevel => 6;
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
            int playerChance = (6 * Level);
            if (chance < playerChance)
            {
                velocity.Z += 100;
                origin.Z += 2;
                victim.PlayerPawn.Value.Teleport(origin, angle, velocity);

                Player.Controller.PrintToChat($"{WCS.Instance.ModuleChatPrefix}{ChatColors.Gold}You {ChatColors.Default}bashed {ChatColors.Red}{victim.PlayerName}{ChatColors.Default}.");
            }
        }
    }

    public class SkillBroomOfVelocity : WarcraftSkill
    {
        public override string InternalName => "broom_of_velocity";
        public override string DisplayName => "Broom Of Velocity";
        public override string Description => $"Grants you 20-44% more movement speed.";

        public override int MaxLevel => 6;
        public override int RequiredLevel => 0;

        public override void Load(IWarcraftPlayer player)
        {
            Player = player;

            HookEvent<EventPlayerSpawn>("player_spawn", PlayerSpawn);
            HookEvent<EventPlayerHurt>("player_hurt", PlayerHurt);
        }

        private void PlayerSpawn(GameEvent @event)
        {
            int auraLevel = Level;
            float speedModifier = 1.2f + (0.04f * auraLevel);
            Player.Controller.PlayerPawn.Value.VelocityModifier = speedModifier;
            Player.Controller.PrintToChat($"{WCS.Instance.ModuleChatPrefix}{ChatColors.Gold}Speed {ChatColors.Default}increased to {ChatColors.Green}x{speedModifier}{ChatColors.Default}.");
        }

        private void PlayerHurt(GameEvent @event)
        {
            int auraLevel = Level;
            float speedModifier = 1.2f + (0.04f * auraLevel);
            Player.Controller.PlayerPawn.Value.VelocityModifier = speedModifier;
        }
    }

    public class SkillWeaponOfTheSorcerer : WarcraftSkill
    {
        public override string InternalName => "weapon_sorc";
        public override string DisplayName => "Weapon of the Sorcerer";
        public override string Description => $"50-80% Chance to receive a Deagle and M4A4.";

        public override int MaxLevel => 6;
        public override int RequiredLevel => 0;

        public override void Load(IWarcraftPlayer player)
        {
            Player = player;

            HookEvent<EventPlayerSpawn>("player_spawn", PlayerSpawn);
        }

        private void PlayerSpawn(GameEvent @event)
        {
            int chanceLevel = 50 + (Level * 5);
            int chance = Convert.ToInt32(Random.Shared.NextSingle() * 100);
            if (chance < chanceLevel)
            {
                new Timer(0.5f, Player.Controller.RemoveWeapons);

                if (Level > 3)
                {
                    new Timer(0.75f, () => Player.Controller.GiveNamedItem("weapon_m4a1"));
                }
                new Timer(0.75f, () => Player.Controller.GiveNamedItem("weapon_deagle"));
            }
        }
    }

    public class SkillLiftOff : WarcraftSkill
    {
        public override string InternalName => "lift_off";
        public override string DisplayName => "Lift Off";
        public override string Description => $"You can fly, while flying you\'ll recieve 8 - 32 extra health. Ultimate.";

        public override int MaxLevel => 6;
        public override int RequiredLevel => 8;

        public Cooldowns cooldowns = new Cooldowns();

        public Dictionary<IntPtr, bool> flyState = new Dictionary<IntPtr, bool>();

        public override void Load(IWarcraftPlayer player)
        {
            Player = player;

            if (player != null)
            {
                cooldowns.SetCooldown(player.Controller, "lift_off", 0);
                cooldowns.AddCooldownExtension(player.Controller, "lift_off", OnCooldownChange);
                flyState.Add(player.Controller.Handle, false);
            }

            HookAbility(1, PlayerUltimate);
        }

        public void OnCooldownChange(float value)
        {
            if (value == 0)
            {
                Player.SetStatusMessage($"Lift Off no longer on Cooldown!");
                return;
            }
            Player.SetStatusMessage($"Lift Off on Cooldown for {value} seconds.");
        }

        private void PlayerUltimate()
        {
            if (Level < 1) return;
            float cooldown = cooldowns.GetCooldown(Player.Controller, "lift_off");

            if (cooldown == 0)
            {
                bool state = flyState[Player.Controller.Handle];
                int bonusHP = (8 + (4 * Level));

                if (state) // is flying
                {
                    Player.Controller.PlayerPawn.Value!.Health += bonusHP;
                    Player.Controller.PlayerPawn.Value!.LastHealth = Player.Controller.PlayerPawn.Value!.Health;
                    Player.Controller.PlayerPawn.Value!.GravityScale = 1.0f;
                    Player.Controller.PlayerPawn.Value!.MoveType = MoveType_t.MOVETYPE_WALK;
                    Player.Controller.PrintToChat($"{WCS.Instance.ModuleChatPrefix}{ChatColors.Gold}... and back down.");
                    flyState[Player.Controller.Handle] = false;
                    DoEffect(Player.Controller.PlayerPawn.Value.CBodyComponent.SceneNode.AbsOrigin);
                }
                else // is walking
                {
                    int curHP = Player.Controller.PlayerPawn.Value!.Health;

                    if (curHP > bonusHP) Player.Controller.PlayerPawn.Value!.Health -= bonusHP;
                    else Player.Controller.PlayerPawn.Value!.Health -= (curHP - 1);
                    Player.Controller.PlayerPawn.Value!.GravityScale = 0;
                    Player.Controller.PlayerPawn.Value!.MoveType = MoveType_t.MOVETYPE_FLYGRAVITY;
                    Player.Controller.PrintToChat($"{WCS.Instance.ModuleChatPrefix}{ChatColors.Gold}Lift off!");
                    flyState[Player.Controller.Handle] = true;
                    DoEffect(Player.Controller.PlayerPawn.Value.CBodyComponent.SceneNode.AbsOrigin);
                }
                cooldowns.SetCooldown(Player.Controller, "lift_off", 4);
            }
        }

        private void DoEffect(Vector playerOrigin)
        {
            playerOrigin.Z += 5;

            List<Vector> circleCoords = CalculateCircleEdgeCoords(playerOrigin, 40, 24);
            Vector lastCoord = circleCoords.Last();
            foreach (Vector coord in circleCoords)
            {
                DrawLaserBetween(
                    Player.Controller,
                    lastCoord,
                    coord,
                    Color.Gold,
                    0.5f,
                    3.0f
                );
                lastCoord = coord;
            }

            List<Vector> circleCoords1 = CalculateCircleEdgeCoords(playerOrigin, 30, 24);
            Vector lastCoord1 = circleCoords1.Last();
            foreach (Vector coord1 in circleCoords1)
            {
                new Timer(0.33f, () => {
                    DrawLaserBetween(
                        Player.Controller,
                        lastCoord1,
                        coord1,
                        Color.Gold,
                        0.5f,
                        3.0f
                    );
                    lastCoord1 = coord1;
                }
                );
                
            }

            List<Vector> circleCoords2 = CalculateCircleEdgeCoords(playerOrigin, 20, 24);
            Vector lastCoord2 = circleCoords2.Last();
            foreach (Vector coord2 in circleCoords2)
            {
                new Timer(0.66f, () => {
                    DrawLaserBetween(
                        Player.Controller,
                        lastCoord2,
                        coord2,
                        Color.Gold,
                        0.5f,
                        3.0f
                    );
                    lastCoord2 = coord2;
                }
                );
            }
        }
    }

    public class RaceArchmageProudmore : WarcraftRace
    {
        public override string InternalName => "archmage_proudmore";
        public override string DisplayName => "Archmage Proudmore";
        public override string Description => "Archmage Proudmore";

        public override int MaxLevel => 24;

        public override void Load(IWarcraftPlayer player)
        {
            Player = player;

            AddSkill(new SkillEarthquake());
            AddSkill(new SkillBroomOfVelocity());
            AddSkill(new SkillWeaponOfTheSorcerer());
            AddSkill(new SkillLiftOff());
        }
    }
}
