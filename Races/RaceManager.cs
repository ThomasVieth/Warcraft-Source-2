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

namespace WCS.Races
{
    public class RaceManager
    {
        private Dictionary<string, Type> _races = new Dictionary<string, Type>();
        private Dictionary<string, WarcraftRace> _raceTemplates = new Dictionary<string, WarcraftRace>();

        public void Initialize()
        {
            RegisterRace<RaceUndeadScourge>();
            RegisterRace<RaceHumanAlliance>();
            RegisterRace<RaceOrcishHorde>();
            RegisterRace<RaceNightElf>();
            RegisterRace<RaceBloodElfMage>();
        }

        private void RegisterRace<T>() where T : WarcraftRace, new()
        {
            var race = new T();
            race.Load(null);
            _races[race.InternalName] = typeof(T);
            _raceTemplates[race.InternalName] = race;
        }

        public WarcraftRace InstantiateRace(string name, WarcraftPlayer player)
        {
            if (!_races.ContainsKey(name)) throw new Exception("Race not found: " + name);

            var race = (WarcraftRace)Activator.CreateInstance(_races[name]);
            race.Load(player);

            return race;
        }

        public WarcraftRace[] GetAllRaces()
        {
            return _raceTemplates.Values.ToArray();
        }

        public string[] GetAllRacesByName()
        {
            return _raceTemplates.Keys.ToArray();
        }

        public WarcraftRace GetRace(string name)
        {
            return _raceTemplates.ContainsKey(name) ? _raceTemplates[name] : null;
        }

        public Type GetRaceType(string name)
        {
            return _races.ContainsKey(name) ? _races[name] : null;
        }
    }
}