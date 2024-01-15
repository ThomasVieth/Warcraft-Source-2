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
using System.IO;
using System.Linq;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Dapper;
using Microsoft.Data.Sqlite;
using WCS.API;
using WCS.Races;

namespace WCS
{
    public class WarcraftDatabase
    {
        private SqliteConnection _connection;

        public void Initialize(string directory)
        {

            if (!Directory.Exists(Path.Join(directory, "Database")))
                Directory.CreateDirectory(Path.Join(directory, "Database"));


            _connection =
                new SqliteConnection(
                    $"Data Source={Path.Join(directory, "Database", "players.db")}");

            _connection.Execute(@"
            CREATE TABLE IF NOT EXISTS `players` (
              `steamid` UNSIGNED BIG INT NOT NULL,
              `currentrace` VARCHAR(32) NOT NULL DEFAULT 'undead_scourge',
              `name` VARCHAR(64),
              PRIMARY KEY (`steamid`));
            ");

            _connection.Execute(@"
            CREATE TABLE IF NOT EXISTS `races` (
              `steamid` UNSIGNED BIG INT NOT NULL,
              `racename` VARCHAR(32) NOT NULL,
              `xp` INT NULL DEFAULT 0,
              `level` INT NULL DEFAULT 0,
              PRIMARY KEY (`steamid`, `racename`));
            ");

            _connection.Execute(@"
            CREATE TABLE IF NOT EXISTS `skills` (
                `steamid` UNSIGNED BIG INT NOT NULL,
                `racename` VARCHAR(32) NOT NULL,
                `skillname` VARCHAR(32) NOT NULL,
                `level` INT NULL DEFAULT 0,
                PRIMARY KEY (`steamid`, `racename`, `skillname`));
            ");
        }

        public bool ClientExistsInDatabase(ulong steamid)
        {
            return _connection.ExecuteScalar<int>("select count(*) from players where steamid = @steamid",
                new { steamid }) > 0;
        }

        public void AddNewClientToDatabase(CCSPlayerController player)
        {
            _connection.Execute(@"
            INSERT INTO players (`steamid`, `currentrace`)
	        VALUES(@steamid, 'undead_scourge')",
                new { steamid = player.GetSteamID() });
            _connection.Execute(@"
            INSERT INTO races (`steamid`, `racename`)
	        VALUES(@steamid, 'undead_scourge')",
                new { steamid = player.GetSteamID() });
            Server.PrintToConsole($"Adding new user ({player.PlayerName}) to database.");
        }

        public IWarcraftPlayer LoadClientFromDatabase(IRaceManager raceManager, CCSPlayerController player)
        {
            var dbPlayer = _connection.QueryFirstOrDefault<DatabasePlayer>(@"
            SELECT * FROM `players` WHERE `steamid` = @steamid",
                new { steamid = player.GetSteamID() });

            if (dbPlayer == null)
            {
                AddNewClientToDatabase(player);
                return null;
            }

            var raceInformationExists = _connection.ExecuteScalar<int>(@"
            select count(*) from `races` where steamid = @steamid AND racename = @racename",
                new { steamid = player.GetSteamID(), racename = dbPlayer.CurrentRace }
            ) > 0;

            if (!raceInformationExists)
            {
                _connection.Execute(@"
                insert into `races` (steamid, racename)
                values (@steamid, @racename);",
                    new { steamid = player.GetSteamID(), racename = dbPlayer.CurrentRace });
                string[] races = raceManager.GetAllRacesByName();

                IWarcraftRace race;

                if (races.Contains(dbPlayer.CurrentRace))
                {
                    race = raceManager.GetRace(dbPlayer.CurrentRace);
                }
                else
                {
                    race = raceManager.GetRace("undead_scourge");
                }

                foreach (WarcraftSkill skill in race.GetSkills())
                {
                    _connection.Execute(@"INSERT INTO `skills` (steamid, racename, skillname) VALUES (@steamid, @racename, @skillname);",
                        new
                        {
                            steamid = player.GetSteamID(),
                            racename = race.InternalName,
                            skillname = skill.InternalName,
                        }
                    );
                }
            }

            var raceInformation = _connection.QueryFirst<DatabaseRaceInformation>(@"
            SELECT * from `races` where `steamid` = @steamid AND `racename` = @racename",
                new { steamid = player.GetSteamID(), racename = dbPlayer.CurrentRace });

            var skillInformation = _connection.Query<DatabaseSkillInformation>(@"
            SELECT * from `skills` where `steamid` = @steamid AND `racename` = @racename",
                new { steamid = player.GetSteamID(), racename = dbPlayer.CurrentRace });

            var wcPlayer = new WarcraftPlayer(player);
            wcPlayer.LoadFromDatabase(raceInformation, skillInformation.ToArray());

            wcPlayer.IsReady = true;

            return wcPlayer;
        }

        public void SaveClientToDatabase(IWarcraftPlayer player)
        {
            Server.PrintToConsole($"Saving {player.Controller.PlayerName} to database...");

            IWarcraftRace race = player.GetRace();

            var raceInformationExists = _connection.ExecuteScalar<int>(@"
            select count(*) from `races` where steamid = @steamid AND racename = @racename",
                new { steamid = player.Controller.GetSteamID(), racename = race.InternalName }
            ) > 0;

            if (!raceInformationExists)
            {
                _connection.Execute(@"
                insert into `races` (steamid, racename)
                values (@steamid, @racename);",
                    new { steamid = player.Controller.GetSteamID(), racename = race.InternalName }
                );

                foreach (IWarcraftSkill skill in race.GetSkills())
                {
                    _connection.Execute(@"INSERT INTO `skills` (steamid, racename, skillname) VALUES (@steamid, @racename, @skillname);",
                        new
                        {
                            steamid = player.Controller.GetSteamID(),
                            racename = race.InternalName,
                            skillname = skill.InternalName,
                        }
                    );
                }
            }

            _connection.Execute(@"UPDATE `races` SET `xp` = @currentXp, `level` = @currentLevel WHERE `steamid` = @steamid AND `racename` = @racename;",
                new
                {
                    currentXp = race.Experience,
                    currentLevel = race.Level,
                    steamid = player.Controller.GetSteamID(),
                    racename = race.InternalName
                }
            );

            foreach (WarcraftSkill skill in race.GetSkills())
            {
                _connection.Query<int>(@"INSERT OR REPLACE INTO `skills` (`steamid`, `racename`, `skillname`, `level`) VALUES (@steamid, @racename, @skillname, @currentLevel);",
                    new
                    {
                        steamid = player.Controller.GetSteamID(),
                        racename = race.InternalName,
                        skillname = skill.InternalName,
                        currentLevel = skill.Level,
                    }
                );
            }
        }

        public int GetClientTotalLevel(CCSPlayerController player)
        {
            int totallevel = _connection.ExecuteScalar<int>(@"SELECT SUM(currentlevel) FROM `races` WHERE `steamid` = @steamid;",
                new
                {
                    steamid = player.GetSteamID(),
                }
            );

            return totallevel;
        }

        public void SaveClients()
        {
            if (_connection.State != System.Data.ConnectionState.Open)
            {
                _connection.Open();
            }

            SqliteTransaction transaction = _connection.BeginTransaction();

            try
            {
                var playerEntities = Utilities.FindAllEntitiesByDesignerName<CCSPlayerController>("cs_player_controller");
                foreach (var player in playerEntities)
                {
                    if (!player.IsValid) continue;

                    var wcPlayer = player.GetWarcraftPlayer();
                    if (wcPlayer == null) continue;

                    SaveClientToDatabase(wcPlayer);
                }

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
            }
            finally
            {
                if (_connection.State == System.Data.ConnectionState.Open)
                {
                    _connection.Close();
                }
            }
        }

        public void SaveCurrentRace(CCSPlayerController player)
        {
            var wcPlayer = player.GetWarcraftPlayer();

            _connection.Execute(@"
            UPDATE `players` SET `currentRace` = @currentRace, `name` = @name WHERE `steamid` = @steamid;",
                new
                {
                    currentRace = wcPlayer.GetRace().InternalName,
                    name = player.PlayerName,
                    steamid = player.GetSteamID()
                }
            );
        }
    }
}