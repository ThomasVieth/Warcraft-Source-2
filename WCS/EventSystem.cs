﻿/*
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
using System.Reflection;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;
using WCS.API;
using WCS.Races;

namespace WCS
{
    public class EventSystem
    {
        private WCS _plugin;

        public EventSystem(WCS plugin)
        {
            _plugin = plugin;
        }

        public void Initialize()
        {
            _plugin.RegisterEventHandler<EventPlayerDeath>(PlayerDeathHandler);
            _plugin.RegisterEventHandler<EventPlayerSpawn>(PlayerSpawnHandler);
            _plugin.RegisterEventHandler<EventPlayerHurt>(PlayerHurtHandler);
            _plugin.RegisterEventHandler<EventBombPlanted>(BombPlantHandler);
            _plugin.RegisterEventHandler<EventBombDefused>(BombDefuseHandler);
            _plugin.RegisterEventHandler<EventBombExploded>(BombExplodeHandler);
            _plugin.RegisterEventHandler<EventRoundStart>(RoundStartHandler);
            _plugin.RegisterEventHandler<EventRoundEnd>(RoundEndHandler);

            VirtualFunctions.CBaseEntity_TakeDamageOldFunc.Hook(OnTakeDamage, HookMode.Pre);
        }

        private HookResult OnTakeDamage(DynamicHook hookData)
        {
            CEntityInstance victimEnt = hookData.GetParam<CEntityInstance>(0);
            CTakeDamageInfo damageInfo = hookData.GetParam<CTakeDamageInfo>(1);

            if (victimEnt.Handle == 0 || victimEnt.DesignerName == "worldent")
            {
                return HookResult.Continue;
            }

            CCSPlayerPawn victimPawn = new CCSPlayerPawn(victimEnt.Handle);

            if (victimPawn.Handle == 0 || victimPawn.Controller.Value.Handle == 0)
            {
                return HookResult.Continue;
            }

            IWarcraftPlayer victim = _plugin.WarcraftPlayers[victimPawn.Controller.Value.Handle];

            CCSPlayerPawn attackerPawn = new CCSPlayerPawn(damageInfo.Attacker.Value.Handle);

            if (attackerPawn.Handle == 0 || attackerPawn.DesignerName == "worldent")
            {
                return HookResult.Continue;
            }

            CCSPlayerController attackerController = new CCSPlayerController(attackerPawn.Controller.Value.Handle);

            IWarcraftPlayer attacker = attackerController.GetWarcraftPlayer();

            attacker?.GetRace()?.InvokeVirtual("player_pre_hurt_other", hookData);
            victim?.GetRace()?.InvokeVirtual("player_pre_hurt", hookData);

            return HookResult.Continue;
        }

        private HookResult BombPlantHandler(EventBombPlanted @event, GameEventInfo _)
        {
            CCSPlayerController player = @event.Userid;

            IWarcraftPlayer wcPlayer = player.GetWarcraftPlayer();
            IWarcraftRace race = wcPlayer?.GetRace();
            int experienceToAdd = _plugin.configuration.experience.BombPlantExperience;

            if (race != null && experienceToAdd > 0)
            {
                race.AddExperience(experienceToAdd);
                string xpString = $" {ChatColors.Gold}+{experienceToAdd} XP {ChatColors.Default}for planting the bomb!";
                player.PrintToChat(xpString);
            }

            return HookResult.Continue;
        }

        private HookResult BombDefuseHandler(EventBombDefused @event, GameEventInfo _)
        {
            CCSPlayerController player = @event.Userid;

            IWarcraftPlayer wcPlayer = player.GetWarcraftPlayer();
            IWarcraftRace race = wcPlayer?.GetRace();
            int experienceToAdd = _plugin.configuration.experience.BombDefuseExperience;

            if (race != null && experienceToAdd > 0)
            {
                race.AddExperience(experienceToAdd);
                string xpString = $" {ChatColors.Gold}+{experienceToAdd} XP {ChatColors.Default}for defusing the bomb!";
                player.PrintToChat(xpString);
            }

            return HookResult.Continue;
        }

        private HookResult BombExplodeHandler(EventBombExploded @event, GameEventInfo _)
        {
            CCSPlayerController player = @event.Userid;

            IWarcraftPlayer wcPlayer = player.GetWarcraftPlayer();
            IWarcraftRace race = wcPlayer?.GetRace();
            int experienceToAdd = _plugin.configuration.experience.BombExplodeExperience;

            if (race != null && experienceToAdd > 0)
            {
                race.AddExperience(experienceToAdd);
                string xpString = $" {ChatColors.Gold}+{experienceToAdd} XP {ChatColors.Default}for the bomb exploding!";
                player.PrintToChat(xpString);
            }

            return HookResult.Continue;
        }

        private HookResult RoundStartHandler(EventRoundStart @event, GameEventInfo _)
        {
            var playerEntities = Utilities.FindAllEntitiesByDesignerName<CCSPlayerController>("cs_player_controller");
            foreach (CCSPlayerController player in playerEntities)
            {
                IWarcraftPlayer wcPlayer = player.GetWarcraftPlayer();
                IWarcraftRace race = wcPlayer?.GetRace();
                race.InvokeEvent("round_start", @event);
            }

            return HookResult.Continue;
        }

        private HookResult RoundEndHandler(EventRoundEnd @event, GameEventInfo _)
        {
            int winner = @event.Winner;

            var playerEntities = Utilities.FindAllEntitiesByDesignerName<CCSPlayerController>("cs_player_controller");
            foreach (CCSPlayerController player in playerEntities)
            {
                if (player.TeamNum == winner)
                {
                    IWarcraftPlayer wcPlayer = player.GetWarcraftPlayer();
                    IWarcraftRace race = wcPlayer?.GetRace();
                    int experienceToAdd = _plugin.configuration.experience.RoundWinExperience;

                    if (race != null && experienceToAdd > 0)
                    {
                        race.AddExperience(experienceToAdd);
                        string xpString = $" {ChatColors.Gold}+{experienceToAdd} XP {ChatColors.Default}for winning the round!";
                        player.PrintToChat(xpString);
                    }
                }
                else if (player.TeamNum == (5 - winner))
                {
                    IWarcraftPlayer wcPlayer = player.GetWarcraftPlayer();
                    IWarcraftRace race = wcPlayer?.GetRace();
                    int experienceToAdd = _plugin.configuration.experience.RoundLossExperience;

                    if (race != null && experienceToAdd > 0)
                    {
                        race.AddExperience(experienceToAdd);
                        string xpString = $" {ChatColors.Gold}+{experienceToAdd} XP {ChatColors.Default}for losing the round!";
                        player.PrintToChat(xpString);
                    }
                }
            }

            return HookResult.Continue;
        }

        private HookResult PlayerHurtHandler(EventPlayerHurt @event, GameEventInfo _)
        {
            var victim = @event.Userid;
            var attacker = @event.Attacker;

            if (attacker.DesignerName != "cs_player_controller")
                return HookResult.Continue;

            victim?.GetWarcraftPlayer()?.GetRace()?.InvokeEvent("player_hurt", @event);
            attacker?.GetWarcraftPlayer()?.GetRace()?.InvokeEvent("player_hurt_other", @event);

            return HookResult.Continue;
        }

        private HookResult PlayerSpawnHandler(EventPlayerSpawn @event, GameEventInfo _)
        {
            var player = @event.Userid;
            if (player.IsValid)
            {
                var wcPlayer = player.GetWarcraftPlayer();
                var race = wcPlayer?.GetRace();

                if (race != null && wcPlayer.IsReady)
                {
                    var name = @event.EventName;
                    Server.NextFrame(() =>
                    {
                        race.InvokeEvent(name, @event);
                    });
                }
            }

            return HookResult.Handled;
        }

        private HookResult PlayerDeathHandler(EventPlayerDeath @event, GameEventInfo _)
        {
            var attacker = @event.Attacker;
            var victim = @event.Userid;
            var headshot = @event.Headshot;

            if (attacker.IsValid && victim.IsValid && (attacker.Index != victim.Index))
            {
                var weaponName = attacker.PlayerPawn.Value.WeaponServices.ActiveWeapon.Value.DesignerName;

                int experienceToAdd = 0;
                int experienceHeadshot = 0;
                int experienceKnife = 0;

                experienceToAdd = _plugin.configuration.experience.KillExperience;

                if (headshot)
                    experienceHeadshot = Convert.ToInt32(experienceToAdd * _plugin.configuration.experience.HeadshotMultiplier);

                if (weaponName == "weapon_knife")
                    experienceKnife = Convert.ToInt32(experienceToAdd * _plugin.configuration.experience.KnifeMultiplier);

                experienceToAdd += experienceHeadshot + experienceKnife;

                attacker.GetWarcraftPlayer()?.GetRace()?.AddExperience(experienceToAdd);

                string hsBonus = "";
                if (experienceHeadshot != 0)
                {
                    hsBonus = $"(+{experienceHeadshot} HS bonus)";
                }

                string knifeBonus = "";
                if (experienceKnife != 0)
                {
                    knifeBonus = $"(+{experienceKnife} knife bonus)";
                }

                string xpString = $" {ChatColors.Gold}+{experienceToAdd} XP {ChatColors.Default}for killing {ChatColors.Green}{victim.PlayerName} {ChatColors.Default}{hsBonus}{knifeBonus}";

                attacker.GetWarcraftPlayer()?.SetStatusMessage(xpString);
                attacker.PrintToChat(xpString);

                attacker?.GetWarcraftPlayer()?.GetRace()?.InvokeEvent("player_kill", @event);
            }

            victim?.GetWarcraftPlayer()?.GetRace()?.InvokeEvent("player_death", @event);
            
            return HookResult.Continue;
        }
    }
}