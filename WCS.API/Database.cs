using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WCS.API
{
    public class DatabasePlayer
    {
        public ulong SteamId { get; set; }
        public string CurrentRace { get; set; }
        public string Name { get; set; }
    }

    public class DatabaseRaceInformation
    {
        public ulong SteamId { get; set; }
        public string RaceName { get; set; }
        public int Xp { get; set; }
        public int Level { get; set; }
    }

    public class DatabaseSkillInformation
    {
        public ulong SteamId { get; set; }
        public string RaceName { get; set; }
        public string SkillName { get; set; }
        public int Level { get; set; }
    }
}
