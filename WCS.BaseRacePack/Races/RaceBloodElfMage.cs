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

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Events;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API;
using Microsoft.Extensions.Logging;
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
            bool canRespawn = CanRespawnFlag[Player.Controller.Handle];
            if (canRespawn)
            {
                @event.Userid.Respawn();
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
                if (Player.Controller.TeamNum == player.TeamNum)
                {
                    int moneyToAdd = 200 + (50 * Level);
                    player.InGameMoneyServices.Account += moneyToAdd;
                    player.PrintToChat($"{WCS.Instance.ModuleChatPrefix}{ChatColors.Blue}You {ChatColors.Default}were {ChatColors.Green}given {ChatColors.Gold}${moneyToAdd} {ChatColors.Default}by {ChatColors.Blue}{Player.Controller.PlayerName}.");
                }
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

        public Dictionary<IntPtr, int> Cooldowns = new Dictionary<IntPtr, int>();

        public override void Load(IWarcraftPlayer player)
        {
            Player = player;

            if (Player != null) damageReduction[Player.Controller.Handle] = 0;

            HookAbility(0, PlayerAbilityExec);

            HookVirtual("player_pre_hurt", PlayerPreHurt);
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
            int dmgRed = 10 + (5 * Level);
            damageReduction[Player.Controller.Handle] = dmgRed;

            Player.Controller.PrintToChat($"{WCS.Instance.ModuleChatPrefix}{ChatColors.Gold}Ice Barrier {ChatColors.Blue}({dmgRed} HP) {ChatColors.Default}has {ChatColors.Red}activated!");

            Cooldowns[Player.Controller.Handle] = 10;
            new Timer(1.0f, () => {
                Cooldowns[Player.Controller.Handle] = 9;
                Player.SetStatusMessage("Ice Barrier on Cooldown for 9 seconds.");
            }, TimerFlags.STOP_ON_MAPCHANGE);
            new Timer(2.0f, () => {
                Cooldowns[Player.Controller.Handle] = 8;
                Player.SetStatusMessage("Ice Barrier on Cooldown for 8 seconds.");
            }, TimerFlags.STOP_ON_MAPCHANGE);
            new Timer(3.0f, () => {
                Cooldowns[Player.Controller.Handle] = 7;
                Player.SetStatusMessage("Ice Barrier on Cooldown for 7 seconds.");
            }, TimerFlags.STOP_ON_MAPCHANGE);
            new Timer(4.0f, () => {
                Cooldowns[Player.Controller.Handle] = 6;
                Player.SetStatusMessage("Ice Barrier on Cooldown for 6 seconds.");
            }, TimerFlags.STOP_ON_MAPCHANGE);
            new Timer(5.0f, () => {
                Cooldowns[Player.Controller.Handle] = 5;
                Player.SetStatusMessage("Ice Barrier on Cooldown for 5 seconds.");
            }, TimerFlags.STOP_ON_MAPCHANGE);
            new Timer(6.0f, () => {
                Cooldowns[Player.Controller.Handle] = 4;
                Player.SetStatusMessage("Ice Barrier on Cooldown for 4 seconds.");
            }, TimerFlags.STOP_ON_MAPCHANGE);
            new Timer(7.0f, () => {
                Cooldowns[Player.Controller.Handle] = 3;
                Player.SetStatusMessage("Ice Barrier on Cooldown for 3 seconds.");
            }, TimerFlags.STOP_ON_MAPCHANGE);
            new Timer(8.0f, () => {
                Cooldowns[Player.Controller.Handle] = 2;
                Player.SetStatusMessage("Ice Barrier on Cooldown for 2 seconds.");
            }, TimerFlags.STOP_ON_MAPCHANGE);
            new Timer(9.0f, () => {
                Cooldowns[Player.Controller.Handle] = 1;
                Player.SetStatusMessage("Ice Barrier on Cooldown for 1 seconds.");
            }, TimerFlags.STOP_ON_MAPCHANGE);
            new Timer(10.0f, () => {
                Cooldowns[Player.Controller.Handle] = 0;
                Player.SetStatusMessage("Ice Barrier no longer on Cooldown.");
            }, TimerFlags.STOP_ON_MAPCHANGE);
        }
    }

    public class SkillCuringRitual : WarcraftSkill
    {
        public override string InternalName => "curing_ritual";
        public override string DisplayName => "Curing Ritual";
        public override string Description => $"Sacrifice $100 to heal yourself. Ultimate.";

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
                Player.Controller.PrintToCenterHtml($"<font color=#FFFFFF>Curing Ritual on Cooldown for {cooldown} seconds.</font>");
                return;
            }

            int cash = Player.Controller.InGameMoneyServices.Account;

            if (cash >= 100)
            {
                int hpToAdd = 20 + (5 * Level);
                Player.Controller.PlayerPawn.Value.Health += hpToAdd;
                Player.Controller.InGameMoneyServices.Account -= 100;
                Player.Controller.PrintToChat($"{WCS.Instance.ModuleChatPrefix}{ChatColors.Blue}You {ChatColors.Green}healed {ChatColors.Gold}{hpToAdd}HP {ChatColors.Default}for {ChatColors.Green}$100.");
            }

            Cooldowns[Player.Controller.Handle] = 10;
            new Timer(1.0f, () => {
                Cooldowns[Player.Controller.Handle] = 9;
                Player.SetStatusMessage("Curing Ritual on Cooldown for 9 seconds.");
            }, TimerFlags.STOP_ON_MAPCHANGE);
            new Timer(2.0f, () => {
                Cooldowns[Player.Controller.Handle] = 8;
                Player.SetStatusMessage("Curing Ritual on Cooldown for 8 seconds.");
            }, TimerFlags.STOP_ON_MAPCHANGE);
            new Timer(3.0f, () => {
                Cooldowns[Player.Controller.Handle] = 7;
                Player.SetStatusMessage("Curing Ritual on Cooldown for 7 seconds.");
            }, TimerFlags.STOP_ON_MAPCHANGE);
            new Timer(4.0f, () => {
                Cooldowns[Player.Controller.Handle] = 6;
                Player.SetStatusMessage("Curing Ritual on Cooldown for 6 seconds.");
            }, TimerFlags.STOP_ON_MAPCHANGE);
            new Timer(5.0f, () => {
                Cooldowns[Player.Controller.Handle] = 5;
                Player.SetStatusMessage("Curing Ritual on Cooldown for 5 seconds.");
            }, TimerFlags.STOP_ON_MAPCHANGE);
            new Timer(6.0f, () => {
                Cooldowns[Player.Controller.Handle] = 4;
                Player.SetStatusMessage("Curing Ritual on Cooldown for 4 seconds.");
            }, TimerFlags.STOP_ON_MAPCHANGE);
            new Timer(7.0f, () => {
                Cooldowns[Player.Controller.Handle] = 3;
                Player.SetStatusMessage("Curing Ritual on Cooldown for 3 seconds.");
            }, TimerFlags.STOP_ON_MAPCHANGE);
            new Timer(8.0f, () => {
                Cooldowns[Player.Controller.Handle] = 2;
                Player.SetStatusMessage("Curing Ritual on Cooldown for 2 seconds.");
            }, TimerFlags.STOP_ON_MAPCHANGE);
            new Timer(9.0f, () => {
                Cooldowns[Player.Controller.Handle] = 1;
                Player.SetStatusMessage("Curing Ritual on Cooldown for 1 seconds.");
            }, TimerFlags.STOP_ON_MAPCHANGE);
            new Timer(10.0f, () => {
                Cooldowns[Player.Controller.Handle] = 0;
                Player.SetStatusMessage("Curing Ritual no longer on Cooldown.");
            }, TimerFlags.STOP_ON_MAPCHANGE);
        }
    }

    public class RaceBloodElfMage : WarcraftRace
    {
        public override string InternalName => "blood_elf_mage";
        public override string DisplayName => "Blood Elf Mage";
        public override string Description => "Blood Elf Mage";

        public override int MaxLevel => 32;

        public override void Load(IWarcraftPlayer player)
        {
            Player = player;

            AddSkill(new SkillPhoenix());
            AddSkill(new SkillArcaneBrilliance());
            AddSkill(new SkillIceBarrier());
            AddSkill(new SkillCuringRitual());
        }
    }
}