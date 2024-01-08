using WCS.API;

namespace WCS.Core.Races
{
    public class CoreRacePack: IWarcraftRacePack
    {
        public string name => "Core";
        public string author => "ThomasVieth";
        public string version => "1.0.0";
        public string Uuid => new System.Guid().ToString();
    }
}
