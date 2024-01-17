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
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Modules.Timers;
using WCS.API;

namespace WCS.Races
{
    public class SkillPhoenix : WarcraftSkill
    {
        public override string InternalName => "phoenix";
        public override string DisplayName => "Phoenix";
        public override string Description => $"Respawn the first teammate that dies.";

        public override int MaxLevel => 8;
        public override int RequiredLevel => 0;

        public Dictionary<IntPtr, bool> CanRespawnFlag = new Dictionary<IntPtr, bool>();

        public override void Load(IWarcraftPlayer player)
        {
            Player = player;

            if (Player != null) CanRespawnFlag[Player.Controller.Handle] = false;

            WCS.Instance.RegisterEventHandler<EventPlayerDeath>(OtherPlayerDeath, HookMode.Post);

            HookEvent<EventRoundStart>("round_start", RoundStart);
        }

        private HookResult OtherPlayerDeath(EventPlayerDeath @event, GameEventInfo _)
        {
            if (Player == null) return HookResult.Continue;
            bool canRespawn = CanRespawnFlag[Player.Controller.Handle];
            CCSPlayerController target = @event.Userid;
            if (canRespawn && target.TeamNum == Player.Controller.TeamNum)
            {
                Timer _t = new Timer(1.0f, @event.Userid.Respawn, TimerFlags.STOP_ON_MAPCHANGE);
                @event.Userid.PrintToChat($"{WCS.Instance.ModuleChatPrefix}{ChatColors.Blue}{Player.Controller.PlayerName} {ChatColors.Default}has {ChatColors.Green}respawned {ChatColors.Gold}you!");
                Player.Controller.PrintToChat($"{WCS.Instance.ModuleChatPrefix}{ChatColors.Blue}You {ChatColors.Default}have {ChatColors.Green}respawned {ChatColors.Gold}{@event.Userid.PlayerName}!");
                CanRespawnFlag[Player.Controller.Handle] = false;
            }

            return HookResult.Continue;
        }

        private void RoundStart(GameEvent @obj)
        {
            CanRespawnFlag[Player.Controller.Handle] = true;
        }
    }

    public class SkillArcaneBrilliance : WarcraftSkill
    {
        public override string InternalName => "arcane_brilliance";
        public override string DisplayName => "Arcane Brilliance";
        public override string Description => $"Give your entire friendly team increased $ upon round start.";

        public override int MaxLevel => 8;
        public override int RequiredLevel => 0;

        public override void Load(IWarcraftPlayer player)
        {
            Player = player;

            HookEvent<EventRoundStart>("round_start", RoundStart);
        }

        private void RoundStart(GameEvent @obj)
        {
            var playerEntities = Utilities.FindAllEntitiesByDesignerName<CCSPlayerController>("cs_player_controller");
            foreach (CCSPlayerController player in playerEntities)
            {
                if (Player != null && Player.Controller.TeamNum == player.TeamNum)
                {
                    int moneyToAdd = 200 + (50 * Level);
                    player.InGameMoneyServices.Account += moneyToAdd;
                    player.PrintToChat($"{WCS.Instance.ModuleChatPrefix}{ChatColors.Blue}You {ChatColors.Default}were {ChatColors.Green}given {ChatColors.Gold}${moneyToAdd} {ChatColors.Default}by {ChatColors.Blue}{Player.Controller.PlayerName}.");
                }
            }
        }
    }

    public class SkillSiphonMana : WarcraftSkill
    {
        public override string InternalName => "siphon_mana";
        public override string DisplayName => "Siphon Mana";
        public override string Description => $"Steal cash from enemies that you attack. 10-30%.";

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

            int chance = Convert.ToInt32(Random.Shared.NextSingle() * 100);
            int playerChance = Convert.ToInt32(10 + (2.5 * Level));
            if (chance < playerChance && victim.InGameMoneyServices.Account >= 200)
            {
                victim.InGameMoneyServices.Account -= 200;
                Player.Controller.InGameMoneyServices.Account += 200;
                if (victim.IsBot) Player.Controller.PrintToChat($"{WCS.Instance.ModuleChatPrefix}{ChatColors.Blue}You {ChatColors.Red}stole {ChatColors.Gold}$200 {ChatColors.Default}from {ChatColors.Red}{victim.PlayerPawn.Value.Bot.Name}!");
                else Player.Controller.PrintToChat($"{WCS.Instance.ModuleChatPrefix}{ChatColors.Blue}You {ChatColors.Red}stole {ChatColors.Gold}$200 {ChatColors.Default}from {ChatColors.Red}{victim.PlayerName}!");
            }
        }
    }

    public class SkillIceBarrier : WarcraftSkill
    {
        public override string InternalName => "ice_barrier";
        public override string DisplayName => "Ice Barrier";
        public override string Description => $"Absorb 10 - 50 damage with your barrier. Ability.";

        public override int MaxLevel => 8;
        public override int RequiredLevel => 0;

        public Dictionary<IntPtr, int> damageReduction = new Dictionary<IntPtr, int>();

        public Cooldowns cooldowns = new Cooldowns();

        public override void Load(IWarcraftPlayer player)
        {
            Player = player;

            if (player != null)
            {
                damageReduction[Player.Controller.Handle] = 0;
                cooldowns.SetCooldown(player.Controller, "ice_barrier", 0);
                cooldowns.AddCooldownExtension(player.Controller, "ice_barrier", OnCooldownChange);
            }

            HookAbility(0, PlayerAbilityExec);

            HookVirtual("player_pre_hurt", PlayerPreHurt);
        }

        public void OnCooldownChange(float value)
        {
            if (value == 0)
            {
                Player.SetStatusMessage($"Ice Barrier no longer on Cooldown!");
                return;
            }
            Player.SetStatusMessage($"Ice Barrier on Cooldown for {value} seconds.");
        }

        private void PlayerPreHurt(DynamicHook hookData)
        {
            int reduction = damageReduction[Player.Controller.Handle];

            if (reduction > 0)
            {
                CTakeDamageInfo damageInfo = hookData.GetParam<CTakeDamageInfo>(1);
                if (damageInfo.Damage > reduction)
                {
                    damageInfo.Damage -= reduction;
                    Player.Controller.PrintToChat($"{WCS.Instance.ModuleChatPrefix}{ChatColors.Blue}Your {ChatColors.Gold}Barrier {ChatColors.Default}has {ChatColors.Red}broken!");
                }
                else
                {
                    damageReduction[Player.Controller.Handle] -= Convert.ToInt32(damageInfo.Damage);
                    damageInfo.Damage = 0;
                }
            }
        }

        private void PlayerAbilityExec()
        {
            float cooldown = cooldowns.GetCooldown(Player.Controller, "ice_barrier");

            if (cooldown == 0)
            {
                int dmgRed = 10 + (5 * Level);
                damageReduction[Player.Controller.Handle] = dmgRed;

                Player.Controller.PrintToChat($"{WCS.Instance.ModuleChatPrefix}{ChatColors.Gold}Ice Barrier {ChatColors.Blue}({dmgRed} HP) {ChatColors.Default}has {ChatColors.Red}activated!");

                cooldowns.SetCooldown(Player.Controller, "ice_barrier", 10);
            }
        }
    }

    public class SkillCuringRitual : WarcraftSkill
    {
        public override string InternalName => "curing_ritual";
        public override string DisplayName => "Curing Ritual";
        public override string Description => $"Sacrifice $100 to heal yourself. Ultimate.";

        public override int MaxLevel => 8;
        public override int RequiredLevel => 0;

        public Cooldowns cooldowns = new Cooldowns();

        public override void Load(IWarcraftPlayer player)
        {
            Player = player;

            if (player != null)
            {
                cooldowns.SetCooldown(player.Controller, "curing_ritual", 0);
                cooldowns.AddCooldownExtension(player.Controller, "curing_ritual", OnCooldownChange);
            }

            HookAbility(1, PlayerUltimate);
        }

        public void OnCooldownChange(float value)
        {
            if (value == 0)
            {
                Player.SetStatusMessage($"Curing Ritual no longer on Cooldown!");
                return;
            }
            Player.SetStatusMessage($"Curing Ritual on Cooldown for {value} seconds.");
        }

        private void PlayerUltimate()
        {
            if (Level < 1) return;
            float cooldown = cooldowns.GetCooldown(Player.Controller, "curing_ritual");

            if (cooldown == 0)
            {
                int cash = Player.Controller.InGameMoneyServices.Account;

                if (cash >= 100)
                {
                    int hpToAdd = 20 + (5 * Level);
                    Player.Controller.PlayerPawn.Value.Health += hpToAdd;
                    Utilities.SetStateChanged(Player.Controller.PlayerPawn.Value, "CBaseEntity", "m_iHealth");
                    Player.Controller.InGameMoneyServices.Account -= 100;
                    Player.Controller.PrintToChat($"{WCS.Instance.ModuleChatPrefix}{ChatColors.Blue}You {ChatColors.Green}healed {ChatColors.Gold}{hpToAdd}HP {ChatColors.Default}for {ChatColors.Green}$100.");
                }

                cooldowns.SetCooldown(Player.Controller, "curing_ritual", 10);
            }
        }
    }

    public class RaceBloodElfMage : WarcraftRace
    {
        public override string InternalName => "blood_elf_mage";
        public override string DisplayName => "Blood Elf Mage";
        public override string Description => "Blood Elf Mage";

        public override int MaxLevel => 40;

        public override void Load(IWarcraftPlayer player)
        {
            Player = player;

            AddSkill(new SkillPhoenix());
            AddSkill(new SkillArcaneBrilliance());
            AddSkill(new SkillSiphonMana());
            AddSkill(new SkillIceBarrier());
            AddSkill(new SkillCuringRitual());
        }
    }
}