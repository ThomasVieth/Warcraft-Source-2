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
using System.Linq;
using WCS.Races;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using Newtonsoft.Json;
using System.IO;
using WCS.API;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace WCS
{
    public static class WarcraftPlayerExtensions
    {
        public static IWarcraftPlayer GetWarcraftPlayer(this CCSPlayerController player)
        {
            IWarcraftPlayer wcPlayer = null;
            WCS.Instance.WarcraftPlayers.TryGetValue(player.Handle, out wcPlayer);
            if (wcPlayer == null)
            {
                if (!WCS.Instance.database.ClientExistsInDatabase(player.GetSteamID()))
                {
                    WCS.Instance.database.AddNewClientToDatabase(player);
                }

                WCS.Instance.WarcraftPlayers[player.Handle] = WCS.Instance.database.LoadClientFromDatabase(WCS.Instance._raceManager, player);
            }
            return wcPlayer;
        }

        public static ulong GetSteamID(this CCSPlayerController player)
        {
            ulong steamid = player.SteamID;
            if (player.IsBot)
                steamid = unchecked((uint)player.PlayerName.GetHashCode());
            return steamid;
        }
    }



    public class WarcraftPlayer : IWarcraftPlayer
    {
        public bool IsReady { get; set; } = false;

        public CCSPlayerController Controller { get; init; }

        public string statusMessage { get; set; }

        private IWarcraftRace race;

        public WarcraftPlayer(CCSPlayerController player)
        {
            Controller = player;
            WCS.Instance.WarcraftPlayers[player.Handle] = this;
        }
        public void Tell(string message)
        {
            Controller.PrintToChat($"{WCS.Instance.ModuleChatPrefix}{message}");
        }

        public void delete()
        {
            WCS.Instance.WarcraftPlayers.Remove(Controller.Handle);
        }
        public void LoadFromDatabase(DatabaseRaceInformation dbRace, DatabaseSkillInformation[] dbSkills)
        {
            race = WCS.Instance._raceManager.InstantiateRace(dbRace.RaceName, this);

            if (!Controller.IsBot)
                Controller.Clan = $"[{race.DisplayName}]";
            else
            {
                //NativeAPI.SetSchemaValueByName<string>(Controller.PlayerPawn.Value.Bot.Handle, 0, "CCSBot", "m_name", $"[{race.DisplayName}] {Controller.PlayerName}");
            }


            race.Level = dbRace.Level;
            race.Experience = dbRace.Xp;

            foreach (DatabaseSkillInformation dbSkill in dbSkills)
            {
                WCS.Instance._logger.LogInformation($"Loading skill: {dbSkill.SkillName}");
                WarcraftSkill skill = (WarcraftSkill)race.GetSkillByName(dbSkill.SkillName);
                skill.Level = dbSkill.Level;
                WCS.Instance._logger.LogInformation($"Skill loaded: {skill.InternalName} (Level {skill.Level})");
            }
        }

        public int TotalLevel
        {
            get
            {
                return WCS.Instance.database.GetClientTotalLevel(Controller);
            }
        }

        public override string ToString()
        {
            return $"{race.DisplayName}: LV {race.Level} | {race.Experience}/{race.RequiredExperience} XP";
        }

        public IWarcraftRace GetRace()
        {
            return race;
        }

        public void QuickChangeRace(string raceName)
        {
            race = (WarcraftRace)WCS.Instance._raceManager.GetRace(raceName);
        }

        public void SetStatusMessage(string status, float duration = 2f)
        {
            statusMessage = status;
        }
    }


    public class WCSServiceCollection : IPluginServiceCollection<WCS>
    {
        public void ConfigureServices(IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<IRaceManager, RaceManager>();
        }
    }


    public class WCS : BasePlugin
    {
        private static WCS _instance;
        public static WCS Instance => _instance;

        public override string ModuleName => "WarcraftSource2";
        public override string ModuleVersion => "2.3.0";

        public string ModuleChatPrefix;

        public int AdvertIndex = 0;
        public string[] AdvertStrings;

        private List<ulong> Admins;
        private Dictionary<IntPtr, Tuple<IntPtr, AdminAction>> AdminDataCache = new();
        private int[] AdminNumberOptions = { 1, 5, 10, 20, 50, 100 };

        public ILogger<WCS> _logger;

        public Dictionary<IntPtr, IWarcraftPlayer> WarcraftPlayers = new();
        public Cooldowns cooldownSystem;
        public EventSystem eventSystem;
        public Utils utilities;
        public Restrictions restrictions;
        public WarcraftDatabase database;
        public WarcraftConfig configuration;
        public IRaceManager _raceManager;

        public WCS(ILogger<WCS> logger, IRaceManager raceManager)
        {
            _raceManager = raceManager;
            _raceManager.wcs = this;
            _logger = logger;
        }


        private void readConfig()
        {
            if (!Directory.Exists(Path.Join(ModuleDirectory, "Config")))
            {
                _logger.LogWarning("Config directory not found, creating one.");
                Directory.CreateDirectory(Path.Join(ModuleDirectory, "Config"));
            }

            if (!File.Exists(Path.Join(ModuleDirectory, "Config", "core.json")))
            {
                using (StreamWriter writer = new StreamWriter(Path.Join(ModuleDirectory, "Config", "core.json")))
                {
                    _logger.LogWarning("Core config not found, creating one from default.");
                    configuration = new WarcraftConfig();
                    writer.Write(JsonConvert.SerializeObject(configuration, Formatting.Indented));
                    return;
                }
            }

            using (StreamReader reader = new StreamReader(Path.Join(ModuleDirectory, "Config", "core.json")))
            {
                string json = reader.ReadToEnd();
                configuration = JsonConvert.DeserializeObject<WarcraftConfig>(json);
            }
        }
        // LOAD AND UNLOAD
        public override void Load(bool hotReload)
        {
            base.Load(hotReload);

            if (_instance == null) _instance = this;

            database = new WarcraftDatabase();

            cooldownSystem = new Cooldowns(this);
            cooldownSystem.Initialize();

            _raceManager.Load();

            utilities = new Utils(this);
            utilities.Initialize();

            eventSystem = new EventSystem(this);
            eventSystem.Initialize();

            restrictions = new Restrictions(this);
            restrictions.Initialize(hotReload);

            database.Initialize(ModuleDirectory);

            AddCommand("ability", "ability", AbilityPressed);
            AddCommand("ultimate", "ultimate", UltimatePressed);

            AddCommand("changerace", "changerace", CommandChangeRace);
            AddCommand("raceinfo", "raceinfo", CommandRaceInfo);
            AddCommand("resetskills", "resetskills", CommandResetSkills);
            AddCommand("spendskills", "spendskills", (client, _) => ShowSkillPointMenu(client.GetWarcraftPlayer()));
            AddCommand("wcsadmin", "wcsadmin", ShowAdminMenu);
            AddCommand("wcs", "wcs", ShowCoreMenu);

            RegisterListener<Listeners.OnClientConnect>(OnClientPutInServerHandler);
            RegisterListener<Listeners.OnClientDisconnect>(OnClientDisconnectHandler);
            RegisterListener<Listeners.OnMapStart>(OnMapStartHandler);

            if (hotReload)
            {
                OnMapStartHandler(null);
            }


            readConfig();

            Admins = configuration.Admins;

            ModuleChatPrefix = $" {ChatColors.Green}{configuration.ChatPrefix} ";
            AdvertStrings = new string[]{
                $" {ChatColors.Green}{configuration.AdvertChatPrefix} {ChatColors.Default}This Server is running Warcraft CS2!",
                $" {ChatColors.Green}{configuration.AdvertChatPrefix} {ChatColors.Default}Bind {ChatColors.Gold}F1, F2, etc {ChatColors.Default}to {ChatColors.Gold}\"css_1\", \"css_2\", etc {ChatColors.Default}to use menus.",
                $" {ChatColors.Green}{configuration.AdvertChatPrefix} {ChatColors.Default}Type {ChatColors.Gold}changerace {ChatColors.Default}in {ChatColors.Gold}the console {ChatColors.Default}to change your race.",
                $" {ChatColors.Green}{configuration.AdvertChatPrefix} {ChatColors.Default}Type {ChatColors.Gold}spendskills {ChatColors.Default}in {ChatColors.Gold}the console {ChatColors.Default}to spend skill points.",
                $" {ChatColors.Green}{configuration.AdvertChatPrefix} {ChatColors.Default}Type {ChatColors.Gold}resetskills {ChatColors.Default}in {ChatColors.Gold}the console {ChatColors.Default}to reset your skill points.",
            };
        }

        public override void Unload(bool hotReload)
        {
            base.Unload(hotReload);           
        }

        // CLIENT HANDLERS
        private void OnClientPutInServerHandler(int slot, string name, string ipAddress)
        {
            var player = new CCSPlayerController(NativeAPI.GetEntityFromIndex(slot + 1));

            if (!player.IsValid) return;

            if (!database.ClientExistsInDatabase(player.GetSteamID()))
            {
                database.AddNewClientToDatabase(player);
            }

            WarcraftPlayers[player.Handle] = database.LoadClientFromDatabase(_raceManager, player);
        }

        private void OnClientDisconnectHandler(int slot)
        {
            var player = new CCSPlayerController(NativeAPI.GetEntityFromIndex(slot + 1));

            if (!player.IsValid) return;

            var wcPlayer = player.GetWarcraftPlayer();
            database.SaveClientToDatabase(wcPlayer);
            wcPlayer.delete();
        }

        // REPEATERS
        private void OnMapStartHandler(string mapName)
        {
            AddTimer(0.5f, PlayerInfoUpdate, TimerFlags.REPEAT | TimerFlags.STOP_ON_MAPCHANGE);
            AddTimer(60.0f, database.SaveClients, TimerFlags.REPEAT | TimerFlags.STOP_ON_MAPCHANGE);
            AddTimer(120.0f, RunAdverts, TimerFlags.REPEAT | TimerFlags.STOP_ON_MAPCHANGE);
        }

        private void RunAdverts()
        {
            string ToSay = AdvertStrings[AdvertIndex];
            Server.PrintToChatAll(ToSay);

            AdvertIndex += 1;
            if (AdvertIndex >= AdvertStrings.Length) AdvertIndex = 0;
        }

        private void PlayerInfoUpdate()
        {
            var playerEntities = Utilities.FindAllEntitiesByDesignerName<CCSPlayerController>("cs_player_controller");
            foreach (var player in playerEntities)
            {
                if (!player.IsValid || !player.PawnIsAlive) continue;

                var wcPlayer = player.GetWarcraftPlayer();

                if (wcPlayer == null) continue;

                var race = wcPlayer.GetRace();

                var message = $"{race.DisplayName} ({race.Level}/{race.MaxLevel})\n" +
                              $"Experience: {race.Experience}/{race.RequiredExperience}\n" +
                              $"{wcPlayer.statusMessage}";

                player.PrintToCenter(message);
            }
        }

        // CLIENT COMMAND LISTENERS
        private void CommandResetSkills(CCSPlayerController? client, CommandInfo? commandinfo)
        {
            var wcPlayer = client.GetWarcraftPlayer();

            foreach (WarcraftSkill skill in wcPlayer.GetRace().GetSkills())
            {
                skill.Level = 0;
            }
        }

        private void AbilityPressed(CCSPlayerController? client, CommandInfo commandinfo)
        {
            client.GetWarcraftPlayer()?.GetRace()?.InvokeAbility(0);
        }

        private void UltimatePressed(CCSPlayerController? client, CommandInfo commandinfo)
        {
            client.GetWarcraftPlayer()?.GetRace()?.InvokeAbility(1);
        }

        // MENUS
        private void ShowCoreMenu(CCSPlayerController? client, CommandInfo commandinfo)
        {
            var menu = new ChatMenu("WCS2 Core Menu");

            menu.AddMenuOption("Change Race", (player, option) => CommandChangeRace(player, null), false);
            menu.AddMenuOption("Spend Skills", (player, option) => ShowSkillPointMenu(player.GetWarcraftPlayer()), false);
            menu.AddMenuOption("Reset Skills", (player, option) => CommandResetSkills(player, null), false);
            menu.AddMenuOption("Race Information", (player, option) => CommandRaceInfo(player, null), false);
            menu.AddMenuOption("Player Information", (player, option) => CommandPlayerInfo(player, null), false);
            if (Admins.Contains<ulong>(client.GetSteamID()))
            {
                menu.AddMenuOption("Admin Panel", (player, option) => ShowAdminMenu(player, null), false);
            }

            ChatMenus.OpenMenu(client, menu);
        }

        public void CommandPlayerInfo(CCSPlayerController? player, CommandInfo commandinfo)
        {
            var menu = new ChatMenu($"WCS2 Player Info");

            var playerEntities = Utilities.FindAllEntitiesByDesignerName<CCSPlayerController>("cs_player_controller");
            foreach (var target in playerEntities)
            {
                menu.AddMenuOption(
                    target.PlayerName,
                    (player, option) => {
                        ShowPlayerInfoMenu(player, target);
                    },
                    false
                );
            }

            ChatMenus.OpenMenu(player, menu);
        }

        private void ShowPlayerInfoMenu(CCSPlayerController? client, CCSPlayerController? target)
        {
            client.PrintToChat($"{ChatColors.Green}{target.PlayerName}");

            var race = target.GetWarcraftPlayer().GetRace();

            client.PrintToChat($"{ChatColors.Red}{race.DisplayName} ({race.Level}/{race.MaxLevel})");

            int i = 0;
            foreach (WarcraftSkill skill in race.GetSkills())
            {
                char color = (i % 2 == 0) ? ChatColors.Gold : ChatColors.Purple;

                client.PrintToChat($" {color}{skill.DisplayName} ({skill.Level}/{skill.MaxLevel})");

                i += 1;
            }
        }

        private void CommandRaceInfo(CCSPlayerController? client, CommandInfo commandinfo)
        {
            var menu = new ChatMenu("WCS2 Race Info");
            var races = _raceManager.GetAllRaces();
            foreach (var race in races.OrderBy(x => x.Requirement).ThenBy(x => x.DisplayName))
            {
                menu.AddMenuOption(race.DisplayName, (player, option) =>
                {
                    player.PrintToChat("--------");
                    int i = 0;
                    foreach (WarcraftSkill skill in race.GetSkills())
                    {
                        char color = (i % 2 == 0) ? ChatColors.Gold : ChatColors.Purple;

                        player.PrintToChat($" {color}{skill.DisplayName}{ChatColors.Default}: {skill.Description}");

                        i += 1;
                    }

                    player.PrintToChat("--------");
                });
            }

            ChatMenus.OpenMenu(client, menu);
        }

        private void CommandChangeRace(CCSPlayerController? client, CommandInfo? commandinfo)
        {
            var menu = new ChatMenu("WCS2 Change Race");
            var races = _raceManager.GetAllRaces();
            foreach (var race in races.OrderBy(x => x.Requirement).ThenBy(x => x.DisplayName))
            {
                menu.AddMenuOption(race.DisplayName, ((player, option) =>
                {
                    IWarcraftPlayer wcPlayer = player.GetWarcraftPlayer();
                    database.SaveClientToDatabase(wcPlayer);

                    // Dont do anything if were already that race.
                    if (race.InternalName == wcPlayer.GetRace().InternalName) return;

                    player.PlayerPawn.Value.CommitSuicide(false, true);

                    wcPlayer.QuickChangeRace(race.InternalName);
                    database.SaveCurrentRace(player);
                    database.LoadClientFromDatabase(_raceManager, player);

                    player.PrintToChat($"{ModuleChatPrefix}{ChatColors.Red}Changed {player.PlayerName} to {ChatColors.Green}{race.DisplayName}{ChatColors.Red}.");
                }));
            }

            ChatMenus.OpenMenu(client, menu);
        }

        public void ShowSkillPointMenu(IWarcraftPlayer wcPlayer)
        {
            var menu = new ChatMenu($"WCS2 Skills ({wcPlayer.GetRace().GetUnusedSkillPoints()} available)");
            var race = wcPlayer.GetRace();

            foreach (WarcraftSkill skill in race.GetSkills())
            {

                var displayString = $"{skill.DisplayName} ({skill.Level})";

                bool disabled = false;
                if (race.GetUnusedSkillPoints() == 0) disabled = true;
                if (skill.Level >= skill.MaxLevel) disabled = true;
                if (race.Level < skill.RequiredLevel) disabled = true;

                menu.AddMenuOption(displayString, (player, option) =>
                {
                    var wcPlayer = player.GetWarcraftPlayer();

                    if (race.GetUnusedSkillPoints() > 0)
                    {
                        race.GetSkillByDisplayName(option.Text.Substring(0, skill.DisplayName.Length)).Level += 1;
                        player.PrintToChat($"{ModuleChatPrefix} Increased {skill.DisplayName} to level {skill.Level}.");
                    }

                    if (race.GetUnusedSkillPoints() > 0)
                    {
                        ShowSkillPointMenu(wcPlayer);
                    }
                }, disabled);
            }

            ChatMenus.OpenMenu(wcPlayer.Controller, menu);
        }

        enum AdminAction
        {
            GiveExperience,
            TakeExperience,
            GiveLevels,
            TakeLevels,
            ChangeRace
        }

        public void ShowAdminMenu(CCSPlayerController? player, CommandInfo? commandinfo)
        {
            if (!Admins.Contains<ulong>(player.GetSteamID()))
            {
                player.PrintToChat($"{ModuleChatPrefix}{ChatColors.Red}You do not have access.");
                return;
            }

            var menu = new ChatMenu($"WCS2 Admin Menu");
            menu.AddMenuOption("Give Experience", ShowAdminGiveExperienceMenu, false);
            menu.AddMenuOption("Give Levels", ShowAdminGiveLevelsMenu, false);
            menu.AddMenuOption("Reset Level", ShowAdminResetLevelsMenu, false);
            menu.AddMenuOption("Change Race", ShowAdminChangeRaceMenu, false);

            ChatMenus.OpenMenu(player, menu);
        }

        public void ShowNumericChoiceMenu(CCSPlayerController? player)
        {
            var menu = new ChatMenu($"WCS2 Number Menu");

            foreach (var number in AdminNumberOptions)
            {
                menu.AddMenuOption(
                    number.ToString(),
                    (player, option) => {
                        Tuple<IntPtr, AdminAction> tuple = AdminDataCache[player.Handle];
                        IWarcraftPlayer target;
                        int value;
                        try
                        {
                            target = WarcraftPlayers[tuple.Item1];
                            value = Int32.Parse(option.Text);
                        }
                        catch
                        {
                            player.PrintToChat($"{ModuleChatPrefix}{ChatColors.Red}Something went wrong, please try again.");
                            return;
                        }
                        switch (tuple.Item2)
                        {
                            case AdminAction.GiveExperience:
                                target.GetRace().AddExperience(value);
                                break;
                            case AdminAction.GiveLevels:
                                target.GetRace().AddLevels(value);
                                break;
                            default:
                                player.PrintToChat($"{ModuleChatPrefix}{ChatColors.Red}Something went wrong, please try again.");
                                return;
                        }
                        AdminDataCache.Remove(player.Handle);
                    },
                    false
                );
            }

            ChatMenus.OpenMenu(player, menu);
        }

        public void ShowRaceChoiceMenu(CCSPlayerController? player)
        {
            var menu = new ChatMenu($"WCS2 Change Race Menu");

            foreach (WarcraftRace race in _raceManager.GetAllRaces())
            {
                menu.AddMenuOption(
                    race.DisplayName,
                    (player, option) => {
                        Tuple<IntPtr, AdminAction> tuple = AdminDataCache[player.Handle];
                        IWarcraftPlayer target = WarcraftPlayers[tuple.Item1];
                        database.SaveClientToDatabase(target);

                        // Dont do anything if were already that race.
                        if (race.InternalName == target.GetRace().InternalName) return;

                        target.Controller.PlayerPawn.Value.CommitSuicide(false, true);

                        target.QuickChangeRace(race.InternalName);
                        database.SaveCurrentRace(target.Controller);
                        database.LoadClientFromDatabase(_raceManager, target.Controller);

                        player.PrintToChat($"{ModuleChatPrefix}{ChatColors.Red}Changed {target.Controller.PlayerName} to {ChatColors.Green}{race.DisplayName}{ChatColors.Red}.");

                        AdminDataCache.Remove(player.Handle);
                    },
                    false
                );
            }

            ChatMenus.OpenMenu(player, menu);
        }

        public void ShowAdminGiveExperienceMenu(CCSPlayerController? player, ChatMenuOption option)
        {
            var menu = new ChatMenu($"WCS2 Give Experience Menu");

            var playerEntities = Utilities.FindAllEntitiesByDesignerName<CCSPlayerController>("cs_player_controller");
            foreach (var target in playerEntities)
            {
                menu.AddMenuOption(
                    target.PlayerName,
                    (player, option) => {
                        AdminDataCache[player.Handle] = new Tuple<IntPtr, AdminAction>(target.Handle, AdminAction.GiveExperience);
                        ShowNumericChoiceMenu(player);
                    },
                    false
                );
            }

            ChatMenus.OpenMenu(player, menu);
        }

        public void ShowAdminGiveLevelsMenu(CCSPlayerController? player, ChatMenuOption option)
        {
            var menu = new ChatMenu($"WCS2 Give Levels Menu");

            var playerEntities = Utilities.FindAllEntitiesByDesignerName<CCSPlayerController>("cs_player_controller");
            foreach (var target in playerEntities)
            {
                menu.AddMenuOption(
                    target.PlayerName,
                    (player, option) => {
                        AdminDataCache[player.Handle] = new Tuple<IntPtr, AdminAction>(target.Handle, AdminAction.GiveLevels);
                        ShowNumericChoiceMenu(player);
                    },
                    false
                );
            }

            ChatMenus.OpenMenu(player, menu);
        }

        public void ShowAdminResetLevelsMenu(CCSPlayerController? player, ChatMenuOption option)
        {
            var menu = new ChatMenu($"WCS2 Reset Level Menu");

            var playerEntities = Utilities.FindAllEntitiesByDesignerName<CCSPlayerController>("cs_player_controller");
            foreach (var target in playerEntities)
            {
                IWarcraftRace race = target.GetWarcraftPlayer().GetRace();
                menu.AddMenuOption(
                    $"{target.PlayerName} (LV {race.Level}/{race.MaxLevel})",
                    (player, option) => {
                        CommandResetSkills(target, null);
                        race.Level = 0;
                        race.Experience = 0;
                        player.PrintToChat($"{ModuleChatPrefix}{ChatColors.Red}You reset {target.PlayerName} to level 0 on {race.DisplayName}.");
                    },
                    false
                );
            }

            ChatMenus.OpenMenu(player, menu);
        }

        public void ShowAdminChangeRaceMenu(CCSPlayerController? player, ChatMenuOption option)
        {
            var menu = new ChatMenu($"WCS2 Change Race Menu");

            var playerEntities = Utilities.FindAllEntitiesByDesignerName<CCSPlayerController>("cs_player_controller");
            foreach (var target in playerEntities)
            {
                menu.AddMenuOption(
                    target.PlayerName,
                    (player, option) => {
                        AdminDataCache[player.Handle] = new Tuple<IntPtr, AdminAction>(target.Handle, AdminAction.ChangeRace);
                        ShowRaceChoiceMenu(player);
                    },
                    false
                );
            }

            ChatMenus.OpenMenu(player, menu);
        }
    }
}