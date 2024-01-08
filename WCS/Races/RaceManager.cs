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

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core.Logging;
using CounterStrikeSharp.API.Core.Plugin;
using McMaster.NETCore.Plugins;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Loader;
using WCS.API;

namespace WCS.Races
{
    public class RaceManager : IRaceManager
    {
        private Dictionary<string, Type> _races = new Dictionary<string, Type>();
        private Dictionary<string, IWarcraftRace> _raceTemplates = new Dictionary<string, IWarcraftRace>();
        private List<PluginLoader> RacePackLoaders { get; set; }
        public WCS wcs { get; set; }
        private ILogger<IRaceManager> _logger;

        private PluginState State = PluginState.Unloaded;


        public RaceManager(ILogger<IRaceManager> logger)
        {
            _logger = logger;
        }

        public void Load()
        {
            if (State == PluginState.Loaded)
            {
                Server.PrintToConsole($"[WCS] RaceManager already loaded");
                return;
            }
            var wcsDir = wcs.ModuleDirectory;

            var racePacksPath = Path.Join(wcsDir, "RacePacks");

            if (!Directory.Exists(racePacksPath))
            {
                Directory.CreateDirectory(racePacksPath);
            }

            var packsDir = Directory.GetDirectories(racePacksPath);
            var racePackAssemblyPaths = packsDir.Select(dir => Path.Combine(dir, Path.GetFileName(dir) + ".dll"))
            .Where(File.Exists)
            .ToArray();


            var loaders = new List<PluginLoader>();
            foreach (var racePackAssembly in racePackAssemblyPaths)
            {
                if (File.Exists(racePackAssembly))
                {
                    Server.PrintToConsole($"[WCS] Collected RacePack: {Path.GetFileName(racePackAssembly)}");
                    var context = AssemblyLoadContext.GetLoadContext(typeof(IWarcraftRace).Assembly);
                    context.LoadFromAssemblyPath(racePackAssembly);
                    var loader = PluginLoader.CreateFromAssemblyFile(
                            racePackAssembly, true, sharedTypes: new[] { typeof(IWarcraftRacePack), typeof(IWarcraftRace) }, config =>
                            {
                                config.DefaultContext = context;
                                config.PreferSharedTypes = true;
                            });
                    loaders.Add(loader);
                }
            }

            RacePackLoaders = loaders;

            State = PluginState.Loading;


            foreach (var loader in RacePackLoaders)
            {
                try
                {
                    LoadRacePack(loader);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Failed to load race pack");
                }
            }

        }

        private void LoadRacePack(PluginLoader loader)
        {


            var racePackAssembly = loader.LoadDefaultAssembly();

            var racePackType = racePackAssembly.GetTypes().FirstOrDefault(t => typeof(IWarcraftRacePack).IsAssignableFrom(t));

            object obj = Activator.CreateInstance(racePackType);

            dynamic metadata = obj;

            var raceTypes = racePackAssembly.GetTypes().Where(t => typeof(IWarcraftRace).IsAssignableFrom(t)).ToArray();
            foreach (var raceType in raceTypes)
            {
                Server.PrintToConsole($"[WCS] Loading Race: {raceType.Name} from {metadata.name} ({metadata.Uuid})");
                var _RegisterRace = typeof(IRaceManager).GetMethod("RegisterRace");
                _RegisterRace.MakeGenericMethod(raceType).Invoke(this, null);
            }

        }

        public void RegisterRace<T>() where T : IWarcraftRace, new()
        {
            var race = new T();
            race.Load(null);
            _races[race.InternalName] = typeof(T);
            _raceTemplates[race.InternalName] = race;
        }

        public IWarcraftRace InstantiateRace(string name, IWarcraftPlayer player)
        {
            if (!_races.ContainsKey(name)) throw new Exception("Race not found: " + name);

            var race = (WarcraftRace)Activator.CreateInstance(_races[name]);
            race.Load(player);

            return race;
        }

        public IWarcraftRace[] GetAllRaces()
        {
            return _raceTemplates.Values.ToArray();
        }

        public string[] GetAllRacesByName()
        {
            return _raceTemplates.Keys.ToArray();
        }

        public IWarcraftRace GetRace(string name)
        {
            return _raceTemplates.ContainsKey(name) ? _raceTemplates[name] : null;
        }

        public Type GetRaceType(string name)
        {
            return _races.ContainsKey(name) ? _races[name] : null;
        }
    }
}