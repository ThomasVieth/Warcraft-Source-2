using CounterStrikeSharp.API.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WCS.API
{
    public interface IWarcraftPlayer
    {
        public bool IsReady { get; set; }
        public CCSPlayerController Controller { get; init; }
        public string statusMessage { get; set; }
        public IWarcraftRace GetRace();
        public void QuickChangeRace(string raceName);

        public void Tell(string message);
        public void SetStatusMessage(string status, float duration = 2f);

        public int TotalLevel { get; }
        public void LoadFromDatabase(DatabaseRaceInformation dbRace, DatabaseSkillInformation[] dbSkills);
        public void delete();
    }
}
