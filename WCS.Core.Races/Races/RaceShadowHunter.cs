using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Events;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API;
using System;
using WCS.API;
using WCS.Races;
using static WCS.API.Effects;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace WCS.Core.Races.Races
{
    public class SkillHealingWave : WarcraftSkill
    {
        public override string InternalName => "healing_wave";
        public override string DisplayName => "Healing Wave";
        public override string Description => $"{ChatColors.LightPurple}Healing Wave {ChatColors.Green}will heal {ChatColors.Blue}nearby teammates {ChatColors.Default}every 8 seconds.";

        public override int MaxLevel => 8;
        public override int RequiredLevel => 0;

        public Dictionary<IntPtr, Timer> Timers = new Dictionary<IntPtr, Timer>();

        public override void Load(IWarcraftPlayer player)
        {
            Player = player;

            HookEvent<EventPlayerSpawn>("player_spawn", OnPlayerSpawn);
            HookEvent<EventPlayerDeath>("player_death", OnPlayerDeath);
        }

        private void OnPlayerSpawn(GameEvent @obj)
        {
            Timers[Player.Controller.Handle] = new Timer(8.0f, HealingWave, TimerFlags.REPEAT | TimerFlags.STOP_ON_MAPCHANGE);
        }

        private void OnPlayerDeath(GameEvent @obj)
        {
            Timers[Player.Controller.Handle].Kill();
        }

        private void HealingWave()
        {
            var playerEntities = Utilities.FindAllEntitiesByDesignerName<CCSPlayerController>("cs_player_controller");
            foreach (var target in playerEntities)
            {
                if (target.TeamNum != Player.Controller.TeamNum) continue;
                if (!target.PawnIsAlive) continue;
                Vector targetPos = target.PlayerPawn.Value!.CBodyComponent!.SceneNode!.AbsOrigin;
                Vector playerPos = Player.Controller.PlayerPawn.Value!.CBodyComponent!.SceneNode!.AbsOrigin;
                float distance = (targetPos - playerPos).Length();
                if (distance > (200 + 10 * Level)) continue;

                int hpToAdd = (4 + Level);
                target.PlayerPawn.Value.Health += hpToAdd;
                Utilities.SetStateChanged(target.PlayerPawn.Value, "CBaseEntity", "m_iHealth");

                DrawLaserBetween(
                    Player.Controller,
                    playerPos + new Vector(0, 0, 30),
                    targetPos + new Vector(0, 0, 30),
                    Color.LightGreen,
                    0.25f,
                    3
                );
            }
        }
    }

    public class SkillHex : WarcraftSkill
    {
        public override string InternalName => "hex";
        public override string DisplayName => "Hex";
        public override string Description => $"10% chance to slow your enemy for 1-4 seconds.";

        public override int MaxLevel => 8;
        public override int RequiredLevel => 0;

        public override void Load(IWarcraftPlayer player)
        {
            Player = player;

            HookEvent<EventPlayerHurt>("player_hurt_other", OnPlayerHurt);
        }

        private void OnPlayerHurt(GameEvent @obj)
        {
            EventPlayerHurt @event = (EventPlayerHurt)@obj;
            CCSPlayerController victimController = @event.Userid;
            float oldModifier = victimController.PlayerPawn.Value.VelocityModifier;
            victimController.PlayerPawn.Value.VelocityModifier = 0.5f;
            new Timer(1 + (Level / 3), () => { victimController.PlayerPawn.Value.VelocityModifier = oldModifier; });
            if (victimController.IsBot) Player.Controller.PrintToChat($"{WCS.Instance.ModuleChatPrefix}{ChatColors.LightPurple}Slowed {ChatColors.Green}{victimController.PlayerPawn.Value.Bot.Name}{ChatColors.Default}.");
            else Player.Controller.PrintToChat($"{WCS.Instance.ModuleChatPrefix}{ChatColors.LightPurple}Slowed {ChatColors.Green}{victimController.PlayerName}{ChatColors.Default}.");
        }
    }

    public class SkillSerpentWard : WarcraftSkill
    {
        public override string InternalName => "serpent_ward";
        public override string DisplayName => "Serpent Ward";
        public override string Description => $"Experience {ChatColors.LightBlue}93-46%{ChatColors.Default} of gravity. Half damage taken while in the air.";

        public override int MaxLevel => 8;
        public override int RequiredLevel => 0;

        public int MaxWards = 3;

        public Cooldowns cooldowns = new Cooldowns();

        public Dictionary<IntPtr, List<Vector>> Wards = new Dictionary<IntPtr, List<Vector>>();

        public Dictionary<IntPtr, List<CBeam>> Beams = new Dictionary<IntPtr, List<CBeam>>();

        public Dictionary<IntPtr, Timer> Timers = new Dictionary<IntPtr, Timer>();

        public override void Load(IWarcraftPlayer player)
        {
            Player = player;

            if (player != null)
            {
                cooldowns.SetCooldown(player.Controller, "wards", 0);
                cooldowns.AddCooldownExtension(player.Controller, "wards", OnCooldownChange);
                Wards[Player.Controller.Handle] = new List<Vector>();
                Beams[Player.Controller.Handle] = new List<CBeam>();
            }

            HookEvent<EventPlayerSpawn>("player_spawn", OnPlayerSpawn);
            HookEvent<EventPlayerDeath>("player_death", OnPlayerDeath);

            HookAbility(0, AddWard);
        }

        private void OnPlayerSpawn(GameEvent @event)
        {
            Timers[Player.Controller.Handle] = new Timer(1.0f, CalculateWards, TimerFlags.REPEAT | TimerFlags.STOP_ON_MAPCHANGE);
        }

        private void OnPlayerDeath(GameEvent @event)
        {
            Wards[Player.Controller.Handle].Clear();
            Timers[Player.Controller.Handle].Kill();
            foreach (CBeam beam in Beams[Player.Controller.Handle])
            {
                beam.Remove();
            }
        }

        public void OnCooldownChange(float value)
        {
            if (value == 0)
            {
                Player.SetStatusMessage($"Serpent Ward no longer on Cooldown!");
                return;
            }
            Player.SetStatusMessage($"Serpent Ward on Cooldown for {value} seconds.");
        }

        private void AddWard()
        {
            float cooldown = cooldowns.GetCooldown(Player.Controller, "wards");

            if (cooldown == 0)
            {
                int amountLoaded = Wards[Player.Controller.Handle].Count;

                if (amountLoaded >= MaxWards)
                {
                    Player.Controller.PrintToChat($"{WCS.Instance.ModuleChatPrefix}{ChatColors.Red}Maximum {ChatColors.LightPurple}Serpent Wards {ChatColors.Blue}({amountLoaded}/{MaxWards}) {ChatColors.Default}placed!");
                    return;
                }

                Vector playerPos = new Vector(Player.Controller.PlayerPawn.Value!.CBodyComponent!.SceneNode!.AbsOrigin.X, Player.Controller.PlayerPawn.Value!.CBodyComponent!.SceneNode!.AbsOrigin.Y, Player.Controller.PlayerPawn.Value!.CBodyComponent!.SceneNode!.AbsOrigin.Z);
                Wards[Player.Controller.Handle].Add(playerPos);
                DrawWard(playerPos);
                Player.Controller.PrintToChat($"{WCS.Instance.ModuleChatPrefix}{ChatColors.LightPurple}Serpent Ward {ChatColors.Blue}({amountLoaded + 1}/{MaxWards}) {ChatColors.Default}has {ChatColors.Red}activated!");

                cooldowns.SetCooldown(Player.Controller, "wards", 4);
            }
            else
            {
                Player.Controller.PrintToChat($"{WCS.Instance.ModuleChatPrefix}{ChatColors.LightPurple}Serpent Ward {ChatColors.Default}is {ChatColors.Red}on cooldown!");
            }
        }

        private void CalculateWards()
        {
            foreach (Vector wardPos in Wards[Player.Controller.Handle])
            {
                var playerEntities = Utilities.FindAllEntitiesByDesignerName<CCSPlayerController>("cs_player_controller");
                foreach (var target in playerEntities)
                {
                    if (target.TeamNum == Player.Controller.TeamNum) continue;
                    if (!target.PawnIsAlive) continue;
                    Vector playerPos = target.PlayerPawn.Value!.CBodyComponent!.SceneNode!.AbsOrigin;
                    float distance = (wardPos - playerPos).Length();
                    if (distance > (140 + 10 * Level)) continue;

                    int hpToAdd = (4 + Level);
                    if (hpToAdd > target.PlayerPawn.Value.Health) continue;
                    target.PlayerPawn.Value.Health -= hpToAdd;
                    Utilities.SetStateChanged(target.PlayerPawn.Value, "CBaseEntity", "m_iHealth");

                    DrawLaserBetween(
                        Player.Controller,
                        playerPos + new Vector(0, 0, 30),
                        wardPos + new Vector(0, 0, 70),
                        Color.Red,
                        0.25f,
                        3
                    );
                }
            }
        }

        private void DrawWard(Vector wardPos)
        {
            // Draw effects
            CBeam beam = DrawLaserBetween(
                Player.Controller,
                wardPos,
                wardPos + new Vector(0, 0, 80),
                Color.AliceBlue,
                10000.0f,
                3.0f
            );
            Beams[Player.Controller.Handle].Add(beam);

            List<Vector> circleCoords = CalculateCircleEdgeCoords(wardPos, (140 + 10 * Level), 24);
            Vector lastCoord = circleCoords.Last();
            foreach (Vector coord in circleCoords)
            {
                beam = DrawLaserBetween(
                    Player.Controller,
                    lastCoord,
                    coord,
                    Color.Gold,
                    10000.0f,
                    3.0f
                );
                lastCoord = coord;
                Beams[Player.Controller.Handle].Add(beam);
            }
        }
    }

    public class SkillBidBadVoodoo : WarcraftSkill
    {
        public override string InternalName => "voodoo";
        public override string DisplayName => "Bid Bad Voodoo";
        public override string Description => $"Experience {ChatColors.LightBlue}93-46%{ChatColors.Default} of gravity. Half damage taken while in the air.";

        public override int MaxLevel => 8;
        public override int RequiredLevel => 0;

        public Cooldowns cooldowns = new Cooldowns();

        public override void Load(IWarcraftPlayer player)
        {
            Player = player;

            if (player != null)
            {
                cooldowns.SetCooldown(player.Controller, "voodoo", 0);
                cooldowns.AddCooldownExtension(player.Controller, "voodoo", OnCooldownChange);
            }

            HookAbility(1, PlayerUltimate);
        }

        public void OnCooldownChange(float value)
        {
            if (value == 0)
            {
                Player.SetStatusMessage($"Bid Bad Voodoo no longer on Cooldown!");
                return;
            }
            Player.SetStatusMessage($"Bid Bad Voodoo on Cooldown for {value} seconds.");
        }

        private void PlayerUltimate()
        {
            if (Level < 1) return;
            float cooldown = cooldowns.GetCooldown(Player.Controller, "voodoo");

            if (cooldown == 0)
            {
                float time = 1 + (Level / 4);

                Player.Controller.PlayerPawn.Value!.TakesDamage = false;
                Player.Controller.PrintToChat($"{WCS.Instance.ModuleChatPrefix}{ChatColors.LightPurple}Bid Bad Voodoo {ChatColors.Red}activated for {time} seconds!");

                Color returnColor = Player.Controller.PlayerPawn.Value.Render;
                new Timer(time, () => {
                    Player.Controller.PlayerPawn.Value!.TakesDamage = true;
                    Player.Controller.PlayerPawn.Value.Render = Color.MediumPurple;
                });
                new Timer(time, () => {
                    Player.Controller.PrintToChat($"{WCS.Instance.ModuleChatPrefix}{ChatColors.LightPurple}Bid Bad Voodoo {ChatColors.Red}deactivated!");
                    Player.Controller.PlayerPawn.Value.Render = returnColor;
                });

                cooldowns.SetCooldown(Player.Controller, "voodoo", 30);
            }
            else
            {
                Player.Controller.PrintToChat($"{WCS.Instance.ModuleChatPrefix}{ChatColors.LightPurple}Bid Bad Voodoo {ChatColors.Default}is {ChatColors.Red}on cooldown!");
            }
        }
    }

    public class RaceShadowHunter : WarcraftRace
    {
        public override string InternalName => "shadow_hunter";
        public override string DisplayName => "Shadow Hunter";
        public override string Description => "";

        public override int MaxLevel => 32;

        public override void Load(IWarcraftPlayer player)
        {
            Player = player;

            AddSkill(new SkillHealingWave());
            AddSkill(new SkillHex());
            AddSkill(new SkillSerpentWard());
            AddSkill(new SkillBidBadVoodoo());
        }
    }
}
