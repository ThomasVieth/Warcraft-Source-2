using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WCS.API;

namespace WCS.Races
{
    public interface IRaceManager
    {
        public WCS wcs { get; set; }

        public void Load();
        IWarcraftRace InstantiateRace(string name, IWarcraftPlayer player);
        public void RegisterRace<T>() where T : IWarcraftRace, new();
        IWarcraftRace[] GetAllRaces();
        string[] GetAllRacesByName();
        IWarcraftRace GetRace(string name);
        Type GetRaceType(string name);
    }
}
