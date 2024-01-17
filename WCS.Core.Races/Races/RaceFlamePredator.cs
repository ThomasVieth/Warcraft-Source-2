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
            Utilities.SetStateChanged(Player.Controller.PlayerPawn.Value, "CBaseEntity", "m_iHealth");
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

        public static string acceptInputWindowsSig = @"\x48\x89\x5C\x24\x10\x48\x89\x74\x24\x18\x57\x48\x83\xEC\x40\x49\x8B\xF0";
        public static string acceptInputLinuxSig = @"\x55\x48\x89\xE5\x41\x57\x49\x89\xFF\x41\x56\x48\x8D\x7D\xC0";

        public static MemoryFunctionVoid<nint, string, nint, nint, nint, int> AcceptEntityInputFunc = new(RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? acceptInputLinuxSig : acceptInputWindowsSig);
        public static Action<nint, string, nint, nint, nint, int> AcceptEntityInput = AcceptEntityInputFunc.Invoke;

        [StructLayout(LayoutKind.Sequential)]
        public struct variant_t
        {
            public nint valuePtr;
            public fieldtype_t fieldType;
            public byte unk0;
        }

        public unsafe void AcceptInput(nint handle, string inputName, nint activator, nint caller, string value)
        {
            byte[] str_bytes = Encoding.ASCII.GetBytes(value + "\0");

            variant_t* _param = (variant_t*)Marshal.AllocHGlobal(0xA);
            IntPtr param_str_ptr = Marshal.AllocHGlobal(str_bytes.Length);

            _param->fieldType = fieldtype_t.FIELD_STRING;
            _param->valuePtr = param_str_ptr;

            Marshal.Copy(str_bytes, 0, param_str_ptr, str_bytes.Length);

            AcceptEntityInput(handle, inputName, activator, caller, (nint)_param, 0);

            Marshal.FreeHGlobal(param_str_ptr);
            Marshal.FreeHGlobal((nint)_param);
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

            CMolotovProjectile fireProjectile = Utilities.CreateEntityByName<CMolotovProjectile>("molotov_projectile");
            CGameSceneNode node = victimPawn.CBodyComponent.SceneNode;
            Vector pos = node!.AbsOrigin;
            pos.Z += 10;
            fireProjectile.TeamNum = Player.Controller.TeamNum;
            fireProjectile.Damage = 45.0f;
            fireProjectile.DmgRadius = 50;
            fireProjectile.Teleport(pos, node!.AbsRotation, new Vector(0, 0, -10));
            fireProjectile.DispatchSpawn();
            AcceptInput(fireProjectile.Handle, "InitializeSpawnFromWorld", Player.Controller.PlayerPawn.Value!.Handle, Player.Controller.PlayerPawn.Value!.Handle, "");

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
