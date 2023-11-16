using System.Collections.Generic;

namespace WCS
{
    public class WarcraftConfig
    {
        public string ChatPrefix;
        public string AdvertChatPrefix;

        public string DefaultRaceInternalName;

        public List<ulong> Admins;

        public WarcraftConfigExperience experience;
    }

    public class WarcraftConfigExperience
    {
        public int KillExperience;
        public int HeadshotMultiplier;
        public int KnifeMultiplier;
        public int DifferencePerLevelAddition;

        public int BombPlantExperience;
        public int BombExplodeExperience;
        public int BombDefuseExperience;

        public int RoundWinExperience;
        public int RoundLossExperience;

        public int AssistExperience;
    }
}
