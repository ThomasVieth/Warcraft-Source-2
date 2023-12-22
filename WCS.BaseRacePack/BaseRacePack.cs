using WCS.API;

namespace WCS.BaseRacePack
{
    public class BaseRacePack: IWarcraftRacePack
    {
        public string name => "BaseRacePack";
        public string author => "WCS Team";
        public string version => "1.0.0";
        public string Uuid => new System.Guid().ToString();
    }
}
