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

namespace WCS
{
    public static class WarcraftPlayerExtensions
    {
        public static WarcraftPlayer GetWarcraftPlayer(this CCSPlayerController player)
        {
            return WCS.Instance.GetWcPlayer(player);
        }
    }

    public class WarcraftPlayer
    {
        public bool IsReady = false;

        public CCSPlayerController Controller { get; init; }

        public string statusMessage;

        private WarcraftRace race;

        public WarcraftPlayer(CCSPlayerController player)
        {
            Controller = player;
        }

        public void LoadFromDatabase(DatabaseRaceInformation dbRace, DatabaseSkillInformation[] dbSkills)
        {
            race = WCS.Instance.raceManager.InstantiateRace(dbRace.RaceName, this);

            Controller.Clan = race.DisplayName;

            race.Level = dbRace.Level;
            race.Experience = dbRace.Xp;

            foreach (DatabaseSkillInformation dbSkill in dbSkills)
            {
                WarcraftSkill skill = race.GetSkillByName(dbSkill.SkillName);
                skill.Level = dbSkill.Level;
            }
        }

        public override string ToString()
        {
            return $"{race.DisplayName}: LV {race.Level} | {race.Experience}/{race.RequiredExperience} XP";
        }

        public WarcraftRace GetRace()
        {
            return race;
        }

        public void QuickChangeRace(string raceName)
        {
            race = WCS.Instance.raceManager.GetRace(raceName);
        }

        public void SetStatusMessage(string status, float duration = 2f)
        {
            statusMessage = status;
            new Timer(duration, () => statusMessage = null, 0);
        }
    }

    public class WCS : BasePlugin
    {
        private static WCS _instance;
        public static WCS Instance => _instance;

        public override string ModuleName => "WarcraftSource2";
        public override string ModuleVersion => "2.0.0";
        public string ModuleChatPrefix = $" {ChatColors.Green}[WCS2]: ";

        public int AdvertIndex = 0;
        public string[] AdvertStrings =
        {
            $" {ChatColors.Green}[SERVER]: {ChatColors.Default}This Server is running Warcraft Source 2!",
            $" {ChatColors.Green}[SERVER]: {ChatColors.Default}Bind {ChatColors.Gold}F1, F2, etc {ChatColors.Default}to {ChatColors.Gold}\"css_1\", \"css_2\", etc {ChatColors.Default}to use menus.",
            $" {ChatColors.Green}[SERVER]: {ChatColors.Default}Type {ChatColors.Gold}changerace {ChatColors.Default}in {ChatColors.Gold}the console {ChatColors.Default}to change your race.",
            $" {ChatColors.Green}[SERVER]: {ChatColors.Default}Type {ChatColors.Gold}spendskills {ChatColors.Default}in {ChatColors.Gold}the console {ChatColors.Default}to spend skill points.",
        };

        private Dictionary<IntPtr, Tuple<IntPtr, AdminAction>> adminCache = new();
        private int[] AdminNumberOptions = { 1, 5, 10, 20, 50, 100 };

        private Dictionary<IntPtr, WarcraftPlayer> WarcraftPlayers = new();
        private EventSystem _eventSystem;
        public RaceManager raceManager;
        private Database _database;

        public int XpPerKill = 80;
        public float XpHeadshotModifier = 0.5f;
        public float XpKnifeModifier = 1f;

        public List<WarcraftPlayer> Players => WarcraftPlayers.Values.ToList();

        // UTILITIES
        public WarcraftPlayer GetWcPlayer(CCSPlayerController player)
        {
            WarcraftPlayers.TryGetValue(player.Handle, out var wcPlayer);
            if (wcPlayer == null)
            {
                if (player.IsValid && !player.IsBot)
                {
                    WarcraftPlayers[player.Handle] = _database.LoadClientFromDatabase(raceManager, player);
                }
                else
                {
                    return null;
                }
            }

            return WarcraftPlayers[player.Handle];
        }

        public void SetWcPlayer(CCSPlayerController player, WarcraftPlayer wcPlayer)
        {
            WarcraftPlayers[player.Handle] = wcPlayer;
        }

        // LOAD AND UNLOAD
        public override void Load(bool hotReload)
        {
            base.Load(hotReload);

            if (_instance == null) _instance = this;

            _database = new Database();
            raceManager = new RaceManager();
            raceManager.Initialize();

            AddCommand("ability", "ability", AbilityPressed);
            AddCommand("ultimate", "ultimate", UltimatePressed);

            AddCommand("changerace", "changerace", CommandChangeRace);
            AddCommand("raceinfo", "raceinfo", CommandRaceInfo);
            AddCommand("resetskills", "resetskills", CommandResetSkills);
            AddCommand("spendskills", "spendskills", (client, _) => ShowSkillPointMenu(GetWcPlayer(client)));
            AddCommand("wcsadmin", "wcsadmin", ShowAdminMenu);

            RegisterListener<Listeners.OnClientConnect>(OnClientPutInServerHandler);
            RegisterListener<Listeners.OnClientDisconnect>(OnClientDisconnectHandler);
            RegisterListener<Listeners.OnMapStart>(OnMapStartHandler);

            if (hotReload)
            {
                OnMapStartHandler(null);
            }

            _eventSystem = new EventSystem(this);
            _eventSystem.Initialize();

            _database.Initialize(ModuleDirectory);
        }

        public override void Unload(bool hotReload)
        {
            base.Unload(hotReload);
        }

        // CLIENT HANDLERS
        private void OnClientPutInServerHandler(int slot, string name, string ipAddress)
        {
            var player = new CCSPlayerController(NativeAPI.GetEntityFromIndex(slot + 1));
            Console.WriteLine($"Put in server {player.Handle}");
            // No bots, invalid clients or non-existent clients.
            if (!player.IsValid || player.IsBot) return;

            if (!_database.ClientExistsInDatabase(player.SteamID))
            {
                _database.AddNewClientToDatabase(player);
                Server.PrintToChatAll($"{ModuleChatPrefix}New player ({name}) just joined!");
            }

            WarcraftPlayers[player.Handle] = _database.LoadClientFromDatabase(raceManager, player);
        }

        private void OnClientDisconnectHandler(int slot)
        {
            var player = new CCSPlayerController(NativeAPI.GetEntityFromIndex(slot + 1));
            // No bots, invalid clients or non-existent clients.
            if (!player.IsValid || player.IsBot) return;

            var wcPlayer = GetWcPlayer(player);
            _database.SaveClientToDatabase(wcPlayer);
            SetWcPlayer(player, null);
        }

        // REPEATERS
        private void OnMapStartHandler(string mapName)
        {
            AddTimer(2f, PlayerInfoUpdate, TimerFlags.REPEAT | TimerFlags.STOP_ON_MAPCHANGE);
            AddTimer(60.0f, _database.SaveClients, TimerFlags.REPEAT | TimerFlags.STOP_ON_MAPCHANGE);
            AddTimer(120.0f, RunAdverts, TimerFlags.REPEAT | TimerFlags.STOP_ON_MAPCHANGE);

            // StringTables.AddFileToDownloadsTable("sound/warcraft/ui/questcompleted.mp3");
            // StringTables.AddFileToDownloadsTable("sound/warcraft/ui/gamefound.mp3");
            //
            // Server.PrecacheSound("warcraft/ui/questcompleted.mp3");
            // Server.PrecacheSound("warcraft/ui/gamefound.mp3");
            //
            // Server.PrecacheModel("models/weapons/w_ied.mdl");
            // Server.PrecacheSound("weapons/c4/c4_click.wav");
            // Server.PrecacheSound("weapons/hegrenade/explode3.wav");
            // Server.PrecacheSound("items/battery_pickup.wav");

            Server.PrintToConsole("Map Load Warcraft\n");
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

                var wcPlayer = GetWcPlayer(player);

                if (wcPlayer == null) continue;

                var race = wcPlayer.GetRace();

                var message = $"{race.DisplayName} ({race.Level}/{race.MaxLevel})\n" +
                              $"Experience: {race.Experience}/{race.RequiredExperience}\n" +
                              $"{wcPlayer.statusMessage}";

                player.PrintToCenter(message);
            }
        }

        // CLIENT COMMAND LISTENERS
        private void CommandResetSkills(CCSPlayerController? client, CommandInfo commandinfo)
        {
            var wcPlayer = GetWcPlayer(client);

            foreach (WarcraftSkill skill in wcPlayer.GetRace().GetSkills())
            {
                skill.Level = 0;
            }
        }

        private void AbilityPressed(CCSPlayerController? client, CommandInfo commandinfo)
        {
            GetWcPlayer(client)?.GetRace()?.InvokeAbility(0);
        }

        private void UltimatePressed(CCSPlayerController? client, CommandInfo commandinfo)
        {
            GetWcPlayer(client)?.GetRace()?.InvokeAbility(1);
        }

        // MENUS
        private void CommandRaceInfo(CCSPlayerController? client, CommandInfo commandinfo)
        {
            var menu = new ChatMenu("Race Information");
            var races = raceManager.GetAllRaces();
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

        private void CommandChangeRace(CCSPlayerController? client, CommandInfo commandinfo)
        {
            var menu = new ChatMenu("Change Race");
            var races = raceManager.GetAllRaces();
            foreach (var race in races.OrderBy(x => x.DisplayName))
            {
                menu.AddMenuOption(race.DisplayName, ((player, option) =>
                {
                    WarcraftPlayer wcPlayer = GetWcPlayer(player);
                    _database.SaveClientToDatabase(wcPlayer);

                    // Dont do anything if were already that race.
                    if (race.InternalName == wcPlayer.GetRace().InternalName) return;

                    wcPlayer.QuickChangeRace(race.InternalName);
                    _database.SaveCurrentRace(player);
                    _database.LoadClientFromDatabase(raceManager, player);

                    player.PlayerPawn.Value.CommitSuicide(false, true);
                }));
            }

            ChatMenus.OpenMenu(client, menu);
        }

        public void ShowSkillPointMenu(WarcraftPlayer wcPlayer)
        {
            var menu = new ChatMenu($"Level up skills ({wcPlayer.GetRace().GetUnusedSkillPoints()} available)");
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
                        race.GetSkillByDisplayName(option.Text.Substring(0, skill.DisplayName.Length)).Level += 1;

                    if (race.GetUnusedSkillPoints() > 0)
                    {
                        //Server.NextFrame(() => ShowSkillPointMenu(wcPlayer));
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

        public void ShowAdminMenu(CCSPlayerController? player, CommandInfo commandinfo)
        {
            if (player.SteamID != 76561198200706498)
            {
                player.PrintToChat($"{ModuleChatPrefix}{ChatColors.Red}You do not have access.");
                return;
            }

            var menu = new ChatMenu($"WCS2 Admin Menu");
            menu.AddMenuOption("Give Experience", ShowAdminGiveExperienceMenu, false);
            menu.AddMenuOption("Give Levels", ShowAdminGiveLevelsMenu, false);
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
                        Tuple<IntPtr, AdminAction> tuple = adminCache[player.Handle];
                        WarcraftPlayer target;
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
                        adminCache.Remove(player.Handle);
                    },
                    false
                );
            }

            ChatMenus.OpenMenu(player, menu);
        }

        public void ShowRaceChoiceMenu(CCSPlayerController? player)
        {
            var menu = new ChatMenu($"WCS2 Change Race Menu");

            foreach (WarcraftRace race in raceManager.GetAllRaces())
            {
                menu.AddMenuOption(
                    race.DisplayName,
                    (player, option) => {
                        Tuple<IntPtr, AdminAction> tuple = adminCache[player.Handle];
                        WarcraftPlayer target = WarcraftPlayers[tuple.Item1];
                        //target.ChangeRace(race.InternalName);
                        player.PrintToChat($"{ModuleChatPrefix}{ChatColors.Red}Currently unsupported.");
                        adminCache.Remove(player.Handle);
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
                if (target.IsBot) continue;
                menu.AddMenuOption(
                    target.PlayerName,
                    (player, option) => {
                        adminCache[player.Handle] = new Tuple<IntPtr, AdminAction>(target.Handle, AdminAction.GiveExperience);
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
                if (target.IsBot) continue;
                menu.AddMenuOption(
                    target.PlayerName,
                    (player, option) => {
                        adminCache[player.Handle] = new Tuple<IntPtr, AdminAction>(target.Handle, AdminAction.GiveLevels);
                        ShowNumericChoiceMenu(player);
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
                if (target.IsBot) continue;
                menu.AddMenuOption(
                    target.PlayerName,
                    (player, option) => {
                        adminCache[player.Handle] = new Tuple<IntPtr, AdminAction>(target.Handle, AdminAction.ChangeRace);
                        ShowRaceChoiceMenu(player);
                    },
                    false
                );
            }

            ChatMenus.OpenMenu(player, menu);
        }
    }
}