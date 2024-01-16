using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Events;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Utils;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using WCS.API;
using WCS.Races;

namespace WCS.Core.Races.Races
{
    public class SkillAdrenaline : WarcraftSkill
    {
        public override string InternalName => "adrenaline";
        public override string DisplayName => "Adrenaline";
        public override string Description => $"Gain health and speed to chase your targets.";

        public override int MaxLevel => 6;
        public override int RequiredLevel => 0;

        public override void Load(IWarcraftPlayer player)
        {
            Player = player;

            HookEvent<EventPlayerSpawn>("player_spawn", OnPlayerSpawn);
            HookEvent<EventPlayerHurt>("player_hurt", OnPlayerHurt);
        }

        public void OnPlayerSpawn(GameEvent @event)
        {
            int auraLevel = Level;
            int healthAddition = 15 * auraLevel;
            Player.Controller.PlayerPawn.Value!.Health += healthAddition;
            Player.Controller.PrintToChat($"{WCS.Instance.ModuleChatPrefix}{ChatColors.Red}Health {ChatColors.Default}increased by {ChatColors.Green}{healthAddition} {ChatColors.Default}HP.");
            
            int unholyAuraLevel = Level;
            float speedModifier = 1.3f + (0.07f * unholyAuraLevel);
            Player.Controller.PlayerPawn.Value.VelocityModifier = speedModifier;
            Player.Controller.PrintToChat($"{WCS.Instance.ModuleChatPrefix}{ChatColors.Red}Speed {ChatColors.Default}increased to {ChatColors.Green}x{speedModifier}{ChatColors.Default}.");
        }

        private void OnPlayerHurt(GameEvent @event)
        {
            int auraLevel = Level;
            float speedModifier = 1.3f + (0.07f * auraLevel);
            Player.Controller.PlayerPawn.Value!.VelocityModifier = speedModifier;
        }
    }

    public class SkillCloakOfInvisibility : WarcraftSkill
    {
        public override string InternalName => "cloak";
        public override string DisplayName => "Cloak of Invisibility";
        public override string Description => $"Reduce visibility by {ChatColors.LightBlue}65-30%{ChatColors.Default}.";

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
            int[] alpha = { 170, 160, 150, 140, 130, 120 };
            Player.Controller.PlayerPawn.Value.RenderMode = RenderMode_t.kRenderTransColor;
            Player.Controller.PlayerPawn.Value.Render = Color.FromArgb(alpha[Level - 1], Player.Controller.PlayerPawn.Value.Render.R, Player.Controller.PlayerPawn.Value.Render.G, Player.Controller.PlayerPawn.Value.Render.B);
        }
    }

    public class SkillLevitationFP : WarcraftSkill
    {
        public override string InternalName => "levitation";
        public override string DisplayName => "Levitation";
        public override string Description => $"Experience {ChatColors.LightBlue}93-60%{ChatColors.Default} of gravity. Half damage taken while in the air.";

        public override int MaxLevel => 6;
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

    public class SkillClawAttack : WarcraftSkill
    {
        public override string InternalName => "claw_attack";
        public override string DisplayName => "Claw Attack";
        public override string Description => $"Force an enemies to drop their ammo and deal extra damage. 45 - 63% chance.";

        public override int MaxLevel => 6;
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

            int chance = Convert.ToInt32(Random.Shared.NextSingle() * 100);
            int playerChance = 45 + (3 * Level);
            if (chance > playerChance)
            {
                return;
            }

            CTakeDamageInfo damageInfo = hookData.GetParam<CTakeDamageInfo>(1);

            float modifier = 1.7f + (0.05f * Level);
            damageInfo.Damage *= modifier;

            CEntityInstance victimEnt = hookData.GetParam<CEntityInstance>(0);
            CCSPlayerPawn victimPawn = new CCSPlayerPawn(victimEnt.Handle);

            hookData.SetParam<CTakeDamageInfo>(1, damageInfo);

            victimPawn.WeaponServices!.ActiveWeapon.Value!.Clip1 = 0;
            Player.Controller.PrintToChat($"{WCS.Instance.ModuleChatPrefix}{ChatColors.Red}{victimPawn.Controller.Value!.PlayerName} {ChatColors.Gold}dropped their clip!");
        }
    }

    public class SkillBurningBlade : WarcraftSkill
    {
        public override string InternalName => "burning_blade";
        public override string DisplayName => "Burning Blade";
        public override string Description => $"Set an enemy on fire when attacking him. 30%";

        public override int MaxLevel => 1;
        public override int RequiredLevel => 0;

        public override void Load(IWarcraftPlayer player)
        {
            Player = player;

            HookEvent<EventPlayerHurt>("player_hurt_other", PlayerHurtOther);
        }

        public void PlayerHurtOther(GameEvent @obj)
        {
            if (Level == 0)
            {
                return;
            }

            var @event = (EventPlayerHurt)obj;

            int chance = Convert.ToInt32(Random.Shared.NextSingle() * 100);
            int playerChance = 30;
            if (chance > playerChance)
            {
                return;
            }

            CCSPlayerController victimController = @event.Userid;
            CCSPlayerPawn victimPawn = victimController.PlayerPawn.Value;

            CFire fire = Utilities.CreateEntityByName<CFire>("env_fire");
            fire.FireSize = 20.0f;
            fire.HeatLevel = 20.0f;
            fire.FireType = 1;
            fire.Spawnflags = 13;
            fire.DispatchSpawn();
            fire.Teleport(victimPawn!.CBodyComponent!.SceneNode!.AbsOrigin, victimPawn.CBodyComponent.SceneNode!.AbsRotation, new Vector());

            Player.Controller.PrintToChat($"{WCS.Instance.ModuleChatPrefix}{ChatColors.Red}{victimController.PlayerName} set on fire!");
        }
    }

    public class RaceFlamePredator : WarcraftRace
    {
        public override string InternalName => "flame_predator";
        public override string DisplayName => "Flame Predator";
        public override string Description => "Flame Predator";

        public override int MaxLevel => 32;

        public Restrictions restricts = new Restrictions();

        public override void Load(IWarcraftPlayer player)
        {
            Player = player;

            AddSkill(new SkillAdrenaline());
            AddSkill(new SkillCloakOfInvisibility());
            AddSkill(new SkillLevitationFP());
            AddSkill(new SkillClawAttack());
            AddSkill(new SkillBurningBlade());

            HookEvent<EventPlayerDeath>("player_spawn", OnPlayerSpawn);
            HookEvent<EventPlayerDeath>("player_death", OnPlayerDeath);
        }

        public void OnPlayerSpawn(GameEvent @event)
        {
            List<string> knifeOnly = restricts.GetAllWeapons();
            knifeOnly.Remove("weapon_knife");
            restricts.Restrict(Player.Controller, knifeOnly);
            Player.Controller.PrintToChat($"{WCS.Instance.ModuleChatPrefix}{ChatColors.Red}You {ChatColors.Default}have been {ChatColors.Green}restricted {ChatColors.Default}to {ChatColors.Red}knife only.");
        }

        public void OnPlayerDeath(GameEvent @event)
        {
            restricts.Clear(Player.Controller);
            Player.Controller.PlayerPawn.Value.RenderMode = RenderMode_t.kRenderTransColor;
            Player.Controller.PlayerPawn.Value.Render = Color.FromArgb(255, Player.Controller.PlayerPawn.Value.Render.R, Player.Controller.PlayerPawn.Value.Render.G, Player.Controller.PlayerPawn.Value.Render.B);
        }
    }
}
